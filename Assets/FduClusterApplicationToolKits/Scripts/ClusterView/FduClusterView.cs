/*
 * FduClusterView
 * 简介：ClusterView类 作为应用中传输数据的主体，可以监控该组件所属物体的创建、销毁以及active状态的改变
 * 原理：在editor中注册好所有observer 保存到列表中 然后每帧遍历所有Observer 访问其数据传输类（FduDTS）的SendOrNot函数决定是否执行obsever的OnSendData函数
 * 从节点在接收到对应数据后 遍历所有obseerver 访问其数据传输类的ReceiveOrNot函数决定其是否执行observer的OnReceiveData函数
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public sealed class FduClusterView : SyncBase
    {

        public FduClusterView()
            : base()
        {
            ObjectID = FduSyncBaseIDManager.getInvalidSyncId();
        }

        #region properties
        public int ViewId
        {
            get { return ObjectID; }
        }


        public bool IsObservingCreation
        {
            get
            {
                return (viewStateFlags & 0x01) != 0;
            }
            set
            {
                if (value)
                    viewStateFlags |= 0x01;
                else
                    viewStateFlags &= 0xfe;
            }
        }

        public bool IsObservingDestruction
        {
            get
            {
                return (viewStateFlags & 0x02) != 0;
            }
            set
            {
                if (value)
                    viewStateFlags |= 0x02;
                else
                    viewStateFlags &= 0xfd;
            }
        }

        public bool IsImmediatelyDeserialize
        {
            get
            {
                return (viewStateFlags & 0x04) != 0;
            }
            set
            {
                if (value)
                    viewStateFlags |= 0x04;
                else
                    viewStateFlags &= 0xfb;
            }
        }

        public bool IsObservingActiveState
        {
            get
            {
                return (viewStateFlags & 0x08) != 0;
            }
            set
            {
                if (value)
                    viewStateFlags |= 0x08;
                else
                    viewStateFlags &= 0xf7;
            }
        }

        public bool IsAutomaticllySend
        {
            get
            {
                return (viewStateFlags & 0x10) != 0;
            }
            set
            {
                if (value)
                    viewStateFlags |= 0x10;
                else
                    viewStateFlags &= 0xef;
            }
        }

        #endregion


        #region serialize members

        //该view所拥有的所有监控器列表
        [SerializeField]
        List<FduObserverBase> observerList = new List<FduObserverBase>();
        //用于创建时 找到对应资源的id
        [SerializeField]
        int AssetId = -1;

        //状态标志 目前已用的：（由低位到高位）1：是否监控创建 2：是否监控销毁 3：是否优先解析 4:是否监控active状态 5:是否自动发送数据
        //默认值优先解析和自动发送数据为true
        [SerializeField]
        byte viewStateFlags = 0x14;

        //这个列表仅为了创建新的带有view的Gameobject时，为其子view分配id使用
        [SerializeField]
        List<FduClusterView> subViewList = new List<FduClusterView>();
        
        //这个应用仅为了创建新的带有cluster view的gameobject时使用
        [SerializeField]
        FduClusterView parentView;
        
        //重发优先级
        [SerializeField]
        public Message.ResendPriority resendPriority = Message.ResendPriority.Delay;

        #endregion

        int index = 0;

        #region Mono methods
        void Awake()
        {
         //非集群版本要摧毁所有的observer以及本view
#if !CLUSTER_ENABLE
        foreach (FduObserverBase ob in observerList)
        {
            if(ob!=null)
                Destroy(ob);
        }
        Destroy(this);
#endif
            
        }
        void Update()
        {
            for (int i = 0; i < observerList.Count; ++i)
            {
                if (observerList[i] != null)
                    observerList[i].AlwaysUpdate();
            }
        }
        void LateUpdate()
        {
            if (_server != null && IsAutomaticllySend)
            {
                _server.SendState(ViewId, this, resendPriority ,this.IsImmediatelyDeserialize);
            }
#if DEBUG
            FduClusterViewManager.updateRunningViewCount();
#endif
        }
        void OnDestroy()
        {
#if !CLUSTER_ENABLE
        return;
#endif
            if (ClusterHelper.Instance.Server != null)
            {
                if (IsObservingDestruction)
                {
                    FduClusterViewManager.NotifyOneViewDisappear(ViewId, true);
                }
                else
                    FduClusterViewManager.NotifyOneViewDisappear(ViewId, false);
            }
            else
                FduClusterViewManager.NotifyOneViewDisappear(ViewId, false);

            foreach (FduObserverBase ob in observerList)
            {
                if (ob != null)
                    ob.OnviewDestroy();
            }
        }
        #endregion
        //SyncBase Strat函数执行前执行的函数 在这里需要监测该场景中的静态物体是否分配了ID 如果不是静态分配的Id 则判定为运行时创建的 则动态申请ID
        //而对于从节点 如果不是静态物体 则ID必须从主节点获取 赋值过程在此函数执行前 所以如果此时从节点上该view还没有分配ID 就说明前面的机制发生了错误
        protected override void BeforeInit()
        {
            if (FduSyncBaseIDManager.needsAllocateSceneViewId() && FduClusterLevelLoader.Instance.isSceneLoaded())
            {
                Debug.Log("first happen " + name);
                FduClusterViewManager.ClearUnusedViews();
                FduSyncBaseIDManager.AllocateSceneObjectViewId();
            }
            if (ClusterHelper.Instance.Server != null)
            {
                if (ObjectID == FduSyncBaseIDManager.getInvalidSyncId())
                {
                    ObjectID = FduSyncBaseIDManager.ApplyNextAvaliableId();
                }
                if (IsObservingCreation) //如果监控这个view的创建
                {
                    if (FduClusterAssetManager.Instance.validateId(AssetId))
                    {
                        FduClusterViewManager.NotifyOneNewViewAppear(AssetId, this);
                    }
                    else
                    {
                        Debug.LogError("This Prefab is not added to FduClusterAssetManager! Name:" + gameObject.name);
                    }
                }
                else
                {
                    FduClusterViewManager.NotifyOneNewViewAppear(this);
                }
                //FduClusterViewManager.RegistToViewManager(this);
            }
            else
            {
                FduClusterViewManager.RegistToViewManager(this); //如果此时从节点没有被分配id 会被放到一个等待列表中 当收到主节点的id信息后进行赋值
            }
            //检测是否有空的observer存在
            checkObserverExist();
        }

        /// <summary>
        /// Send cluster view data. Generally it is called in LateUpdate of cluster view script automatically.
        /// Instead, you can call this method manually. This method will call SendOrNot method of every observer's data transmit strategy class. 
        /// Accroding to the SendOrNot result, the corresponding observer's OnSendData will be called.
        /// Notice: When you are working with UNSAFE_MODE and one of the observers is using Every_N_Frame data transmit strategy class, please make sure it is called after the update method (cluster view) is called.
        /// </summary>
        public void Send()
        {
            _server.SendState(ViewId, this, resendPriority, IsImmediatelyDeserialize);
        }
        /// <summary>
        /// Send cluster view data. Generally it is called in LateUpdate of cluster view script automatically.
        /// Instead, you can call this method manually. This method will call SendOrNot method of every observer's data transmit strategy class. 
        /// Accroding to the SendOrNot result, the corresponding observer's OnSendData will be called.
        /// Notice: When you are working with UNSAFE_MODE and one of the observers is using Every_N_Frame data transmit strategy class, please make sure it is called after the update method (cluster view) is called.
        /// </summary>
        /// <param name="priority">Resend Priority of this Cluster view's data.</param>
        /// <param name="isImmediatelyDeserialize">Is data is immediately deserialized by slave nodes.</param>
        public void Send(Message.ResendPriority priority,bool isImmediatelyDeserialize)
        {
            _server.SendState(ViewId, this, priority, isImmediatelyDeserialize);
        }


        /// <summary>
        /// Get Obsever ID by instance.
        /// </summary>
        /// <param name="ob">Observer Instance</param>
        /// <returns>Observer ID</returns>
        public int getObserverId(FduObserverBase ob)
        {
            if (ob == null)
                return -1;
            return observerList.FindIndex((FduObserverBase _ob) => { return _ob.Equals(ob); });
        }
        /// <summary>
        /// Get Observer Instance by ID
        /// </summary>
        /// <param name="index">Observer ID</param>
        /// <returns>Observer Instance</returns>
        public FduObserverBase getObserverById(int index)
        {
            if (index >= 0 && index < observerList.Count)
                return observerList[index];
            else
                return null;
        }
        /// <summary>
        /// Get The number of observers belong to this view
        /// </summary>
        /// <returns></returns>
        public int getObserverCount()
        {
            return observerList.Count;
        }

        protected override void OnServerInited(ObjectSyncMaster server_)
        {
            base.OnServerInited(server_);
            if (_server != null)
            {
            }
        }

        protected override void OnClientInited(ObjectSyncSlave client_)
        {
            base.OnClientInited(client_);
            if (_client != null)
            {

            }
        }
        /// <summary>
        /// Rpc Function. You can call any function marked with FduRPC attribute,which is DEFINED in the classes derived from FduObserverBase or other monobehaviors belong to  the gameobject.
        /// Notice that you can only use it on MasterNode.
        /// </summary>
        /// <param name="methodName">Rpc method name</param>
        /// <param name="target">Rpc tartget</param>
        /// <param name="paras">Rpc Function parameters</param>
        public object Rpc(string methodName, RpcTarget target, params object[] paras)
        {
            return FduRpcManager.Instance.RPC(this, methodName, target, paras);
        }
        /// <summary>
        /// Get All Observers.
        /// </summary>
        /// <returns></returns>
        public List<FduObserverBase>.Enumerator getObservers()
        {
            return observerList.GetEnumerator();
        }


        public void registToView(FduObserverBase observer)
        {
#if UNITY_EDITOR
            if (observer != null && !observerList.Find((FduObserverBase ob) => { if (ob == null) return false;else return ob.Equals(observer); }))
            {
                observerList.Add(observer);
            }
#endif
        }


        /// <summary>
        /// Add an oberver instance to this view. 
        /// Please make sure that it is called both on master node and slave node.
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
        public bool addObserver(FduObserverBase ob)
        {
            if (ob != null)
            {
                observerList.Add(ob);
                ob.setViewInstance(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an observer instance of this view. [Not recommand]
        /// Please Make sure that it is called both on master node and slave node AT THE SAME FRAME.
        /// Please Make sure that it is called before the LateUpdate method of this cluster view is called.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool removeObserver(int index)
        {
            if (index >= 0 && index < observerList.Count)
            {
                observerList[index].setViewInstance(null);
                observerList.RemoveAt(index);
                return true;
            }
            return false;
        }


        //检查是否有空的observer存在 主要是在unity editor中有可能observer被删除 然而没有回调函数通知该view从列表中移除observer
        void checkObserverExist()
        {
            foreach (FduObserverBase ob in observerList)
            {
                if (ob == null)
                {
                    Debug.LogError("Invalid Observer Exists in Cluster View. View Id:" + ViewId + ". Object name: " + gameObject.name + ". Please refresh this Cluster view in Inspector window.");
                }
            }
        }
        public override void Serialize()
        {
            if (ViewId == FduSyncBaseIDManager.getInvalidSyncId())
                return;
            FduObserverBase observer = null;
            for (index = 0; index < observerList.Count; ++index)
            {
                try
                {
                    observer = observerList[index];
#if !UNSAFE_MODE
                    if (observer != null )
                    {
                        bool send = false;
                        if(observer.getDataTransmitStrategy() != null)
                            send = observer.getDataTransmitStrategy().sendOrNot();

                        BufferedNetworkUtilsServer.SendBool(send);
                        if (send)
                        {
                            FduClusterViewManager.updateSentDataObserverCount();
                            observer.OnSendData();
                        }
                    }
#else
                    if (observer.getDataTransmitStrategy().sendOrNot())
                    {
#if DEBUG
                        FduClusterViewManager.updateSentDataObserverCount();
#endif
                        observer.OnSendData();
                    }
#endif
                }
                catch (System.NullReferenceException) { }
                catch (System.Exception e)
                {
                    string erroMessage = string.Format("An error occured when observer {0} call OnSendData  method. Game Object Name:{1},View Id:{2}, Error Message:{3}, Stack Trace:{4}", observer.GetType().Name, observer.name, ViewId, e.Message, e.StackTrace);
                    Debug.LogError(erroMessage);
                    throw;
                }
            }
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            if (this == null) //由于跨场景时 在新的view 没有在start中完成注册之前 旧的view会接收数据
                return NetworkState.NETWORK_STATE_TYPE.SUCCESS;

            if (ViewId == FduSyncBaseIDManager.getInvalidSyncId())
                return NetworkState.NETWORK_STATE_TYPE.SUCCESS;

            UnityEngine.Profiling.Profiler.BeginSample("OnReceiveData");
            NetworkState.NETWORK_STATE_TYPE networkState = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            FduObserverBase observer = null;
            for (index = 0; index < observerList.Count; ++index)
            {
                try
                {
                    observer = observerList[index];
#if !UNSAFE_MODE
                    if (observer != null)
                    {
                        bool receiveOrNot = BufferedNetworkUtilsClient.ReadBool(ref networkState);
                        if (receiveOrNot)
                        {
                            observer.OnReceiveData(ref networkState);
                            FduClusterViewManager.updateSentDataObserverCount();
                        }
                    }
#else
                    if (observer.getDataTransmitStrategy().receiveOrNot())
                    {
                        observer.OnReceiveData(ref networkState);
#if DEBUG
                        FduClusterViewManager.updateSentDataObserverCount();
#endif
                    }
#endif
                }
                catch (System.NullReferenceException) { }
                catch (System.Exception e)
                {
                    string erroMessage = string.Format("An error occured when observer {0} call OnReceiveDataMethod. Game Object Name:{1},View Id:{2}, Error Message:{3}, Stack Trace:{4}", observer.GetType().Name, observer.name, ViewId, e.Message, e.StackTrace);
                    Debug.LogError(erroMessage);
                    throw;
                }
                if (networkState == NetworkState.NETWORK_STATE_TYPE.FORMAT_ERROR)
                {
                    Debug.LogError("Data length not match in this observer! Observer name" + observer.GetType().FullName + " View id " + ViewId + " Game Object name " + observer.gameObject.name);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return networkState;
        }

        public bool getObserveActiveState()
        {
            return IsObservingActiveState;
        }

        //内部使用 给对应的observer设置每N帧传送所需要的计数数据 如果不是每N帧传送的数据传输类 则返回
        public void setFrameCountForEveryNFrameDTS(int observerId, int frameCount)
        {
            var ob = getObserverById(observerId);
            if (ob != null && ob.getDataTransmitStrategy() != null && ob.getDataTransmitStrategy().GetType().Equals(typeof(FduDTS_EveryNFrame)) && frameCount >= 0)
            {
                if (!ob.getDataTransmitStrategy().setCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount, frameCount))
                {
                    Debug.LogError("Set FduDTS_EveryNFrame Custom Data failed!");
                }
            }
        }
        //内部使用 获取对应observer每N帧传送所需要的计数数据 如果不是每N帧传送的数据传输类 则返回-1
        public int getFrameCountForEveryNFrameDTSById(int observerId)
        {
            var ob = getObserverById(observerId);
            if (ob != null && ob.getDataTransmitStrategy() != null && ob.getDataTransmitStrategy().GetType().Equals(typeof(FduDTS_EveryNFrame)))
            {
                return (int)ob.getDataTransmitStrategy().getCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount);
            }
            return -1;
        }
        //内部使用 获取所有observer每N帧传送所需要的计数数据 如果不是每N帧传送的数据传输类 则不返回对应数据
        public Dictionary<int, int> getAllFrameCountForEveryNFrameDTS()
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            for (int i = 0; i < observerList.Count; ++i)
            {

                if (observerList[i] != null && observerList[i].getDataTransmitStrategy() != null && observerList[i].getDataTransmitStrategy().GetType().Equals(typeof(FduDTS_EveryNFrame)))
                {
                    result.Add(i, (int)observerList[i].getDataTransmitStrategy().getCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount));
                }
            }
            return result;
        }
        //内部使用 获取所有子view
        public List<FduClusterView> getSubViews()
        {
            return subViewList;
        }

#if UNITY_EDITOR
        //内部使用 移除所有obsevrer
        public void removeObserver(FduObserverBase ob)
        {
            observerList.Remove(ob);
        }
#endif

    }
}

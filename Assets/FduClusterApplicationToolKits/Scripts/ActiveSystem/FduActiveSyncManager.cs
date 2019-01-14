/*
 * FduActiveSyncManager
 * 简介：用于同步Cluster View 的active和非active状态 需要clusterview在Inspector视图中设置好监控active状态
 * 原理：每一帧查询view所在物体的activeSelf状态 将active发生变化的view id和active状态通知给所有从节点
 * 从节点利用setactive函数更改物体的active状态
 * 最后修改时间：Hayate 2017.7.8
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public class FduActiveSyncManager : SyncBase,IFduLateUpdateFunc
    {

        //传递active状态需要的参数 包括viewid 以及此时主节点所有obsevrer的计数，计数用于每N帧传送时判断当前帧是否需要传送数据
        public class ActiveSynPara
        {
            public int viewId; 
            public Dictionary<int, int> obFrameCount = new Dictionary<int, int>(); //键为属于该clusterview的observer的id 值为帧计数
        }

        //需要激活的列表
        static List<ActiveSynPara> _WaitForActiveList = new List<ActiveSynPara>();
        //需要去激活的列表
        static List<ActiveSynPara> _WaitForInActiveList = new List<ActiveSynPara>();
        //激活非激活状态的cache 确保激活状态变化时才发送数据
        static BitArray viewActiveStates = new BitArray(FduGlobalConfig.MAX_VIEW_COUNT, false);
        //由于场景切换时导致数据丢失，所以当flushedCount大于0时，不管激活状态是否发生变化，都发送数据，flushCount每帧减1，直到为0
        static int flushedCount = 3;

        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getActiveManagerSyncId();
        }

        public void LateUpdateFunc()
        {
            if (_client != null)
                return;

            //每帧遍历检测激活状态
            Dictionary<int, FduClusterView>.Enumerator enumerator = FduClusterViewManager.getClusterViews();
            while (enumerator.MoveNext())
            {
                FduClusterView view = enumerator.Current.Value;
                if (view != null && view.gameObject != null)
                {
                    if (view.getObserveActiveState()) //该view必须设置是否监控激活状态
                    {
                        if (view.gameObject.activeSelf != viewActiveStates[enumerator.Current.Key] || flushedCount > 0) //如果激活状态更改 或者flushedCount大于0 则发送数据
                        {
                            var para = new ActiveSynPara();
                            para.viewId = enumerator.Current.Key;
                            para.obFrameCount = view.getAllFrameCountForEveryNFrameDTS();
                            if (enumerator.Current.Value.gameObject.activeSelf)
                            {

                                _WaitForActiveList.Add(para);
                            }
                            else
                                _WaitForInActiveList.Add(para);

                            viewActiveStates[enumerator.Current.Key] = view.gameObject.activeSelf;
                        }
                    }
                }
            }
            flushedCount = flushedCount > 0 ? flushedCount - 1 : flushedCount;

            _server.SendState(ObjectID, this);
            //_server.SendState(ObjectID, this,false);
        }

        //将数据序列化发送出去
        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendInt(_WaitForActiveList.Count);
            foreach (ActiveSynPara para in _WaitForActiveList)
            {
                BufferedNetworkUtilsServer.SendInt(para.viewId);
                BufferedNetworkUtilsServer.SendInt(para.obFrameCount.Count);
                foreach (KeyValuePair<int, int> kvp in para.obFrameCount)
                {
                    BufferedNetworkUtilsServer.SendInt(kvp.Key);
                    BufferedNetworkUtilsServer.SendInt(kvp.Value);
                }
            }
            BufferedNetworkUtilsServer.SendInt(_WaitForInActiveList.Count);
            foreach (ActiveSynPara para in _WaitForInActiveList)
            {
                BufferedNetworkUtilsServer.SendInt(para.viewId);
                BufferedNetworkUtilsServer.SendInt(para.obFrameCount.Count);
                foreach (KeyValuePair<int, int> kvp in para.obFrameCount)
                {
                    BufferedNetworkUtilsServer.SendInt(kvp.Key);
                    BufferedNetworkUtilsServer.SendInt(kvp.Value);
                }
            }
            BufferedNetworkUtilsServer.SendString("FduActieSyncManagerEndFlag");
            _WaitForActiveList.Clear();
            _WaitForInActiveList.Clear();

        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;

            //active部分数据
            int count = BufferedNetworkUtilsClient.ReadInt(ref state);
            int viewId, obCount; FduClusterView view;
            int obId, obFrameCount;
            for (int i = 0; i < count; ++i)
            {
                viewId = BufferedNetworkUtilsClient.ReadInt(ref state);
                obCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                view = FduClusterViewManager.getClusterView(viewId);
                if (view != null)
                {
                    try
                    {
                        for (int j = 0; j < obCount; ++j)
                        {
                            obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                            obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                            view.setFrameCountForEveryNFrameDTS(obId, obFrameCount);
                        }
                        view.gameObject.SetActive(true);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }
                else
                {
                    //view是空的也要读完
                    for (int j = 0; j < obCount; ++j)
                    {
                        obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                        obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                    }
                    Debug.LogWarning("[FduActiveSyncManager-active]Can not find a view with ViewId " + viewId);
                }
            }
            //Inactive部分数据
            count = BufferedNetworkUtilsClient.ReadInt(ref state);
            for (int i = 0; i < count; ++i)
            {
                viewId = BufferedNetworkUtilsClient.ReadInt(ref state);
                obCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                view = FduClusterViewManager.getClusterView(viewId);
                if (view != null)
                {
                    try
                    {
                        for (int j = 0; j < obCount; ++j)
                        {
                            obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                            obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                            view.setFrameCountForEveryNFrameDTS(obId, obFrameCount);
                        }
                        view.gameObject.SetActive(false);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }
                else
                {
                    //view是空的也要读完
                    for (int j = 0; j < obCount; ++j)
                    {
                        obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                        obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                    }
                    Debug.LogWarning("[FduActiveSyncManager-Inactive]Can not find a view with ViewId " + viewId);
                }
            }
            if (BufferedNetworkUtilsClient.ReadString(ref state) != "FduActieSyncManagerEndFlag")
            {
                Debug.LogError("Wrong end of FduActiveSyncManager!");
            }
            return state;
        }

        //跨场景时要清空数据
        public static void OnLevelLoaded()
        {
            _WaitForActiveList.Clear();
            _WaitForInActiveList.Clear();
            flushedCount = 3;
            viewActiveStates.SetAll(false);
        }
    }
}

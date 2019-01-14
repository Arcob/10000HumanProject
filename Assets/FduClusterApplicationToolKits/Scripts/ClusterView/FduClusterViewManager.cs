/*
 * FduClusterViewManager
 * 简介：所有ClusterView的管理类
 * 负责监控view的创建和销毁
 * 
 * 原理：将需要创建或销毁的游戏物体参数传递给从节点 从节点根据参数完成创建和销毁
 * 主要由View的生命周期控制函数通知本管理类的工作 存储到一个待创建列表或待销毁列表中
 * 每帧将列表中的内容发出 然后清空
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduClusterViewManager : SyncBase,IFduLateUpdateFunc
    {

        //用于监控创建时所需要发送的参数
        public class ClusterGameObjectCreatePara
        {
            //对应预制体的id
            public int assetId;
            //view id
            public int viewId;
            //该view 所有observer 每N帧传送所需要用到的计数 键为ob id 值为计数
            public Dictionary<int, int> viewFrameCount;
            //所有子view的id
            public List<int> subViewId;
            //每一个子view对应着的 每N帧传送所需要用到的计数
            public List<Dictionary<int, int>> subViewFrameCount;
            //位置
            public Vector3 position;
            //旋转
            public Quaternion rotation;
            //父节点的路径
            public string parentPath;
            public int frameCount = -1;
            public FduClusterView viewInstance;//接收端是没有的
            public ClusterGameObjectCreatePara(int viewid, int assid, Vector3 pos, Quaternion rot)
            {
                viewId = viewid;
                assetId = assid;
                position = pos;
                rotation = rot;
            }
            public ClusterGameObjectCreatePara(int viewid, int assid, Vector3 pos, Quaternion rot, string path)
            {
                viewId = viewid;
                assetId = assid;
                position = pos;
                rotation = rot;
                parentPath = path;
            }
            public void ClearData()
            {
                if (viewFrameCount != null)
                    viewFrameCount.Clear();
                if (subViewId != null)
                    subViewId.Clear();
                if (subViewFrameCount != null)
                {
                    foreach (Dictionary<int, int> data in subViewFrameCount)
                        data.Clear();
                    subViewFrameCount.Clear();
                }
            }
        }

        public class SyncViewIdData
        {
            public string name = "";
            public int id = 0;
        }
        //保存所有view的字典 键是viewId 值是对应引用
        static Dictionary<int, FduClusterView> _FduClusterViews = new Dictionary<int, FduClusterView>();

        //存储所有未分配view id的 cluster view列表 仅在slave上有效 等待主节点传来的id
        static List<FduClusterView> _unallocateViews = new List<FduClusterView>();

        //等待创建的列表
        static List<ClusterGameObjectCreatePara> _waitForCreateList = new List<ClusterGameObjectCreatePara>();
        //等待摧毁的列表
        static List<int> _wairForDestoryList = new List<int>();
        //等待赋值id列表
        static List<SyncViewIdData> _waitForAssignViewIdList = new List<SyncViewIdData>();

        //跨场景时标识的前缀
        static string levelPrefix = "";

        //当前帧正在运行的view数量 有的view所属的物体有可能为inactive的状态
        static int runningViewCount = 0;
        //当前帧执行OnSendData/OnReceiveData的 observer数量
        static int sentDataObserverCount = 0;

        //注册至此manager 每一个view都需要注册
        static public void RegistToViewManager(FduClusterView _view)
        {
            if (_view == null) return;
            if (_view.ViewId == FduSyncBaseIDManager.getInvalidSyncId())
            {
                if (!FduSupportClass.isMaster)
                {
                    _unallocateViews.Add(_view);
                }
                else
                    Debug.LogError("[FduClusterViewManager]The Cluster View can not regist to view manager, please allocate a validate view id first");
                return;
            }
            _FduClusterViews.Add_overlay(_view.ViewId, _view);
        }
        /// <summary>
        /// Get View Instance by View ID
        /// </summary>
        /// <param name="viewId"></param>
        /// <returns></returns>
        static public FduClusterView getClusterView(int viewId)
        {
            if (_FduClusterViews.ContainsKey(viewId))
                return _FduClusterViews[viewId];
            else
                return null;
        }
        /// <summary>
        /// Get All Cluster View Instances.
        /// </summary>
        /// <returns></returns>
        static public Dictionary<int, FduClusterView>.Enumerator getClusterViews()
        {
            return _FduClusterViews.GetEnumerator();
        }
        /// <summary>
        /// Get The Number of cluster views.
        /// </summary>
        /// <returns></returns>
        static public int getViewCount()
        {
            return _FduClusterViews.Count;
        }

        static internal void updateRunningViewCount() { runningViewCount++; }
        static internal void updateSentDataObserverCount() { sentDataObserverCount++; }

        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getClusterViewManagerSyncId();
        }
        
        void LateUpdate()
        {

        }
        public void LateUpdateFunc()
        {
            if (_server != null)
            {
                _server.SendState(ObjectID, this);
                //_server.SendState(ObjectID, this,false);
            }
            ClusterViewStatisticClass.instance.RefreshData(_FduClusterViews.Count, runningViewCount, sentDataObserverCount);
            runningViewCount = 0;
            sentDataObserverCount = 0;
        }


        //发送待创建列表和待销毁列表中的数据
        public override void Serialize()
        {
            levelPrefix = levelPrefix == "" ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : levelPrefix;
            BufferedNetworkUtilsServer.SendString(levelPrefix);
            BufferedNetworkUtilsServer.SendInt(_waitForCreateList.Count);
            //Debug.Log("Create Count:" + _waitForCreateList.Count);
            foreach (ClusterGameObjectCreatePara para in _waitForCreateList)
            {
                BufferedNetworkUtilsServer.SendInt(para.viewId);
                BufferedNetworkUtilsServer.SendInt(para.assetId);
                BufferedNetworkUtilsServer.SendVector3(para.position);
                BufferedNetworkUtilsServer.SendQuaternion(para.rotation);
                if (para.parentPath == null)
                    BufferedNetworkUtilsServer.SendString("");
                else
                    BufferedNetworkUtilsServer.SendString(para.parentPath);
                para.viewFrameCount = para.viewInstance.getAllFrameCountForEveryNFrameDTS();
                BufferedNetworkUtilsServer.SendInt(para.viewFrameCount.Count);
                foreach (KeyValuePair<int, int> kvp in para.viewFrameCount)
                {
                    BufferedNetworkUtilsServer.SendInt(kvp.Key); //observer在view中的id
                    BufferedNetworkUtilsServer.SendInt(kvp.Value);//observer中EveryNFrameDTS所需要同步的参数frameCount
                }


                var subViews = para.viewInstance.getSubViews();
                BufferedNetworkUtilsServer.SendInt(subViews.Count);
                foreach (FduClusterView view in subViews)
                {
                    if (view == null)
                    {
                        Debug.LogError("Find Invalid sub view in one Cluster view.View id :" + view.ViewId + ". Please press Refresh Button of this view in Inspector window");
                        break;
                    }
                    BufferedNetworkUtilsServer.SendInt(view.ViewId);
                    var list = view.getAllFrameCountForEveryNFrameDTS();
                    BufferedNetworkUtilsServer.SendInt(list.Count);
                    foreach (KeyValuePair<int, int> kvp in list)
                    {
                        BufferedNetworkUtilsServer.SendInt(kvp.Key);//observer在subview中的id
                        BufferedNetworkUtilsServer.SendInt(kvp.Value);//observer中EveryNFrameDTS所需要同步的参数frameCount
                    }
                }
            }
            BufferedNetworkUtilsServer.SendInt(_wairForDestoryList.Count);
            foreach (int viewId in _wairForDestoryList)
            {
                BufferedNetworkUtilsServer.SendInt(viewId);
            }
            BufferedNetworkUtilsServer.SendInt(_waitForAssignViewIdList.Count);
            foreach (SyncViewIdData data in _waitForAssignViewIdList)
            {
                BufferedNetworkUtilsServer.SendString(data.name);
                BufferedNetworkUtilsServer.SendInt(data.id);
            }
            BufferedNetworkUtilsServer.SendString("FduClusterViewManagerEndFlag");
            ClearListData();
        }
        //解析需要创建和销毁的数据 并通知对应类完成创建和销毁工作
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            string sceneName = BufferedNetworkUtilsClient.ReadString(ref state);
            int count = BufferedNetworkUtilsClient.ReadInt(ref state);
            int viewId, assetId, subViewCount, viewObCount;
            Vector3 position; Quaternion rotation;
            string path;
            for (int i = 0; i < count; i++)
            {
                viewId = BufferedNetworkUtilsClient.ReadInt(ref state);
                assetId = BufferedNetworkUtilsClient.ReadInt(ref state);
                position = BufferedNetworkUtilsClient.ReadVector3(ref state);
                rotation = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                path = BufferedNetworkUtilsClient.ReadString(ref state);
                ClusterGameObjectCreatePara para = new ClusterGameObjectCreatePara(viewId, assetId, position, rotation, path);

                viewObCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                if (viewObCount > 0)
                {
                    para.viewFrameCount = new Dictionary<int, int>();
                    for (int j = 0; j < viewObCount; ++j)
                    {
                        int obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                        int obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                        para.viewFrameCount.Add(obId, obFrameCount);
                    }
                }

                subViewCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                if (subViewCount > 0)
                {
                    para.subViewId = new List<int>();
                    para.subViewFrameCount = new List<Dictionary<int, int>>();
                }
                for (int j = 0; j < subViewCount; ++j)
                {
                    para.subViewId.Add(BufferedNetworkUtilsClient.ReadInt(ref state));
                    int obCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                    para.subViewFrameCount.Add(new Dictionary<int, int>());

                    for (int k = 0; k < obCount; ++k)
                    {
                        var oneSubViewData = para.subViewFrameCount[para.subViewFrameCount.Count - 1];
                        var obId = BufferedNetworkUtilsClient.ReadInt(ref state);
                        var obFrameCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                        oneSubViewData.Add(obId, obFrameCount);
                    }
                }
                _waitForCreateList.Add(para);
            }
            count = BufferedNetworkUtilsClient.ReadInt(ref state);
            for (int i = 0; i < count; ++i)
            {
                _wairForDestoryList.Add(BufferedNetworkUtilsClient.ReadInt(ref state));
            }
            count = BufferedNetworkUtilsClient.ReadInt(ref state);
            for (int i = 0; i < count; ++i)
            {
                SyncViewIdData data = new SyncViewIdData();
                data.name = BufferedNetworkUtilsClient.ReadString(ref state);
                data.id = BufferedNetworkUtilsClient.ReadInt(ref state);
                _waitForAssignViewIdList.Add(data);
            }
            if (BufferedNetworkUtilsClient.ReadString(ref state) != "FduClusterViewManagerEndFlag")
            {
                Debug.LogError("Wrong end of FduClusterViewManagerEndFlag!");
            }
            if (sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                ProcessCreateRequest();
                ProcessDestoryRequest();
                ProcessAssignViewIdRequest();
            }
            ClearListData();
            return state;
        }

        //Execute on slave node 处理创建请求
        void ProcessCreateRequest()
        {
            if (ClusterHelper.Instance.Server != null)
                return;
            ClusterGameObjectCreatePara para;
            for (int i = 0; i < _waitForCreateList.Count; ++i)
            {
                para = _waitForCreateList[i];
                var go = ClusterGameObjectCreator.createGameObjectWithClusterView(para);
                var clusterView = go.GetClusterView();
                var subViews = clusterView.getSubViews();
                if (para.viewFrameCount != null)
                {
                    foreach (KeyValuePair<int, int> kvp in para.viewFrameCount)
                    {
                        clusterView.setFrameCountForEveryNFrameDTS(kvp.Key, kvp.Value);
                    }
                }
                if (para.subViewFrameCount != null)
                {
                    for (int j = 0; j < para.subViewFrameCount.Count; ++j)
                    {
                        var dic = para.subViewFrameCount[j];
                        foreach (KeyValuePair<int, int> kvp in dic)
                        {
                            subViews[j].setFrameCountForEveryNFrameDTS(kvp.Key, kvp.Value);
                        }
                    }
                }
                RegistToViewManager(go.GetClusterView());
            }
        }
        //Execute on slave node 处理销毁请求
        void ProcessDestoryRequest()
        {
            if (FduSupportClass.isMaster)
                return;
            for (int i = 0; i < _wairForDestoryList.Count; ++i)
            {
                if (_FduClusterViews.ContainsKey(_wairForDestoryList[i]))
                {
                    Destroy(_FduClusterViews[_wairForDestoryList[i]].gameObject);
                }
            }
        }

        //对于非监控的创建游戏物体 从节点不可以自己分配id 需要等待主节点发送过来的id信息
        void ProcessAssignViewIdRequest()
        {
            if (FduSupportClass.isMaster)
                return;
            for (int i = 0; i < _waitForAssignViewIdList.Count; ++i)
            {
                Debug.Log("Before assign:" + _unallocateViews.Count);
                for (int j = 0; j < _unallocateViews.Count; ++j)
                {
                    if (_unallocateViews[j].gameObject.name == _waitForAssignViewIdList[i].name)
                    {
                        _unallocateViews[j].UnRegistToObjectSyncSlave();
                        _unallocateViews[j].ObjectID = _waitForAssignViewIdList[i].id;
                        if (_waitForAssignViewIdList[i].id != FduSyncBaseIDManager.getInvalidSyncId())
                        {
                            _unallocateViews[j].RegistToObjectSyncSlave();
                            RegistToViewManager(_unallocateViews[j]);
                            _unallocateViews.RemoveAt(j);
                        }
                        else
                            Debug.LogError("Get Invalid Id from Master Node! Object Name:" + _waitForAssignViewIdList[i].name);
                        break;
                    }
                }
                Debug.Log("After assign:" + _unallocateViews.Count);
            }
        }

        //Only called in matser node 通知一个新的view出现
        public static void NotifyOneNewViewAppear(int assetId, FduClusterView view)
        {
            ClusterGameObjectCreatePara para = new ClusterGameObjectCreatePara(view.ViewId, assetId, view.transform.position, view.transform.rotation);
            if (view.transform.parent != null)
                para.parentPath = FduSupportClass.getGameObjectPath(view.transform.parent.gameObject);
            para.viewInstance = view;
            var subViews = view.getSubViews();
            if (subViews != null)
            {
                for (int i = 0; i < subViews.Count; ++i)
                {
                    if (subViews[i] != null)
                    {
                        if (!subViews[i].gameObject.activeSelf)
                        {
                            subViews[i].ObjectID = FduSyncBaseIDManager.ApplyNextAvaliableId();
                            FduClusterViewManager.RegistToViewManager(subViews[i]);
                        }
                    }
                    else
                    {
                        Debug.LogError("Find Invalid sub view in one FduClusterView.View id :" + view.ViewId + " Object name:" + view.name + ". Please press the Refresh Button in Inspector");
                    }
                }
            }
            if (_waitForCreateList.Count == 0)
            {
                levelPrefix = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            _waitForCreateList.Add(para);
        }
        public static void NotifyOneNewViewAppear(FduClusterView view)
        {
            SyncViewIdData data = new SyncViewIdData();
            data.id = view.ViewId;
            data.name = view.gameObject.name;
            _waitForAssignViewIdList.Add(data);
        }
        //通知一个view消失了
        public static void NotifyOneViewDisappear(int viewId, bool isSendingEvent)
        {

            if (isSendingEvent)
            {
                if (_wairForDestoryList.Count == 0)
                {
                    levelPrefix = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                }
                _wairForDestoryList.Add(viewId);
            }
            FduSyncBaseIDManager.RetrieveId(viewId);
            _FduClusterViews.Remove(viewId);
        }

        public static void OnLevelLoad()
        {
            ClearListData();
        }
        //场景切换时 有的已注册view从来没有被激活过 所以不会执行OnDestroy函数 需要在这里手动清除已变为空的view
        public static void ClearUnusedViews()
        {
            List<int> removeList = new List<int>();
            foreach (KeyValuePair<int, FduClusterView> kvp in _FduClusterViews)
            {
                if (kvp.Value == null)
                {
                    removeList.Add(kvp.Key);
                    FduSyncBaseIDManager.RetrieveId(kvp.Key);
                }
            }
            foreach (int id in removeList)
            {
                _FduClusterViews.Remove(id);
            }
        }
        /// <summary>
        /// Get the number of all observers.
        /// </summary>
        /// <returns></returns>
        public static int getTotalObserverCount()
        {
            int count = 0;
            foreach (KeyValuePair<int, FduClusterView> kvp in _FduClusterViews)
            {
                count += kvp.Value.getObserverCount();
            }
            return count;
        }
        //场景切换时要清除其中的数据
        static void ClearListData()
        {
            foreach (ClusterGameObjectCreatePara data in _waitForCreateList)
                data.ClearData();
            _waitForCreateList.Clear();
            _wairForDestoryList.Clear();
            _waitForAssignViewIdList.Clear();
        }
    }
}
namespace FDUClusterAppToolKits
{
    //用于Debug工具中的数据统计类 主要用于Profile视图
    public class ClusterViewStatisticClass
    {
        static ClusterViewStatisticClass _instance;

        public static readonly int MAX_FRAME_COUNT = 200; //最多统计帧数

        public static bool isRunnig = false;

        //每帧的view数量
        Queue<int> _viewCountQueue;
        //每帧激活的view数量
        Queue<int> _activeViewCountQueue;
        //每帧执行OnSendData的Observer数量
        Queue<int> _activeObserverCountQueue;
        //每帧的所有observer数量
        Queue<int> _observerCountQueue;
        //每个元素代表一个帧号
        Queue<int> _frameNumberQueue;

        public ClusterViewStatisticClass()
        {
            _viewCountQueue = new Queue<int>(MAX_FRAME_COUNT + 1);
            _activeObserverCountQueue = new Queue<int>(MAX_FRAME_COUNT + 1);
            _activeViewCountQueue = new Queue<int>(MAX_FRAME_COUNT + 1);
            _observerCountQueue = new Queue<int>(MAX_FRAME_COUNT + 1);
            _frameNumberQueue = new Queue<int>(MAX_FRAME_COUNT + 1);
        }

        public static ClusterViewStatisticClass instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Application.isPlaying)
                        return null;

                    _instance = new ClusterViewStatisticClass();
                    return _instance;
                }
                else
                    return _instance;
            }
        }
        public void ClearData()
        {
            _viewCountQueue.Clear();
            _activeObserverCountQueue.Clear();
            _activeViewCountQueue.Clear();
            _observerCountQueue.Clear();
            _frameNumberQueue.Clear();
        }
        public void RefreshData(int viewCount, int activeViewCount, int observerCount)
        {
            Debug.Assert(_viewCountQueue.Count == _activeViewCountQueue.Count, "ClusterViewStatisticClass assert not passed");
            if (isRunnig && ClusterHelper.Instance != null)
            {
                if (_viewCountQueue.Count >= MAX_FRAME_COUNT)
                {
                    _viewCountQueue.Dequeue();
                    _activeViewCountQueue.Dequeue();
                    _activeObserverCountQueue.Dequeue();
                    _observerCountQueue.Dequeue();
                    _frameNumberQueue.Dequeue();
                }
                _viewCountQueue.Enqueue(viewCount);
                _activeViewCountQueue.Enqueue(activeViewCount);
                _observerCountQueue.Enqueue(FduClusterViewManager.getTotalObserverCount());
                _activeObserverCountQueue.Enqueue(observerCount);
                _frameNumberQueue.Enqueue(ClusterHelper.Instance.FrameCount);
            }
        }
        public int getCachedFrameDataCount()
        {
            return _viewCountQueue.Count;
        }
        public Queue<int>.Enumerator getTotalViewCounts()
        {
            return _viewCountQueue.GetEnumerator();
        }
        public Queue<int>.Enumerator getActiveViewCounts()
        {
            return _activeViewCountQueue.GetEnumerator();
        }
        public Queue<int>.Enumerator getTotalObserverCounts()
        {
            return _observerCountQueue.GetEnumerator();
        }
        public Queue<int>.Enumerator getActiveObserverCounts()
        {
            return _activeObserverCountQueue.GetEnumerator();
        }
        public Queue<int>.Enumerator getFrameNumbers()
        {
            return _frameNumberQueue.GetEnumerator();
        }

    }
}
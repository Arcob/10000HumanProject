/*
 * ClusterCommandManager
 * 简介：集群事件的管理类，负责管理所有在主节点上发送的事件实例，然后发送到从节点上并解析生成相同的事件实例
 * 继承自SyncBase类，有固定的ID 每帧都会传送
 * 原理：主节点上创建好对应的事件实例，本管理类会将其保存到一个list中，然后在LateUpdate中将其序列化发送
 * 从节点接收到数据后，将数据反序列化为不同的事件实例，并通知事件派发器进行事件派发
 * 最后修改时间：Hayate 2017.08.27
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class ClusterCommandManager : SyncBase,IFduLateUpdateFunc
    {
        //待发送列表
        List<ClusterCommand> _clusterCommandWatingList = new List<ClusterCommand>();

        protected override void Init()
        {
        }
        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getCommandManagerSyncId();
        }
        public ClusterCommandManager()
            : base()
        {
            FduClusterCommandDispatcher.setClusterCommandManager(this);
        }

        public void LateUpdateFunc()
        {
            if (_server != null)
            {
                _server.SendState(ObjectID, this);
                //_server.SendState(ObjectID, this,false);
            }
        }

        //添加一个事件实例
        public void addClusterCommand(ClusterCommand clusterCommand, bool multiCommandAllow = true)
        {
            if (ClusterHelper.Instance.Client != null)
                return;                    //Slave node can not raise cluster Command

            if (clusterCommand == null)
            {
                Debug.LogError("[ClusterCommand]ClusterCommand instance can not be null");
                return;
            }

            if (!multiCommandAllow)
            {
                var result = _clusterCommandWatingList.Find((e) => { return e.getCommandName().Equals(clusterCommand.getCommandName()); });
                if (result != null)
                    return;
            }
            _clusterCommandWatingList.Add(clusterCommand);
        }
        //清除所有事件实例
        public void clearClusterCommands()
        {
            _clusterCommandWatingList.Clear();
        }
        public int getWaitingListCount()
        {
            return _clusterCommandWatingList.Count;
        }
        //根据Index获取等待发送列表中的事件实例
        public ClusterCommand getClusterCommandFromWattingList(int index)
        {
            if (index < 0 || index >= _clusterCommandWatingList.Count)
                return null;
            return _clusterCommandWatingList[index];
        }
        //获取所有待发送事件
        public List<ClusterCommand>.Enumerator getUnprocessedCommands()
        {
            return _clusterCommandWatingList.GetEnumerator();
        }
        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendInt(_clusterCommandWatingList.Count);//total number of cluster Commands
            try
            {
                foreach (ClusterCommand e in _clusterCommandWatingList)
                {

                    e.SerializeParameterData();
                    e.SerializeGenericData();
                }
                ClusterCommandStatisticClass.instance.RefreshData();
                FduClusterCommandDispatcher.NotifyDispatch();
            }
            catch (InvalidSendableDataException e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                clearClusterCommands();
            }
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            clearClusterCommands();
            int CommandCount = BufferedNetworkUtilsClient.ReadInt(ref state); //total number of cluster Commands
            try
            {
                for (int i = 0; i < CommandCount; ++i)
                {
                    ClusterCommand newCommand = new ClusterCommand("1");
                    newCommand.DeserializeParameterData(ref state);
                    newCommand.DeserializeGenericData(ref state);
                    _clusterCommandWatingList.Add(newCommand);
                }
                ClusterCommandStatisticClass.instance.RefreshData();
                FduClusterCommandDispatcher.NotifyDispatch();
            }
            catch (InvalidSendableDataException e)
            {
                Debug.LogError(e.Message);
            }
            return state;
        }
    }

    //用于在Debug工具中展示集群事件数据的类
    public class ClusterCommandShowData
    {
        public ClusterCommand e;
        public int frameCount = 0;
        public ClusterCommandShowData(ClusterCommand ce, int frame)
        {
            e = ce;
            frameCount = frame;
        }
    }
    //集群事件统计类 为Debug工具中查看对应事件提供数据源 如果Debug工具没有启动 该统计类也不会启动（isRunning变量控制）
    public class ClusterCommandStatisticClass
    {

        static ClusterCommandStatisticClass _instance;
        //可以缓存的最大事件实例数量
        static readonly int MAX_Command_COUNT = 100;
        //是否运行
        public static bool isRunnig = false;
        //事件队列
        Queue<ClusterCommandShowData> _CommandQueue;
        //每个事件累积发生次数统计
        Dictionary<string, int> _CommandAccumulatedTimes;

        public ClusterCommandStatisticClass()
        {
            _CommandQueue = new Queue<ClusterCommandShowData>(MAX_Command_COUNT + 1);
            _CommandAccumulatedTimes = new Dictionary<string, int>();
        }

        public static ClusterCommandStatisticClass instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Application.isPlaying)
                        return null;

                    _instance = new ClusterCommandStatisticClass();
                    return _instance;
                }
                else
                    return _instance;
            }
        }
        //清除数据
        public void ClearData()
        {
            _CommandQueue.Clear();
            _CommandAccumulatedTimes.Clear();
        }
        //从FduClusterCommandDispatcher中获取一次数据
        public void RefreshData()
        {

            if (!isRunnig || ClusterHelper.Instance == null)
                return;

            List<ClusterCommand>.Enumerator Commands = FduClusterCommandDispatcher.getCommands();
            while (Commands.MoveNext())
            {
                if (_CommandQueue.Count >= MAX_Command_COUNT)
                    _CommandQueue.Dequeue();

                if (_CommandAccumulatedTimes.ContainsKey(Commands.Current.getCommandName()))
                    _CommandAccumulatedTimes[Commands.Current.getCommandName()]++;
                else
                    _CommandAccumulatedTimes[Commands.Current.getCommandName()] = 0;

                _CommandQueue.Enqueue(new ClusterCommandShowData(ClusterCommand.createSnapShot(Commands.Current), ClusterHelper.Instance.FrameCount));
            }
        }

        public Queue<ClusterCommandShowData>.Enumerator getStatisticCommandData()
        {
            return _CommandQueue.GetEnumerator();
        }

        public Dictionary<string, int>.Enumerator getCommandAccumulatedData()
        {
            return _CommandAccumulatedTimes.GetEnumerator();
        }

        public int getAccumulatedTimesByCommandName(string CommandName)
        {
            if (CommandName == null)
                return 0;
            if (_CommandAccumulatedTimes.ContainsKey(CommandName))
                return _CommandAccumulatedTimes[CommandName];
            else
                return 0;
        }

        public int getCommandCount()
        {
            return _CommandQueue.Count;
        }

    }
}

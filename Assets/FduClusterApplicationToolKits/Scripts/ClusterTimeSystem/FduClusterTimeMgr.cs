/*
 * FduClusterTimeMgr 集群时间管理类
 * 
 * 从主节点获取时间信息 然后同步到其他节点上
 * 保证主从节点在同一帧获取的时间数据是一致的
 * 与input同理 数据会有一帧的延迟
 * 
 * 可以替换大部分unity中Time类提供的获取时间接口
 * 
 * Hayate 17.09.17
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using FDUObjectSync;
using System.Collections.Specialized;

namespace FDUClusterAppToolKits
{

    public class FduClusterTimeMgr : SyncBase, IFduEndOfFrameFunc
    {

        static FduClusterTimeMgr _instance;

        BitVector32 _bitArr;

        public class TimeData
        {
            public float _setValue;
            public float _getValue;
            public int _index;
            public int _countDown;

            public TimeData(int index)
            {
                _index = index;
                reset();
            }
            public void reset()
            {
                _setValue = 0.0f;
                _getValue = 0.0f;
            }
            public void swapValue()
            {
                _getValue = _setValue;
            }
            public float getValue()
            {
                return _getValue;
            }
            public void setValue(float value)
            {
                _setValue = value;
            }
            public bool isDifferent()
            {
                return !_setValue.Equals(_getValue);
            }
            public void countDown()
            {
                if (_countDown > 0)
                    _countDown--;
            }
            public void resetCountDown()
            {
                _countDown = 5;
            }
        }

        Dictionary<string, TimeData> _timeDataMap = new Dictionary<string, TimeData>();


        #region public methods
        public static float deltaTime
        {
            get
            {
                return _getData("deltaTime");
            }
        }

        public static float unscaledDeltaTime
        {
            get
            {
                return _getData("unscaledDeltaTime");
            }
        }

        public static float time
        {
            get
            {
                return _getData("time");
            }
        }
        public static float unscaledTime
        {
            get
            {
                return _getData("unscaledTime");
            }
        }
        public static float fixedTime
        {
            get
            {
                return _getData("fixedTime");
            }
        }
        public static float fixedUnscaledTime
        {
            get
            {
                return _getData("fixedUnscaledTime");
            }
        }
        public static float fixedDeltaTime
        {
            get
            {
                return _getData("fixedDeltaTime");
            }
            set
            {
                _instance._timeDataMap["fixedDeltaTime"].resetCountDown();
                Time.fixedDeltaTime = value;
            }
        }
        public static float fixedUnscaledDeltaTime
        {
            get
            {
                return _getData("fixedUnscaledDeltaTime");
            }
        }
        public static float smoothDeltaTime
        {
            get
            {
                return _getData("smoothDeltaTime");
            }
        }
        public static float timeSinceLevelLoad
        {
            get
            {
                return _getData("timeSinceLevelLoad");
            }
        }
        public static float timeScale
        {
            get
            {
                return _getData("timeScale");
            }
            set
            {
                _instance._timeDataMap["timeScale"].resetCountDown();
                Time.timeScale = value;
            }
        }
        public static int captureFramerate
        {
            get
            {
                return (int)_getData("captureFramerate");
            }
            set
            {
                _instance._timeDataMap["captureFramerate"].resetCountDown();
                Time.captureFramerate = value;
            }
        }
        public static float renderedFrameCount
        {
            get
            {
                return _getData("renderedFrameCount");
            }
        }
        public static float maximumDeltaTime
        {
            get
            {
                return _getData("maximumDeltaTime");
            }
            set
            {
                _instance._timeDataMap["maximumDeltaTime"].resetCountDown();
                Time.maximumDeltaTime = value;
            }
        }
        public static float maximumParticleDeltaTime
        {
            get
            {
                return _getData("maximumParticleDeltaTime");
            }
            set
            {
                _instance._timeDataMap["maximumParticleDeltaTime"].resetCountDown();
                Time.maximumParticleDeltaTime = value;
            }
        }
        public static int frameCount { get { return Time.frameCount; } }



        static float _getData(string name)
        {
            if (_instance != null)
            {
                _instance._timeDataMap[name].resetCountDown();
                return _instance._timeDataMap[name].getValue();
            }
            else
                return 0.0f;
        }

        #endregion

        #region mono methods
        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getClusterTimeManagerSyncId();
            _instance = this;
            _bitArr = new BitVector32(0);
            initData();
        }


        void LateUpdate()
        {
            if (_server != null)
            {
                refreshTimeData();
                _server.SendState(ObjectID, this);
            }
        }
        #endregion

        void initData()
        {
            _timeDataMap.Add("deltaTime", new TimeData(1));
            _timeDataMap.Add("unscaledDeltaTime", new TimeData(2));
            _timeDataMap.Add("time", new TimeData(3));
            _timeDataMap.Add("unscaledTime", new TimeData(4));
            _timeDataMap.Add("fixedTime", new TimeData(5));
            _timeDataMap.Add("fixedUnscaledTime", new TimeData(6));
            _timeDataMap.Add("fixedDeltaTime", new TimeData(7));
            _timeDataMap.Add("fixedUnscaledDeltaTime", new TimeData(8));
            _timeDataMap.Add("smoothDeltaTime", new TimeData(9));
            _timeDataMap.Add("timeSinceLevelLoad", new TimeData(10));
            _timeDataMap.Add("timeScale", new TimeData(11));
            _timeDataMap.Add("captureFramerate", new TimeData(12));
            _timeDataMap.Add("renderedFrameCount", new TimeData(13));
            _timeDataMap.Add("maximumDeltaTime", new TimeData(14));
            _timeDataMap.Add("maximumParticleDeltaTime", new TimeData(15));
        }

        void refreshTimeData()
        {
            if (!FduSupportClass.isMaster)
                return;

            _timeDataMap["deltaTime"].setValue(Time.deltaTime);
            _timeDataMap["unscaledDeltaTime"].setValue(Time.unscaledDeltaTime);

            _timeDataMap["time"].setValue(Time.time);
            _timeDataMap["unscaledTime"].setValue(Time.unscaledTime);

            _timeDataMap["fixedTime"].setValue(Time.fixedTime);
            _timeDataMap["fixedUnscaledTime"].setValue(Time.fixedUnscaledTime);

            _timeDataMap["fixedDeltaTime"].setValue(Time.fixedDeltaTime);
            _timeDataMap["fixedUnscaledDeltaTime"].setValue(Time.fixedUnscaledDeltaTime);

            _timeDataMap["smoothDeltaTime"].setValue(Time.smoothDeltaTime);
            _timeDataMap["timeSinceLevelLoad"].setValue(Time.timeSinceLevelLoad);
            _timeDataMap["timeScale"].setValue(Time.timeScale);

            _timeDataMap["captureFramerate"].setValue(Time.captureFramerate);
            _timeDataMap["renderedFrameCount"].setValue(Time.renderedFrameCount);

            _timeDataMap["maximumDeltaTime"].setValue(Time.maximumDeltaTime);
            _timeDataMap["maximumParticleDeltaTime"].setValue(Time.maximumParticleDeltaTime);

            foreach (KeyValuePair<string, TimeData> kvp in _timeDataMap)
            {
                kvp.Value.countDown();
                if (kvp.Value._countDown <= 0)
                {
                    _bitArr[FduGlobalConfig.BIT_MASK[kvp.Value._index]] = false;
                }
                else
                {
                    _bitArr[FduGlobalConfig.BIT_MASK[kvp.Value._index]] = true;
                }
            }

            //StartCoroutine(SwapCo());
        }

        public void EndOfFrameFunc()
        {
            foreach (KeyValuePair<string, TimeData> kvp in _timeDataMap)
            {
                kvp.Value.swapValue();
            }
        }


        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendInt(_bitArr.Data);
            for (int i = 1; i < 30; ++i)
            {
                if (!_bitArr[FduGlobalConfig.BIT_MASK[i]]) continue;
                switch (i)
                {
                    case 1://deltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["deltaTime"]._setValue);
                        break;
                    case 2://unscaledDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["unscaledDeltaTime"]._setValue);
                        break;
                    case 3://time
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["time"]._setValue);
                        break;
                    case 4://unscaledTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["unscaledTime"]._setValue);
                        break;
                    case 5://fixedTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["fixedTime"]._setValue);
                        break;
                    case 6://fixedUnscaledTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["fixedUnscaledTime"]._setValue);
                        break;
                    case 7://fixedDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["fixedDeltaTime"]._setValue);
                        break;
                    case 8://fixedUnscaledDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["fixedUnscaledDeltaTime"]._setValue);
                        break;
                    case 9://smoothDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["smoothDeltaTime"]._setValue);
                        break;
                    case 10://timeSinceLevelLoad
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["timeSinceLevelLoad"]._setValue);
                        break;
                    case 11://timeScale
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["timeScale"]._setValue);
                        break;
                    case 12://captureFramerate
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["captureFramerate"]._setValue);
                        break;
                    case 13://renderedFrameCount
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["renderedFrameCount"]._setValue);
                        break;
                    case 14://maximumDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["maximumDeltaTime"]._setValue);
                        break;
                    case 15://maximumParticleDeltaTime
                        BufferedNetworkUtilsServer.SendFloat(_timeDataMap["maximumParticleDeltaTime"]._setValue);
                        break;
                }
            }
            //BufferedNetworkUtilsServer.SendString("FduClusterTimeEndFlag");
        }

        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            _bitArr = new BitVector32(BufferedNetworkUtilsClient.ReadInt(ref state));
            for (int i = 1; i < 30; ++i)
            {
                if (!_bitArr[FduGlobalConfig.BIT_MASK[i]]) continue;
                switch (i)
                {
                    case 1://deltaTime
                        _timeDataMap["deltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 2://unscaledDeltaTime
                        _timeDataMap["unscaledDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 3://time
                        _timeDataMap["time"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 4://unscaledTime
                        _timeDataMap["unscaledTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 5://fixedTime
                        _timeDataMap["fixedTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 6://fixedUnscaledTime
                        _timeDataMap["fixedUnscaledTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 7://fixedDeltaTime
                        _timeDataMap["fixedDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        if (_timeDataMap["fixedDeltaTime"]._setValue != Time.fixedDeltaTime)
                        {
                            Time.fixedDeltaTime = _timeDataMap["fixedDeltaTime"]._setValue;
                        }
                        break;
                    case 8://fixedUnscaledDeltaTime
                        _timeDataMap["fixedUnscaledDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 9://smoothDeltaTime
                        _timeDataMap["smoothDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 10://timeSinceLevelLoad
                        _timeDataMap["timeSinceLevelLoad"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 11://timeScale
                        _timeDataMap["timeScale"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        if (_timeDataMap["timeScale"]._setValue != Time.timeScale)
                        {
                            Time.timeScale = _timeDataMap["timeScale"]._setValue;
                        }
                        break;
                    case 12://captureFramerate
                        _timeDataMap["captureFramerate"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        if ((int)_timeDataMap["captureFramerate"]._setValue != Time.captureFramerate)
                        {
                            Time.captureFramerate = (int)_timeDataMap["captureFramerate"]._setValue;
                        }
                        break;
                    case 13://renderedFrameCount
                        _timeDataMap["renderedFrameCount"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 14://maximumDeltaTime
                        _timeDataMap["maximumDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        if (_timeDataMap["maximumDeltaTime"]._setValue != Time.maximumDeltaTime)
                        {
                            Time.maximumDeltaTime = _timeDataMap["maximumDeltaTime"]._setValue;
                        }
                        break;
                    case 15://maximumParticleDeltaTime
                        _timeDataMap["maximumParticleDeltaTime"].setValue(BufferedNetworkUtilsClient.ReadFloat(ref state));
                        if (_timeDataMap["maximumParticleDeltaTime"]._setValue != Time.maximumParticleDeltaTime)
                        {
                            Time.maximumParticleDeltaTime = _timeDataMap["maximumParticleDeltaTime"]._setValue;
                        }
                        break;
                }
            }
            //string end = BufferedNetworkUtilsClient.ReadString(ref state);
            //if (end != "FduClusterTimeEndFlag")
            //{
            //    Debug.LogError("Wrong end of Cluseter Time!");
            //}
            //StartCoroutine(SwapCo());
            return state;
        }
    }

    public class ClusterWaitForSeconds : CustomYieldInstruction
    {
        float _timeDes = 0.0f;

        public ClusterWaitForSeconds(float time)
        {
            time = time < 0 ? 0 : time;
            _timeDes = FduClusterTimeMgr.time + time;
        }

        public override bool keepWaiting
        {
            get
            {
                if (FduClusterTimeMgr.time >= _timeDes)
                    return false;
                else
                    return true;
            }
        }
    }

}

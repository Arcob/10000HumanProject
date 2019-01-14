/*
 * FduClusterInputMgr 集群输入信息管理类
 * 
 * 本类负责将Input的信息从主节点同步到其他节点上
 * 为了确保主从节点在同一帧拿到相同的输入数据（因为输入数据一般会用在update中 这个动作先于从节点接收数据）
 * 通过set方法设置的值会在下一帧生效
 * 
 * 只发送发生变化的输入信息值
 * 
 * 最后修改时间： Hayate 2017.9.11
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public enum FduClusterInputType
    {
        Button,Axis,Tracker
    }

    public class FduClusterInputMgr : SyncBase,IFduEndOfFrameFunc
    {
        #region Data Defination
        public class ButtonData {

            public bool _setValue;
            public bool _getValue;
            public bool _lastgetValue;
            public void reset()
            {
                _setValue = false;
                _getValue = false;
                _lastgetValue = false;
            }
            public void swapValue()
            {
                _lastgetValue = _getValue;
                _getValue = _setValue;
            }
            public bool getValue()
            {
                return _getValue;
            }
            public void setValue(bool value)
            {
                _setValue = value;
            }
            public bool isDifferent()
            {
                return !_setValue.Equals(_getValue);
            }

            public bool isDown()
            {
                if (!_lastgetValue && _getValue)
                    return true;
                else
                    return false;
            }

            public bool isUp()
            {
                if (_lastgetValue && !_getValue)
                    return true;
                else
                    return false;
            }
        }

        public class AxisData
        {
            public float _setValue;
            public float _getValue;
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
        }

        public class TrackerData
        {
            public Vector3 _setPosValue;
            public Vector3 _getPosValue;
            public Quaternion _setRotValue;
            public Quaternion _getRotValue;
            public void reset()
            {
                _getPosValue = _setPosValue = Vector3.zero;
                _getRotValue = _setRotValue = Quaternion.identity;
            }
            public void swapValue()
            {
                _getPosValue = _setPosValue;
                _getRotValue = _setRotValue;

            }
            public Vector3 getPosValue()
            {
                return _getPosValue;
            }
            public Quaternion getRotValue()
            {
                return _getRotValue;
            }
            public void setPosValue(Vector3 value)
            {
                _setPosValue = value;
            }
            public void setRotValue(Quaternion value)
            {
                _setRotValue = value;
            }
            public bool isDifferent()
            {
                return !_setPosValue.Equals(_getPosValue) || !_setRotValue.Equals(_getRotValue);
            }
        }

        public class SerializeData {

            public string name;
            public FduClusterInputType type;

            public SerializeData(string _name, FduClusterInputType _type)
            {
                name = _name;
                type = _type;
            }
            public SerializeData()
            {

            }
        }
        #endregion

        #region Parameters

        static FduClusterInputMgr _instance;

        bool isMaster = false;

        //如果要修改这个值 记得修改FduUnityInputCollector中对应的刷新函数
        public static readonly string keyBoardPrefix = "UKey_";
        public static readonly string mousePrefix = "UMouse_";
        public static readonly string inputPropertyPrefix = "UInput_";

        Dictionary<string, ButtonData> _buttonMap = new Dictionary<string, ButtonData>();
        Dictionary<string, AxisData> _axisMap = new Dictionary<string, AxisData>();
        Dictionary<string, TrackerData> _trackerMap = new Dictionary<string, TrackerData>();

        List<SerializeData> _sendList = new List<SerializeData>();

        FduUnityInputCollector kmCollector = new FduUnityInputCollector();

        #endregion

        #region public properties

        public static bool anyKey { get{
            string name = inputPropertyPrefix + "anyKey";
            if (_instance != null)
                _instance.kmCollector.addInputPropertyName(name);
            return GetButton(name);
        }}

        public static bool anyKeyDown{get{
            string name = inputPropertyPrefix + "anyKeyDown";
            if (_instance != null)
                _instance.kmCollector.addInputPropertyName(name);
            return GetButton(name);
        }}

        public static Vector3 mousePosition { get{
            string name = inputPropertyPrefix + "mousePosition";
            if (_instance != null)
                _instance.kmCollector.addInputPropertyName(name);
            return GetPosition(name);
        }}

        public static Vector2 mouseScrollDelta { get {
            string name = inputPropertyPrefix + "mouseScrollDelta";
            if (_instance != null)
                _instance.kmCollector.addInputPropertyName(name);
            return GetPosition(name);
        } }

        public static Vector3 scaledMousePosition
        {
            get
            {
                string name = inputPropertyPrefix + "scaledMousePosition";
                if (_instance != null)
                    _instance.kmCollector.addInputPropertyName(name);
                Vector3 normalized = GetPosition(name);
                return new Vector3(normalized.x * Screen.width, normalized.y * Screen.height, normalized.z);
            }
        }

        #endregion


        #region mono methods
        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getClusterInputManagerSyncId();
            _instance = this;
            
        }

        protected override void Init()
        {
            isMaster = FduSupportClass.isMaster;
        }


        void LateUpdate()
        {
            if (_server != null)
            {
                kmCollector.refreshInputData();
                CheckDifferent();
                _server.SendState(ObjectID, this);
                //StartCoroutine(swapValueCo());
            }
        }
        void MessageShow()
        {
            Debug.Log("button num:" + _buttonMap.Count);
            Debug.Log("send count :" + _sendList.Count);
            Debug.Log("Axis num:" + _axisMap.Count);
            Debug.Log("Tracker num:" + _trackerMap.Count);
        }

        void CheckDifferent()
        {
            var butEnu = _buttonMap.GetEnumerator();
            while (butEnu.MoveNext())
            {
                if (butEnu.Current.Value.isDifferent())
                {
                    _sendList.Add(new SerializeData(butEnu.Current.Key, FduClusterInputType.Button));
                }
            }

            var axisEnu = _axisMap.GetEnumerator();
            while (axisEnu.MoveNext())
            {
                if (axisEnu.Current.Value.isDifferent())
                {
                    _sendList.Add(new SerializeData(axisEnu.Current.Key, FduClusterInputType.Axis));
                }
            }

            var trackEnu = _trackerMap.GetEnumerator();
            while (trackEnu.MoveNext())
            {
                if (trackEnu.Current.Value.isDifferent())
                {
                    _sendList.Add(new SerializeData(trackEnu.Current.Key, FduClusterInputType.Tracker));
                }
            }
        }
        #endregion

        public void EndOfFrameFunc()
        {
            var buttonEnu = _buttonMap.GetEnumerator();
            while (buttonEnu.MoveNext())
            {
                buttonEnu.Current.Value.swapValue();
            }

            var axisEnu = _axisMap.GetEnumerator();
            while (axisEnu.MoveNext())
            {
                axisEnu.Current.Value.swapValue();
            }

            var trackEnu = _trackerMap.GetEnumerator();
            while (trackEnu.MoveNext())
            {
                trackEnu.Current.Value.swapValue();
            }
        }
        #region serialization
        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendInt(_sendList.Count);
            foreach(SerializeData data in _sendList)
            {
                BufferedNetworkUtilsServer.SendString(data.name);
                BufferedNetworkUtilsServer.SendByte((byte)data.type);
                switch (data.type) {

                    case FduClusterInputType.Axis:
                        BufferedNetworkUtilsServer.SendFloat(_axisMap[data.name]._setValue);
                        break;
                    case FduClusterInputType.Button:
                        BufferedNetworkUtilsServer.SendBool(_buttonMap[data.name]._setValue);
                        break;
                    case FduClusterInputType.Tracker:
                        BufferedNetworkUtilsServer.SendVector3(_trackerMap[data.name]._setPosValue);
                        BufferedNetworkUtilsServer.SendQuaternion(_trackerMap[data.name]._setRotValue);
                        break;
                }
            }
            //BufferedNetworkUtilsServer.SendString("ClusterInputMgrEndFlag");
            _sendList.Clear();
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            int count = BufferedNetworkUtilsClient.ReadInt(ref state);
            for(int i = 0; i < count; ++i)
            {
                string name = BufferedNetworkUtilsClient.ReadString(ref state);
                FduClusterInputType type = (FduClusterInputType)BufferedNetworkUtilsClient.ReadByte(ref state);
                switch (type)
                {

                    case FduClusterInputType.Axis:
                        float fvalue = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        if (!_axisMap.ContainsKey(name))
                        {
                            AxisData _data = new AxisData();
                            _data.reset();
                            _data.setValue(fvalue);
                            _axisMap.Add(name,_data);
                        }else
                        {
                            _axisMap[name].setValue(fvalue);
                        }
                        break;
                    case FduClusterInputType.Button:
                        bool bvalue = BufferedNetworkUtilsClient.ReadBool(ref state);
                        if (!_buttonMap.ContainsKey(name))
                        {
                            ButtonData _data = new ButtonData();
                            _data.reset();
                            _data.setValue(bvalue);
                            _buttonMap.Add(name, _data);
                        }
                        else
                        {
                            _buttonMap[name].setValue(bvalue);
                        }
                        break;
                    case FduClusterInputType.Tracker:
                        Vector3 v3Value = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        Quaternion quValue = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                        if (!_trackerMap.ContainsKey(name))
                        {
                            TrackerData _data = new TrackerData();
                            _data.reset();
                            _data.setPosValue(v3Value);
                            _data.setRotValue(quValue);
                            _trackerMap.Add(name, _data);
                        }else
                        {
                            _trackerMap[name].setPosValue(v3Value);
                            _trackerMap[name].setRotValue(quValue);
                        }
                        break;
                }
            }
            //if(!BufferedNetworkUtilsClient.ReadString(ref state).Equals("ClusterInputMgrEndFlag"))
            //{
            //    Debug.LogError("Wrong end！");
            //}
            //StartCoroutine(swapValueCo());
            return state;
        }
        #endregion

        #region public methods
        static bool _getButton(string name,int type)
        {
            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Button name can not be null");
                return false;
            }
            if (!_instance._buttonMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    _instance.kmCollector.addButtonName(name);

                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Button;
                    ButtonData bData = new ButtonData();
                    bData.reset();
                    _instance._sendList.Add(_data);
                    _instance._buttonMap.Add_overlay(name, bData);

                }
                return false;
            }
            else
            {
                if (type == 0)
                    return _instance._buttonMap[name].getValue();
                else if (type == 1)
                    return _instance._buttonMap[name].isDown();
                else
                    return _instance._buttonMap[name].isUp();
            }
        }

        public static bool GetButton(string name)
        {
            return _getButton(name, 0);
        }

        public static bool GetButtonDown(string name)
        {
            return _getButton(name, 1);
        }

        public static bool GetButtonUp(string name)
        {
            return _getButton(name, 2);
        }

        public static void SetButton(string name,bool value)
        {
            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Button name can not be null");
                return;
            }
            if (!_instance.isMaster) return;
            if (!_instance._buttonMap.ContainsKey(name))
            {
                _instance.kmCollector.addButtonName(name);

                SerializeData _data = new SerializeData();
                _data.name = name;
                _data.type = FduClusterInputType.Button;
                ButtonData bData = new ButtonData();
                bData.reset();
                bData.setValue(value);
                _instance._sendList.Add(_data);
                _instance._buttonMap.Add_overlay(name, bData);
                return;
            }
            else
            {
                _instance._buttonMap[name].setValue(value);
            }
        }
        
        public static bool GetKey(KeyCode code)
        {
            string name = FduClusterInputMgr.keyBoardPrefix + code.ToString();
            _instance.kmCollector.addKeyboardName(code);
            return GetButton(name);
        }
        public static bool GetKeyDown(KeyCode code)
        {
            string name = FduClusterInputMgr.keyBoardPrefix + code.ToString();
            _instance.kmCollector.addKeyboardName(code);
            return GetButtonDown(name);
        }
        public static bool GetKeyUp(KeyCode code)
        {
            string name = FduClusterInputMgr.keyBoardPrefix + code.ToString();
            _instance.kmCollector.addKeyboardName(code);
            return GetButtonUp(name);
        }

        public static bool GetMouseButton(int num)
        {
            string name = FduClusterInputMgr.mousePrefix + num.ToString();
            _instance.kmCollector.addMouseName(num);
            return GetButton(name);
        }

        public static bool GetMouseButtonDown(int num)
        {
            string name = FduClusterInputMgr.mousePrefix + num.ToString();
            _instance.kmCollector.addMouseName(num);
            return GetButtonDown(name);
        }

        public static bool GetMouseButtonUp(int num)
        {
            string name = FduClusterInputMgr.mousePrefix + num.ToString();
            _instance.kmCollector.addMouseName(num);
            return GetButtonUp(name);
        }


        public static void SetKey(KeyCode code,bool value)
        {
            string name = FduClusterInputMgr.keyBoardPrefix + code.ToString();
            SetButton(name,value);
        }
        public static void SetMouse(int num,bool value)
        {
            string name = FduClusterInputMgr.mousePrefix + num.ToString();
            SetButton(name, value);
        }

        public static float GetAxis(string name)
        {
            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Axis name can not be null");
                return 0.0f;
            }
            if (!_instance._axisMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    _instance.kmCollector.addAxisName(name);

                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Axis;
                    AxisData aData = new AxisData();
                    aData.reset();
                    _instance._sendList.Add(_data);
                    _instance._axisMap.Add_overlay(name, aData);
                }
                return 0.0f;
            }
            else
            {
                return _instance._axisMap[name].getValue();
            }
        }
        public static void SetAxis(string name,float value)
        {
            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Axis name can not be null");
                return;
            }
            if (!_instance.isMaster) return;
            if (!_instance._axisMap.ContainsKey(name))
            {
                _instance.kmCollector.addAxisName(name);

                SerializeData _data = new SerializeData();
                _data.name = name;
                _data.type = FduClusterInputType.Axis;
                AxisData aData = new AxisData();
                aData.reset();
                aData.setValue(value);
                _instance._sendList.Add(_data);
                _instance._axisMap.Add_overlay(name, aData);
            }
            else
            {
                _instance._axisMap[name].setValue(value);
            }
        }

        public static Vector3 GetPosition(string name) {

            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Tracker name can not be null");
                return Vector3.zero ;
            }
            if (!_instance._trackerMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Tracker;
                    TrackerData tData = new TrackerData();
                    tData.reset();
                    _instance._sendList.Add(_data);
                    _instance._trackerMap.Add_overlay(name, tData);
                }
                return Vector3.zero;
            }
            else
            {
                return _instance._trackerMap[name].getPosValue();
            }
        }

        public static void SetPosition(string name, Vector3 value) {

            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Tracker name can not be null");
                return;
            }
            if (!_instance.isMaster) return;
            if (!_instance._trackerMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Tracker;
                    TrackerData tData = new TrackerData();
                    tData.reset();
                    tData.setPosValue(value);
                    _instance._sendList.Add(_data);
                    _instance._trackerMap.Add_overlay(name, tData);
                }
            }
            else
            {
                _instance._trackerMap[name].setPosValue(value);
            }

        }

        public static Quaternion GetQuaternion(string name) {
            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Tracker name can not be null");
                return Quaternion.identity;
            }
            if (!_instance._trackerMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Tracker;
                    TrackerData tData = new TrackerData();
                    tData.reset();
                    _instance._sendList.Add(_data);
                    _instance._trackerMap.Add_overlay(name, tData);
                }
                return Quaternion.identity;
            }
            else
            {
                return _instance._trackerMap[name].getRotValue();
            }

        }

        public static void SetQuaternion(string name, Quaternion value) {

            if (name == null)
            {
                Debug.LogError("[FduClusterInput]Tracker name can not be null");
                return;
            }
            if (!_instance.isMaster) return;
            if (!_instance._trackerMap.ContainsKey(name))
            {
                if (_instance.isMaster)
                {
                    SerializeData _data = new SerializeData();
                    _data.name = name;
                    _data.type = FduClusterInputType.Tracker;
                    TrackerData tData = new TrackerData();
                    tData.reset();
                    tData.setRotValue(value);
                    _instance._sendList.Add(_data);
                    _instance._trackerMap.Add_overlay(name, tData);
                }
            }
            else
            {
                _instance._trackerMap[name].setRotValue(value);
            }

        }
        #endregion
    }

}

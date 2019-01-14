using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public class FduUniversalObserver : FduMultiAttributeObserverBase
    {

        [System.Serializable]
        public class BitArrayContainer
        {
            public byte[] arr = null;
        }

        [SerializeField]
        string _bitArrayJson = null;

        [SerializeField]
        Component _ObservedComponent;

        System.Type _ComponentType;

        System.Reflection.PropertyInfo[] _props;

        BitArray _bitArray;


        void Awake()
        {
#if CLUSTER_ENABLE
            fduObserverInit();
            Init();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
#endif
        }

        void Init()
        {
            if (_ObservedComponent != null)
            {
                _ComponentType = _ObservedComponent.GetType();
                _props = _ComponentType.GetProperties();
            }
            if (_bitArrayJson != null && _bitArrayJson.Length>0)
            {
                _bitArray = new BitArray(JsonUtility.FromJson<BitArrayContainer>(_bitArrayJson).arr);
#if !UNITY_EDITOR
                _bitArrayJson = "";
#endif
            }
            else
            {
                if (_props != null)
                    _bitArray = new BitArray(_props.Length);
                else
                    _bitArray = new BitArray(1);
            }
        }

        public override bool setObservedState(string name, bool value)
        {
#if !UNSAFE_MODE
            if (name == null || _ObservedComponent==null) return false;
            if (_ComponentType == null || _props == null) { Init(); }
            for (int i = 0; i < _props.Length; ++i)
            {
                if (FduSupportClass.isSendableGenericType(_props[i].PropertyType) && _props[i].CanRead && _props[i].CanWrite)
                {
                    if (_props[i].Name.ToUpper().Equals(name.ToUpper()))
                    {
                        _bitArray[i] = value;
                        return true;
                    }
                }
            }
            return false;
#else
            Debug.LogWarning("You can not use setObservedState method in unsafe mode!");
            return false;
#endif
        }

        public override bool getObservedState(string name)
        {
            if (name == null) return false;
            if (name == null || _ObservedComponent == null) return false;
            if (_ComponentType == null || _props == null) { Init(); }
            for (int i = 0; i < _props.Length; ++i)
            {
                if (FduSupportClass.isSendableGenericType(_props[i].PropertyType) && _props[i].CanRead && _props[i].CanWrite)
                {
                    if (_props[i].Name.ToUpper().Equals(name))
                    {
                        return _bitArray[i] ;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Set the Component which will be observed. Can not be used in unsafe mode.
        /// 1. You must make sure it is called both on master node and client node AT THE SAME FRAME.
        /// 2. You must make sure it is called before the LateUpdate of the corresponding cluster view is called.
        /// </summary>
        /// <param name="com"></param>
        public void setObservedComponent(Component com)
        {
#if !UNSAFE_MODE
            _ObservedComponent = com;
            _bitArrayJson = "";
            Init();
#else
            Debug.LogWarning("You can not use setObservedComponent method in unsafe mode!");
            return;
#endif
        }

        public override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
        }

        public override void OnSendData()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;

#if !UNSAFE_MODE
            byte [] temp = new byte[_bitArray.Length / 8 +1];
            _bitArray.CopyTo(temp, 0);
            BufferedNetworkUtilsServer.SendByteArray(temp);
#endif
            switchCaseFunc(FduMultiAttributeObserverOP.SendData, ref state);
        }

        public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
#if !UNSAFE_MODE
            byte[] temp = BufferedNetworkUtilsClient.ReadByteArray(ref state);
            _bitArray = new BitArray(temp);
#endif
            switchCaseFunc(FduMultiAttributeObserverOP.Receive_Direct, ref state);
        }

#if UNITY_EDITOR
        public BitArray getBitArray()
        {
            return _bitArray;
        }
#endif

        void switchCaseFunc(FduMultiAttributeObserverOP op, ref NetworkState.NETWORK_STATE_TYPE state)
        {
            if (_ObservedComponent == null || _props == null) return;

            for (int i = 0; i < _bitArray.Length; ++i)
            {
                if (!_bitArray[i]) continue;
                if (op == FduMultiAttributeObserverOP.SendData)
                {
                    if (_props[i].PropertyType.IsEnum)
                    {
                        BufferedNetworkUtilsServer.SendInt(System.Convert.ToInt32(_props[i].GetValue(_ObservedComponent, null)));
                    }
                    else if (_props[i].PropertyType.Equals(typeof(string)))
                    {
                        BufferedNetworkUtilsServer.SendString((string)_props[i].GetValue(_ObservedComponent, null));
                    }
                    else
                    {
                        BufferedNetworkUtilsServer.SendStruct(_props[i].GetValue(_ObservedComponent, null));
                    }
                }
                else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                {
                    if (_props[i].PropertyType.IsEnum)
                    {
                        _props[i].SetValue(_ObservedComponent, BufferedNetworkUtilsClient.ReadInt(ref state), null);
                    }
                    else if (_props[i].PropertyType.Equals(typeof(string)))
                    {
                        _props[i].SetValue(_ObservedComponent, BufferedNetworkUtilsClient.ReadString(ref state), null);
                    }
                    else
                    {
                        _props[i].SetValue(_ObservedComponent, BufferedNetworkUtilsClient.ReadStruct(_props[i].PropertyType, ref state), null);
                    }
                }
            }
        }
    }
}

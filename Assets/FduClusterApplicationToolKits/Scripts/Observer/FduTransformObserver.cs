/*
 * FduTransformObserver
 * 简介：transform的监控器
 * 
 * 最后修改时间：Hayate 2017.09.14
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduTransformObserver : FduMultiAttributeObserverBase
    {

        Transform _transform;

        NetworkState.NETWORK_STATE_TYPE stateTemp = NetworkState.NETWORK_STATE_TYPE.SUCCESS;

        public static readonly string[] attributeList = new string[] { 
            "NULL","Position","Rotation","LocalScale","Parent*","LocalPosition","LocalRotation","LocalScale","EulerAngles","LocalEulerAngles"
        };

        void Awake()
        {
            _transform = GetComponent<Transform>();
#if CLUSTER_ENABLE
            fduObserverInit();
            loadObservedState();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
#endif
        }

        public override bool setObservedState(string name, bool value)
        {
#if !UNSAFE_MODE
            if (name == null) return false;
            for (int i = 1; i < attributeList.Length; ++i)
            {
                if (attributeList[i].ToUpper().Contains(name.ToUpper()))
                {
                    setObservedState(i, value);
                    return true;
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
            for (int i = 1; i < attributeList.Length; ++i)
            {
                if (attributeList[i].ToUpper().Contains(name.ToUpper()))
                {
                    return getObservedState(i);
                }
            }
            return false;
        }

        public override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
            if (!getInterpolationState() || FduSupportClass.isMaster) return; //标志位为真且为从节点才进行插值
            switchCaseFunc(FduMultiAttributeObserverOP.Update, ref stateTemp);
        }
        public override void OnSendData()
        {
#if !UNSAFE_MODE
            BufferedNetworkUtilsServer.SendInt(observedState.Data);
#endif
            switchCaseFunc(FduMultiAttributeObserverOP.SendData, ref stateTemp);
        }
        public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            //如果需要插值 则缓存数据 否则直接赋值
            bool interFlag = false;
            if (dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount) != null && getInterpolationState())
                interFlag = true;

#if !UNSAFE_MODE
            observedState = new System.Collections.Specialized.BitVector32(BufferedNetworkUtilsClient.ReadInt(ref state));
#endif
            if (!interFlag)
            {
                switchCaseFunc(FduMultiAttributeObserverOP.Receive_Direct, ref state);
            }
            else
            {
                switchCaseFunc(FduMultiAttributeObserverOP.Receive_Interpolation, ref state);
            }
        }
        void switchCaseFunc(FduMultiAttributeObserverOP op, ref NetworkState.NETWORK_STATE_TYPE state)
        {
            for (int i = 1; i < attributeList.Length; ++i)
            {
                if (!getObservedState(i)) //如果editor中没有选择监控该属性 则直接跳过
                    continue;

                switch (i)
                {
                    case 1://position
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if(getCachedProperytyCount(i)>0)
                                _transform.position = FduInterpolationInterface.getNextVector3Value_new(_transform.position, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.position);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.position = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 2://rotation
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                _transform.rotation = FduInterpolationInterface.getNextQuaternionValue_new(_transform.rotation, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendQuaternion(_transform.rotation);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.rotation = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadQuaternion(ref state));
                        break;
                    case 3://scale
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                _transform.localScale = FduInterpolationInterface.getNextVector3Value_new(_transform.localScale, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.localScale);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localScale = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 4://parent: 对于父节点的监控 这里只是一个不完全的解决方案 获取到父节点的路径 传输过去 然后在从节点解析。如果路径不唯一（因为可以同名）就会找不到对于的父节点
                        if (op == FduMultiAttributeObserverOP.SendData)
                        {
                            string parentPath;
                            if (_transform.parent == null)
                                parentPath = "";
                            else
                                parentPath = FduSupportClass.getGameObjectPath(_transform.parent.gameObject);

                            if (parentPath == null)
                                parentPath = "";
                            BufferedNetworkUtilsServer.SendString(parentPath);
                        }
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                        {
                            string parentPath;
                            if (_transform.parent == null)
                                parentPath = "";
                            else
                                parentPath = FduSupportClass.getGameObjectPath(_transform.parent.gameObject);

                            if (parentPath == null)
                                parentPath = "";
                            string getPath = BufferedNetworkUtilsClient.ReadString(ref state);
                            if (parentPath != getPath) //hierarchy changed
                            {
                                GameObject go = FduSupportClass.getGameObjectByPath(getPath);
                                if (go == null)
                                    _transform.SetParent(null);
                                else
                                    _transform.SetParent(go.transform);
                            }
                        }
                        break;
                    case 5://LocalPosition
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if(getCachedProperytyCount(i)>0)
                                _transform.localPosition = FduInterpolationInterface.getNextVector3Value_new(_transform.localPosition, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.localPosition);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localPosition = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 6://LocalRotation;
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                _transform.localRotation = FduInterpolationInterface.getNextQuaternionValue_new(_transform.localRotation, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendQuaternion(_transform.localRotation);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localRotation = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadQuaternion(ref state));
                        break;
                    case 7://LocalScale
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if(getCachedProperytyCount(i)>0)
                                _transform.localScale = FduInterpolationInterface.getNextVector3Value_new(_transform.localScale, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.localScale);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localScale = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 8://EulerAngle
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if(getCachedProperytyCount(i)>0)
                                _transform.eulerAngles = FduInterpolationInterface.getNextVector3Value_new(_transform.eulerAngles, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.eulerAngles);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.eulerAngles = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 9://LocalEulerAngle
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if(getCachedProperytyCount(i)>0)
                                _transform.localEulerAngles = FduInterpolationInterface.getNextVector3Value_new(_transform.localEulerAngles, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.localEulerAngles);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localEulerAngles = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

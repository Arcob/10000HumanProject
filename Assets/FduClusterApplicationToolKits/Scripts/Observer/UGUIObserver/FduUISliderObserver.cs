/*
 * FduUISliderObserver
 * 
 * 简介：UI Slider组件的监控器
 * 
 * 最后修改时间：Hayate 2017.09.14
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [RequireComponent(typeof(Slider))]
    public class FduUISliderObserver : FduMultiAttributeObserverBase
    {

        Slider slider;

        static public readonly string[] attrList = { 
            "NULL", "MaxValue" , "CurrentValue" , "MinValue" , "Direction",
            "NormalizedValue" , "WholeNumbers"
        };

        void Awake()
        {
#if CLUSTER_ENABLE
            slider = GetComponent<Slider>();
            fduObserverInit();
            loadObservedState();
            processRemoveFunc();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
#endif
        }
        void processRemoveFunc()
        {
            if (ClusterHelper.Instance.Server != null)
                return;
            if (getObservedState(30))
            {
                for (int i = 0; i < slider.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    slider.onValueChanged.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                }
                slider.onValueChanged.RemoveAllListeners();
            }

        }
        public override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
            if (!getInterpolationState() || FduSupportClass.isMaster) return; //标志位为真且为从节点才进行插值

            NetworkState.NETWORK_STATE_TYPE stateTemp = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            switchCaseFunc(FduMultiAttributeObserverOP.Update, ref stateTemp);
        }

        public override void OnSendData()
        {
#if !UNSAFE_MODE
            BufferedNetworkUtilsServer.SendInt(observedState.Data);
#endif
            NetworkState.NETWORK_STATE_TYPE stateTemp = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            switchCaseFunc(FduMultiAttributeObserverOP.SendData, ref stateTemp);
        }
        public override void OnReceiveData(ref FDUObjectSync.NetworkState.NETWORK_STATE_TYPE state)
        {
#if !UNSAFE_MODE
            observedState = new System.Collections.Specialized.BitVector32(BufferedNetworkUtilsClient.ReadInt(ref state));
#endif
            bool interFlag = false;
            if (dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount) != null && getInterpolationState())
                interFlag = true;
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
            for (int i = 1; i < attrList.Length; ++i)
            {
                if (!getObservedState(i))
                    continue;
                switch (i)
                {
                    case 1://MaxValue
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                slider.maxValue = FduInterpolationInterface.getNextFloatValue_new(slider.maxValue, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendFloat(slider.maxValue);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            slider.maxValue = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 2://CurrentValue
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                slider.value = FduInterpolationInterface.getNextFloatValue_new(slider.value, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendFloat(slider.value);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            slider.value = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 3://MinValue
                         if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                slider.minValue = FduInterpolationInterface.getNextFloatValue_new(slider.minValue, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                             BufferedNetworkUtilsServer.SendFloat(slider.minValue);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                             slider.minValue = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 4://Direction
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)slider.direction);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            slider.direction = (Slider.Direction)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 5://NormalizedValue
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                slider.normalizedValue = FduInterpolationInterface.getNextFloatValue_new(slider.normalizedValue, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendFloat(slider.normalizedValue);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            slider.normalizedValue = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 6://WholeNumbers
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(slider.wholeNumbers);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            slider.wholeNumbers = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                }
            }
        }
        public override bool setObservedState(string name, bool value)
        {
#if !UNSAFE_MODE
            if (name == null) return false;
            for (int i = 1; i < attrList.Length; ++i)
            {
                if (attrList[i].ToUpper().Contains(name.ToUpper()))
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
            for (int i = 1; i < attrList.Length; ++i)
            {
                if (attrList[i].ToUpper().Contains(name.ToUpper()))
                {
                    return getObservedState(i);
                }
            }
            return false;
        }


    }
}

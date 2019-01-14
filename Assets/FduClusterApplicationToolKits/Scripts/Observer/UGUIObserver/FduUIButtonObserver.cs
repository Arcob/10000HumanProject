/*
 * FduUIButtonObserver
 * 
 * 简介：UI button监控器
 * 能监控Interactable属性和Transition属性
 * 可以选择移除从节点的回调函数
 * 
 * 最近修改时间：Hayate 2017.09.14
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FDUObjectSync;
using UnityEngine.Events;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [RequireComponent(typeof(Button))]
    public class FduUIButtonObserver : FduMultiAttributeObserverBase
    {
        public static readonly string[] attrList = {
            "NULL","Interactable","Transition"                                            
        };

        Button button;
        void Awake()
        {
#if CLUSTER_ENABLE
            button = GetComponent<Button>();
            fduObserverInit();
            loadObservedState();
            processRemoveFunc();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
#endif
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
        void processRemoveFunc()
        {
            if (ClusterHelper.Instance.Server != null)
                return;
            if (getObservedState(31))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); ++i)
                {
                    button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                button.onClick.RemoveAllListeners();
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
                    case 1://Interactable
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(button.interactable);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            button.interactable = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 2://Transition
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)button.transition);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            button.transition = (Selectable.Transition)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 30://remove func
                        break;
                    case 31://Interpolation Option
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

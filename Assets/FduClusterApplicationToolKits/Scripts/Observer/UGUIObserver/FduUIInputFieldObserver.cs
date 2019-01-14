/*
 * FduUIInputFieldObserver
 * 
 * 简介：InputField组件监控器
 * 可以监控输入的内容和光标位置
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
    [RequireComponent(typeof(InputField))]
    public class FduUIInputFieldObserver : FduMultiAttributeObserverBase
    {

        InputField inputField;

        public static readonly string[] attrList = { 
            "NULL","InputContent" , "CaretPosition" ,"CharacterValidation" ,"ContentType","InputType",
            "KeyboardType" , "LineType"                             
        };

        void Awake()
        {
#if CLUSTER_ENABLE
            inputField = GetComponent<InputField>();
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
            if (getObservedState(29))
            {
                for (int i = 0; i < inputField.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    inputField.onValueChanged.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                }
                inputField.onValueChanged.RemoveAllListeners();
            }
            if (getObservedState(30))
            {
                for (int i = 0; i < inputField.onEndEdit.GetPersistentEventCount(); ++i)
                {
                    inputField.onEndEdit.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                }
                inputField.onEndEdit.RemoveAllListeners();
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
                    case 1: //InputContent
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendString(inputField.text);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.text = BufferedNetworkUtilsClient.ReadString(ref state);
                        break;
                    case 2://CaretPosition
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                inputField.caretPosition = FduInterpolationInterface.getNextIntValue_new(inputField.caretPosition, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendInt(inputField.caretPosition);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            inputField.caretPosition = BufferedNetworkUtilsClient.ReadInt(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadInt(ref state));
                        break;
                    case 3://CharacterValidation
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)inputField.characterValidation);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.characterValidation = (InputField.CharacterValidation)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 4://ContentType
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)inputField.contentType);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.contentType = (InputField.ContentType)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 5://InputType
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)inputField.inputType);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.inputType = (InputField.InputType)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 6://KeyboardType
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)inputField.keyboardType);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.keyboardType = (TouchScreenKeyboardType)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 7://LineType
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)inputField.lineType);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            inputField.lineType = (InputField.LineType)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 29://Remove On Value Change CallBack On Slave
                        break;
                    case 30://Remove On End Edit CallBack On Slave
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

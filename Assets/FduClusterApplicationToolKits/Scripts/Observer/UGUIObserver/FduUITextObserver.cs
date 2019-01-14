/*
 * FduUITextObserver
 * 
 * 简介:UI Text组件的监控器
 * 
 * 最后修改时间：2017.09.14
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using UnityEngine.UI;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [RequireComponent(typeof(Text))]
    public class FduUITextObserver : FduMultiAttributeObserverBase
    {
        Text text;

        public static readonly string[] attrList = { 
            "NULL","TextContent" ,"Color", "FontStyle" , "FontSize" , "LineSpacing" ,"SupportRichText",
            "Aligment", "AlignByGeometry", "HorizontalOverflow", "VerticalOverflow" , "BestFit",
            "RaycastTarget"                         
        };

        void Awake()
        {
#if CLUSTER_ENABLE
            text = GetComponent<Text>();
            fduObserverInit();
            loadObservedState();
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
        void switchCaseFunc(FduMultiAttributeObserverOP op, ref NetworkState.NETWORK_STATE_TYPE state)
        {
            for (int i = 1; i < attrList.Length; ++i)
            {
                if (!getObservedState(i))
                    continue;
                switch (i)
                {
                    case 1: //TextContent
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendString(text.text);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.text = BufferedNetworkUtilsClient.ReadString(ref state);
                        break;
                    case 2://Color
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                text.color = FduInterpolationInterface.getNextColorValue_new(text.color, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendColor(text.color);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            text.color = BufferedNetworkUtilsClient.ReadColor(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadColor(ref state));
                        break;
                    case 3: //FontStyle
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)text.fontStyle);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.fontStyle = (FontStyle)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 4://FontSize
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                text.fontSize = FduInterpolationInterface.getNextIntValue_new(text.fontSize, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendInt(text.fontSize);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            text.fontSize = BufferedNetworkUtilsClient.ReadInt(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadInt(ref state));
                        break;
                    case 5://LineSpacing
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                text.lineSpacing = FduInterpolationInterface.getNextFloatValue_new(text.lineSpacing, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendFloat(text.lineSpacing);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            text.lineSpacing = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 6://SupportRichText
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(text.supportRichText);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.supportRichText = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 7://Aligment
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)text.alignment);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.alignment = (TextAnchor)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 8://AlignByGeometry
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(text.alignByGeometry);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.alignByGeometry = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 9://HorizontalOverflow
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)text.horizontalOverflow);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.horizontalOverflow = (HorizontalWrapMode)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 10://VerticalOverflow
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)text.verticalOverflow);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.verticalOverflow = (VerticalWrapMode)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 11://BestFit
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(text.resizeTextForBestFit);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.resizeTextForBestFit = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 12://RaycastTarget
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(text.raycastTarget);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            text.raycastTarget = BufferedNetworkUtilsClient.ReadBool(ref state);
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

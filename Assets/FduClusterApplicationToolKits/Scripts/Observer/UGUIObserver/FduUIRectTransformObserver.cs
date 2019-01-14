/*
 * FduUIRectTransformObserver
 * 
 * 简介：UI 的RectTransform组件的监控器
 * 
 * 最后修改时间：2017.09.14
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    [RequireComponent(typeof(RectTransform))]
    public class FduUIRectTransformObserver : FduMultiAttributeObserverBase
    {

        public static readonly string[] attrList = {
           "NULL","AnchoredPosition","AnchoredPosition3D","Rotation","Scale","Parent*","AnchorMax","AnchorMin","OffsetMax","OffsetMin",
           "Pivot" , "SizeDelta"
        };

        RectTransform rectTransform;

        void Awake()
        {
#if CLUSTER_ENABLE
            rectTransform = GetComponent<RectTransform>();
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
            switchCaseFunc(FduMultiAttributeObserverOP.SendData,ref stateTemp);
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
                    case 1: //AnchoredPosition
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.anchoredPosition = FduInterpolationInterface.getNextVector2Value_new(rectTransform.anchoredPosition, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.anchoredPosition);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.anchoredPosition = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 2://AnchoredPosition3D
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                rectTransform.anchoredPosition3D = FduInterpolationInterface.getNextVector3Value_new(rectTransform.anchoredPosition3D,i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(rectTransform.anchoredPosition3D);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.anchoredPosition3D = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 3://Rotation
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.rotation = FduInterpolationInterface.getNextQuaternionValue_new(rectTransform.rotation, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendQuaternion(rectTransform.rotation);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.rotation = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i,BufferedNetworkUtilsClient.ReadQuaternion(ref state));
                        break;
                    case 4://Scale
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                rectTransform.localScale = FduInterpolationInterface.getNextVector3Value_new(rectTransform.localScale, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(rectTransform.localScale);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.localScale = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 5://Parent
                        if (op == FduMultiAttributeObserverOP.SendData)
                        {
                            string parentPath;
                            if (rectTransform.parent == null)
                                parentPath = "";
                            else
                                parentPath = FduSupportClass.getGameObjectPath(rectTransform.parent.gameObject);

                            if (parentPath == null)
                                parentPath = "";
                            BufferedNetworkUtilsServer.SendString(parentPath);
                        }
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                        {
                            string parentPath;
                            if (rectTransform.parent == null)
                                parentPath = "";
                            else
                                parentPath = FduSupportClass.getGameObjectPath(rectTransform.parent.gameObject);

                            if (parentPath == null)
                                parentPath = "";
                            string getPath = BufferedNetworkUtilsClient.ReadString(ref state);
                            if (parentPath != getPath) //hierarchy changed
                            {
                                GameObject go = FduSupportClass.getGameObjectByPath(getPath);
                                if (go == null)
                                    rectTransform.SetParent(null);
                                else
                                    rectTransform.SetParent(go.transform);
                            }
                        }
                        break;
                    case 6://AnchorMax
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.anchorMax = FduInterpolationInterface.getNextVector2Value_new(rectTransform.anchorMax, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.anchorMax);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.anchorMax = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 7://AnchorMin
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.anchorMin = FduInterpolationInterface.getNextVector2Value_new(rectTransform.anchorMin, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                             BufferedNetworkUtilsServer.SendVector2(rectTransform.anchorMin);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                             rectTransform.anchorMin = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 8://OffsetMax
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.offsetMax = FduInterpolationInterface.getNextVector2Value_new(rectTransform.offsetMax, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.offsetMax);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.offsetMax = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 9://OffsetMin
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.offsetMin = FduInterpolationInterface.getNextVector2Value_new(rectTransform.offsetMin, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.offsetMin);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.offsetMin = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 10://Pivot
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.pivot = FduInterpolationInterface.getNextVector2Value_new(rectTransform.pivot, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.pivot);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.pivot = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
                        break;
                    case 11://SizeDelta
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                rectTransform.sizeDelta = FduInterpolationInterface.getNextVector2Value_new(rectTransform.sizeDelta, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector2(rectTransform.sizeDelta);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            rectTransform.sizeDelta = BufferedNetworkUtilsClient.ReadVector2(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty(i, BufferedNetworkUtilsClient.ReadVector2(ref state));
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

/*
 * FduUIImageObserver
 * 
 * 简介：UI Image组件的监控器
 * 可以监控图片颜色 fillAmount等属性 以及图片本身
 * 
 * 最近修改时间：2017.09.14
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [RequireComponent(typeof(Image))]
    public class FduUIImageObserver : FduMultiAttributeObserverBase
    {

        public static readonly string[] attrList = { 
            "NULL", "Color","RaycastTarget","FillMethod","FillAmount","FillCenter","FillClockwise","FillOrigin" ,"ImageType" ,"Image*"                                      
        };

        Image image;
        int spriteId = -1;

        void Awake()
        {
#if CLUSTER_ENABLE
            image = GetComponent<Image>();
            fduObserverInit();
            loadObservedState();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
            if (image.sprite != null)
                spriteId = image.sprite.GetInstanceID();
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
                    case 1://Color
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i)>0)
                                image.color = FduInterpolationInterface.getNextColorValue_new(image.color, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendColor(image.color);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            image.color = BufferedNetworkUtilsClient.ReadColor(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadColor(ref state));
                        break;
                    case 2://RaycastTarget
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(image.raycastTarget);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            image.raycastTarget = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 3://FillMethod
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)image.fillMethod);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            image.fillMethod = (Image.FillMethod)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 4://FillAmount
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                image.fillAmount = FduInterpolationInterface.getNextFloatValue_new(image.fillAmount, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendFloat(image.fillAmount);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            image.fillAmount = BufferedNetworkUtilsClient.ReadFloat(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                        break;
                    case 5://FillCenter
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(image.fillCenter);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            image.fillCenter = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 6://FillClockwise
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendBool(image.fillClockwise);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            image.fillClockwise = BufferedNetworkUtilsClient.ReadBool(ref state);
                        break;
                    case 7://FillOrigin
                        if (op == FduMultiAttributeObserverOP.Update)
                        {
                            if (getCachedProperytyCount(i) > 0)
                                image.fillOrigin = FduInterpolationInterface.getNextIntValue_new(image.fillOrigin, i, this);
                        }
                        else if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendInt(image.fillOrigin);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            image.fillOrigin = BufferedNetworkUtilsClient.ReadInt(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadInt(ref state));
                        break;
                    case 8://ImageType
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendByte((byte)image.type);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            image.type = (Image.Type)BufferedNetworkUtilsClient.ReadByte(ref state);
                        break;
                    case 9://Image
                        if (op == FduMultiAttributeObserverOP.SendData)
                        {
                            bool changeFlag = false;
                            if (image.sprite != null && image.sprite.GetInstanceID() != spriteId)
                            { //通过对比InstanceId判断该精灵是否被替换
                                changeFlag = true;
                                spriteId = image.sprite.GetInstanceID();
                            }

                            BufferedNetworkUtilsServer.SendBool(changeFlag);
                            if (changeFlag)//若已经变化了 则将图片分解为JPG格式传送至节点
                            {
                                byte[] arr;
                                try
                                {
                                    arr = image.sprite.texture.EncodeToJPG();
                                    BufferedNetworkUtilsServer.SendByteArray(arr);
                                    BufferedNetworkUtilsServer.SendRect(image.sprite.textureRect);
                                    BufferedNetworkUtilsServer.SendVector2(image.sprite.pivot);
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError(e.Message);
                                }

                            }
                        }
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation) //从节点先判断有没有图片传来 然后再解析
                        {
                            bool changeFlag = BufferedNetworkUtilsClient.ReadBool(ref state);
                            if (changeFlag)
                            {
                                byte[] bytes = BufferedNetworkUtilsClient.ReadByteArray(ref state);
                                Rect rect = BufferedNetworkUtilsClient.ReadRect(ref state);
                                Vector2 pivot = BufferedNetworkUtilsClient.ReadVector2(ref state);
                                Texture2D texture = new Texture2D((int)GetComponent<RectTransform>().rect.width, (int)GetComponent<RectTransform>().rect.height);
                                texture.LoadImage(bytes);
                                image.sprite = Sprite.Create(texture,rect,pivot);
                            }
                        }
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

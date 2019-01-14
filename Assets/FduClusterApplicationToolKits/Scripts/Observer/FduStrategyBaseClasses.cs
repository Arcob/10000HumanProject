/*
 * FduDataTransmitStrategyBase
 * 
 * 简介：数据传输策略类的基类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public abstract class FduDataTransmitStrategyBase
    {
        /// <summary>
        /// The Instance of FduObserverBase which this data transmit strategy class belongs to.
        /// </summary>
        public FduObserverBase targetObject;
        /// <summary>
        /// Set the FduObserver instance. Not recommended.
        /// </summary>
        /// <param name="_ob"></param>
        public virtual void setObserver(FduObserverBase _ob) { targetObject = _ob; }
        /// <summary>
        /// Check whether the corresponding fduObserver will execute the onSendData function this frame.
        /// </summary>
        /// <returns></returns>
        public virtual bool sendOrNot() { return false; }
        /// <summary>
        /// Check whether the corresponding fduObserver will execute the onReceiveData function this frame. ONLY WORK FOR UNSAFE_MODE.
        /// In safe Mode, the OnReceiveData method will be called accroding to the bool value received from Master Node (i.e. sendOrNot Value).
        /// In unsafe Mode, you must make sure slave node can get the same bool value(using receiveOrNot method) with master node (using sendOrNot method) at the same frame.
        /// </summary>
        /// <returns></returns>
        public virtual bool receiveOrNot() { return false; }

        /// <summary>
        /// Called by the alwaysUpdate method in the correspondin fduobserver.
        /// </summary>
        public virtual void Update() { }
        /// <summary>
        /// Get Custom Data. You can override this function in your own dts class.
        /// </summary>
        /// <returns></returns>
        public virtual object getCustomData() { return "null"; }
        /// <summary>
        /// Set Custom Data. You can override this function in your own dts class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool setCustomData(object data) { return false; }
        public virtual object getCustomData(string propertyName) { return "null"; }
        public virtual bool setCustomData(string propertyName, object data) { return false; }
        public virtual object getCustomData(FduDTSCustomDataType dtsCustomDataType) { return "null"; }
        public virtual bool setCustomData(FduDTSCustomDataType dtsCustomDataType, object data) { return false; }
        /// <summary>
        /// Get interpolation state.
        /// </summary>
        /// <returns></returns>
        public virtual bool getInterpolationState() { return false; }
        public virtual void Destroy() { }
    }
    //Todo:这个只能由内部类继承
    public class FduUnityDataTransmitStrategyBase : FduDataTransmitStrategyBase
    {
        public virtual void Init(string param) { }
    }

    public enum FduDTSCustomDataType
    {
        Direct,
        OnEvent,
        EveryNFrame_Interval,EveryNFrame_CurFrameCount,EveryNFrame_Interpolation,EveryNFrame_Extrapolation,EveryNFrame_CachedMaxCount,EveryNFrame_LerpSpeed
    }

}


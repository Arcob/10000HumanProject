/*
 * 简介：本文件中定义了所有工具自己的数据传输策略类
 * 目前包括直接传输类 每N帧传输类以及当收到集群事件传输类
 * 
 * 最近修改日期：Hayate 2017.08.30
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    //工厂类 创建对应的数据传输策略类实例
    internal static class FduDTS_Factory
    {
        public static FduDataTransmitStrategyBase create(string name, string parameter)
        {
            if (name == null) { Debug.LogError("[FduDTS_Factory]Data Transmit Strategy class name can not be null"); }
            FduUnityDataTransmitStrategyBase instance = null;
            switch (name)
            {
                case "FduDTS_NULL":
                    instance = null;
                    break;
                case "FduDTS_Direct":
                    instance = new FduDTS_Direct();
                    break;
                case "FduDTS_EveryNFrame":
                    instance = new FduDTS_EveryNFrame();
                    break;
                case "FduDTS_OnClusterCommand":
                    instance = new FduDTS_OnClusterCommand();
                    break;
            }
            if (instance != null)
            {
                instance.Init(parameter);
            }
            return instance;
        }
    }
    //直接传送的数据传输策略类 每帧都传输数据 所以返回永远是true
    public class FduDTS_Direct : FduUnityDataTransmitStrategyBase
    {
        public override bool sendOrNot()
        {
            return true;
        }
        public override bool receiveOrNot()
        {
            return true;
        }
    }
    //每N帧传输一次的数据传输策略类 可以设置传输的间隔 达到一定间隔时返回true
    public class FduDTS_EveryNFrame : FduUnityDataTransmitStrategyBase
    {
        //内插选项
        public enum InterpolationOption
        {
            Disable, FixedStep, Lerp, EstimateStep
        }
        //外插选项
        public enum ExtrapolationOption
        {
            Disable, CachedLatest, CachedEarliest
        }
        //间隔帧数
        int _intervalFrame = 2;
        //当前的帧数计数
        int _currentFrame = 0;

        InterpolationOption interpolationOption = InterpolationOption.Disable;
        ExtrapolationOption extrapolationOption = ExtrapolationOption.Disable;
        //从节点缓存数据的最大数量
        int cachedPropertyMaxCount = 1;
        //如果使用lerp插值 用到的插值速度
        int lerpSpeed = 1;

        public override bool sendOrNot()
        {
            if (_currentFrame >= _intervalFrame-1)
            {
                return true;
            }
            else
                return false;
        }
        public override bool receiveOrNot()
        {
            return sendOrNot();
        }

        public override void Update()
        {
            _currentFrame = _currentFrame +1 >= _intervalFrame? 0:_currentFrame+1;
        }
        public override void Init(string para)
        {
            try
            {
                string[] paras = para.Split('&');
                _intervalFrame = int.Parse(paras[0]);
                _intervalFrame = _intervalFrame > FduGlobalConfig.EVERY_N_FRAME_MAX_FRAME ? FduGlobalConfig.EVERY_N_FRAME_MAX_FRAME : _intervalFrame;
                //_currentFrame = _intervalFrame;
                if (paras.Length == 5)
                {
                    interpolationOption = (InterpolationOption)int.Parse(paras[1]);
                    extrapolationOption = (ExtrapolationOption)int.Parse(paras[2]);
                    cachedPropertyMaxCount = int.Parse(paras[3]);
                    lerpSpeed = int.Parse(paras[4]);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FduDTS_EveryNFrame]Wrong interval parameter! " + e.Message);
            }
        }
        #region customData Set & Get
        public override object getCustomData()
        {
            return _currentFrame;
        }
        public override bool setCustomData(object data)
        {
            try
            {
                _currentFrame = (int)data;
            }
            catch (System.InvalidCastException)
            {
                return false;
            }
            return true;
        }
        public override object getCustomData(string propertyName)
        {
            if (propertyName == "curFrameCount")
                return _currentFrame;
            if (propertyName == "interval")
                return _intervalFrame;
            if (propertyName == "interpolationOption")
                return interpolationOption;
            if (propertyName == "extrapolationOption")
                return extrapolationOption;
            if (propertyName == "cachedPropertyMaxCount")
                return cachedPropertyMaxCount;
            if (propertyName == "lerpSpeed")
                return lerpSpeed;
            return null;
        }
        public override bool setCustomData(string propertyName, object data)
        {
            try
            {
                if (propertyName == "curFrameCount")
                    _currentFrame = (int)data;
                else if (propertyName == "interval")
                    _intervalFrame = (int)data;
                else if (propertyName == "interpolationOption")
                    interpolationOption = (FduDTS_EveryNFrame.InterpolationOption)data;
                else if (propertyName == "extrapolationOption")
                    extrapolationOption = (FduDTS_EveryNFrame.ExtrapolationOption)data;
                else if (propertyName == "cachedPropertyMaxCount")
                    cachedPropertyMaxCount = (int)data;
                else if (propertyName == "lerpSpeed")
                    lerpSpeed = (int)data;
            }
            catch (System.InvalidCastException)
            {
                return false;
            }
            return true;
        }
        public override object getCustomData(FduDTSCustomDataType dtsCustomDataType)
        {
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_CurFrameCount)
                return _currentFrame;
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Interval)
                return _intervalFrame;
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Interpolation)
                return interpolationOption;
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Extrapolation)
                return extrapolationOption;
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_CachedMaxCount)
                return cachedPropertyMaxCount;
            if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_LerpSpeed)
                return lerpSpeed;
            return null;
        }
        public override bool setCustomData(FduDTSCustomDataType dtsCustomDataType, object data)
        {
            try
            {
                if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_CurFrameCount)
                    _currentFrame = (int)data;
                else if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Interval)
                    _intervalFrame = (int)data;
                else if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Interpolation)
                    interpolationOption = (FduDTS_EveryNFrame.InterpolationOption)data;
                else if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_Extrapolation)
                    extrapolationOption = (FduDTS_EveryNFrame.ExtrapolationOption)data;
                else if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_CachedMaxCount)
                    cachedPropertyMaxCount = (int)data;
                else if (dtsCustomDataType == FduDTSCustomDataType.EveryNFrame_LerpSpeed)
                    lerpSpeed = (int)data;
            }
            catch (System.InvalidCastException)
            {
                return false;
            }
            return true;
        }
        #endregion

        public int getInterval()
        {
            return _intervalFrame;
        }
        public int getCurFrame()
        {
            return _currentFrame;
        }
        public InterpolationOption getInterPolationOption()
        {
            return interpolationOption;
        }
        public ExtrapolationOption getExtrapolationOption()
        {
            return extrapolationOption;
        }
        public int getCachedPropertyMaxCount()
        {
            return cachedPropertyMaxCount;
        }
        public int getLerpSpeed()
        {
            return lerpSpeed;
        }

        public override bool getInterpolationState()
        {
            return true;
        }
    }
    //当收到某个事件时传输的数据传输策略类
    //原理是主从节点都添加对应事件的监听器，监听器的回调是将trigger置为true 所以在下一帧就会传送数据
    public class FduDTS_OnClusterCommand : FduUnityDataTransmitStrategyBase
    {
        string _CommandName = "";
        bool trigger = false;
        uint dtsId = 0;
        public override void Init(string para)
        {
            if (para == null)
            {
                Debug.LogError("[FduDTS_OnClusterCommand]Command name set to FduDTS_OnClusterCommand can not be null");
            }
            _CommandName = para;
            //dtsId = FduDTS_OnCommandIDGenerator.getNextId();
            dtsId = FduClusterCommandDispatcher.AddCommandExecutor(_CommandName, onReceiveCommand);
        }
        public override void Update()
        {
            if (ClusterHelper.Instance.Server != null)
            {

            }
        }
        public override object getCustomData()
        {
            return _CommandName;
        }
        void onReceiveCommand(ClusterCommand e)
        {
            trigger = true;
        }
        public override bool sendOrNot()
        {
            if (trigger)
            {
                trigger = false;
                return true;
            }
            return false;
        }
        public override bool receiveOrNot()
        {
            return sendOrNot();
        }
        public override void Destroy()
        {
            FduClusterCommandDispatcher.RemoveCommandExecutor(_CommandName, dtsId);
        }
    }


    //内部使用 用来区分不同的事件触发的策略实例 
    //id的必要性在于添加事件监听器时确保一个唯一的名字（后缀）
    public static class FduDTS_OnCommandIDGenerator
    {
        static int current = 0;
        public static int getNextId()
        {
            return current++;
        }
    }
}


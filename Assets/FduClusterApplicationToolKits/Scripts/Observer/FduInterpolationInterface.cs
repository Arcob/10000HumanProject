/*
 * FduInterpolationInterface
 * 
 * 简介：插值所用到的通用接口
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class FduInterpolationInterface
    {
        //计算下一个Vector3的值
        public static Vector3 getNextVector3Value_new(Vector3 now,int propertyIndex,FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Vector3.zero;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector3 offset = Vector3.zero;
                Vector3 lastValue = (Vector3)observer.getCachedProperty_lastElement(propertyIndex);
                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (Vector3)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (Vector3)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        float step = Vector3.Distance(lastValue-offset, (Vector3)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2) ) / interval;
                        return Vector3.MoveTowards(now, lastValue, step);
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Vector3.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Vector3.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Vector3.zero;
            }
        }
        //计算下一个四元数值 其实是转换成欧拉角进行计算
        public static Quaternion getNextQuaternionValue_new(Quaternion now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Quaternion.identity;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector3 offset = Vector3.zero;
                Quaternion lastValue = (Quaternion)observer.getCachedProperty_lastElement(propertyIndex);
                Vector3 eulerValue = lastValue.eulerAngles;
                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (eulerValue - ((Quaternion)observer.getCachedProperty_firstElement(propertyIndex)).eulerAngles) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (eulerValue - ((Quaternion)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2)).eulerAngles);
                        }
                    }
                }
                eulerValue = eulerValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return Quaternion.Euler(eulerValue);
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep || interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Quaternion.Lerp(now, Quaternion.Euler(eulerValue), 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                    {
                        return Quaternion.Euler(eulerValue);
                    }
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Quaternion.Lerp(now, Quaternion.Euler(eulerValue), Time.deltaTime * dtsN.getLerpSpeed());
                }
                return Quaternion.Euler(eulerValue);
            }
            catch (System.Exception)
            {
                return Quaternion.identity;
            }
        }
        //计算下一个Vector2值
        public static Vector2 getNextVector2Value_new(Vector2 now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Vector2.zero;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector2 offset = Vector2.zero;
                Vector2 lastValue = (Vector2)observer.getCachedProperty_lastElement(propertyIndex);

                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (Vector2)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (Vector2)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        float step = Vector2.Distance(lastValue - offset, (Vector2)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2)) / interval;
                        return Vector2.MoveTowards(now, lastValue, step);
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Vector2.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Vector2.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Vector2.zero;
            }
        }
        //计算下一个Vector4值
        public static Vector4 getNextVector4Value_new(Vector4 now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Vector4.zero;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector4 offset = Vector4.zero;
                Vector4 lastValue = (Vector4)observer.getCachedProperty_lastElement(propertyIndex);

                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (Vector4)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (Vector4)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        float step = Vector4.Distance(lastValue - offset, (Vector4)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2)) / interval;
                        return Vector4.MoveTowards(now, lastValue, step);
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Vector4.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Vector4.Lerp(now, lastValue, Time.deltaTime *dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Vector4.zero;
            }
        }
        //计算下一个颜色值
        public static Color getNextColorValue_new(Color now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Color.black;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Color offset = Color.black;
                Color lastValue = (Color)observer.getCachedProperty_lastElement(propertyIndex);

                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (Color)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (Color)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep || interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Color.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Color.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Color.black;
            }
        }
        //计算下一个Int值
        public static int getNextIntValue_new(int now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return 0;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                int offset = 0;
                int lastValue = (int)observer.getCachedProperty_lastElement(propertyIndex);

                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (int)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (int)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        int step = ((lastValue - offset) - (int)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2)) / interval;
                        return now + step;
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        int res = (int)Mathf.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return (int)Mathf.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return 0;
            }
        }
        //计算下一个Float值
        public static float getNextFloatValue_new(float now, int propertyIndex, FduMultiAttributeObserverBase observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return 0;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption() ;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                float offset = 0;
                float lastValue = (float)observer.getCachedProperty_lastElement(propertyIndex);

                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - (float)observer.getCachedProperty_firstElement(propertyIndex)) / (count - 1);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - (float)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedProperytyCount(propertyIndex);
                    if (count >= 2)
                    {
                        float step = ((lastValue - offset) - (float)observer.getCachedProperty_atArrayIndex(propertyIndex, count - 2)) / interval;
                        return now + step;
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        float res = Mathf.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Mathf.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

    }
}

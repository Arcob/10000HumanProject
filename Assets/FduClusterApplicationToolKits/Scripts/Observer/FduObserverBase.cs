/*
 * FduObserverBase
 * 
 * 简介：Observer基类
 * 所有Observer都最终继承自此类
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using System.Collections.Specialized;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public abstract class FduObserverBase : MonoBehaviour
    {

        //该Observer所属的view
        [SerializeField]
        [HideInInspector]
        protected FduClusterView _viewInstance;
        //数据传输类实例
        protected FduDataTransmitStrategyBase dataTransmitStrategy;

        //用于实例化数据传输类数据传输类名字
        [SerializeField]
        [HideInInspector]
        protected string dataTransmitStrategyName = "FduDTS_Direct";
        //用于实例化数据传输类的数据传输类参数
        [SerializeField]
        [HideInInspector]
        protected string dataTransmitStrategyParameter = "";

        /// <summary>
        /// Init observer. Create the Instance of corresponding data transmit strategy class
        /// </summary>
        protected void fduObserverInit()
        {
#if CLUSTER_ENABLE
            if (dataTransmitStrategy == null)
            {
                dataTransmitStrategy = getDTS_Instance(dataTransmitStrategyName, dataTransmitStrategyParameter);
                if (dataTransmitStrategy == null)
                    dataTransmitStrategy = getCustomDTS_Instance(dataTransmitStrategyName, dataTransmitStrategyParameter);
            }
#endif
            dataTransmitStrategyName = null;
            dataTransmitStrategyParameter = null;
        }
        /// <summary>
        /// Set Direct Data Transmit strategy class data. Call this method before fduObserverInit.
        /// </summary>
        protected void setDTSData_Direct()
        {
            dataTransmitStrategyName = FduGlobalConfig.editorDTSEnum2nameMap[EditorDTSEnum.Direct];
        }
        /// <summary>
        /// Set Every-N-Frame Data Transmit strategy class data.Call this method before fduObserverInit.
        /// </summary>
        /// <param name="frameCount">Data Transmit interval</param>
        protected void setDTSData_EveryNFrame(int frameCount)
        {
            dataTransmitStrategyName = FduGlobalConfig.editorDTSEnum2nameMap[EditorDTSEnum.EveryNFrame];
            dataTransmitStrategyParameter = frameCount.ToString();
        }
        /// <summary>
        /// Set OnClusterCommand Data Transmit strategy class data.Call this method before fduObserverInit.
        /// </summary>
        /// <param name="CommandName">Command Name</param>
        protected void setDTSData_OnClusterCommand(string CommandName)
        {
            dataTransmitStrategyName = FduGlobalConfig.editorDTSEnum2nameMap[EditorDTSEnum.OnClusterCommand];
            dataTransmitStrategyParameter = CommandName;
        }
        /// <summary>
        /// Set Custom Data Transmit strategy class data.Call this method before fduObserverInit.
        /// </summary>
        /// <param name="customDTSClassName">class name of the custom data transmit strategy class</param>
        /// <param name="parameter"></param>
        protected void setDTSData_CustomDTS(string customDTSClassName, string parameter)
        {
            dataTransmitStrategyName = customDTSClassName;
            dataTransmitStrategyParameter = parameter;
        }

        /// <summary>
        /// Find an Appropriate view instance which can control this observer.
        /// </summary>
        /// <returns></returns>
        public FduClusterView findViewInstance()
        {
            FduClusterView view = null;
            Transform ts = transform;
            while (ts != null)
            {
                view = ts.gameObject.GetClusterView();
                if (view == null)
                    ts = ts.parent;
                else
                    break;
            }
            return view;
        }
        /// <summary>
        /// Get Id of this observer in the view.
        /// </summary>
        /// <returns></returns>
        public int getIndexInClusterView()
        {
            return _viewInstance.getObserverId(this);
        }
        /// <summary>
        /// Get View instance.
        /// </summary>
        /// <returns></returns>
        public FduClusterView GetClusterView()
        {
            return _viewInstance;
        }
        //注册到cluster view中
        protected void registToClusterView()
        {
            if (_viewInstance == null)
                _viewInstance = findViewInstance();
            if (_viewInstance != null)
            {
                _viewInstance.registToView(this);
            }
            else
            {
                Debug.LogWarning("[FduObserver]Can not find any cluster view which can be registed to");
            }
        }
        /// <summary>
        /// Set data transmit strategy class manually. Not recommended. You can not change the data transmit strategy class instance once it is inited.
        /// </summary>
        /// <param name="strategy">The instance of dts class</param>
        public void setDataTransmitStrategy(FduDataTransmitStrategyBase strategy)
        {
            if (strategy == null)
                return;

            dataTransmitStrategy = strategy;
            dataTransmitStrategy.setObserver(this);

        }
        /// <summary>
        /// Get the instance of data transmit strategy class.
        /// </summary>
        /// <returns></returns>
        public FduDataTransmitStrategyBase getDataTransmitStrategy() { return dataTransmitStrategy; }

#if UNITY_EDITOR
        public void removeFromClusterView_editor()
        {
            if (_viewInstance != null)
                _viewInstance.removeObserver(this);

        }
#endif
        /// <summary>
        /// This method is always been called if the cluster view is active.
        /// </summary>
        public virtual void AlwaysUpdate()
        {
            if (dataTransmitStrategy != null)
                dataTransmitStrategy.Update();
        }
        public virtual void OnSendData()
        {

        }
        public virtual void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {

        }
        //生成系统定义好的DTS类实例
        FduDataTransmitStrategyBase getDTS_Instance(string dataTransmitStrategyClassName, string parameter)
        {
            if (dataTransmitStrategyClassName == null) return null;
            FduDataTransmitStrategyBase instance = FduDTS_Factory.create(dataTransmitStrategyClassName, parameter);
            return instance;
        }
        /// <summary>
        /// Generate instance of custom data transmit strategy class. Called in fduObserverinit.
        /// </summary>
        /// <param name="dataTransmitStrategyClassName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual FduDataTransmitStrategyBase getCustomDTS_Instance(string dataTransmitStrategyClassName, string parameter)
        {
            return null;
        }

        internal void setViewInstance(FduClusterView view)
        {
            _viewInstance = view;
        }

        //注意这个访问权限 只能由Clusterview调用
        internal void OnviewDestroy()
        {
            if (dataTransmitStrategy != null)
            {
                dataTransmitStrategy.Destroy();
            }
        }

        void Reset()
        {

        }


    }
    //本工具集所采用的observer基类 比普通的FduObserverBase具有更多特性
    public abstract class FduMultiAttributeObserverBase : FduObserverBase
    {
        //保存好的位数组  最终以int形式存在
        [SerializeField]
        protected int observedState_backup = 0;

        //位数组 标识这个observer是否监控某个属性
        protected BitVector32 observedState;

        //从节点保存的已从网络上获取的数据 可以用作插值或其他作用 只有每N帧传送的observer才实例化这个
        protected Dictionary<int, List<object>> propertyCachedMaps;

        protected enum FduMultiAttributeObserverOP { Update, SendData, Receive_Direct, Receive_Interpolation };

        #region cachedProperty Functions
        //根据index获取第一个缓存的数据
        internal object getCachedProperty(int index)
        {
            if (propertyCachedMaps != null && propertyCachedMaps.ContainsKey(index))
            {
                return propertyCachedMaps[index][0];
            }
            return null;
        }
        //根据index获取第一个缓存的数据
        internal object getCachedProperty_firstElement(int index)
        {
            return getCachedProperty(index);
        }
        //根据index获取最后一个缓存的数据
        internal object getCachedProperty_lastElement(int index)
        {
            if (propertyCachedMaps != null && propertyCachedMaps.ContainsKey(index))
            {
                var element = propertyCachedMaps[index];
                return element[element.Count - 1];
            }
            return null;
        }
        //根据index获取已缓存数据的个数
        internal int getCachedProperytyCount(int index)
        {
            if (propertyCachedMaps != null && propertyCachedMaps.ContainsKey(index))
            {
                return propertyCachedMaps[index].Count;
            }
            return -1;
        }
        //根据index获取对应缓存序列中位于arrayIndex的数据
        internal object getCachedProperty_atArrayIndex(int index,int arrayIndex)
        {
            if (propertyCachedMaps != null && propertyCachedMaps.ContainsKey(index))
            {
                var element = propertyCachedMaps[index];
                if (arrayIndex >= 0 && arrayIndex<element.Count)
                    return element[arrayIndex];
            }
            return null;
        }
        //根据index设置缓存数据
        internal bool setCachedProperty(int index, object data)
        {
            try
            {
                if (propertyCachedMaps != null)
                {
                    if (propertyCachedMaps.ContainsKey(index))
                    {
                        propertyCachedMaps[index][0] = data;
                    }
                    else
                    {
                        var list = new List<object>();
                        propertyCachedMaps.Add(index, list);
                        list.Add(data);
                    }
                }
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        //在末尾增加一个获取的数据
        internal bool setCachedProperty_append(int index, object data)
        {
            try
            {
                if (propertyCachedMaps != null)
                {
                    if (propertyCachedMaps.ContainsKey(index))
                    {
                        var element = propertyCachedMaps[index];
                        element.Add(data);
                        if (element.Count > getCachedMaxCount())
                        {
                            element.RemoveAt(0);
                        }
                    }
                    else
                    {
                        var list = new List<object>();
                        propertyCachedMaps.Add(index, list);
                        list.Add(data);
                    }
                }
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        //获取缓存数据量的最大值
        int getCachedMaxCount()
        {
            if (dataTransmitStrategy != null && dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CachedMaxCount)!=null)
            {
                return (int)dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CachedMaxCount);
            }
            return 1;
        }
        
#endregion

        //读取监控状态
        protected void loadObservedState()
        {
            observedState = new BitVector32(observedState_backup);
        }
        //设置监控状态
        protected void setObservedState(int index, bool flag)
        {
            if (index >= 0 && index < FduGlobalConfig.BIT_MASK.Length)
                observedState[FduGlobalConfig.BIT_MASK[index]] = flag;
            else
                Debug.LogError("[FduUnityComponentObserverBase]Invalid state index :" + index);
        }
        //获取监控状态 index范围1--31 获取由editor里面设置好的变量 比如是否检测position
        public bool getObservedState(int index)
        {
            //由于函数调用频率高 所以不进行参数检测 由于此函数一般用于内部使用 所以工具开发者负责确保传入参数
            return observedState[FduGlobalConfig.BIT_MASK[index]];
        }
        public int getObservedIntData()
        {
            return observedState.Data;
        }
        //是否采用插值状态
        public bool getInterpolationState()
        {
            if (dataTransmitStrategy != null)
            {
                return dataTransmitStrategy.getInterpolationState();
            }
            return false;
        }
        //getObservedState的参数检测版本 以便之后暴露给应用开发者使用
        public bool getObservedStateSafe(int index)
        {
            if (index >= 0 && index < FduGlobalConfig.BIT_MASK.Length)
            {
                return observedState[FduGlobalConfig.BIT_MASK[index]];
            }
            else
            {
                Debug.LogError("[FduUnityComponentObserverBase]Invalid state index :" + index);
                return false;
            }
        }

        public virtual bool setObservedState(string name, bool value)
        {
            throw new System.NotImplementedException("SetObservedState method of this observer is not implemented yet.");
        }

        public virtual bool getObservedState(string name)
        {
            throw new System.NotImplementedException("GetObservedState method of this observer is not implemented yet.");
        }


    }


    public interface IFduObserverValueChange
    {
        bool isValueChanged();
    }

}

namespace FDUClusterAppToolKits
{
    public class FduAccessableQueue<T>
    {
        private T[] m_array;
        private int m_count = 0;
        private int m_headIndex = 0;
        public int Count { get {return m_count;}}
        public FduAccessableQueue(int count)
        {
            count = count > 0 ? count : 1;
            m_array = new T[count];
        }
        public void PushAndPop(T element)
        {
            if (m_count >= m_array.Length)
            {
                m_array[m_headIndex] = element;
                m_headIndex = (m_headIndex + 1) % m_array.Length;
            }
            else
            {
                m_array[m_count] = element;
                m_count++;
            }
        }
        public T getElementAt(int index)
        {
            if (index < 0 || index >= m_count)
                throw new System.IndexOutOfRangeException();
            else
            {
                index = (m_headIndex + index) % m_count;
                return m_array[index];
            }
        }
        

    }

}

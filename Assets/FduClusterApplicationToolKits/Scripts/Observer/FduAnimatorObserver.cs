/*
 * FduAnimatorParameter
 * 简介：animator的监控器
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [System.Serializable]
    public class FduAnimatorParameter
    {
        //animator的参数类型
        public AnimatorControllerParameterType type;
        //参数名
        public string name;
        public FduAnimatorParameter(AnimatorControllerParameterType _type, string _name)
        {
            type = _type;
            name = _name;
        }
    }
    [RequireComponent(typeof(Animator))]
    public class FduAnimatorObserver : FduMultiAttributeObserverBase
    {

        Animator animator;

        //所有同步的参数列表
        [SerializeField]
        [HideInInspector]
        List<FduAnimatorParameter> m_parameterList = new List<FduAnimatorParameter>();

        //cache的trigger
        HashSet<string> triggerCacheList = new HashSet<string>();
        

        void Awake()
        {
#if CLUSTER_ENABLE
            fduObserverInit();
            loadObservedState();
            animator = GetComponent<Animator>();
            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                propertyCachedMaps = new Dictionary<int, List<object>>();
            }
#endif
        }
        void Update()
        {
            
            for (int i = 1; i < m_parameterList.Count; ++i)
            {
                FduAnimatorParameter para = m_parameterList[i];
                if (para.type == AnimatorControllerParameterType.Trigger && animator.GetBool(para.name))
                {
                    triggerCacheList.Add(para.name);
                }
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
            NetworkState.NETWORK_STATE_TYPE stateTemp = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            switchCaseFunc(FduMultiAttributeObserverOP.SendData, ref stateTemp);
            triggerCacheList.Clear();
        }
        public override void OnReceiveData(ref FDUObjectSync.NetworkState.NETWORK_STATE_TYPE state)
        {
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
            for (int i = 0; i < m_parameterList.Count; ++i)
            {
                FduAnimatorParameter para = m_parameterList[i];
                if (para.type == AnimatorControllerParameterType.Bool )//参数为布尔类型
                {
                    if (op == FduMultiAttributeObserverOP.SendData)
                    {
                        BufferedNetworkUtilsServer.SendBool(animator.GetBool(para.name));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Direct || op == FduMultiAttributeObserverOP.Receive_Interpolation)
                    {
                        animator.SetBool(para.name, BufferedNetworkUtilsClient.ReadBool(ref state));
                    }
                }
                else if (para.type == AnimatorControllerParameterType.Trigger)//参数为trigger类型
                {
                    if (op == FduMultiAttributeObserverOP.SendData)
                    {
                        BufferedNetworkUtilsServer.SendBool(triggerCacheList.Contains(para.name));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Direct )
                    {
                        animator.SetBool(para.name, BufferedNetworkUtilsClient.ReadBool(ref state));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                    {
                        bool triggerValue = BufferedNetworkUtilsClient.ReadBool(ref state);
                        if (triggerValue)
                            animator.SetTrigger(para.name);
                        else
                            animator.ResetTrigger(para.name);
                    }
                }
                else if (para.type == AnimatorControllerParameterType.Int)//参数为int类型
                {
                    if (op == FduMultiAttributeObserverOP.SendData)
                    {
                        BufferedNetworkUtilsServer.SendInt(animator.GetInteger(para.name));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                    {
                        animator.SetInteger(para.name, BufferedNetworkUtilsClient.ReadInt(ref state));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                    {
                        setCachedProperty_append(i,BufferedNetworkUtilsClient.ReadInt(ref state));
                    }
                    else if (op == FduMultiAttributeObserverOP.Update)
                    {
                        if (getCachedProperytyCount(i)>0)
                            animator.SetInteger(para.name, FduInterpolationInterface.getNextIntValue_new(animator.GetInteger(para.name), i, this));
                    }
                }
                else if (para.type == AnimatorControllerParameterType.Float)//参数为float类型
                {
                    if (op == FduMultiAttributeObserverOP.SendData)
                    {
                        BufferedNetworkUtilsServer.SendFloat(animator.GetFloat(para.name));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                    {
                        animator.SetFloat(para.name, BufferedNetworkUtilsClient.ReadFloat(ref state));
                    }
                    else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                    {
                        setCachedProperty_append(i, BufferedNetworkUtilsClient.ReadFloat(ref state));
                    }
                    else if (op == FduMultiAttributeObserverOP.Update)
                    {
                        if (getCachedProperytyCount(i)>0)
                            animator.SetFloat(para.name, FduInterpolationInterface.getNextFloatValue_new(animator.GetFloat(para.name), i, this));
                    }

                }
            }
        }

        public List<FduAnimatorParameter> getParameterList()
        {
            return m_parameterList;
        }

    }
}

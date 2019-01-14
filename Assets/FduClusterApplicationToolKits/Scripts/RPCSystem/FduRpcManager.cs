/*
 * FduRpcManager
 * 简介：RPC系统的管理类
 * 负责RPC事件的生成 管理和接发 这里的rpc是没有回应的 主节点发过去就不管了
 * 写法参考了许多Photon中的RPC写法
 * 
 * 原理：开发者调用Rpc之后 自动根据传入的参数生成RPCEvent 并存储到列表中
 * 在LateUpdate中遍历列表 将所有事件发送出去 序列化和反序列化过程在RpcEvent中执行
 * 从节点在接收到RpcEvent之后，会调用rpcManager的executeRPC方法，传入必要的参数，执行对应的rpc函数.
 * RPC目标设置为全体 那么主节点会立刻调用该RPC函数
 * 
 * 0829修改记录：原来rpc必须附属在observerBase上 现在fdurpc可以附属在任何与该view相同物体上的monobehavior 以及该view所拥有的所有observer
 * 最后修改时间：2017.08.29
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public enum RpcTarget { All, SlaveOnly }
    public class FduRpcManager : SyncBase
    {

        public static FduRpcManager Instance;
        //rpc事件列表
        static List<FduRPCEvent> _rpcEventList = new List<FduRPCEvent>();
        static bool inited = false;
        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getRpcManagerSyncId();
            Instance = this;
        }
        protected override void Init()
        {
            inited = true;
        }
        protected override void OnClientInited(ObjectSyncSlave client_)
        {
            base.OnClientInited(client_);
            if (_client != null)
            {
                //client side init
                _client.RegisterEvent(new FduRPCEvent());
            }
        }
        void LateUpdate()
        {
            if (_server != null)
            {
                for (int i = 0; i < _rpcEventList.Count; ++i)
                {
                    _server.SendEvent(_rpcEventList[i]);
                }
                _rpcEventList.Clear();
            }
        }
        /// <summary>
        /// Rpc Function. You can call any function marked with FduRPC attribute,which is DEFINED in the classes derived from FduObserverBase or other monobehaviors belong to  the gameobject.
        /// Notice that you can only use it on MasterNode.
        /// </summary>
        /// <param name="view">the view instance which will execute the rpc</param>
        /// <param name="methodName">name of the rpc mehthod</param>
        /// <param name="target">rpc target</param>
        /// <param name="paras">rpc parameters</param>
        public object RPC(FduClusterView view, string methodName, RpcTarget target, params object[] paras)
        {

            if (!inited) { Debug.LogWarning("[FduRpc]Rpc Manager is not set up yet. Ignore this operation"); return null; }
            if (view == null || methodName == null) { Debug.LogError("[FduRpc]Cluster view or method name can not be null!"); return null; };

            if (ClusterHelper.Instance.Client != null)
                return null;

            FduRPCEvent e = new FduRPCEvent();
            e.setUpViewId(view.ViewId, methodName);
            e.setUpParameters(paras);
            _rpcEventList.Add(e);

            if (target == RpcTarget.All)
                return executeRpc(e.getRpcData());
            else
                return null;
        }



        //根据rpc数据执行对应的rpc方法
        public object executeRpc(Dictionary<byte, object> rpcdata)
        {
            object returnValue = null;
            int viewId = (int)rpcdata[(byte)0];
            string methodName = (string)rpcdata[(byte)1];
            int paraCount = (int)rpcdata[(byte)2];
            object[] parameters = (object[])rpcdata[(byte)3];

            FduClusterView view = FduClusterViewManager.getClusterView(viewId);

            if (view == null)
            {
                Debug.LogWarning("[FduRpc]FduSyncView not exist .View id is " + viewId + " method Name is " + methodName);
                return returnValue;
            }

            bool isSent = false;
            List<FduObserverBase>.Enumerator observerEnum = view.getObservers();

            var monos = view.GetComponents<MonoBehaviour>();
            HashSet<MonoBehaviour> monoset = new HashSet<MonoBehaviour>(monos);
            while (observerEnum.MoveNext())
            {
                monoset.Add(observerEnum.Current);
            }
            foreach(MonoBehaviour mono in monoset)
            {
                List<MethodInfo> methodInfo = GetMethods(mono.GetType(), typeof(FduRPC));
                if (methodInfo == null)
                    return returnValue;
                Type[] argTypes = new Type[paraCount];
                for (int i = 0; i < paraCount; i++)
                {
                    argTypes[i] = parameters[i].GetType();
                }

                for (int i = 0; i < methodInfo.Count; ++i)
                {
                    MethodInfo mi = methodInfo[i];
                    if (mi.Name.Equals(methodName))
                    {
                        ParameterInfo[] paraInfo = mi.GetParameters();
                        if (paraInfo.Length == parameters.Length)
                        {
                            if (this.CheckTypeMatch(paraInfo, argTypes))
                            {
                                if (!isSent)
                                {
                                    returnValue = mi.Invoke((object)mono, parameters);
                                    isSent = true;
                                }
                                else
                                    Debug.LogWarning("[FduRpc]More than one function can match the method name and parameters. Please check.");
                            }
                        }
                    }
                }
            }
            if (!isSent)
            {
                Debug.LogWarning("[FduRpc]Parameters not matched for FduRpc Call. Method Name:" + methodName);
            }
            return returnValue;
        }
        //根据类型获取方法
        public static List<MethodInfo> GetMethods(Type type, Type attribute)
        {
            if (type == null)
            {
                return new List<MethodInfo>();
            }

            MethodInfo[] declaredMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (attribute == null)
            {
                return new List<MethodInfo>(declaredMethods);
            }
            List<MethodInfo> fittingMethods = new List<MethodInfo>();
            foreach (MethodInfo mi in declaredMethods)
            {
                if (mi.IsDefined(attribute, false))
                {
                    fittingMethods.Add(mi);
                }
            }
            return fittingMethods;
        }
        //检查类型是否匹配
        private bool CheckTypeMatch(ParameterInfo[] methodParameters, Type[] callParameterTypes)
        {
            if (methodParameters.Length < callParameterTypes.Length)
            {
                return false;
            }
            for (int index = 0; index < callParameterTypes.Length; index++)
            {
                Type type = methodParameters[index].ParameterType;
                if (callParameterTypes[index] != null && !type.IsAssignableFrom(callParameterTypes[index]) && !(type.IsEnum && System.Enum.GetUnderlyingType(type).IsAssignableFrom(callParameterTypes[index])))
                {
                    return false;
                }
            }
            return true;
        }
        public static void OnLevelLoaded()
        {
            if (_rpcEventList.Count > 0)
            {
                Debug.LogWarning("[FduRpc]Any Rpc Request raised at the frame of scene loading will be removed!");
            }
            _rpcEventList.Clear();
        }
    }
}

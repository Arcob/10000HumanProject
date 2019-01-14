/*
 * FduRPCEvent
 * 简介：RPC事件 继承自GameEvent
 * RPC事件中存储了必要的rpc调用的数据 存储在一张映射表中
 * RPC事件的实例会由对应的Manager进行管理和分发
 * 
 * 最后修改时间：2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduRPCEvent : GameEvent
    {
        //0--> view Id  1-->方法名称 2-->rpc方法的参数个数 3-->所有参数的参数值
        Dictionary<byte, object> _rpcData;
        bool inited = false;

        public FduRPCEvent()
        {
            eventID = 1;
            objectName = "rpc";
            _rpcData = new Dictionary<byte, object>();
        }
        //设置该RPC执行的view的ID
        public void setUpViewId(int viewId, string methodName)
        {
            if (viewId < 0 || viewId == FduSyncBaseIDManager.getInvalidSyncId())
                Debug.LogError("[FDURPC]Invalid view id");
            if (methodName == "" || methodName == null)
                Debug.LogError("[RPCRPC]Method Name can not be null");

            _rpcData.Add((byte)0, viewId);
            _rpcData.Add((byte)1, methodName);

            inited = true;
        }
        //设置该RPC的参数
        public void setUpParameters(object[] paras)
        {
            if (paras == null)
            {
                paras = new object[0];
                _rpcData.Add((byte)2, 0);
                _rpcData.Add((byte)3, (object[])paras);
            }
            else
            {
                _rpcData.Add((byte)2, paras.Length);
                _rpcData.Add((byte)3, (object[])paras);
            }
        }
        //获取rpc数据
        public Dictionary<byte, object> getRpcData()
        {
            return _rpcData;
        }
        public override void Serialize()
        {
            if (!inited)
            {
                Debug.LogError("[FDURPC]RPC EVENT not set up yet");
                return;
            }
            serializeParameters();
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            deserializeParameters(ref state);
            FduRpcManager.Instance.executeRpc(_rpcData);
            _rpcData.Clear();
            return state;
        }
        //测试用的信息展示函数
        void showData()
        {
            Debug.Log("View ID " + _rpcData[(byte)0]);
            Debug.Log("Method Name " + _rpcData[(byte)1]);
            int len = (int)_rpcData[(byte)2];
            Debug.Log("Parameter count " + len);
            object[] ob = (object[])_rpcData[(byte)3];
            for (int i = 0; i < len; ++i)
            {
                Debug.Log(ob[i].GetType().FullName);
                Debug.Log(ob[i]);
            }
        }
        //将rpc事件中的各项参数都序列化发出去
        void serializeParameters()
        {
            BufferedNetworkUtilsServer.SendInt((int)_rpcData[(byte)0]);
            BufferedNetworkUtilsServer.SendString((string)_rpcData[(byte)1]);
            int paraCount = ((int)_rpcData[(byte)2]);
            BufferedNetworkUtilsServer.SendInt(paraCount);
            object[] parameters = (object[])_rpcData[(byte)3];
            try
            {
                for (int i = 0; i < paraCount; ++i)
                {
                    FduSupportClass.serializeOneParameter(parameters[i]);
                }
            }
            catch (InvalidSendableDataException e)
            {
                Debug.LogError(e.Message);
            }
        }
        //接收到rpc事件中的数据 反序列化为可用的事件实例
        void deserializeParameters(ref  NetworkState.NETWORK_STATE_TYPE state)
        {
            _rpcData.Clear();
            _rpcData.Add((byte)0, BufferedNetworkUtilsClient.ReadInt(ref state));
            _rpcData.Add((byte)1, BufferedNetworkUtilsClient.ReadString(ref state));
            int paraCount = BufferedNetworkUtilsClient.ReadInt(ref state);
            _rpcData.Add((byte)2, paraCount);
            object[] parameters = new object[paraCount];
            try
            {
                for (int i = 0; i < paraCount; ++i)
                {
                    object ob = FduSupportClass.deserializeOneParameter(ref state);
                    parameters[i] = ob;
                }
            }
            catch (InvalidSendableDataException e)
            {
                Debug.LogError(e.Message);
                return;
            }
            _rpcData.Add((byte)3, (object[])parameters);
        }

    }
}

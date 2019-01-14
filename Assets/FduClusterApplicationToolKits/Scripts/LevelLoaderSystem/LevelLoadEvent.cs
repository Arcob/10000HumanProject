/*
 * LevelLoadEvent
 * 简介：场景读取用的事件 继承自GameEvent 和ClusterEvent不同机制
 * 事件在反序列化后执行对应的回调函数--从节点读取场景函数
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
    public class LevelLoadEvent : GameEvent
    {
        public string levelName;

        public LevelLoadEvent()
        {
            eventID = 2;
            objectName = "levelLoad";
        }

        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendString(levelName);
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            levelName = BufferedNetworkUtilsClient.ReadString(ref state);
            FduClusterLevelLoader.Instance._slaveStartLoadScene(levelName);
            return state;
        }
    }
}
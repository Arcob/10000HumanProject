/*
 * FduClusterRandomSync 集群随机数同步类
 * 
 * 每一帧都把主节点的随机数状态同步到从节点上
 * 保证这一帧随机数随机的结果相同
 * 
 * 注意：在第一帧主从节点用一个死值初始化随机数状态（因为来不及同步）
 * 初始化时会保存Random.state 这个状态每次启动都会有所变化 所以每次程序运行随机数序列都不一样
 * 现在有一个弊端 就是第一帧的随机数序列不会发生变化，而在第二帧恢复正常。
 *
 * Hayate 17.09.17
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public class FduClusterRandomSync : SyncBase, IFduLateUpdateFunc
    {

        Random.State initState;

        Random.State receiveState;

        int initSeed = 441688437;

        bool isFirstTimeAssign = false;

        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getClusterRandomManagerSyncId();

            initState = Random.state;

            Random.state = initState;

            Random.InitState(initSeed);
        }

        public void LateUpdateFunc()
        {
            if (_server != null)
            {
                if (!isFirstTimeAssign)
                {
                    Random.state = initState;
                    isFirstTimeAssign = true;
                }
                _server.SendState(ObjectID, this);
            }
            else
            {
                Random.state = receiveState;
            }
        }

        public override void Serialize()
        {
            BufferedNetworkUtilsServer.SendStruct(Random.state);
        }
        public override NetworkState.NETWORK_STATE_TYPE Deserialize()
        {
            NetworkState.NETWORK_STATE_TYPE state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
            receiveState = (Random.State)BufferedNetworkUtilsClient.ReadStruct(typeof(Random.State), ref state);
            return state;
        }
    }
}

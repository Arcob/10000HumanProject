/*
 * FduParticleSystemObserver
 * 
 * 简介：例子系统的监控器
 * 
 * 原理：通过设置主从节点相同的randomSeed和播放时间保证主从节点模拟的粒子系统效果相同
 * 主节点的操作通过格式化为FduParticleSystemOP传递给从节点
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
    [RequireComponent(typeof(ParticleSystem))]
    public class FduParticleSystemObserver : FduMultiAttributeObserverBase
    {

        ParticleSystem particleSys;

        public static readonly uint unifyRandomSeed = 441688437; //如果开发者不特地指定randomSeed 则主从节点都使用这个randomseed保证模拟一致

        //待传输的操作对象列表
        static List<FduParticleSystemOP> _operationList = new List<FduParticleSystemOP>();

        void Awake()
        {
#if CLUSTER_ENABLE
            bool awakePlayFlag = false;
            fduObserverInit();
            particleSys = GetComponent<ParticleSystem>();

            if (particleSys.isPlaying)
                awakePlayFlag = true;

            //一开始就停止粒子系统
            particleSys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            //禁用useAutoRandomSeed
            particleSys.useAutoRandomSeed = false;
            //设置randomSeed
            particleSys.randomSeed = unifyRandomSeed;

            if (awakePlayFlag)
                particleSys.Play();
#endif

        }

        public override void OnSendData()
        {
            BufferedNetworkUtilsServer.SendInt(_operationList.Count);
            foreach (FduParticleSystemOP op in _operationList)
            {
                BufferedNetworkUtilsServer.SendByte((byte)op.operation);
                if (op.paras != null)
                {
                    BufferedNetworkUtilsServer.SendInt(op.paras.Length);
                    for (int i = 0; i < op.paras.Length; ++i)
                    {
                        FduSupportClass.serializeOneParameter(op.paras[i]);
                    }
                }
                else
                    BufferedNetworkUtilsServer.SendInt(0);
            }
            _operationList.Clear();
        }

        public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            _operationList.Clear();
            int opCount = BufferedNetworkUtilsClient.ReadInt(ref state);
            for (int i = 0; i < opCount; ++i)
            {
                FduParticleSystemOP.Operation operationType = (FduParticleSystemOP.Operation)BufferedNetworkUtilsClient.ReadByte(ref state);
                int paraCount = BufferedNetworkUtilsClient.ReadInt(ref state);
                FduParticleSystemOP op = new FduParticleSystemOP();
                op.operation = operationType;
                if (paraCount > 0)
                    op.paras = new object[paraCount];
                for (int j = 0; j < paraCount; ++j)
                {
                    op.paras[j] = FduSupportClass.deserializeOneParameter(ref state);
                }
                _operationList.Add(op);
            }
            //反序列化结束 执行每一项粒子系统的操作
            foreach (FduParticleSystemOP op in _operationList)
            {
                op.executeOpOnSlave(particleSys);
            }
            _operationList.Clear();
        }
        public void addOperation(FduParticleSystemOP op)
        {
            _operationList.Add(op);
        }

    }
}

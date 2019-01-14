//旧的程序文件 等待剔除
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
public class ParticleObserverOld : FduMultiAttributeObserverBase {

    ParticleSystem particleSys;

    //public static readonly string[] attributeList = new string[] {  };

    public static readonly uint unifyRandomSeed = 441688437; //如果开发者不特地指定randomSeed 则主从节点都使用这个randomseed保证模拟一致

    //static List<FduParticleSystemOP> _operationList = new List<FduParticleSystemOP>();

    [SerializeField]
    bool isUsingRandomSeed = false;


    void Awake()
    {
        bool awakePlayFlag = false;
        fduObserverInit();
        particleSys = GetComponent<ParticleSystem>();

        if (ClusterHelper.Instance.Server != null && particleSys.isPlaying)
            awakePlayFlag = true;

        particleSys.Stop();

        particleSys.useAutoRandomSeed = false;
        if (!isUsingRandomSeed)
            particleSys.randomSeed = unifyRandomSeed;

        if (awakePlayFlag)
            particleSys.Play();

        Debug.Log(particleSys.randomSeed);

    }
    //将播放状态压缩成1个byte
    byte playState2Byte(bool isPlaying, bool isPausing, bool isStop, bool isEmit)
    {
        byte result = 0x00;
        if (isPlaying)
            result |= 0x01;
        if (isPausing)
            result |= 0x02;
        if (isStop)
            result |= 0x04;
        if (isEmit)
            result |= 0x08;
        return result;
    }
    //将压缩的byte解析成对应的播放状态
    void byte2PlayState(byte state, ref bool isPlaying, ref bool isPausing, ref bool isStop, ref bool isEmit)
    {
        if ((state & 0x01) == 0x00)
            isPlaying = false;
        else
            isPlaying = true;

        if ((state & 0x02) == 0x00)
            isPausing = false;
        else
            isPausing = true;

        if ((state & 0x04) == 0x00)
            isStop = false;
        else
            isStop = true;

        if ((state & 0x08) == 0x00)
            isEmit = false;
        else
            isEmit = true;
    }
    //根据主节点的状态 对从节点粒子系统的播放状态进行调整
    void processPlayState(byte state)
    {
        bool isPlaying = false;
        bool isPausing = false;
        bool isStop = false;
        bool isEmit = false;

        byte2PlayState(state, ref isPlaying, ref isPausing, ref isStop, ref isEmit);

        if (particleSys.isEmitting != isEmit)
        {
            if (!isEmit && !isPausing)
            {
                if (!isStop) //这种情况是 主节点调用了不清除粒子的stop函数 所以isStop状态尚未转变为true
                    particleSys.Stop();
                else//这种情况是 主节点调用了清除粒子的stop函数，isStop状态会立刻变为true
                {
                    particleSys.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (isEmit && isPlaying) //这种情况是，当主节点调用了stop函数，但是在isStop状态变为true之前 又一次调用play的情况
            {
                particleSys.Play();
            }

        }

        if (particleSys.isPlaying != isPlaying && isPlaying)
        {
            particleSys.Play();

        }
        if (particleSys.isPaused != isPausing)
        {
            if (isPausing)
                particleSys.Pause();
            else if (!isPausing && isStop) //这种情况是 在例子系统暂停的情况下 主节点调用stop（ParticleSystemStopBehavior.StopEmittingAndClear）方法使系统停止并清除粒子 但是只用stop而不清除粒子 则不会引起任何反应
                particleSys.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }



    public override void OnSendData()
    {

        BufferedNetworkUtilsServer.SendByte(playState2Byte(particleSys.isPlaying, particleSys.isPaused, particleSys.isStopped, particleSys.isEmitting));
        BufferedNetworkUtilsServer.SendFloat(particleSys.time);
        if (isUsingRandomSeed)
            BufferedNetworkUtilsServer.SendInt((int)particleSys.randomSeed);
    }

    public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
    {

        processPlayState(BufferedNetworkUtilsClient.ReadByte(ref state));
        particleSys.time = BufferedNetworkUtilsClient.ReadFloat(ref state);
        if (isUsingRandomSeed)
        {
            uint seed = (uint)BufferedNetworkUtilsClient.ReadInt(ref state);
            if (particleSys.randomSeed != seed)
            {
                if (!particleSys.isPlaying)
                {
                    particleSys.randomSeed = seed;
                }
            }
        }
    }
    //public void addOperation(FduParticleSystemOP op)
    //{
       // _operationList.Add(op);
    //}
}

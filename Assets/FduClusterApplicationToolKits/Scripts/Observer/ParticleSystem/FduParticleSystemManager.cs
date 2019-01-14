/*
 * FduParticleSystemManager
 * 简介：静态的扩展类 为原来的particleSystem拓展了对应的Play、stop等函数
 * 主节点调用这些函数后 manager生成对应的FduParticleSystemOP操作实例添加到
 * FduParticleSystemObserver中传输 从节点接收到这些命令后执行particleSystem对应的播放函数
 * 使得主从节点的粒子系统表现一致
 * 
 * 最后修改时间：Hayate  2017.07.08
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduParticleSystemOP
    {
        //操作类型
        public enum Operation
        {
            play, pause, stop, clear, emit, simulate, setRandomSeed
        }
        public Operation operation;
        public object[] paras;

        //从节点的执行操作的函数
        public void executeOpOnSlave(ParticleSystem ps)
        {
            if (!FduSupportClass.isSlave)
                return;
            switch (operation)
            {
                case Operation.play:
                    if (paras == null)
                        ps.Play();
                    else
                        ps.Play((bool)paras[0]);
                    break;
                case Operation.pause:
                    if (paras == null)
                        ps.Pause();
                    else
                        ps.Pause((bool)paras[0]);
                    break;
                case Operation.stop:
                    if (paras == null)
                        ps.Stop();
                    else if (paras.Length == 1)
                        ps.Stop((bool)paras[0]);
                    else
                    {
                        byte middle = (byte)paras[1];
                        ps.Stop((bool)paras[0], (ParticleSystemStopBehavior)middle);
                    }
                    break;
                case Operation.clear:
                    if (paras == null)
                        ps.Clear();
                    else
                        ps.Clear((bool)paras[0]);
                    break;
                case Operation.emit:
                    ps.Emit((int)paras[0]);
                    break;
                case Operation.simulate:
                    if (paras.Length == 1)
                        ps.Simulate((float)paras[0]);
                    else if (paras.Length == 2)
                        ps.Simulate((float)paras[0], (bool)paras[1]);
                    else if (paras.Length == 3)
                        ps.Simulate((float)paras[0], (bool)paras[1], (bool)paras[2]);
                    else if (paras.Length == 4)
                        ps.Simulate((float)paras[0], (bool)paras[1], (bool)paras[2], (bool)paras[3]);
                    break;
                case Operation.setRandomSeed:
                    int randomSeed = (int)paras[0];
                    ps.randomSeed = (uint)randomSeed;
                    break;
            }
        }
    }
}

namespace FDUClusterAppToolKits
{
    public static class FduParticleSystemManager
    {
        //合法性检测
        static FduParticleSystemObserver validateCheck(ParticleSystem ps)
        {
            if (!FduSupportClass.isMaster)
                return null;
            return ps.GetComponent<FduParticleSystemObserver>();
        }

        public static void ClusterPlay(this UnityEngine.ParticleSystem ps)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Play();
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.play;
            observer.addOperation(op);
        }
        public static void ClusterPlay(this UnityEngine.ParticleSystem ps, bool withChildren)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Play(withChildren);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.play;
            op.paras = new object[1];
            op.paras[0] = withChildren;
            observer.addOperation(op);
        }
        public static void ClusterPause(this UnityEngine.ParticleSystem ps)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Pause();
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.pause;
            observer.addOperation(op);
        }
        public static void ClusterPause(this UnityEngine.ParticleSystem ps, bool withChildren)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Pause(withChildren);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.pause;
            op.paras = new object[1];
            op.paras[0] = withChildren;
            observer.addOperation(op);
        }
        public static void ClusterStop(this UnityEngine.ParticleSystem ps)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Stop();
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.stop;
            observer.addOperation(op);
        }
        public static void ClusterStop(this UnityEngine.ParticleSystem ps, bool withChildren)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Stop(withChildren);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.stop;
            op.paras = new object[1];
            op.paras[0] = withChildren;
            observer.addOperation(op);
        }
        public static void ClusterStop(this UnityEngine.ParticleSystem ps, bool withChildren, ParticleSystemStopBehavior stopBehavior)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Stop(withChildren, stopBehavior);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.stop;
            op.paras = new object[2];
            op.paras[0] = withChildren;
            op.paras[1] = (byte)stopBehavior;
            observer.addOperation(op);
        }
        public static void ClusterClear(this UnityEngine.ParticleSystem ps)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Clear();
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.clear;
            observer.addOperation(op);
        }
        public static void ClusterClear(this UnityEngine.ParticleSystem ps, bool withChildren)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Clear(withChildren);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.clear;
            op.paras = new object[1];
            op.paras[0] = withChildren;
            observer.addOperation(op);
        }
        public static void ClusterEmit(this UnityEngine.ParticleSystem ps, int count)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Emit(count);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.emit;
            op.paras = new object[1];
            op.paras[0] = count;
            observer.addOperation(op);
        }
        public static void ClusterSimulate(this UnityEngine.ParticleSystem ps, float t)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Simulate(t);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.simulate;
            op.paras = new object[1];
            op.paras[0] = t;
            observer.addOperation(op);

        }
        public static void ClusterSimulate(this UnityEngine.ParticleSystem ps, float t, bool withChildren)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Simulate(t, withChildren);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.simulate;
            op.paras = new object[2];
            op.paras[0] = t;
            op.paras[1] = withChildren;
            observer.addOperation(op);
        }
        public static void ClusterSimulate(this UnityEngine.ParticleSystem ps, float t, bool withChildren, bool restart)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Simulate(t, withChildren, restart);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.simulate;
            op.paras = new object[3];
            op.paras[0] = t;
            op.paras[1] = withChildren;
            op.paras[2] = restart;
            observer.addOperation(op);
        }
        public static void ClusterSimulate(this UnityEngine.ParticleSystem ps, float t, bool withChildren, bool restart, bool fixedTimeStep)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.Simulate(t, withChildren, restart, fixedTimeStep);
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.simulate;
            op.paras = new object[4];
            op.paras[0] = t;
            op.paras[1] = withChildren;
            op.paras[2] = restart;
            op.paras[3] = fixedTimeStep;
            observer.addOperation(op);
        }
        public static void ClusterSetRandomSeed(this UnityEngine.ParticleSystem ps, uint randomSeed)
        {
            var observer = validateCheck(ps);
            if (observer == null)
                return;
            ps.randomSeed = randomSeed;
            FduParticleSystemOP op = new FduParticleSystemOP();
            op.operation = FduParticleSystemOP.Operation.setRandomSeed;
            op.paras = new object[1];
            op.paras[0] = (int)randomSeed;
            observer.addOperation(op);
        }
    }
}

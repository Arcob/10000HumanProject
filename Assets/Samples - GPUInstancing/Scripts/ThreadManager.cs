using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class ThreadManager : MonoBehaviour {
    public int threadNum = 8;
    public Thread[] threads;
    public List<Action>[] actions;

    private static bool isInit;
    public static ThreadManager Instance;

    // 线程执行结束后的休眠时间
    public static int deltaMilliSecond_Int = 15;
    static float deltaSecond_Float = 0.015f;

    private double threadCircleTime = 30.0;
    void Awake()
    {
#if CLUSTER_ENABLE
        if (FDUClusterAppToolKits.FduSupportClass.isSlave)
        {
            Destroy(this);
            return;
        }
#endif
        //int completionPorts
        //ThreadPool.GetMinThreads(out threadNum, out completionPorts);

        threadNum = SettingData.instance.data.threadCount;

        Instance = this;
        threads = new Thread[threadNum];
        actions = new List<Action>[threadNum];
        // 初始化线程任务
        for (int i = 0; i < threadNum; i++)
            actions[i] = new List<Action>();
    }

    private void Start()
    {
        // 开始执行线程
        StartThreads();
    }

    // 将任务分配给指定ID的线程
    public void SetThreadJobWithThreadID(Action a,int threadID)
    {
        if (threadID < threadNum)
            actions[threadID].Add(a);
    }

    // 根据ID将任务分配给不同的线程
    public void SetThreadJob(Action a,int id)
    {
        //其中一个线程处理GPUSkinning
        actions[(id % threadNum)].Add(a);
    }

    //线程启用后执行的函数
    public void ThreadJob(object parameter)
    {
        int id = (int)parameter;
        while (true)
        {
            print("thread " + id + ":" + actions[id].Count);
            //DateTime beforeDT = System.DateTime.Now;
            for (int i = 0; i < actions[id].Count; i++)
            {
                try
                {
                    actions[id][i]();
                }
                catch(Exception e)
                {
                    Debug.LogError("Thread " + id + ": Job " + i);
                }
            }
            //double t = (System.DateTime.Now.Subtract(beforeDT)).TotalMilliseconds;
            //Thread.Sleep(Mathf.Max(1, (int)(threadCircleTime - t)));
            //print(id);
            Thread.Sleep(5);
        }
    }

    //所有Controller初始化完成后
    public void StartThreads()
    {
        for (int i = 0; i < threadNum; i++)
        {
            ParameterizedThreadStart a = new ParameterizedThreadStart(ThreadJob);
            threads[i] = new Thread(a);
            threads[i].IsBackground = true;
            threads[i].Start(i);
            //print("Thread " + i + " Start");
        }
    }

    // 脚本销毁时禁用线程，否则无法停止
    private void OnDestroy()
    {
        for (int i = 0; i < threadNum; i++)
        {
            if (threads != null && threads.Length>i)
                threads[i].Abort();
        }
    }
}

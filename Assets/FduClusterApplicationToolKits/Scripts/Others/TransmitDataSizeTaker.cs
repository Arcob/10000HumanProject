/*
 * TransmitDataSizeTaker
 * 
 * 简介：获取每帧传送的数据量 可以在Debug工具中查看
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class TransmitDataSizeTaker : MonoBehaviour
    {

        void NotifyMessageSize(int curFrameSize)
        {
            TransmitDataSizeStatisticClass.instance.refreshData(curFrameSize);
        }
    }
    public class TransmitDataSizeStatisticClass
    {
        static TransmitDataSizeStatisticClass _instance;

        Queue<int> dataSizeQueue = new Queue<int>();
        Queue<int> frameNumber = new Queue<int>();

        public static readonly int MAX_FRAME_COUNT = 200;

        public static bool isRunning = false;

        public static TransmitDataSizeStatisticClass instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Application.isPlaying)
                        return null;

                    _instance = new TransmitDataSizeStatisticClass();
                    return _instance;
                }
                else
                    return _instance;
            }
        }
        public void refreshData(int curFrameDataSize)
        {
            if (isRunning && ClusterHelper.Instance != null)
            {
                if (dataSizeQueue.Count >= MAX_FRAME_COUNT)
                {
                    dataSizeQueue.Dequeue();
                    frameNumber.Dequeue();
                }
                dataSizeQueue.Enqueue(curFrameDataSize);
                frameNumber.Enqueue(ClusterHelper.Instance.FrameCount);
            }
        }
        public void ClearData()
        {
            dataSizeQueue.Clear();
            frameNumber.Clear();
        }
        public Queue<int>.Enumerator getTransmitDataSize()
        {
            return dataSizeQueue.GetEnumerator();
        }
        public Queue<int>.Enumerator getFrameNumbers()
        {
            return frameNumber.GetEnumerator();
        }
    }
}

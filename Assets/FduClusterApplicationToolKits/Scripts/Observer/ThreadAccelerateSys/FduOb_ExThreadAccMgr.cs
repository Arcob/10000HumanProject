using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using System.Threading;
namespace FDUClusterAppToolKits{

    public class FduOb_ExThreadAccMgr : MonoBehaviour {
        
        const int _THREAD_COUNT = 2;

        public enum BufferType
        {
            Buffer1,Buffer2
        }

        public class Vector3Pair{
            public Vector3 value1;
            public Vector3 value2;
            public Vector3Pair(Vector3 v1, Vector3 v2)
            {
                value1 = v1; value2 = v2;
            }
        }

        public class QuaternionPair{
            public Quaternion value1;
            public Quaternion value2;
            public QuaternionPair(Quaternion q1, Quaternion q2)
            {
                value1 = q1; value2 = q2;
            }
        }

        public class BufferData
        {
            public bool isDirty = false;
            public FduTransformObserver_Ex observer;
            public Vector3Pair posBuff;
            public Vector3Pair scaleBuff;
            public QuaternionPair rotBuff;
            public void clearData()
            {
                observer = null;
                posBuff = null;
                scaleBuff = null;
                rotBuff = null;
                isDirty = true;
            }
        }

        class ThreadDataSet
        {
            public Thread instance;
            public int threadId;
            public List<int> processIdList = new List<int>();
            public bool isComplete = false;
        }

        public static FduOb_ExThreadAccMgr instance;

        List<BufferData> dataMap;

        List<ThreadDataSet> threadData;

        List<FduTransformObserver_Ex> waitForAddList = new List<FduTransformObserver_Ex>();

        List<int> waitForDelList = new List<int>();

        object waitForDelLock = new object();

        bool buffer1Complete = false;
        bool buffer2Complete = false;

        BufferType curUsedBuffer = BufferType.Buffer1;


        static int minUnusedID = 0;

        ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        bool isBoost = false;

        int nextTaskThreadId = 0;

        int testProceeTime = 0;
        int updateTime = 0;

        int testShotCount = 0;
        int testMissCount = 0;

        void Awake()
        {
            instance = this;
        }
	
	    void Update () {

            if (FduSupportClass.isSlave)
            {
                updateTime++;
                if (updateTime % 60 == 0)
                    messageReport();
                //Debug.Log("Update time:" + updateTime + " Exe time: " + testProceeTime);
            }
	    }

        void messageReport()
        {
            string message = "Message Report: ";

            message += " Shot Count: " + testShotCount + " Miss Count: " + testMissCount;

            if (threadData != null)
            {
                foreach (ThreadDataSet data in threadData)
                {
                    message += "Thread Id:" + data.threadId + " Thread process Count: " + data.processIdList.Count + " ";
                }
            }
            //Debug.Log(message);
;
        }

        int getNextAvailableId()
        {
            if (dataMap != null)
            {
                if (minUnusedID >= dataMap.Count)
                {
                    return minUnusedID++;
                }
                else
                {
                    for (int i = minUnusedID; i < dataMap.Count; ++i)
                    {
                        if (dataMap[i].isDirty)
                        {
                            minUnusedID = i;
                            return minUnusedID++;
                        }
                    }
                    minUnusedID = dataMap.Count;
                    return minUnusedID++;
                }
            }
            return 0;
        }

        void LateUpdate()
        {
            if(FduSupportClass.isSlave)
                StartCoroutine(PostProcess());
        }

        public void RegistToManager(FduTransformObserver_Ex observer) {
            
            if (FduSupportClass.isMaster)
                return;

            if (observer != null)
            {

                if (dataMap == null)
                {
                    dataMap = new List<BufferData>();
                    ThreadBoost();
                    isBoost = true;
                }
                waitForAddList.Add(observer);
            }
        }

        void ProcessNewTasks()
        {
            if (waitForAddList.Count == 0)
                return;
            FduTransformObserver_Ex observer;

            for (int i = 0; i < waitForAddList.Count; ++i)
            {
                observer = waitForAddList[i];
                int obId = getNextAvailableId();
                observer.setAccThreadId(obId);
                if (observer != null)
                {
                    BufferData newBuffer = new BufferData();
                    newBuffer.isDirty = false;
                    if (obId >= dataMap.Count)
                    {
                        dataMap.Add(newBuffer);
                    }
                    else
                    {
                        dataMap[obId] = newBuffer;
                    }
                    if (observer.getObservedState(1)) //Pos
                    {
                        newBuffer.posBuff = new Vector3Pair(observer.transform.position, observer.transform.position);
                    }
                    if (observer.getObservedState(2)) //Rotation
                    {
                        newBuffer.rotBuff = new QuaternionPair(observer.transform.rotation, observer.transform.rotation);
                    }
                    if (observer.getObservedState(3)) //Scale
                    {
                        newBuffer.scaleBuff = new Vector3Pair(observer.transform.localScale, observer.transform.localScale);
                    }
                    if (observer.getObservedState(1) || observer.getObservedState(2) || observer.getObservedState(3))
                    {
                        newBuffer.observer = observer;
                    }
                }
                int processThreadId = nextTaskThreadId++;
                processThreadId %= _THREAD_COUNT;
                threadData[processThreadId].processIdList.Add(obId);
            }
            //添加队列的增加与删除都在主线程 不用加锁
            waitForAddList.Clear();
        }
        void ProcessDelTasks()
        {
            if (waitForDelList.Count == 0 || !isBoost)
                return;

            int obId;
            for (int i = 0; i < waitForDelList.Count; ++i)
            {
                obId = waitForDelList[i];
                dataMap[obId].clearData();
                if (obId < minUnusedID)
                    minUnusedID = obId;
            }
            System.GC.Collect();
            //移除队列的增加与删除在不同线程发生 所以要加锁
            lock(waitForDelLock)
            {
                waitForDelList.Clear();
            }
        }
        void ProcessJobComplete()
        {
            if (threadData == null)
                return;

            BufferType temp = curUsedBuffer;

            bool allComplete = true;
            foreach (ThreadDataSet data in threadData)
            {
                if (!data.isComplete)
                    allComplete = false;
            }
            if (allComplete)
            {
                if (!buffer1Complete && !buffer2Complete)
                {
                    buffer1Complete = true;
                    curUsedBuffer = BufferType.Buffer1;
                }
                else if (buffer1Complete && !buffer2Complete)
                {
                    buffer2Complete = true;
                    curUsedBuffer = BufferType.Buffer2;
                }
                else if (!buffer1Complete && buffer2Complete)
                {
                    buffer1Complete = true;
                    curUsedBuffer = BufferType.Buffer1;
                }
                foreach (ThreadDataSet data in threadData)
                {
                    data.isComplete = false;
                }
                testShotCount++;
            }
            else //没有全部处理完的话 就用计算线程没有占用的buffer
            {
                if (!buffer1Complete && !buffer2Complete)
                {
                    curUsedBuffer = BufferType.Buffer2;
                }
                else if (buffer1Complete && !buffer2Complete)
                {
                    curUsedBuffer = BufferType.Buffer1;
                }
                else if (!buffer1Complete && buffer2Complete)
                {
                    curUsedBuffer = BufferType.Buffer2;
                }
                testMissCount++;
            }
            //Debug.Log("all com: " + allComplete + " buffer1: " + buffer1Complete + " buffer2:" + buffer2Complete);

            if (temp != curUsedBuffer)
            {
                if (temp == BufferType.Buffer1)
                    buffer1Complete = false;
                else
                    buffer2Complete = false;
            }
        }

       [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void requestDeleteTransformObserver(int [] obIds)
        {
            lock (waitForDelLock)
            {
                if (obIds != null)
                {
                    foreach (int id in obIds)
                        waitForDelList.Add(id);
                }
            }
        }
        IEnumerator PostProcess()
        {
            yield return new WaitForEndOfFrame();

            bufferLock.EnterWriteLock();
            try
            {
                ProcessNewTasks();
                ProcessDelTasks();
                ProcessJobComplete();
            }
            finally
            {
                bufferLock.ExitWriteLock();
            }
        }

        void ThreadBoost()
        {
            threadData = new List<ThreadDataSet>();
            for (int i = 0; i < _THREAD_COUNT; ++i)
            {
                ThreadDataSet data = new ThreadDataSet();
                Thread t = new Thread(new ParameterizedThreadStart(ThreadFunc));
                data.instance = t;
                data.threadId = i;
                threadData.Add(data);
                t.Start(i);
            }
        }

        void ThreadFunc(object threadId)
        {
            int id = (int)threadId;
            ThreadDataSet data = threadData[id];
            while (true)
            {
                if (data.isComplete)
                {
                    Thread.Sleep(3);
                }
                else
                {
                    bufferLock.EnterReadLock();
                    try
                    {
                        BufferType calDes = BufferType.Buffer1;
                        List<int> _waitForDel = new List<int>();
                        if (!buffer1Complete && !buffer2Complete)
                        {
                            calDes = BufferType.Buffer1;
                        }
                        else if (buffer1Complete && !buffer2Complete)
                        {
                            calDes = BufferType.Buffer2;
                        }
                        else if (!buffer1Complete && buffer2Complete)
                        {
                            calDes = BufferType.Buffer1;
                        }
                        FduTransformObserver_Ex ob; int obId;
                        for (int i = 0; i < data.processIdList.Count; ++i)
                        {
                            obId =data.processIdList[i]; 
                            ob = dataMap[obId].observer;
                            if (ob != null)
                            {
                                //watch out  这里代码写的不好 如果数值修改 这边判断条件也需要改变
                                if (ob.getObservedState(1)) //pos
                                {
                                    setPosBufferData(calDes, obId, FduInterpolationInterface_EX.getNextPosValue_Ex(getPosBufferData(getAnotherBuffer(calDes),obId), ob));
                                }
                                if (ob.getObservedState(2)) //rot
                                {
                                    setRotationBufferData(calDes, obId, FduInterpolationInterface_EX.getNextQuaternionValue_Ex(getRotateBufferData(getAnotherBuffer(calDes), obId), ob));
                                }
                                if (ob.getObservedState(3)) //scale
                                {
                                    setScaleBufferData(calDes, obId, FduInterpolationInterface_EX.getNextScaleValue_Ex(getScaleBufferData(getAnotherBuffer(calDes), obId), ob));
                                }
                            }
                            else
                            {
                                _waitForDel.Add(data.processIdList[i]);
                            }
                        }
                        foreach (int delId in _waitForDel)
                        {
                            data.processIdList.Remove(delId);
                        }
                        requestDeleteTransformObserver(_waitForDel.ToArray());
                        //Debug.Log("Cal Des:" + calDes.ToString() + " buffer1 : " + buffer1Complete + " buffer2: " +buffer2Complete + " task amount:" + data.processIdList.Count + " threadId " +id);
                    }
                    finally
                    {
                        data.isComplete = true;
                        bufferLock.ExitReadLock();
                        testProceeTime++;
                    }
                }
            }
        }
        public Vector3 getNextPosition(int id)
        {
            return getPosBufferData(curUsedBuffer, id);
        }
        public Vector3 getNextScale(int id)
        {
            return getScaleBufferData(curUsedBuffer, id);
        }
        public Quaternion getNextRotation(int id)
        {
            return getRotateBufferData(curUsedBuffer, id);
        }
        public BufferData getNextData(int id)
        {
            if (containData(id))
                return dataMap[id];
            else
                return null;
        }
        public bool containData(int id)
        {
            if (id < dataMap.Count && id>=0)
            {
                if (!dataMap[id].isDirty)
                    return true;
                else
                    return false;
            }
            return false;
        }
        public BufferType getCurBufferType()
        {
            return curUsedBuffer;
        }

        void setPosBufferData(BufferType bf,int id,Vector3 data)
        {
            if (containData(id) && dataMap[id].posBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    dataMap[id].posBuff.value1 = data;
                else if (bf == BufferType.Buffer2)
                    dataMap[id].posBuff.value2 = data;
            }
        }
        Vector3 getPosBufferData(BufferType bf, int id)
        {
            if (containData(id) && dataMap[id].posBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    return dataMap[id].posBuff.value1;
                else
                    return dataMap[id].posBuff.value2;
            }
            return Vector3.zero;
        }
        void setRotationBufferData(BufferType bf, int id, Quaternion data)
        {
            if (containData(id) && dataMap[id].rotBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    dataMap[id].rotBuff.value1 = data;
                else if (bf == BufferType.Buffer2)
                    dataMap[id].rotBuff.value2 = data;
            }
        }
        Quaternion getRotateBufferData(BufferType bf, int id)
        {
            if (containData(id) && dataMap[id].rotBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    return dataMap[id].rotBuff.value1;
                else
                    return dataMap[id].rotBuff.value2;
            }
            return Quaternion.identity;
        }
        void setScaleBufferData(BufferType bf, int id, Vector3 data)
        {
            if (containData(id) && dataMap[id].scaleBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    dataMap[id].scaleBuff.value1 = data;
                else if (bf == BufferType.Buffer2)
                    dataMap[id].scaleBuff.value2 = data;
            }
        }
        Vector3 getScaleBufferData(BufferType bf, int id)
        {
            if (containData(id) && dataMap[id].scaleBuff != null)
            {
                if (bf == BufferType.Buffer1)
                    return dataMap[id].scaleBuff.value1;
                else
                    return dataMap[id].scaleBuff.value2;
            }
            return Vector3.zero;
        }
        BufferType getAnotherBuffer(BufferType bf)
        {
            if (bf == BufferType.Buffer1)
                return BufferType.Buffer2;
            else
                return BufferType.Buffer1;
        }

        void OnApplicationQuit()
        {
            if (threadData != null)
            {
                Debug.Log("Stop thread!");
                foreach (ThreadDataSet data in threadData)
                {
                    data.instance.Abort();
                }
            }
        }

    }


    #region 专门定制的插值函数 
    public static class FduInterpolationInterface_EX
    {

        public static Vector3 getNextPosValue_Ex(Vector3 now, FduTransformObserver_Ex observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Vector3.zero;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption();
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector3 offset = Vector3.zero;
                Vector3 lastValue = observer.getCachedPos(observer.getCachedPosCount() - 1);
                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedPosCount();
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - observer.getCachedPos(0)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - observer.getCachedPos(count-2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedPosCount();
                    if (count >= 2)
                    {
                        float step = Vector3.Distance(lastValue - offset, observer.getCachedPos(count-2)) / interval;
                        return Vector3.MoveTowards(now, lastValue, step);
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Vector3.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Vector3.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Vector3.zero;
            }
        }
        public static Vector3 getNextScaleValue_Ex(Vector3 now, FduTransformObserver_Ex observer)
        {

            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Vector3.zero;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption();
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector3 offset = Vector3.zero;
                Vector3 lastValue = observer.getCachedScale(observer.getCachedScaleCount()-1);
                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedScaleCount();
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (lastValue - observer.getCachedScale(0)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (lastValue - observer.getCachedScale(count -2));
                        }
                    }
                }
                lastValue = lastValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    int count = observer.getCachedScaleCount();
                    if (count >= 2)
                    {
                        float step = Vector3.Distance(lastValue - offset, observer.getCachedScale(count - 2)) / interval;
                        return Vector3.MoveTowards(now, lastValue, step);
                    }
                    return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Vector3.Lerp(now, lastValue, 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                        return lastValue;
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Vector3.Lerp(now, lastValue, Time.deltaTime * dtsN.getLerpSpeed());
                }
                return lastValue;
            }
            catch (System.Exception)
            {
                return Vector3.zero;
            }

        }
        public static Quaternion getNextQuaternionValue_Ex(Quaternion now, FduTransformObserver_Ex observer)
        {
            var dts = observer.getDataTransmitStrategy();
            if (dts == null || !dts.GetType().Equals(typeof(FduDTS_EveryNFrame))) return Quaternion.identity;
            FduDTS_EveryNFrame dtsN = (FduDTS_EveryNFrame)dts;
            try
            {
                FduDTS_EveryNFrame.InterpolationOption interOp = dtsN.getInterPolationOption();
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = dtsN.getExtrapolationOption();

                int interval = dtsN.getInterval();
                int cur = dtsN.getCurFrame();

                Vector3 offset = Vector3.zero;
                Quaternion lastValue = observer.getCachedRotation(observer.getCachedRotationCount()-1);
                Vector3 eulerValue = lastValue.eulerAngles;
                if (extraOp != FduDTS_EveryNFrame.ExtrapolationOption.Disable)
                {
                    int count = observer.getCachedRotationCount();
                    if (count >= 2)
                    {
                        if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedEarliest)
                        {
                            offset = (eulerValue - (observer.getCachedRotation(0).eulerAngles)) / (count - 1.0f);
                        }
                        else if (extraOp == FduDTS_EveryNFrame.ExtrapolationOption.CachedLatest)
                        {
                            offset = (eulerValue - (observer.getCachedRotation(count-2)).eulerAngles);
                        }
                    }
                }
                eulerValue = eulerValue + offset;
                if (interOp == FduDTS_EveryNFrame.InterpolationOption.Disable)
                {
                    return Quaternion.Euler(eulerValue);
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.FixedStep || interOp == FduDTS_EveryNFrame.InterpolationOption.EstimateStep)
                {
                    if ((interval - cur + 1) > 0)
                    {
                        var res = Quaternion.Lerp(now, Quaternion.Euler(eulerValue), 1.0f / (interval - cur + 1));
                        return res;
                    }
                    else
                    {
                        return Quaternion.Euler(eulerValue);
                    }
                }
                else if (interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                {
                    return Quaternion.Lerp(now, Quaternion.Euler(eulerValue), Time.deltaTime * dtsN.getLerpSpeed());
                }
                return Quaternion.Euler(eulerValue);
            }
            catch (System.Exception)
            {
                return Quaternion.identity;
            }
        }
    }

    #endregion
}

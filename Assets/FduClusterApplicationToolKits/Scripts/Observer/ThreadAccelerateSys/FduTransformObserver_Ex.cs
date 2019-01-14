using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public sealed class  FduTransformObserver_Ex : FduMultiAttributeObserverBase
    {

        Transform _transform;

        NetworkState.NETWORK_STATE_TYPE stateTemp = NetworkState.NETWORK_STATE_TYPE.SUCCESS;

        public static readonly string[] attributeList = new string[] { 
            "NULL","Position","Rotation","Scale"
        };

        FduAccessableQueue<Vector3> cachedPosition;
        FduAccessableQueue<Vector3> cachedScale;
        FduAccessableQueue<Quaternion> cachedRotation;

        int cachedMaxCount = 1;
        int _threadAccId = -1;

        FduOb_ExThreadAccMgr instance;

        bool _interpolationState = false;
        bool _isMaster = false;
        bool [] _stateArray;

        //CubeSyncLight cl;

        void Awake()
        {
            _transform = GetComponent<Transform>();
#if CLUSTER_ENABLE
            fduObserverInit();
            loadObservedState();

            if (FduOb_ExThreadAccMgr.instance != null)
                FduOb_ExThreadAccMgr.instance.RegistToManager(this);

            if (getInterpolationState() && FduSupportClass.isSlave)
            {
                cachedMaxCount = (int)dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CachedMaxCount);

                cachedPosition = new FduAccessableQueue<Vector3>(cachedMaxCount);
                cachedRotation = new FduAccessableQueue<Quaternion>(cachedMaxCount);
                cachedScale = new FduAccessableQueue<Vector3>(cachedMaxCount);

                if (getObservedState(1))
                    cachedPosition.PushAndPop(transform.position);
                if (getObservedState(2))
                    cachedRotation.PushAndPop(transform.rotation);
                if (getObservedState(3))
                    cachedScale.PushAndPop(transform.localScale);
            }
            _interpolationState = getInterpolationState();
            _isMaster = FduSupportClass.isMaster;
            _stateArray = new bool[attributeList.Length];
            for (int i = 0; i < _stateArray.Length; ++i)
            {
                _stateArray[i] = getObservedState(i);
            }

            //if(_isMaster)
            //    cl = GetComponent<CubeSyncLight>();
#endif
        }

        public Vector3 getCachedPos(int index) { return cachedPosition.getElementAt(index); }
        public Vector3 getCachedScale(int index) { return cachedScale.getElementAt(index); }
        public Quaternion getCachedRotation(int index) { return cachedRotation.getElementAt(index); }

        public int getCachedPosCount() { return cachedPosition.Count; }
        public int getCachedScaleCount() { return cachedScale.Count; }
        public int getCachedRotationCount() { return cachedRotation.Count; }


        void appendNewPos(Vector3 pos)
        {
            if (cachedPosition != null)
            {
                cachedPosition.PushAndPop(pos);
            }
        }
        void appendNewScale(Vector3 scale)
        {
            if (cachedScale != null)
            {
                cachedScale.PushAndPop(scale);
            }
        }
        void appendNewRotation(Quaternion rot)
        {
            if (cachedRotation != null)
            {
                cachedRotation.PushAndPop(rot);
            }
        }
        public void setAccThreadId(int id) { _threadAccId = id; }

        public override void AlwaysUpdate()
        {
            base.AlwaysUpdate();

            //if (cl != null)
            //    cl.alwaysUpdate();

            if (!_interpolationState || _isMaster) return; //标志位为真且为从节点才进行插值

            updateFunc();
        }
        public override void OnSendData()
        {
            switchCaseFunc(FduMultiAttributeObserverOP.SendData, ref stateTemp);
        }
        public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            bool interFlag = false;
            if (dataTransmitStrategy.getCustomData(FduDTSCustomDataType.EveryNFrame_CurFrameCount) != null && _interpolationState)
                interFlag = true;
            if (!interFlag)
            {
                switchCaseFunc(FduMultiAttributeObserverOP.Receive_Direct, ref state);
            }
            else
            {
                switchCaseFunc(FduMultiAttributeObserverOP.Receive_Interpolation, ref state);
            }
        }
        void updateFunc()
        {
            //为了update专门开个分支 减少对底层数据结构的访问次数（以及减少数据有效性的检测）
            if (FduOb_ExThreadAccMgr.instance == null) return;

            FduOb_ExThreadAccMgr.BufferData data = FduOb_ExThreadAccMgr.instance.getNextData(_threadAccId);
            FduOb_ExThreadAccMgr.BufferType type = FduOb_ExThreadAccMgr.instance.getCurBufferType();

            if (data == null) return;

#if UNITY_5_6
            if (_stateArray[1] && _stateArray[2])
            {
                if (type == FduOb_ExThreadAccMgr.BufferType.Buffer1)
                    _transform.SetPositionAndRotation(data.posBuff.value1, data.rotBuff.value2);
                else
                    _transform.SetPositionAndRotation(data.posBuff.value2, data.rotBuff.value2);

                if (_stateArray[3])
                {
                    if (type == FduOb_ExThreadAccMgr.BufferType.Buffer1)
                        _transform.localScale = data.scaleBuff.value1;
                    else
                        _transform.localScale = data.scaleBuff.value2;
                }
                return;
            }
#endif
            for (int i = 1; i < attributeList.Length; ++i)
            {
                if (!_stateArray[i])
                    continue;
                switch (i)
                {
                    case 1:
                        if (type == FduOb_ExThreadAccMgr.BufferType.Buffer1)
                            _transform.position = data.posBuff.value1;
                        else
                            _transform.position = data.posBuff.value2;
                        break;
                    case 2:
                        if (type == FduOb_ExThreadAccMgr.BufferType.Buffer1)
                            _transform.rotation = data.rotBuff.value1;
                        else
                            _transform.rotation = data.rotBuff.value2;
                        break;
                    case 3:
                        if (type == FduOb_ExThreadAccMgr.BufferType.Buffer1)
                            _transform.localScale = data.scaleBuff.value1;
                        else
                            _transform.localScale = data.scaleBuff.value2;
                        break;
                }
            }
        }
        void switchCaseFunc(FduMultiAttributeObserverOP op, ref NetworkState.NETWORK_STATE_TYPE state)
        {
            for (int i = 1; i < attributeList.Length; ++i)
            {
                if (!getObservedState(i))
                    continue;
                switch (i)
                {
                    case 1://position
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.position);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.position = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            appendNewPos(BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                    case 2://rotation
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendQuaternion(_transform.rotation);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.rotation = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            appendNewRotation(BufferedNetworkUtilsClient.ReadQuaternion(ref state));
                        break;
                    case 3://scale
                        if (op == FduMultiAttributeObserverOP.SendData)
                            BufferedNetworkUtilsServer.SendVector3(_transform.localScale);
                        else if (op == FduMultiAttributeObserverOP.Receive_Direct)
                            _transform.localScale = BufferedNetworkUtilsClient.ReadVector3(ref state);
                        else if (op == FduMultiAttributeObserverOP.Receive_Interpolation)
                            appendNewScale(BufferedNetworkUtilsClient.ReadVector3(ref state));
                        break;
                }
            }
        }
    }
}

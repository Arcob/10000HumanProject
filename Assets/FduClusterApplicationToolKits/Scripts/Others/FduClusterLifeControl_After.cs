using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public interface IFduLateUpdateFunc
    {
        void LateUpdateFunc();
    }

    public interface IFduEndOfFrameFunc
    {
        void EndOfFrameFunc();
    }

    public class FduClusterLifeControl_After : MonoBehaviour
    {
        [SerializeField]
        FduClusterViewManager _clusterViewMgr;

        [SerializeField]
        FduActiveSyncManager _activeSyncMgr;

        [SerializeField]
        ClusterCommandManager _commandMgr;

        [SerializeField]
        FduClusterRandomSync _randomMgr;

        [SerializeField]
        FduClusterInputMgr _inputMgr;

        [SerializeField]
        FduClusterTimeMgr _timeMgr;


        void LateUpdate()
        {
            if (_randomMgr != null)
                _randomMgr.LateUpdateFunc();
            if (_clusterViewMgr != null)
                _clusterViewMgr.LateUpdateFunc();
            if (_activeSyncMgr != null)
                _activeSyncMgr.LateUpdateFunc();
            if (_commandMgr != null)
                _commandMgr.LateUpdateFunc();

            StartCoroutine(EndofFrame());
        }

        IEnumerator EndofFrame()
        {
            yield return new WaitForEndOfFrame();

            if (_inputMgr != null)
                _inputMgr.EndOfFrameFunc();
            if (_timeMgr != null)
                _timeMgr.EndOfFrameFunc();
        }

    }
}

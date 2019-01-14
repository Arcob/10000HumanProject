using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using FDUObjectSync;

public class CharacterObserver : FduObserverBase{

    GPUSkinningController gsc;

    Transform _transform;

    Vector4 animationCache;


    [HideInInspector]
    public int index = -1;

	void Start () {

        gsc = GetComponent<GPUSkinningController>();
        _transform = GetComponent<Transform>();
        setDTSData_Direct();
        fduObserverInit();
	}

    public override void AlwaysUpdate()
    {
        base.AlwaysUpdate();

        if (FduSupportClass.isMaster)
        {
            _transform.SetPositionAndRotation(RVO.Simulator.Instance.agents_[index].position_v3,
                RVO.Simulator.Instance.agents_[index].rotation);
        }
    }

    public override void OnSendData()
    {
        BufferedNetworkUtilsServer.SendVector3(RVO.Simulator.Instance.agents_[index].position_v3);
        BufferedNetworkUtilsServer.SendQuaternion(RVO.Simulator.Instance.agents_[index].rotation);

        if ((FrameCounter.frameCount ) % FrameCounter.interval == 0)
        {
            BufferedNetworkUtilsServer.SendVector4(gsc._tempFramePixelSegmentation);
        }

        
    }

    public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
    {
        _transform.SetPositionAndRotation(BufferedNetworkUtilsClient.ReadVector3(ref state), BufferedNetworkUtilsClient.ReadQuaternion(ref state));

        if ((FrameCounter.frameCount) % FrameCounter.interval == 0)
        {
            gsc.mpbs[0].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, BufferedNetworkUtilsClient.ReadVector4(ref state));
            gsc.mrs[0].SetPropertyBlock(gsc.mpbs[0]);
        }
    }
}

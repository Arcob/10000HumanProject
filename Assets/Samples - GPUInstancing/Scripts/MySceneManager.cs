using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;
using UnityEngine.Profiling;

public class MySceneManager : MonoBehaviour
{
    public static MySceneManager instance;

    [Header("Agent")]
    public GameObject agentPrefab;
    public Transform agentsContainer;

    [Header("Agent Parameter")]
    public float neighborDist = 10.0f;
    public int maxNeighbors = 10;
    public float timeHorizon = 0.5f;
    public float timeHorizonObst = 0.5f;
    public float agentRadius = 0.5f;
    public float agentMaxSpeed = 10.0f;
    public float objectPositionY = 0.0f;

    [Header("Other Settings")]
    public float timeStep = 0.015f;
    public static bool isThread = true;

    protected float timer;
    protected IList<Transform> agents;
    protected IList<Vector3> goals;
    protected GPUSkinningController[] gpuSkinningControllers;

    void Awake()
    {

#if CLUSTER_ENABLE
        if (FDUClusterAppToolKits.FduSupportClass.isSlave)
        {
            Destroy(this);
            return;
        }
#endif

        instance = this;

        timer = 0.0f;
        agents = new List<Transform>();
        goals = new List<Vector3>();
        SetupScenario();
        if (isThread)
            AllocateThreadJobs();
    }
    [Header("人物数量")]
    [SerializeField]
    protected int agentCount = 10000;
    
    protected virtual void SetupScenario()
    {

    }

    // 设置多线程任务
    void AllocateThreadJobs()
    {
        Simulator.Instance.SetWorkers();
    }


    public void UpdateTransform()
    {
        for (int i = 0; i < agentCount; ++i)
        {
            agents[i].SetPositionAndRotation(Simulator.Instance.agents_[i].position_v3, Simulator.Instance.agents_[i].rotation);
        }
    }

}

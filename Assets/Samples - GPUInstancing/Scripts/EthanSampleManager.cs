using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;
using UnityEngine.Profiling;

public class EthanSampleManager : MonoBehaviour {

    [Header("Agent")]
    public GameObject agentPrefab;
    public Transform agentsContainer;

    [Header("Agent Parameter")]
    public float neighborDist = 20.0f;
    public int maxNeighbors = 20;
    public float timeHorizon = 5.0f;
    public float timeHorizonObst = 5.0f;
    public float agentRadius = 1.0f;
    public float agentMaxSpeed = 20.0f;
    public float objectPositionY = 0.0f;

    [Header("Other Settings")]
    public Transform plane;
    public float timeStep = 0.015f;
    public static bool isThread = true;

    float timer;
    IList<Transform> agents;
    IList<Vector3> goals;
    GPUSkinningController[] gpuSkinningControllers;

    void Awake ()
    {
        timer = 0.0f;
        agents = new List<Transform>();
        goals = new List<Vector3>();
        SetupScenario();
        if (isThread)
            AllocateThreadJobs();
    }

    private int agentCount = 10000;

    int num = 0;
    void SetupScenario()
    {
        /* Specify the global time step of the simulation. */
        Simulator.Instance.setTimeStep(timeStep);
        
        Simulator.Instance.setAgentDefaults(
            neighborDist,
            maxNeighbors,
            timeHorizon,
            timeHorizonObst,
            agentRadius, 
            agentMaxSpeed, 
            new Vector2(0.0f, 0.0f));

        gpuSkinningControllers = new GPUSkinningController[agentCount];
        //plane.localScale = new Vector3(agentCount * 0.05f, 1.0f, agentCount * 0.05f);
        /*
         * Add agents, specifying their start position, and store their
         * goals on the opposite side of the environment.
         */
        Vector3 tempVector3;
        Vector3 goal;
        GameObject go;
        GPUSkinningController gpuSkinningController;
        int row = 10, column = 500;
        for (int j = 0; j < row; j++)
        {
            for (int i = 0; i < column; ++i)
            {
                tempVector3 = (200 + j * 5) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                        (float)Mathf.Sin(i * 2.0f * Mathf.PI / column));
                goal = new Vector3(-tempVector3.x, objectPositionY, -tempVector3.z);
                goals.Add(goal);

                go = GameObject.Instantiate(agentPrefab, tempVector3, Quaternion.identity, agentsContainer);
                gpuSkinningController = go.GetComponent<GPUSkinningController>();

                Simulator.Instance.addAgent(tempVector3, goal, objectPositionY, gpuSkinningController, true);
                agents.Add(go.transform);
                gpuSkinningControllers[num++] = gpuSkinningController;
            }
        }
        Simulator.Instance.kdTree_.buildAgentTree();
    }

    // 设置多线程任务
    void AllocateThreadJobs()
    {
        Simulator.Instance.SetWorkers();
    }


    void UpdateTransform()
    {
        /* Output the current global time. */
        //Console.Write(Simulator.Instance.getGlobalTime());

        /* Output the current position of all the agents. */
        for (int i = 0; i < agentCount; ++i)
        {
            //agents[i].position = Simulator.Instance.getAgentPosition_V3(i, objectPositionY);
            //agents[i].position = Simulator.Instance.agents_[i].position_v3;
            agents[i].SetPositionAndRotation(Simulator.Instance.agents_[i].position_v3, Simulator.Instance.agents_[i].rotation);
        }

        //Console.WriteLine();
    }

    void SetPreferredVelocities()
    {
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            Vector3 goalVector = goals[i] - Simulator.Instance.getAgentPosition_V3(i, objectPositionY);
            //if (RVOMath.absSq(goalVector) > 1.0f)
            // fzy modify: 速度限制在0.0f~agentMaxSpeed
            if (RVOMath.absSq(goalVector) > agentMaxSpeed)
            {
                goalVector = goalVector.normalized * agentMaxSpeed;
            }

            Simulator.Instance.setAgentPrefVelocity(i, goalVector);
        }
        //Debug.Log(Simulator.Instance.getAgentVelocity(0));
    }

    void Update ()
    {
        /*
        Profiler.BeginSample("UpdatePreferredVelocity");
        SetPreferredVelocities();
        Profiler.EndSample();*/

        if (!isThread)
        {
            Profiler.BeginSample("DoStep");
            Simulator.Instance.doStep();
            Profiler.EndSample();
        }

        timer += Time.deltaTime;
        if (timer >= timeStep)
        {
            timer = 0.0f;
            Profiler.BeginSample("UpdateTransform");
            UpdateTransform();
            Profiler.EndSample();
        }

        // GPUSkinningController合并
        Profiler.BeginSample("GPUSkinningController.UpdatePlayingData");
        for (int i = 0; i < num; i++)
        {
            //gpuSkinningControllers[i].UpdatePlayingData(); 10000次函数调用优化
            for (int j = 0; j < gpuSkinningControllers[i].playerMonosCount; j++)
            {
                gpuSkinningControllers[i].mpbs[j].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, gpuSkinningControllers[i].framePixelSegmentation);
                gpuSkinningControllers[i].mrs[j].SetPropertyBlock(gpuSkinningControllers[i].mpbs[j]);
            }
        }
        Profiler.EndSample();
    }

    bool reachedGoal()
    {
        /* Check if all agents have reached their goals. */
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            if (RVOMath.absSq(Simulator.Instance.getAgentPosition_V3(i,objectPositionY) - goals[i]) > Simulator.Instance.getAgentRadius(i) * Simulator.Instance.getAgentRadius(i))
            {
                return false;
            }
        }
        return true;
    }
}

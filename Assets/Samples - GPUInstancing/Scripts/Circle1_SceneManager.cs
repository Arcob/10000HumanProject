using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;
using UnityEngine.Profiling;

public class Circle1_SceneManager : MySceneManager {

    public GameObject agentPrefab_other;

    int num = 0;
    protected override void SetupScenario()
    {
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

        Vector3 tempVector1, tempVector2;
        Vector3 goal;
        GameObject go;
        GPUSkinningController gpuSkinningController;
        int row = 20, column = 250;
        for (int j = 0; j < row; j++)
        {
            for (int i = 0; i < column; ++i)
            {
                tempVector1 = (200 + j * 5) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                        (float)Mathf.Sin(i * 2.0f * Mathf.PI / column));
                tempVector2 = (50 + j * 5) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                        (float)Mathf.Sin(i * 2.0f * Mathf.PI / column));

                //-----------------------------------------------------------------

                goal = tempVector2;
                goals.Add(goal);

                go = GameObject.Instantiate(agentPrefab, tempVector1, Quaternion.identity, agentsContainer);
                gpuSkinningController = go.GetComponent<GPUSkinningController>();

                Simulator.Instance.addAgent(tempVector1, goal, objectPositionY, gpuSkinningController, true);
                agents.Add(go.transform);
                gpuSkinningControllers[num++] = gpuSkinningController;

                //-----------------------------------------------------------------

                goal = tempVector1;
                goals.Add(goal);

                go = GameObject.Instantiate(agentPrefab_other, tempVector2, Quaternion.identity, agentsContainer);
                gpuSkinningController = go.GetComponent<GPUSkinningController>();

                Simulator.Instance.addAgent(tempVector2, goal, objectPositionY, gpuSkinningController, true);
                agents.Add(go.transform);
                gpuSkinningControllers[num++] = gpuSkinningController;
            }
        }
        Simulator.Instance.kdTree_.buildAgentTree();
        
    }


    void Update()
    {
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
        for (int i = 0; i < agentCount; i++)
        {
            //gpuSkinningControllers[i].UpdatePlayingData(); 10000次函数调用优化
            for (int j = 0; j < gpuSkinningControllers[i].playerMonosCount; j++)
            {
                gpuSkinningControllers[i].mpbs[j].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, gpuSkinningControllers[i].framePixelSegmentation);
                gpuSkinningControllers[i].mrs[j].SetPropertyBlock(gpuSkinningControllers[i].mpbs[j]);
            }
        }
        Profiler.EndSample();

        if (Input.GetKeyDown(KeyCode.L))
        {
            Vector3 tempVector3;
            for(int i = 0; i < agentCount; i++)
            {
                tempVector3 = Simulator.Instance.agents_[i].origin;
                Simulator.Instance.agents_[i].origin = Simulator.Instance.agents_[i].goal;
                Simulator.Instance.agents_[i].goal = tempVector3;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;
using UnityEngine.Profiling;
using FDUClusterAppToolKits;
public class OlympicStadiumSceneManager2 : MySceneManager
{
    [Header("集群Prefab")]

    [SerializeField]
    GameObject Group1000;

    [SerializeField]
    GameObject Group200;

    [SerializeField]
    GameObject Group100;

    [SerializeField]
    GameObject Group10;

    [SerializeField]
    GameObject Group1;


    public GameObject agentPrefab_other;

    int num = 0;
    protected override void SetupScenario()
    {
        Simulator.Instance.setTimeStep(timeStep);
        //agentCount = 13000;

        agentCount = SettingData.instance.data.humanCount;

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

        int group1000Count = agentCount / 1000;
        group1000Count = 0;
        int group100Count = (agentCount - 1000 * group1000Count) / 100;
        int group10Count = (agentCount -1000 * group1000Count - 100 * group100Count) / 10;
        int group1Count = agentCount - 1000 * group1000Count - 100 * group100Count - 10 * group10Count;

        int group200Count = agentCount / 200;
        

        Debug.Log("100:" + group100Count + " 10:" + group10Count + " 1:"+group1Count);

        GPUSkinningController[] gscArr = new GPUSkinningController[agentCount];
        int index = 0;

        //for (int i = 0; i < group200Count; ++i)
        //{
        //    var ingo = GameObject.Instantiate(Group200, agentsContainer);
        //    var gpus = ingo.GetComponentsInChildren<GPUSkinningController>();
        //    foreach (GPUSkinningController ins in gpus)
        //    {
        //        gscArr[index++] = ins;
        //    }
        //}

        for (int i = 0; i < group1000Count; ++i)
        {
            var ingo = GameObject.Instantiate(Group1000, agentsContainer);
            var gpus = ingo.GetComponentsInChildren<GPUSkinningController>();
            foreach (GPUSkinningController ins in gpus)
            {
                ins.GetComponent<CharacterObserver>().index = index;
                gscArr[index++] = ins;
            }
            if (i % 2 == 0)
                ingo.GetClusterView().IsImmediatelyDeserialize = false;
            else
                ingo.GetClusterView().IsImmediatelyDeserialize = true;
        }
        for (int i = 0; i < group100Count; ++i)
        {
            var ingo = GameObject.Instantiate(Group100, agentsContainer);
            var gpus = ingo.GetComponentsInChildren<GPUSkinningController>();
            foreach (GPUSkinningController ins in gpus)
            {
                ins.GetComponent<CharacterObserver>().index = index;
                gscArr[index++] = ins;
            }

            
            if (i % 2 == 0)
            {
                ingo.GetClusterView().IsImmediatelyDeserialize = false;
            }
            else
                ingo.GetClusterView().IsImmediatelyDeserialize = true;
            
        }
        for (int i = 0; i < group10Count; ++i)
        {
            var ingo = GameObject.Instantiate(Group10, agentsContainer);
            var gpus = ingo.GetComponentsInChildren<GPUSkinningController>();
            foreach (GPUSkinningController ins in gpus)
            {
                ins.GetComponent<CharacterObserver>().index = index;
                gscArr[index++] = ins;
            }

            if (i % 2 == 0)
                ingo.GetClusterView().IsImmediatelyDeserialize = false;
            else
                ingo.GetClusterView().IsImmediatelyDeserialize = true;
        }
        for (int i = 0; i < group1Count; ++i)
        {
            var ingo = GameObject.Instantiate(Group1, agentsContainer);
            var gpus = ingo.GetComponentsInChildren<GPUSkinningController>();
            foreach (GPUSkinningController ins in gpus)
            {
                ins.GetComponent<CharacterObserver>().index = index;
                gscArr[index++] = ins;
            }

            if (i % 2 == 0)
                ingo.GetClusterView().IsImmediatelyDeserialize = false;
            else
                ingo.GetClusterView().IsImmediatelyDeserialize = true;
        }
        
        if (index != agentCount)
            Debug.LogError("AIYOWOCAO");

        index = 0;
        try
        {

            //------------Circle 6000----------------
            // 150x10x2x2 = 6000
            // 250x10x2x2 = 10000
            Vector3 benchMarkCircle = new Vector3(400.0f, 0.0f, 0.0f);
            //int row = 10, column = 150;
            int row = 12, column = 250;
            for (int j = 0; j < row; j++)
            {
                for (int i = 0; i < column; ++i)
                {
                    tempVector1 = (130f + j * 4.5f) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                            (float)Mathf.Sin(i * 2.0f * Mathf.PI / column)) + benchMarkCircle;
                    tempVector2 = (50f + j * 5f) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                            (float)Mathf.Sin(i * 2.0f * Mathf.PI / column)) + benchMarkCircle;


                    goal = tempVector2;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector1, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();

                    Simulator.Instance.addAgent(tempVector1, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    goal = tempVector1;
                    goals.Add(goal);


                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector2, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();

                    Simulator.Instance.addAgent(tempVector2, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    //------------------------------------------------------------------------------------

                    tempVector1 = (150f + j * 4.5f) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                            (float)Mathf.Sin(i * 2.0f * Mathf.PI / column)) - benchMarkCircle - new Vector3(20.0f, 0.0f, 0.0f);
                    tempVector2 = (50f + j * 5f) * new Vector3(Mathf.Cos(i * 2.0f * Mathf.PI / column), objectPositionY,
                            (float)Mathf.Sin(i * 2.0f * Mathf.PI / column)) - benchMarkCircle - new Vector3(20.0f, 0.0f, 0.0f);


                    goal = tempVector2;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector1, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();

                    Simulator.Instance.addAgent(tempVector1, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    goal = tempVector1;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector2, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();

                    Simulator.Instance.addAgent(tempVector2, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;
                }
            }

            //------------phalanx 4000----------------
            // 100x20x2 = 4000
            float benchmarkX = -198f, benchmarkZ = 90f;
            row = 100; column = 40; float interval = 4.0f;
            for (int j = 0; j < row; ++j)
            {
                for (int i = 0; i < column; ++i)
                {
                    tempVector1 = new Vector3(benchmarkX + j * interval, objectPositionY, benchmarkZ - i * interval);
                    tempVector2 = new Vector3(benchmarkX + j * interval, objectPositionY, -benchmarkZ + (column - 1 - i) * interval);

                    goal = tempVector2;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector1, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();
                    Simulator.Instance.addAgent(tempVector1, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    goal = tempVector1;
                    goals.Add(goal);

                    //Debug.Log("方阵:" + index);
                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector2, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();
                    Simulator.Instance.addAgent(tempVector2, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    //-----------------------------------------------------------------
                    /*
                    tempVector1 = new Vector3(-(benchmarkX + j * interval), objectPositionY, benchmarkZ - i * interval);
                    tempVector2 = new Vector3(-(benchmarkX + j * interval), objectPositionY, -benchmarkZ + (column - 1 - i) * interval);

                    goal = tempVector2;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector1, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();
                    Simulator.Instance.addAgent(tempVector1, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;

                    goal = tempVector1;
                    goals.Add(goal);

                    go = gscArr[index++].gameObject;
                    go.transform.SetPositionAndRotation(tempVector2, Quaternion.identity);
                    gpuSkinningController = go.GetComponent<GPUSkinningController>();
                    Simulator.Instance.addAgent(tempVector2, goal, objectPositionY, gpuSkinningController, true);
                    agents.Add(go.transform);
                    gpuSkinningControllers[num++] = gpuSkinningController;*/
                }
            }

            //int circleRange = 250;
            //int circleCount = agentCount / circleRange;

            //int _circlePart = (int)(circleRange * 0.611f);
            //int _rectPart = circleRange - _circlePart;

            //for (int i = 0; i < circleCount; ++i)
            //{
            //    float maxZvalueIn = float.MinValue;
            //    float minZvalueIn = float.MaxValue;

            //    float maxZvalueOut = float.MinValue;
            //    float minZvalueOut = float.MaxValue;

            //    float innerRadius = (50f + i * 5.0f);
            //    float outerRadius = (200f + i * 4.5f);

            //    for (int j = 0; j < _circlePart; ++j)
            //    {
            //        tempVector1 = outerRadius * new Vector3(Mathf.Cos(j * 2.0f * Mathf.PI / _circlePart), objectPositionY,
            //                 (float)Mathf.Sin(j * 2.0f * Mathf.PI / _circlePart));
            //        tempVector2 = innerRadius * new Vector3(Mathf.Cos(j * 2.0f * Mathf.PI / _circlePart), objectPositionY,
            //                (float)Mathf.Sin(j * 2.0f * Mathf.PI / _circlePart));

            //        if (tempVector1.x > 0)
            //            tempVector1.x += outerRadius;
            //        else
            //            tempVector1.x -= outerRadius;

            //        if (tempVector2.x > 0)
            //            tempVector2.x += innerRadius;
            //        else
            //            tempVector2.x -= innerRadius;

            //        maxZvalueOut = tempVector1.z > maxZvalueOut ? tempVector1.z : maxZvalueOut;
            //        minZvalueOut = tempVector1.z < minZvalueOut ? tempVector1.z : minZvalueOut;

            //        maxZvalueIn = tempVector2.z > maxZvalueIn ? tempVector2.z : maxZvalueIn;
            //        minZvalueIn = tempVector2.z < minZvalueIn ? tempVector2.z : minZvalueIn;

            //    }
            //    for (int j = 0; j < _rectPart; ++j)
            //    {

            //    }
            //}


        }
        
        catch (System.Exception e)
        {
            Debug.LogError("MTF:" + e.Message);
        }
        finally
        {
            Simulator.Instance.kdTree_.buildAgentTree();
        }
    }


    void LateUpdate()
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
            //UpdateTransform();
            Profiler.EndSample();
        }

        // GPUSkinningController合并
        Profiler.BeginSample("GPUSkinningController.UpdatePlayingData");
        for (int i = 0; i < agentCount; i++)
        {
            for (int j = 0; j < gpuSkinningControllers[i].playerMonosCount; j++)
            {
                gpuSkinningControllers[i]._tempFramePixelSegmentation = gpuSkinningControllers[i].framePixelSegmentation;
                gpuSkinningControllers[i].mpbs[j].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, gpuSkinningControllers[i].framePixelSegmentation);
                gpuSkinningControllers[i].mrs[j].SetPropertyBlock(gpuSkinningControllers[i].mpbs[j]);
            }
        }
        Profiler.EndSample();

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Switch Origin and Goal");
            Vector3 tempVector3;
            for (int i = 0; i < agentCount; i++)
            {
                tempVector3 = Simulator.Instance.agents_[i].origin;
                Simulator.Instance.agents_[i].origin = Simulator.Instance.agents_[i].goal;
                Simulator.Instance.agents_[i].goal = tempVector3;
            }
        }

        //fzy test:
        if (Input.GetKey(KeyCode.F))
        {
            UpdateTransform();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO2Simulator : MonoBehaviour {
    // Simulator每次执行的时间间隔
    public float timeStep_Simulator = 0.15f;
    float timer;

    List<RVO2Agent> agentList;
    int agentCount;

	void Awake () {
        agentList = new List<RVO2Agent>();
        Simulator.Instance.setTimeStep(timeStep_Simulator);
        timer = 0.0f;
	}
	
    public void AddAgent(RVO2Agent newAgent)
    {
        newAgent.InitAgent();
        agentList.Add(newAgent);
        agentCount = agentList.Count;
        //设置target
        newAgent.SetTarget(-Simulator.Instance.getAgentPosition_V3(agentCount - 1));
    }

	void Update ()
    {
        if (agentCount <= 0) return;
        timer += Time.deltaTime;
        if (timer >= timeStep_Simulator)
        {
            Simulator.Instance.doStep();

            timer = 0.0f;
        }
	}

    void UpdateAgentsPreferredVelocity()
    {
        for(int i = 0; i < agentCount; i++)
        {
            agentList[i].SetAgentPrefVelocity();
        }
    }

    void UpdateAgentsPosition()
    {
        for(int i = 0; i < agentCount; i++)
        {
            agentList[i].UpdatePosition();
        }
    }
}

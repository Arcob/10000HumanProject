using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO2Agent : MonoBehaviour {
    [Header("Agent Parameter")]
    public float neighborDist = 30.0f;
    public int maxNeighbors = 10;
    public float timeHorizon = 5.0f;
    public float timeHorizonObst = 5.0f;
    public float radius = 2.0f;
    public float maxSpeed = 10.0f;
    public bool isKinematic = false;

    //[Header("Others")]
    private Vector3 targetPosition;
    private Transform targetTransform;

    // -1表示未初始化或者初始化失败
    private int agentID = -1;
    private Vector3 preferredVelocity = Vector3.zero;
    private float positionY;


    private void Start()
    {
        Vector3 newVector3 = new Vector3(1.0f, 2.0f, 3.0f);
        Debug.Log(newVector3);
        newVector3.x = -1.0f;
        Debug.Log(newVector3);
    }
    /// <summary>
    /// 初始化Agent，使用Simulator管理Agent的行为
    /// </summary>
    public void InitAgent()
    {
        SetPositionY();
        agentID = Simulator.Instance.addAgent(transform.position, neighborDist, maxNeighbors, 
            timeHorizon, timeHorizonObst, radius, maxSpeed, Vector2.zero);
    }

    /// <summary>
    /// 设置Agent的高度
    /// </summary>
    public void SetPositionY()
    {
        positionY = transform.position.y;
    }

    /// <summary>
    /// 设置Target位置（同时将TargetTransform设为null）
    /// targetTransform设为空，表示Agent目标是一个静态点，非移动物体
    /// </summary>
    /// <param name="_targetPosition">Target位置</param>
    public void SetTarget(Vector3 _targetPosition)
    {
        targetTransform = null;
        targetPosition = _targetPosition;
    }

    /// <summary>
    /// 设置Target对象
    /// </summary>
    /// <param name="_targetTransform">Target对象</param>
    public void SetTarget(Transform _targetTransform)
    {
        targetTransform = _targetTransform;
        targetPosition = targetTransform.position;
    }

    /// <summary>
    /// 设置Agent的PrefVelocity，无参时使用target的位置计算PrefVelocity
    /// </summary>
    public void SetAgentPrefVelocity()
    {
        // targetTransform不为空，表示agent的目标是动态物体（targetTransform）,使用targetTransform的位置作为targetPosition
        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
        }

        // 产生随机偏移，避免完全对称的场景
        float angle = Random.Range(0.0f, 2.0f) * Mathf.PI;
        Vector3 deltaVector3 = Random.Range(0.0f, 0.001f) * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

        Simulator.Instance.setAgentPrefVelocity(agentID, (targetPosition - transform.position) + deltaVector3);
    }

    /// <summary>
    /// 设置Agent的PrefVelocity
    /// </summary>
    /// <param name="_prefVelocity"> agent的prefVelocity </param>
    public void SetAgentPreVelocity(Vector3 _prefVelocity)
    {
        preferredVelocity = _prefVelocity;

        // 产生随机偏移，避免完全对称的场景
        float angle = Random.Range(0.0f, 2.0f) * Mathf.PI;
        Vector3 deltaVector3 = Random.Range(0.0f, 0.001f) * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

        Simulator.Instance.setAgentPrefVelocity(agentID, preferredVelocity + deltaVector3);
    }

    /// <summary>
    /// 更新agent位置
    /// </summary>
    public void UpdatePosition()
    {
        if (agentID < 0) return;
        // 更新坐标（or 使用刚体移动?）
        transform.position = Simulator.Instance.getAgentPosition_V3(agentID, positionY);
    }
}

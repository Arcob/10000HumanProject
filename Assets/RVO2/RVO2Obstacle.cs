using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

[RequireComponent(typeof(MeshFilter))]
public class RVO2Obstacle : MonoBehaviour {

    MeshFilter meshFilter;
    Mesh mesh;
    Vector3[] boundingBoxLocalPoints;
    float obstacleHeight;

    int obstacleID = -1;

    void Start () {

        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        Bounds bounds = mesh.bounds;
        Vector3 extents = bounds.extents;

        // 使用Mesh包围盒点作为Obstacle的底线
        boundingBoxLocalPoints = new Vector3[4];
        boundingBoxLocalPoints[0] = bounds.center - extents;
        extents.x = -extents.x;
        boundingBoxLocalPoints[1] = bounds.center - extents;
        extents.z = -extents.z;
        boundingBoxLocalPoints[2] = bounds.center - extents;
        extents.x = -extents.x;
        boundingBoxLocalPoints[3] = bounds.center - extents;

        Vector3 heightVector = new Vector3(0.0f, bounds.size.y, 0.0f);
        obstacleHeight = transform.TransformVector(heightVector).magnitude;

        AddObstacle();

        drawPoints = new Vector3[boundingBoxLocalPoints.Length];
    }

    void AddObstacle()
    {
        Simulator.Instance.RemoveObstacle(obstacleID);
        List<Vector2> obstaclePoints = new List<Vector2>();
        foreach (Vector3 v in boundingBoxLocalPoints)
            obstaclePoints.Add(RVOMath.V3ToV2(transform.TransformPoint(v)));

        obstacleID = Simulator.Instance.addObstacle(obstaclePoints);
        Simulator.Instance.processObstacles();
        transform.hasChanged = false;
    }
	
	void Update () {
        if (transform.hasChanged)
        {
            AddObstacle();
        }
        //if (Input.GetKeyDown(KeyCode.D))
            SetDrawPoints();
        DrawLines();
	}

    Vector3[] drawPoints = null;
    void SetDrawPoints()
    {
        drawPoints = new Vector3[boundingBoxLocalPoints.Length];
        for (int i = 0; i < boundingBoxLocalPoints.Length; i++)
        {
            drawPoints[i] = transform.TransformPoint(boundingBoxLocalPoints[i]);
        }
    }
    void DrawLines()
    {
        for (int i = 0; i < drawPoints.Length; i++)
        {
            if (i == drawPoints.Length - 1)
                Debug.DrawLine(drawPoints[i], drawPoints[0], Color.red);
            else if (i == 0)
                Debug.DrawLine(drawPoints[i], drawPoints[i + 1], Color.green);
            else
                Debug.DrawLine(drawPoints[i], drawPoints[i + 1], Color.white);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityAPITest : MonoBehaviour {
    SkinnedMeshRenderer smr;
    Mesh mesh;
    Vector4[] tangents;
    Vector3[] normals;
    Color[] colors;
    int length;

    void Start () {
        smr = GetComponent<SkinnedMeshRenderer>();
        mesh = smr.sharedMesh;
        colors = mesh.colors;
        length = colors.Length;
        Debug.Log(length);
        
        for (int i = 0; i < length; i++)
            colors[i] = new Color(1.0f, 0.0f, 0.0f);
        mesh.colors = colors;

        /*
        tangents = mesh.tangents;
        normals = mesh.normals;
        Debug.Log(normals.Length);
        for(int i = 0; i < normals.Length; i++)
        {
            Debug.Log(normals[i].ToString() + tangents[i].ToString());
        }
        */
    }

    void Update () {
	}
}

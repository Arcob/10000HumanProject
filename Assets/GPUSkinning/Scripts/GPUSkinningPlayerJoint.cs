using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于生成模型使用的骨骼
/// </summary>
[ExecuteInEditMode]
public class GPUSkinningPlayerJoint : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private int boneIndex = 0;

    [HideInInspector]
    [SerializeField]
    private string boneGUID = null;

    private Transform bone = null;

    public int BoneIndex
    {
        get { return boneIndex; }
    }

    public string BoneGUID
    {
        get { return boneGUID; }
    }

    public Transform Transform
    {
        get { return bone; }
    }

    private void Awake()
    {
        // author version:
        hideFlags = HideFlags.HideInInspector;
        // fzy modify:
        // hideFlags = HideFlags.None;
        this.bone = transform;
    }

    public void Init(int boneIndex, string boneGUID)
    {
        this.boneIndex = boneIndex;
        this.boneGUID = boneGUID;
    }
}

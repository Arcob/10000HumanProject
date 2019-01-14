using UnityEngine;
using System.Collections;

/// <summary>
/// 保存所有采样动画片段信息的数据结构
/// </summary>
public class GPUSkinningAnimation : ScriptableObject
{
    //分配唯一标识符
    public string guid = null;

    //动画集合名称
    public string animName = null;

    //模型网格
    //public GPUSkinningMesh[] meshes = null;
    
    //动画集合所影响的骨骼
    //fzy remark:bones.bindpose注意
    public GPUSkinningBone[] bones = null;
    public int rootBoneIndex = 0;

    //动画集合包括动画片段数据
    public GPUSkinningClip[] clips = null;

    public Bounds bounds;

    #region 动画数据压缩为纹理
    public int textureWidth = 0;

    public int textureHeight = 0;
    #endregion

    #region LOD & Culling相关
    public float[] lodDistances = null;

    public Mesh[] lodMeshes = null;

    public float sphereRadius = 1.0f;
    #endregion
}

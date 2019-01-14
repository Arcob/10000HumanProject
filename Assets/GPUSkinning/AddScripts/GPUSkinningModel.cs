using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 同一个模型/动画集合使用的网格数据
/// </summary>
public class GPUSkinningModel : ScriptableObject
{
    GPUSkinningAnimation[] anims;

    Material[] materials;

    Mesh[] meshes;

    TextAsset[] animationTextures;

    #region obsolete
    /*
    /// <summary>
    /// 网格名称
    /// </summary>
    public string meshName = null;
    /// <summary>
    /// 网格
    /// </summary>
    public Mesh mesh = null;
    /// <summary>
    /// 网格使用的材质（可能有多个，这里暂时使用一个）
    /// </summary>
    public Material mtr = null;
    /// <summary>
    /// 网格包围盒
    /// </summary>
    public Bounds bounds;

    /// <summary>
    /// 动画集合所影响的骨骼
    /// </summary>
    public GPUSkinningBone[] bones = null;
    public int rootBoneIndex = 0;

    /// <summary>
    /// 网格动画数据
    /// </summary>
    public GPUSkinningClip[] clips = null;

    #region 动画数据压缩纹理的相关参数
    /// <summary>
    /// 动画数据的压缩纹理
    /// </summary>
    public TextAsset textureRawData = null;
    public int textureWidth = 0;
    public int textureHeight = 0;
    #endregion

    //LOD待修改*/
    #endregion
}

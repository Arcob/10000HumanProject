using UnityEngine;
using System.Collections;

[System.Serializable]
public class GPUSkinningBone
{
	[System.NonSerialized]
	public Transform transform = null;

    /// <summary>
    /// 移动至GPUSkinningMesh中实现
    /// </summary>
	public Matrix4x4 bindpose;

	public int parentBoneIndex = -1;

	public int[] childrenBonesIndices = null;

	public string name = null;

    public string guid = null; 

    //是否暴露?
    public bool isExposed = false;

    //bindPose逆矩阵，用于原始姿态绑定
    [System.NonSerialized]
    private bool bindposeInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 bindposeInv;
    public Matrix4x4 BindposeInv
    {
        get
        {
            if(!bindposeInvInit)
            {
                bindposeInv = bindpose.inverse;
                bindposeInvInit = true;
            }
            return bindposeInv;
        }
    }
}

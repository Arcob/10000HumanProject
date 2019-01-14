using UnityEngine;
using System.Collections;

[System.Serializable]
public class GPUSkinningFrame
{
    //各个骨骼的变换矩阵
    public Matrix4x4[] matrices = null;

    //RootMotion的位移四元数？
    public Quaternion rootMotionDeltaPositionQ;

    //RootMotion的位移距离
    public float rootMotionDeltaPositionL;

    //RootMotion的旋转四元数
    public Quaternion rootMotionDeltaRotation;

    [System.NonSerialized]
    private bool rootMotionInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 rootMotionInv;
    public Matrix4x4 RootMotionInv(int rootBoneIndex)
    {
        if (!rootMotionInvInit)
        {
            rootMotionInv = matrices[rootBoneIndex].inverse;
            rootMotionInvInit = true;
        }
        return rootMotionInv;
    }
}

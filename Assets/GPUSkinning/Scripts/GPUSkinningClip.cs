using UnityEngine;
using System.Collections;

/// <summary>
/// 采样的动画片段数据
/// </summary>
[System.Serializable]
public class GPUSkinningClip
{
    public string name = null;

    public float length = 0.0f;

    public int fps = 0;

    public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;

    public GPUSkinningFrame[] frames = null;

    /// <summary>
    /// 动画数据对应的初始纹理像素位置
    /// </summary>
    public int pixelSegmentation = 0;

    public bool rootMotionEnabled = false;

    public bool individualDifferenceEnabled = false;

    public GPUSkinningAnimEvent[] events = null;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 判断当前帧是否执行过的数据结构
/// </summary>
public class GPUSkinningExecuteOncePerFrame
{
    private int frameCount = -1;

    //判断该帧是否执行
    public bool CanBeExecute()
    {
        if (Application.isPlaying)
        {
            return frameCount != Time.frameCount;
        }
        else
        {
            return true;
        }
    }

    //执行后设置参数，表示该帧已执行
    public void MarkAsExecuted()
    {
        if (Application.isPlaying)
        {
            frameCount = Time.frameCount;
        }
    }
}

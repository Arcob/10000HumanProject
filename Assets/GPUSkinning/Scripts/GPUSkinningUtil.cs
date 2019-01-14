using UnityEngine;
using System.Collections;
using System.Security.Cryptography;

public class GPUSkinningUtil
{
    public static void MarkAllScenesDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
            DelayCall = () =>
            {
                // fzy Error：This cannot be used during play mode
                // 但是函数并未在Appliction.Playing执行
                // 作用：场景更变为未保存（Dirty->修改、未保存）
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                // 回调函数执行一次后解除
                UnityEditor.EditorApplication.delayCall -= DelayCall;
            };
            // 在所有Inspectors更新后调用一次
            UnityEditor.EditorApplication.delayCall += DelayCall;
        }
#endif
    }

    //从TextAsset创建Animation对应的Texture
    public static Texture2D CreateTexture2D(TextAsset textureRawData, GPUSkinningAnimation anim)
    {
        if(textureRawData == null || anim == null)
        {
            return null;
        }

        Texture2D texture = new Texture2D(anim.textureWidth, anim.textureHeight, TextureFormat.RGBAHalf, false, true);
        texture.name = "GPUSkinningTextureMatrix";
        // 点过滤
        texture.filterMode = FilterMode.Point;
        //从RawTexture中读取Texture的bytes数据（加载压缩后的纹理数据，生成新的2D纹理）
        texture.LoadRawTextureData(textureRawData.bytes);

        //false：不重新计算纹理映射
        //true：纹理不再可读，texture发送到GPU后内存将被释放
        texture.Apply(false, true);

        return texture;
    }

    public static string BonesHierarchyTree(GPUSkinningAnimation gpuSkinningAnimation)
    {
        if(gpuSkinningAnimation == null || gpuSkinningAnimation.bones == null)
        {
            return null;
        }

        string str = string.Empty;
        BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[gpuSkinningAnimation.rootBoneIndex], string.Empty, ref str);
        return str;
    }

    public static void BonesHierarchy_Internal(GPUSkinningAnimation gpuSkinningAnimation, GPUSkinningBone bone, string tabs, ref string str)
    {
        str += tabs + bone.name + "\n";

        int numChildren = bone.childrenBonesIndices == null ? 0 : bone.childrenBonesIndices.Length;
        for(int i = 0; i < numChildren; ++i)
        {
            BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[bone.childrenBonesIndices[i]], tabs + "    ", ref str);
        }
    }

    /// <summary>
    /// 获取Bone Hierarchy路径字符串
    /// </summary>
    public static string BoneHierarchyPath(GPUSkinningBone[] bones, int boneIndex)
    {
        if (bones == null || boneIndex < 0 || boneIndex >= bones.Length)
        {
            return null;
        }

        GPUSkinningBone bone = bones[boneIndex];
        string path = bone.name;
        //父骨骼不为空，一直迭代
        while (bone.parentBoneIndex != -1)
        {
            bone = bones[bone.parentBoneIndex];
            path = bone.name + "/" + path;
        }
        return path;
    }

    public static string BoneHierarchyPath(GPUSkinningAnimation gpuSkinningAnimation, int boneIndex)
    {
        if(gpuSkinningAnimation == null)
        {
            return null;
        }

        return BoneHierarchyPath(gpuSkinningAnimation.bones, boneIndex);
    }

    public static string MD5(string input)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] bytValue, bytHash;
        bytValue = System.Text.Encoding.UTF8.GetBytes(input);
        bytHash = md5.ComputeHash(bytValue);
        md5.Clear();
        string sTemp = string.Empty;
        
        //转换为16进制
        for (int i = 0; i < bytHash.Length; i++)
        {
            sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
        }
        return sTemp.ToLower();
    }

    public static int NormalizeTimeToFrameIndex(GPUSkinningClip clip, float normalizedTime)
    {
        if(clip == null)
        {
            return 0;
        }

        normalizedTime = Mathf.Clamp01(normalizedTime);
        return (int)(normalizedTime * (clip.length * clip.fps - 1));
    }

    public static float FrameIndexToNormalizedTime(GPUSkinningClip clip, int frameIndex)
    {
        if(clip == null)
        {
            return 0;
        }

        int totalFrams = (int)(clip.fps * clip.length);
        frameIndex = Mathf.Clamp(frameIndex, 0, totalFrams - 1);
        return (float)frameIndex / (float)(totalFrams - 1);
    }
}

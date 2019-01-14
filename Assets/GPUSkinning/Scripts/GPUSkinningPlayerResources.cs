using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GPUSkinningPlayerResources
{
    public enum MaterialState
    {
        RootOn_BlendOff = 0, 
        RootOn_BlendOn_CrossFadeRootOn,
        RootOn_BlendOn_CrossFadeRootOff,
        RootOff_BlendOff,
        RootOff_BlendOn_CrossFadeRootOn,
        RootOff_BlendOn_CrossFadeRootOff, 
        Count = 6
    }

    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public Texture2D texture = null;

    //使用该资源的PlayerMono
    public List<GPUSkinningPlayerMono> players = new List<GPUSkinningPlayerMono>();

    private CullingGroup cullingGroup = null;

    private GPUSkinningBetterList<BoundingSphere> cullingBounds = new GPUSkinningBetterList<BoundingSphere>(100);

    //设置6种不同的Material,供不同情况选择传递给MeshRenderer组件
    private GPUSkinningMaterial[] mtrls = null;

    private static string[] keywords = new string[] {
        "ROOTON_BLENDOFF", "ROOTON_BLENDON_CROSSFADEROOTON", "ROOTON_BLENDON_CROSSFADEROOTOFF",
        "ROOTOFF_BLENDOFF", "ROOTOFF_BLENDON_CROSSFADEROOTON", "ROOTOFF_BLENDON_CROSSFADEROOTOFF" };

    private GPUSkinningExecuteOncePerFrame executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();

    private float time = 0;
    public float Time
    {
        get
        {
            return time;
        }
        set
        {
            time = value;
        }
    }

    //Shader属性索引（比Material.SetProperty访问速度快）
    public static int shaderPropID_GPUSkinning_TextureMatrix = -1;

    public static int shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame = 0;

    public static int shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation = 0;

    public static int shaderPropID_GPUSkinning_RootMotion = 0;

    public static int shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = 0;

    public static int shaderPropID_GPUSkinning_RootMotion_CrossFade = 0;

    public GPUSkinningPlayerResources()
    {
        if (shaderPropID_GPUSkinning_TextureMatrix == -1)
        {
            shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_TextureSize_NumPixelsPerFrame");
            shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");
            shaderPropID_GPUSkinning_RootMotion = Shader.PropertyToID("_GPUSkinning_RootMotion");
            shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade");
            shaderPropID_GPUSkinning_RootMotion_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotion_CrossFade");
        }
    }

    ~GPUSkinningPlayerResources()
    {
        DestroyCullingGroup();
    }

    public void Destroy()
    {
        anim = null;
        mesh = null;

        if(cullingBounds != null)
        {
            cullingBounds.Release();
            cullingBounds = null;
        }

        DestroyCullingGroup();

        if(mtrls != null)
        {
            for(int i = 0; i < mtrls.Length; ++i)
            {
                mtrls[i].Destroy();
                mtrls[i] = null;
            }
            mtrls = null;
        }

        if (texture != null)
        {
            Object.DestroyImmediate(texture);
            texture = null;
        }

        if (players != null)
        {
            players.Clear();
            players = null;
        }
    }

    /// <summary>
    /// 为新的PlayerMono添加/设置CullingBounds
    /// cullingGroup的目标相机设置为Camera.main（可能需要修改）
    /// </summary>
    public void AddCullingBounds()
    {
        if (cullingGroup == null)
        {
            cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = Camera.main;
            //设置LOD和Culling距离
            cullingGroup.SetBoundingDistances(anim.lodDistances);
            //计算Distance的初始位置（Transform索引或Vector3固定点）
            cullingGroup.SetDistanceReferencePoint(Camera.main.transform);
            //距离发生改变时调用的回调函数
            cullingGroup.onStateChanged = OnLodCullingGroupOnStateChangedHandler;
        }

        //为新的PlayerMono添加一个BoundingSphere，如何建立索引？（与Players的索引相同？）
        cullingBounds.Add(new BoundingSphere());
        cullingGroup.SetBoundingSpheres(cullingBounds.buffer);
        cullingGroup.SetBoundingSphereCount(players.Count);
    }

    public void RemoveCullingBounds(int index)
    {
        cullingBounds.RemoveAt(index);
        cullingGroup.SetBoundingSpheres(cullingBounds.buffer);
        cullingGroup.SetBoundingSphereCount(players.Count);
    }

    /// <summary>
    /// LODSetting发生变化时调用
    /// fzy：LOD功能关闭
    /// </summary>
    /// <param name="player"></param>
    public void LODSettingChanged(GPUSkinningPlayer player)
    {
        //fzy add：
        player.SetLODMesh(null);
        return;
        
        if(player.LODEnabled)
        {
            int numPlayers = players.Count;
            for(int i = 0; i < numPlayers; ++i)
            {
                if(players[i].Player == player)
                {
                    int distanceIndex = cullingGroup.GetDistance(i);
                    SetLODMeshByDistanceIndex(distanceIndex, players[i].Player);
                    break;
                }
            }
        }
        else
        {
            player.SetLODMesh(null);
        }
    }
    
    /// <summary>
    /// 计算是否可见，这里关闭这个功能
    /// </summary>
    /// <param name="evt"></param>
    private void OnLodCullingGroupOnStateChangedHandler(CullingGroupEvent evt)
    {
        GPUSkinningPlayerMono player = players[evt.index];
        if(evt.isVisible)
        {
            SetLODMeshByDistanceIndex(evt.currentDistance, player.Player);
            player.Player.Visible = true;
        }
        else
        {
            player.Player.Visible = false;
        }
    }

    private void DestroyCullingGroup()
    {
        if (cullingGroup != null)
        {
            cullingGroup.Dispose();
            cullingGroup = null;
        }
    }

    private void SetLODMeshByDistanceIndex(int index, GPUSkinningPlayer player)
    {
        Mesh lodMesh = null;
        if (index == 0)
        {
            lodMesh = this.mesh;
        }
        else
        {
            Mesh[] lodMeshes = anim.lodMeshes;
            lodMesh = lodMeshes == null || lodMeshes.Length == 0 ? this.mesh : lodMeshes[Mathf.Min(index - 1, lodMeshes.Length - 1)];
            if (lodMesh == null) lodMesh = this.mesh;
        }
        player.SetLODMesh(lodMesh);
    }

    // fzy remark:循环次数多导致CPU消耗提升
    private void UpdateCullingBounds()
    {
        //Profiler.BeginSample("res.UpdateCullingBounds");
        int numPlayers = players.Count;
        for (int i = 0; i < numPlayers; ++i)
        {
            GPUSkinningPlayerMono player = players[i];
            BoundingSphere bounds = cullingBounds[i];
            bounds.position = player.Player.Position;
            bounds.radius = anim.sphereRadius;
            cullingBounds[i] = bounds;
        }
        //Profiler.EndSample();
    }

    // fzy remark:CPU资源消耗过多
    public void Update(float deltaTime, GPUSkinningMaterial mtrl)
    {
        //Profiler.BeginSample("GPUSkinningPlayerResources.Update()_Internal");

        //Profiler.BeginSample("GPUSkinningPlayerResources.UpdateCullingBounds");
        
        //多个Player可能共享同一个Resources，保证每个Resources在同一帧内只执行一次
        if (executeOncePerFrame.CanBeExecute())
        {
            executeOncePerFrame.MarkAsExecuted();
            time += deltaTime;
            //fzy delete:注释以后不播放动画（猜测：未更新CullingBounds信息，默认判断模型动画出了Camera视锥区域）
            //更新CullingBounds信息（与LOD、Camera Culling相关）
            //UpdateCullingBounds();

            //fzy log
            //Debug.Log(cullingBounds.size);
            //if (cullingBounds.size != 0)
            //{
            //    for (int i = 0; i < cullingBounds.size; ++i)
            //    {
            //        Debug.Log(cullingBounds[i]);
            //    }
            //}
        }
        //Profiler.EndSample();

        //Profiler.BeginSample("GPUSkinningPlayerResources.executeOncePerFrame");

        //Material的更新可能在一帧内调用多次？
        if (mtrl.executeOncePerFrame.CanBeExecute())
        {
            mtrl.executeOncePerFrame.MarkAsExecuted();
            // texture在PlayerMono或PlayerMonoManager中赋值，该帧对应的Material属性是新创建的，未赋值？
            // 初始化时赋值
            // mtrl.material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
            // 设置当前动画片段帧对应的动画Texture信息
            //mtrl.material.SetVector(shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame, 
            //    new Vector4(anim.textureWidth, anim.textureHeight, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
        }
        //Profiler.EndSample();

        //Profiler.EndSample();
    }

    /// <summary>
    /// 设置动画片段的Material->Shader属性，使用GPU计算动画片段数据
    /// </summary>
    /// <param name="mpb"> 需要设置的MaterialPropertyBlock </param>
    /// <param name="playingClip"> 当前播放的动画片段 </param>
    /// <param name="frameIndex"> 帧索引 </param>
    /// <param name="frame"> 帧数据 </param>
    /// <param name="rootMotionEnabled"> 是否应用RootMotion </param>
    /// <param name="lastPlayedClip"> 上一次播放的动画片段 </param>
    /// <param name="frameIndex_crossFade"> 需要CrossFade的上一个动画片段帧索引（上一次播放到的动画片段） </param>
    /// <param name="crossFadeTime"> CrossFade所花的时间 </param>
    /// <param name="crossFadeProgress"> CrossFade已经处理的时间 </param>
    public void UpdatePlayingData(
        MaterialPropertyBlock mpb, GPUSkinningClip playingClip, int frameIndex, GPUSkinningFrame frame, bool rootMotionEnabled,
        GPUSkinningClip lastPlayedClip, int frameIndex_crossFade, float crossFadeTime, float crossFadeProgress)
    {
        //Profiler.BeginSample("Res.UpdatePlayingData");

        //设置mpb的Vector属性
        mpb.SetVector(shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0));
        //如果开启RootMotion
        if (rootMotionEnabled)
        {
            Matrix4x4 rootMotionInv = frame.RootMotionInv(anim.rootBoneIndex);
            mpb.SetMatrix(shaderPropID_GPUSkinning_RootMotion, rootMotionInv);
        }

        //如果开启CrossFade
        if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            if (lastPlayedClip.rootMotionEnabled)
            {
                mpb.SetMatrix(shaderPropID_GPUSkinning_RootMotion_CrossFade, lastPlayedClip.frames[frameIndex_crossFade].RootMotionInv(anim.rootBoneIndex));
            }

            mpb.SetVector(shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade,
                new Vector4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime)));
        }


        //Profiler.EndSample();
    }

    public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
    {
        return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
    }

    //判断是否应用CrossFadeBlending
    public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, 
        float crossFadeTime, 
        float crossFadeProgress)
    {
        return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
    }

    public GPUSkinningMaterial GetMaterial(MaterialState state)
    {
        return mtrls[(int)state];
    }

    //根据初始Material设置GPUSkinningMaterial数组
    public void InitMaterial(Material originalMaterial, HideFlags hideFlags)
    {
        if(mtrls != null)
        {
            return;
        }

        Debug.Log("InitMaterial");

        //6种，与Keywords[]数组对应
        mtrls = new GPUSkinningMaterial[(int)MaterialState.Count];

        for (int i = 0; i < mtrls.Length; ++i)
        {
            mtrls[i] = new GPUSkinningMaterial() { material = new Material(originalMaterial) };
            mtrls[i].material.name = keywords[i];
            mtrls[i].material.hideFlags = hideFlags;

            //fzy add optimize:从SetTexture移动到这，没必要每帧调用SetTexture
            mtrls[i].material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
            mtrls[i].material.SetVector(shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame,
            new Vector4(anim.textureWidth, anim.textureHeight, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
            // GPU Instancing 开关
#if UNITY_5_6
            mtrls[i].material.enableInstancing = true; // enable instancing in Unity 5.6
#endif
            EnableKeywords(i, mtrls[i]);
        }
    }

    /// <summary>
    /// 根据Root、Blend、CrossFade的开关，设置Material->Shader的变量启用和禁用
    /// </summary>
    /// <param name="ki"> 第ki个Keyword </param>
    /// <param name="mtrl"> 需要设置的Material </param>
    private void EnableKeywords(int ki, GPUSkinningMaterial mtrl)
    {
        for(int i = 0; i < mtrls.Length; ++i)
        {
            if(i == ki)
            {
                mtrl.material.EnableKeyword(keywords[i]);
            }
            else
            {
                mtrl.material.DisableKeyword(keywords[i]);
            }
        }
    }












    /// <summary>
    /// fzy add/remark：
    /// 每帧调用次数与网格个数相关，已将该实现代码移至初始化时赋值（作者为何要每帧调用）
    /// </summary>
    public void SetTexture(GPUSkinningMaterial currMtrl)
    {
        //Profiler.BeginSample("Res.SetTexture");
        //currMtrl.material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);

        // 设置当前动画片段帧对应的动画Texture信息
        
        //currMtrl.material.SetVector(shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame,
        //    new Vector4(anim.textureWidth, anim.textureHeight, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
        //Profiler.EndSample();
    }
}

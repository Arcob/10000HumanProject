using System;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;

public class GPUSkinningController : MonoBehaviour
{
    // 已初始化的Controller数量，用于分配ControllerID
    public static int controllerNum = 0;
    private int controllerID;
    private int updateRemark;
    public float playSpeed = 3.0f;

    // 是否启用多线程优化
    public bool isThread;

    // Debug标记
    public bool isDebug = false;

    public GPUSkinningAnimation anim;
    private GPUSkinningClip[] animClips;
    public string defaultPlayingClipName = "idle";
    public bool rootMotionEnabled = false;
    public bool isTimeDiff = false;

    #region 动画片段播放相关变量
    /// <summary>
    /// 当前动画片段已经播放的时间
    /// </summary>
    private float time = 0;
    /// <summary>
    /// 动画片段初始播放偏差
    /// </summary>
    private float timeDiff = 0;

    /// <summary>
    /// fzy add，res.time相同
    /// </summary>
    private float resTime = 0;

    /// <summary>
    /// 淡入淡出的总时间
    /// </summary>
    private float crossFadeTime = -1;

    /// <summary>
    /// 淡入淡出已经经过的时间
    /// </summary>
    private float crossFadeProgress = -1;

    /// <summary>
    /// 上一个播放的动画片段
    /// </summary>
    private GPUSkinningClip lastPlayedClip = null;

    /// <summary>
    /// 距上一个动画片段停止播放所经过的时间
    /// </summary>
    private float lastPlayedTime = 0.0f;

    /// <summary>
    /// 上一帧播放的动画片段
    /// </summary>
    private GPUSkinningClip lastPlayingClip = null;

    /// <summary>
    /// 上一帧播放的动画片段帧数
    /// </summary>
    private int lastPlayingFrameIndex = -1;

    /// <summary>
    /// 当前帧播放的动画片段
    /// </summary>
    /// 注意，造成内存过大的原因
    [System.NonSerialized]
    public GPUSkinningClip playingClip = null;

    [SerializeField]
    private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;
    public GPUSKinningCullingMode CullingMode
    {
        get { return Application.isPlaying ? cullingMode : GPUSKinningCullingMode.AlwaysAnimate; }
        set { cullingMode = value; }
    }
    /*LOD相关暂未实现*/
    private bool visible = false;
    public bool Visible
    {
        get
        {
            if (isThread) return true;
            return Application.isPlaying ? visible : true;
        }
        set
        {
            visible = value;
        }
    }

    [SerializeField]
    private bool isPlaying;
    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    public string PlayingClipName
    {
        get { return playingClip == null ? null : playingClip.name; }
    }
    public GPUSkinningWrapMode PlayingClipWrapMode
    {
        get { return playingClip == null ? GPUSkinningWrapMode.Once : playingClip.wrapMode; }
    }

    public bool IsTimeAtTheEndOfLoop
    {
        get
        {
            if (playingClip == null)
            {
                return false;
            }
            else
            {
                return GetFrameIndex() == ((int)(playingClip.length * playingClip.fps) - 1);
            }
        }
    }

    #endregion
    private GPUSkinningPlayerMono[] playerMonos;
    internal int playerMonosCount;

    internal MeshRenderer[] mrs;
    private GPUSkinningPlayerResources[] resources;
    private GPUSkinningMaterial[] currMtrils;
    internal MaterialPropertyBlock[] mpbs;


    // 线程时间记录
    private DateTime lastThreadTime;
    // 线程随机
    System.Random rnd = new System.Random();


    private void Awake()
    {
        if (FDUClusterAppToolKits.FduSupportClass.isMaster)
        {

            controllerID = controllerNum++;

            if (anim == null)
                Debug.LogWarning("未设置Anim属性");

            animClips = anim.clips;
            //始终可见
            visible = true;
            isPlaying = false;
            foreach (GPUSkinningClip clip in animClips)
            {
                if (clip.name == defaultPlayingClipName)
                {
                    SetCurPlayingClip(clip);
                    break;
                }
            }
        }

        playerMonos = GetComponentsInChildren<GPUSkinningPlayerMono>();
        playerMonosCount = playerMonos.Length;


        // 模型资源引用（数量对应网格数量）
        mrs = new MeshRenderer[playerMonosCount];
        resources = new GPUSkinningPlayerResources[playerMonosCount];
        currMtrils = new GPUSkinningMaterial[playerMonosCount];
        mpbs = new MaterialPropertyBlock[playerMonosCount];
        for (int i = 0; i < playerMonosCount; i++)
        {
            playerMonos[i].Init();

            mrs[i] = playerMonos[i].mr;
            mrs[i].sortingOrder = i;
            resources[i] = playerMonos[i].myRes;
            currMtrils[i] = playerMonos[i].currMtrl;
            mpbs[i] = playerMonos[i].mpb;
        }

        if (FDUClusterAppToolKits.FduSupportClass.isMaster)
        {
            // 线程任务分配，设置处理该Controller相关计算的线程
            if (isThread)
            {
                SetThread();
                lastThreadTime = DateTime.Now;
            }
        }
    }

    float deltaTime;
    
    #region Update函数
    // 对应GPUSkinningPlayer的Update_Internal(float deltaTime)函数
    // 每个模型调用一次Update，消耗过大，如果需要合并，禁用Update代码（默认禁用）
    //void Update()
    //{
    //    if (!isPlaying || playingClip == null)
    //        return;
    //    // 启用多线程优化
    //    if (isThread)
    //    {
    //        if (Time.frameCount % updatePreFrame == updateRemark)
    //            UpdatePlayingData(framePixelSegmentation, rootMotionInv, isRootMotionEnabled, rootMotionInv_Last, framePixelSegmentation_Blend_CrossFade);
    //        return;
    //    }

    //    //set多次消耗性能，转移到Play和CrossFade函数中调用
    //    //SetMaterial();
    //    //Profiler.BeginSample("UpdateMaterial");
    //    deltaTime = Time.deltaTime;
    //    if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
    //    {
    //        UpdateMaterial(deltaTime);
    //    }
    //    else if (playingClip.wrapMode == GPUSkinningWrapMode.Once)
    //    {
    //        if (time >= playingClip.length)
    //        {
    //            time = playingClip.length;
    //            UpdateMaterial(deltaTime);
    //        }
    //        else
    //        {
    //            UpdateMaterial(deltaTime);
    //            time += deltaTime;
    //            if (time > playingClip.length)
    //            {
    //                time = playingClip.length;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //    //Profiler.EndSample();

    //    crossFadeProgress += Time.deltaTime;
    //    lastPlayedTime += Time.deltaTime;
    //}
    #endregion


    #region Thread Version
    // 与上次线程执行经过的时间
    //int deltaMilliSecond_Int = 16;
    //float deltaSecond_Float = 0.016f;
    //Thread thread;

    // 调用线程管理器的任务分配函数
    void SetThread()
    {
        // 平均分配
        //ThreadManager.Instance.SetThreadJob(ThreadFunction, controllerID);
        // 让最后一个线程执行（最后一个线程还负责Kd-tree构建）
        ThreadManager.Instance.SetThreadJobWithThreadID(ThreadFunction, ThreadManager.Instance.threadNum - 1);
    }

    // 线程需要处理的计算
    void ThreadFunction()
    {
        if (!isPlaying || playingClip == null)
            return;

        // 计算与上次线程计算所经过的时间
        deltaTime = (float)DateTime.Now.Subtract(lastThreadTime).TotalSeconds;
        lastThreadTime = DateTime.Now;
        // 倍乘播放速度
        deltaTime = deltaTime * playSpeed;

        if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            UpdateMaterial_Thread(deltaTime);
        }
        else if (playingClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (time >= playingClip.length)
            {
                time = playingClip.length;
                UpdateMaterial_Thread(deltaTime);
            }
            else
            {
                UpdateMaterial_Thread(deltaTime);
                time += deltaTime;
                if (time > playingClip.length)
                {
                    time = playingClip.length;
                }
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }


        crossFadeProgress += deltaTime;
        lastPlayedTime += deltaTime;
        
        //    Thread.Sleep(deltaMilliSecond_Int);
        //}
    }

    private void UpdateMaterial_Thread(float deltaTime)
    {
        int frameIndex = GetFrameIndex();
        //动画片段（WrapMode.Once）播放完毕
        if (lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
        {
            resTime += deltaTime;
            //for (int i = 0; i < playerMonosCount; i++)
            //    playerMonos[i].UpdateMaterial(deltaTime);
            return;
        }

        //记录上一帧播放的动画片段
        lastPlayingClip = playingClip;
        //记录上一次播放的动画片段帧数（有可能跳帧）
        lastPlayingFrameIndex = frameIndex;

        blend_crossFade = 1;
        frameIndex_crossFade = -1;
        // 新建动画帧，用于crossFade
        GPUSkinningFrame frame_crossFade = null;

        if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            frameIndex_crossFade = GetCrossFadeFrameIndex();
            frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            blend_crossFade = CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
        }

        GPUSkinningFrame frame = playingClip.frames[frameIndex];

        if (Visible ||
            CullingMode == GPUSKinningCullingMode.AlwaysAnimate)
        {
            resTime += deltaTime;

            //计算临时变量
            isRootMotionEnabled = playingClip.rootMotionEnabled && rootMotionEnabled;
            framePixelSegmentation = new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0);
            if (lastPlayedClip != null && frameIndex_crossFade >= 0)
            {
                framePixelSegmentation_Blend_CrossFade = new Vector4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime));
                rootMotionInv_Last = lastPlayedClip.frames[frameIndex_crossFade].RootMotionInv(anim.rootBoneIndex);
            }
            //Unity API无法在线程内调用
            //UpdatePlayingData(framePixelSegmentation, rootMotionInv, isRootMotionEnabled, rootMotionInv_Last, framePixelSegmentation_Blend_CrossFade);
        }
    }
    #endregion

    /// <summary>
    /// fzy add:每帧计算的临时变量
    /// </summary>
    float blend_crossFade;
    int frameIndex_crossFade;
    bool isRootMotionEnabled;
    internal Vector4 framePixelSegmentation;
    internal Vector4 _tempFramePixelSegmentation;
    Vector4 framePixelSegmentation_Blend_CrossFade;
    Matrix4x4 rootMotionInv;
    Matrix4x4 rootMotionInv_Last;
    /// <summary>
    /// 根据当前动画片段播放状态设置Material
    /// fzy remak:CPU资源消耗过多
    /// </summary>
    /// <param name="deltaTime">本帧Update消耗的时间</param>
    private void UpdateMaterial(float deltaTime)
    {
        //Profiler.BeginSample("UpdateMaterial.Other");
        int frameIndex = GetFrameIndex();

        //动画片段（WrapMode.Once）播放完毕
        if (lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
        {
            //res.Update(deltaTime);
            resTime += deltaTime;
            //for (int i = 0; i < playerMonosCount; i++)
            //    playerMonos[i].UpdateMaterial(deltaTime);
            return;
        }

        //记录上一帧播放的动画片段
        lastPlayingClip = playingClip;
        //记录上一次播放的动画片段帧数（有可能跳帧）
        lastPlayingFrameIndex = frameIndex;

        blend_crossFade = 1;
        frameIndex_crossFade = -1;
        // 新建动画帧，用于crossFade
        GPUSkinningFrame frame_crossFade = null;

        if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            frameIndex_crossFade = GetCrossFadeFrameIndex();
            frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            blend_crossFade = CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
        }

        GPUSkinningFrame frame = playingClip.frames[frameIndex];

        //Profiler.EndSample();

        //模型可以被看见（Culling）或者CullingMode为AlwaysAnimate
        if (Visible ||
            CullingMode == GPUSKinningCullingMode.AlwaysAnimate)
        {
            //author version
            //res.Update(deltaTime);
            resTime += deltaTime;
            //fzy remark：不需要每帧调用，已在初始化时设置属性
            //for (int i = 0; i < playerMonosCount; i++)
            //{
            //    playerMonos[i].UpdateMaterial(deltaTime);
            //}

            /*author version
            res.UpdatePlayingData(
                mpb, playingClip, frameIndex, frame, playingClip.rootMotionEnabled && rootMotionEnabled,
                lastPlayedClip, GetCrossFadeFrameIndex(), crossFadeTime, crossFadeProgress
            );
            mr.SetPropertyBlock(mpb);*/

            //if (Time.frameCount % updatePreFrame != updateRemark) return;
            //Profiler.BeginSample("UpdatePlayingData");

            //计算临时变量
            isRootMotionEnabled = playingClip.rootMotionEnabled && rootMotionEnabled;
            framePixelSegmentation = new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0);
            rootMotionInv = frame.RootMotionInv(anim.rootBoneIndex);
            if (lastPlayedClip != null && frameIndex_crossFade >= 0)
            {
                framePixelSegmentation_Blend_CrossFade = new Vector4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime));
                rootMotionInv_Last = lastPlayedClip.frames[frameIndex_crossFade].RootMotionInv(anim.rootBoneIndex);
            }

            //设置动画播放的Material数据
            //UpdatePlayingData(framePixelSegmentation, rootMotionInv, isRootMotionEnabled, rootMotionInv_Last, framePixelSegmentation_Blend_CrossFade);
            UpdatePlayingData();


            //Profiler.EndSample();

            //bone.isExposed
            //UpdateJoints(frame);
        }

        //Profiler.BeginSample("RootMotion");
        /*
        if (playingClip.rootMotionEnabled && rootMotionEnabled && frameIndex != rootMotionFrameIndex)
        {
            if (CullingMode != GPUSKinningCullingMode.CullCompletely)
            {
                rootMotionFrameIndex = frameIndex;
                DoRootMotion(frame_crossFade, 1 - blend_crossFade, false);
                DoRootMotion(frame, blend_crossFade, true);
            }
        }*/
        //Profiler.EndSample();

        //UpdateEvents(playingClip, frameIndex, frame_crossFade == null ? null : lastPlayedClip, frameIndex_crossFade);
    }

    /// <summary>
    /// fzy note:核心函数
    /// </summary>
    public void UpdatePlayingData()
    {
        //UpdatePlayingData(framePixelSegmentation, rootMotionInv, isRootMotionEnabled, rootMotionInv_Last, framePixelSegmentation_Blend_CrossFade);
        for (int i = 0; i < playerMonosCount; i++)
        {
            mpbs[i].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, framePixelSegmentation);
            mrs[i].SetPropertyBlock(mpbs[i]);
        }
    }

    /// <summary>
    /// Called in slave node.
    /// </summary>
    /// <param name="v4"></param>
    public void SetAnimationDataForSlave(Vector4 animationV4)
    {
        for (int i = 0; i < playerMonosCount; i++)
        {
            mpbs[i].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, animationV4);
            mrs[i].SetPropertyBlock(mpbs[i]);
        }
    }


    private void UpdatePlayingData(Vector4 m_framePixelSegmentation, Matrix4x4 m_rootMotionInv, bool m_isRootMotionEnabled,
        Matrix4x4 m_rootMotionInv_Last, Vector4 m_framePixelSegmentation_Blend_CrossFade)
    {
        for (int i = 0; i < playerMonosCount; i++)
            mpbs[i].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, m_framePixelSegmentation);
        if (m_isRootMotionEnabled)
        {
            for (int i = 0; i < playerMonosCount; i++)
            {
                mpbs[i].SetMatrix(GPUSkinningPlayerResources.shaderPropID_GPUSkinning_RootMotion, m_rootMotionInv);
            }
        }
        /*
        if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            for (int i = 0; i < playerMonosCount; i++)
            {
                if (lastPlayedClip.rootMotionEnabled)
                {
                    mpbs[i].SetMatrix(GPUSkinningPlayerResources.shaderPropID_GPUSkinning_RootMotion_CrossFade, m_rootMotionInv_Last);
                }
                mpbs[i].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade, m_framePixelSegmentation_Blend_CrossFade);
            }
        }*/

        for (int i = 0; i < playerMonosCount; i++)
            mrs[i].SetPropertyBlock(mpbs[i]);

        #region obsolete version
        /*
        for (int i = 0; i < playerMonosCount; i++)
        {
            mpbs[i].SetVector(shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0));
            if (rootMotionEnabled)
            {
                mpbs[i].SetMatrix(GPUSkinningPlayerResources.shaderPropID_GPUSkinning_RootMotion, rootMotionInv);
            }
            if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                if (lastPlayedClip.rootMotionEnabled)
                {
                    mpbs[i].SetMatrix(GPUSkinningPlayerResources.shaderPropID_GPUSkinning_RootMotion_CrossFade, rootMotionInv_Last);
                }
                mpbs[i].SetVector(GPUSkinningPlayerResources.shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade, FramePixelSegmentation_Blend_CrossFade);
            }
            mrs[i].SetPropertyBlock(mpbs[i]);
        }*/
        #endregion
    }

    public void Play(string newClipName)
    {
        foreach (GPUSkinningClip clip in animClips)
        {
            if (clip.name == newClipName)
            {
                if (playingClip == null
                    || playingClip != clip
                    || playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop
                    || isPlaying)
                    SetCurPlayingClip(clip);
            }
        }
    }


    /// <summary>
    /// 设置当前播放的动画片段
    /// </summary>
    /// <param name="newClip"> 新的动画片段 </param>
    private void SetCurPlayingClip(GPUSkinningClip newClip)
    {
        lastPlayedClip = playingClip;
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        playingClip = newClip;
        time = 0;
        //timeDiff = Random.Range(0, playingClip.length);
        if (isTimeDiff)
            timeDiff = ((float)rnd.NextDouble()) * playingClip.length;
        else
            timeDiff = 0.0f;
    }

    /// <summary>
    /// 获取当前动画播放的帧数
    /// </summary>
    /// <returns></returns>
    private int GetFrameIndex()
    {
        //return 0;
        float time = GetCurrentTime();
        if (playingClip.length == time)
            return GetTheLastFrameIndex_WrapMode_Once(playingClip);
        else
            return GetFrameIndex_WrapMode_Loop(playingClip, time);
    }

    /// <summary>
    /// 获得当前动画已经播放的时间
    /// </summary>
    /// <returns></returns>
    private float GetCurrentTime()
    {
        float currentTime = 0;
        if (PlayingClipWrapMode == GPUSkinningWrapMode.Once)
            currentTime = this.time;
        else if (PlayingClipWrapMode == GPUSkinningWrapMode.Loop)
            //currentTime = resTime + (playingClip.individualDifferenceEnabled ? this.timeDiff : 0);
            currentTime = resTime + (isTimeDiff ? this.timeDiff : 0);
        else
        {
            throw new System.NotImplementedException();
        }
        return currentTime;
    }

    /// <summary>
    /// 获取CrossFade相关的帧索引
    /// 与GetFrameIndex类似，获取的不是当前动画，而是上一帧动画的播放帧数
    /// </summary>
    /// <returns> 上一次播放动画的帧数 </returns>
    private int GetCrossFadeFrameIndex()
    {
        if (lastPlayedClip == null)
        {
            return 0;
        }

        if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (lastPlayedTime >= lastPlayedClip.length)
            {
                return GetTheLastFrameIndex_WrapMode_Once(lastPlayedClip);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
            }
        }
        else if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

    /// <summary>
    /// 获取动画片段的最后一帧索引数
    /// </summary>
    /// <param name="clip"></param>
    /// <returns></returns>
    private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
    {
        return (int)(clip.length * clip.fps) - 1;
    }

    /// <summary>
    /// 获取动画片段当前帧的索引（注意考虑循环）
    /// </summary>
    /// <param name="clip"> 当前动画片段 </param>
    /// <param name="time"> 当前动画片段已播放时间 </param>
    /// <returns></returns>
    private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
    {
        return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
    }

    //判断是否应用CrossFadeBlending
    public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip,
        float crossFadeTime,
        float crossFadeProgress)
    {
        return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
    }
    public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
    {
        return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
    }

    #region 设置当前使用的Material种类
    /// <summary>
    /// 根据当前动画状态获取Material
    /// 原本在Update函数中调用，性能消耗较大，应该在动画发生变化时调用，例如Play、或CrossFade函数（尚未实现）
    /// </summary>
    private void SetMaterial()
    {
        if (playingClip == null)
        {
            BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
            return;
        }
        if (playingClip.rootMotionEnabled && rootMotionEnabled)
        {
            if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                if (lastPlayedClip.rootMotionEnabled)
                {
                    BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOn);
                }
                else BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOff);
                return;
            }
            BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOff);
            return;
        }

        if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            if (lastPlayedClip.rootMotionEnabled)
            {
                BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOn);
            }
            else BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOff);
        }
        else
        {
            BatchSetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        }
    }

    private void BatchSetMaterial(GPUSkinningPlayerResources.MaterialState ms)
    {
        foreach (GPUSkinningPlayerMono pm in playerMonos)
            pm.SetMaterial(ms);
    }
    #endregion
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class GPUSkinningPlayer
{
    public delegate void OnAnimEvent(GPUSkinningPlayer player, int eventId);

    //与GPUSkinningPlayer实例关联的游戏对象
    private GameObject go = null;

    private Transform transform = null;

    private MeshRenderer mr = null;

    private MeshFilter mf = null;

    //当前动画播放的时间
    private float time = 0;

    private float timeDiff = 0;

    private float crossFadeTime = -1;

    private float crossFadeProgress = 0;

    private float lastPlayedTime = 0;

    private GPUSkinningClip lastPlayedClip = null;

    private int lastPlayingFrameIndex = -1;

    private GPUSkinningClip lastPlayingClip = null;

    private GPUSkinningClip playingClip = null;

    private GPUSkinningPlayerResources res = null;

    private MaterialPropertyBlock mpb = null;

    private int rootMotionFrameIndex = -1;

    public event OnAnimEvent onAnimEvent;

    private bool rootMotionEnabled = false;
    public bool RootMotionEnabled
    {
        get
        {
            return rootMotionEnabled;
        }
        set
        {
            rootMotionFrameIndex = -1;
            rootMotionEnabled = value;
        }
    }

    private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;
    public GPUSKinningCullingMode CullingMode
    {
        get
        {
            return Application.isPlaying ? cullingMode : GPUSKinningCullingMode.AlwaysAnimate;
        }
        set
        {
            cullingMode = value;
        }
    }

    private bool visible = false;
    public bool Visible
    {
        get
        {
            return Application.isPlaying ? visible : true;
        }
        set
        {
            visible = value;
        }
    }

    private bool lodEnabled = true;
    public bool LODEnabled
    {
        get
        {
            return lodEnabled;
        }
        set
        {
            lodEnabled = value;
            res.LODSettingChanged(this);
        }
    }

    private bool isPlaying = false;
    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
    }

    public string PlayingClipName
    {
        get
        {
            return playingClip == null ? null : playingClip.name;
        }
    }
    
    public Vector3 Position
    {
        get
        {
            return transform == null ? Vector3.zero : transform.position;
        }
    }

    public Vector3 LocalPosition
    {
        get
        {
            return transform == null ? Vector3.zero : transform.localPosition;
        }
    }

    private List<GPUSkinningPlayerJoint> joints = null;
    public List<GPUSkinningPlayerJoint> Joints
    {
        get
        {
            return joints;
        }
    }

    public GPUSkinningWrapMode WrapMode
    {
        get
        {
            return playingClip == null ? GPUSkinningWrapMode.Once : playingClip.wrapMode;
        }
    }

    public bool IsTimeAtTheEndOfLoop
    {
        get
        {
            if(playingClip == null)
            {
                return false;
            }
            else
            {
                return GetFrameIndex() == ((int)(playingClip.length * playingClip.fps) - 1);
            }
        }
    }

    public float NormalizedTime
    {
        get
        {
            if(playingClip == null)
            {
                return 0;
            }
            else
            {
                return (float)GetFrameIndex() / (float)((int)(playingClip.length * playingClip.fps) - 1);
            }
        }
        set
        {
            if(playingClip != null)
            {
                float v = Mathf.Clamp01(value);
                if(WrapMode == GPUSkinningWrapMode.Once)
                {
                    this.time = v * playingClip.length;
                }
                else if(WrapMode == GPUSkinningWrapMode.Loop)
                {
                    if(playingClip.individualDifferenceEnabled)
                    {
                        res.Time = playingClip.length +  v * playingClip.length - this.timeDiff;
                    }
                    else
                    {
                        res.Time = v * playingClip.length;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
        }
    }

    /// <summary>
    /// 在GPUSkinningPlayerMono脚本的Init函数中调用，完成PlayerMono使用的Player实例创建
    /// </summary>
    /// <param name="attachToThisGo">PlayerMono->Object索引</param>
    /// <param name="res"> Player使用的资源 </param>
    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningPlayerResources res)
    {
        //fzy add：
        visible = true;

        go = attachToThisGo;
        transform = go.transform;
        this.res = res;

        mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
        }
        mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
        }

        //初始化时未播放动画，返回Material：RootOff_BlendOff
        GPUSkinningMaterial mtrl = GetCurrentMaterial();
        mr.sharedMaterial = mtrl == null ? null : mtrl.material;
        mf.sharedMesh = res.mesh;

        //初始化MaterialPropertyBlock（为何不在脚本创建时完成初始化？）
        mpb = new MaterialPropertyBlock();

        ConstructJoints();
    }

    /// <summary>
    /// 播放指定的动画（仅设置当前播放的动画PlayingClip）
    /// </summary>
    /// <param name="clipName"> 动画名称 </param>
    public void Play(string clipName)
    {
        GPUSkinningClip[] clips = res.anim.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for(int i = 0; i < numClips; ++i)
        {
            if(clips[i].name == clipName)
            {
                // 更新当前播放动画的条件（三满足一即可）：
                // 1.设置动画不等于当前动画
                // 2.设置/当前动画（仅播放一次）已经播放完毕
                // 3.设置/当前动画未在播放
                // ? 当前动画 == null ?
                if (playingClip != clips[i] || 
                    (playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || 
                    (playingClip != null && !isPlaying))
                {
                    SetNewPlayingClip(clips[i]);
                }
                return;
            }
        }
    }

    public void CrossFade(string clipName, float fadeLength)
    {
        if (playingClip == null)
        {
            Play(clipName);
        }
        else
        {
            GPUSkinningClip[] clips = res.anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    if (playingClip != clips[i])
                    {
                        crossFadeProgress = 0;
                        crossFadeTime = fadeLength;
                        SetNewPlayingClip(clips[i]);
                        return;
                    }
                    if ((playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                        (playingClip != null && !isPlaying))
                    {
                        SetNewPlayingClip(clips[i]);
                        return;
                    }
                }
            }
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        if(playingClip != null)
        {
            isPlaying = true;
        }
    }

    public void SetLODMesh(Mesh mesh)
    {
        if(!LODEnabled)
        {
            mesh = res.mesh;
        }

        if(mf != null && mf.sharedMesh != mesh)
        {
            mf.sharedMesh = mesh;
        }
    }

#if UNITY_EDITOR
    //
    /// <summary>
    /// Editor中更新PlayerMono参数，与Update函数相同
    /// </summary>
    /// <param name="timeDelta"> 本帧（本次调用）执行所用的时间 </param>
    public void Update_Editor(float timeDelta)
    {
        Update_Internal(timeDelta);
    }
#endif

    /// <summary>
    /// 根据当前时间更新模型的姿态
    /// </summary>
    /// <param name="timeDelta"> 本帧（本次调用）执行所用的时间 </param>
    public void Update(float timeDelta)
    {
        Update_Internal(timeDelta);
    }

    private void FillEvents(GPUSkinningClip clip, GPUSkinningBetterList<GPUSkinningAnimEvent> events)
    {
        events.Clear();
        if(clip != null && clip.events != null && clip.events.Length > 0)
        {
            events.AddRange(clip.events);
        }
    }

    /// <summary>
    /// 更新当前播放的动画
    /// </summary>
    /// <param name="clip"></param>
    private void SetNewPlayingClip(GPUSkinningClip clip)
    {
        //记录上一个播放的动画片段（用处？）
        lastPlayedClip = playingClip;
        //记录播放上一个动画片段的时间
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        playingClip = clip;
        rootMotionFrameIndex = -1;
        time = 0;
        // 随机生成动画帧数偏移
        timeDiff = Random.Range(0, playingClip.length);
    }

    /// <summary>
    /// GPUSkinningPlayer.Update的真正实现
    /// </summary>
    /// <param name="timeDelta"> 本帧（本次调用） </param>
    private void Update_Internal(float timeDelta)
    {
        //Profiler.BeginSample("GPUSkinningPlayer.Update_Internal()");
        if (!isPlaying || playingClip == null)
        {
            return;
        }

        //Profiler.BeginSample("GPUSkinningPlayer.GetCurrentMaterial");
        //根据当前动画播放状态（RootMotion、CullingUpdate），获取当前帧使用的Material
        GPUSkinningMaterial currMtrl = GetCurrentMaterial();
        if(currMtrl == null)
        {
            return;
        }
        //Profiler.EndSample();
        //Material应用到MeshRenderer组件
        if (mr.sharedMaterial != currMtrl.material)
        {
            mr.sharedMaterial = currMtrl.material;
        }

        //Profiler.BeginSample("GPUSkinningPlayer.UpdateMaterial()");
        if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            UpdateMaterial(timeDelta, currMtrl);
        }
        else if(playingClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (time >= playingClip.length)
            {
                time = playingClip.length;
                UpdateMaterial(timeDelta, currMtrl);
            }
            else
            {
                UpdateMaterial(timeDelta, currMtrl);
                time += timeDelta;
                if(time > playingClip.length)
                {
                    time = playingClip.length;
                }
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }
        //Profiler.EndSample();
        
        // 更新动画播放时间相关
        //crossFade已经经历的时间
        crossFadeProgress += timeDelta;
        lastPlayedTime += timeDelta;
        //Profiler.EndSample();
    }

    /// <summary>
    /// 更新动画事件（无用）
    /// </summary>
    /// <param name="playingClip"> 当前播放的动画片段 </param>
    /// <param name="playingFrameIndex"> 当前动画片段播放的帧索引 </param>
    /// <param name="corssFadeClip"> 上一次播放的动画片段 </param>
    /// <param name="crossFadeFrameIndex"> 上一个动画片段播放停止时的帧索引 </param>
    private void UpdateEvents(GPUSkinningClip playingClip, int playingFrameIndex, GPUSkinningClip corssFadeClip, int crossFadeFrameIndex)
    {
        UpdateClipEvent(playingClip, playingFrameIndex);
        UpdateClipEvent(corssFadeClip, crossFadeFrameIndex);
    }

    private void UpdateClipEvent(GPUSkinningClip clip, int frameIndex)
    {
        if(clip == null || clip.events == null || clip.events.Length == 0)
        {
            return;
        }

        GPUSkinningAnimEvent[] events = clip.events;
        int numEvents = events.Length;
        for(int i = 0; i < numEvents; ++i)
        {
            if(events[i].frameIndex == frameIndex && onAnimEvent != null)
            {
                onAnimEvent(this, events[i].eventId);
                break;
            }
        }
    }
    
    /// <summary>
    /// 根据当前动画片段播放状态设置Material
    /// fzy remak:CPU资源消耗过多
    /// </summary>
    /// <param name="deltaTime">本帧Update消耗的时间</param>
    /// <param name="currMtrl">当前使用的Material</param>
    private void UpdateMaterial(float deltaTime, GPUSkinningMaterial currMtrl)
    {
        int frameIndex = GetFrameIndex();
        //Profiler.BeginSample("PlayerResources.Update");
        //动画片段（WrapMode.Once）播放完毕
        if (lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
        {
            res.Update(deltaTime, currMtrl);
            return;
        }
        //Profiler.EndSample();
        //记录上一帧播放的动画片段
        lastPlayingClip = playingClip;
        //记录上一次播放的动画片段帧数（有可能跳帧）
        lastPlayingFrameIndex = frameIndex;

        float blend_crossFade = 1;
        int frameIndex_crossFade = -1;
        // 新建动画帧，用于crossFade
        GPUSkinningFrame frame_crossFade = null;

        //Profiler.BeginSample("PlayerResources.CrossFadeBlending");
        if (res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            frameIndex_crossFade = GetCrossFadeFrameIndex();
            frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
        }
        //Profiler.EndSample();

        //Profiler.BeginSample("PlayerResources.Update");
        GPUSkinningFrame frame = playingClip.frames[frameIndex];
        //模型可以被看见（Culling）或者CullingMode为AlwaysAnimate
        if (Visible || 
            CullingMode == GPUSKinningCullingMode.AlwaysAnimate)
        {
            res.Update(deltaTime, currMtrl);
            res.UpdatePlayingData(
                mpb, playingClip, frameIndex, frame, playingClip.rootMotionEnabled && rootMotionEnabled,
                lastPlayedClip, GetCrossFadeFrameIndex(), crossFadeTime, crossFadeProgress
            );
            mr.SetPropertyBlock(mpb);
            //bone.isExposed
            UpdateJoints(frame);
        }
        //Profiler.EndSample();

        //Profiler.BeginSample("RootMotion");
        if (playingClip.rootMotionEnabled && rootMotionEnabled && frameIndex != rootMotionFrameIndex)
        {
            if (CullingMode != GPUSKinningCullingMode.CullCompletely)
            {
                rootMotionFrameIndex = frameIndex;
                DoRootMotion(frame_crossFade, 1 - blend_crossFade, false);
                DoRootMotion(frame, blend_crossFade, true);
            }
        }
        //Profiler.EndSample();

        UpdateEvents(playingClip, frameIndex, frame_crossFade == null ? null : lastPlayedClip, frameIndex_crossFade);
    }

    //根据动画片段的RootMotion和CullingUpdate属性，设置模型使用的Material（共6种）
    private GPUSkinningMaterial GetCurrentMaterial()
    {
        if(res == null)
        {
            return null;
        }

        if(playingClip == null)
        {
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        }
        if(playingClip.rootMotionEnabled && rootMotionEnabled)
        {
            if(res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                if(lastPlayedClip.rootMotionEnabled)
                {
                    return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOn);
                }
                return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOff);
            }
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOff);
        }
        if(res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            if (lastPlayedClip.rootMotionEnabled)
            {
                return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOn);
            }
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOff);
        }
        else
        {
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        }
    }

    private void DoRootMotion(GPUSkinningFrame frame, float blend, bool doRotate)
    {
        if(frame == null)
        {
            return;
        }

        Quaternion deltaRotation = frame.rootMotionDeltaPositionQ;
        Vector3 newForward = deltaRotation * transform.forward;
        Vector3 deltaPosition = newForward * frame.rootMotionDeltaPositionL * blend;
        transform.Translate(deltaPosition, Space.World);

        if (doRotate)
        {
            transform.rotation *= frame.rootMotionDeltaRotation;
        }
    }

    /// <summary>
    /// 获得当前动画已经播放的时间
    /// </summary>
    /// <returns></returns>
    private float GetCurrentTime()
    {
        float time = 0;
        if (WrapMode == GPUSkinningWrapMode.Once)
        {
            time = this.time;
        }
        else if (WrapMode == GPUSkinningWrapMode.Loop)
        {
            //是否产生偏移
            //同一个模型的多个网格timeDiff需要相同
            time = res.Time + (playingClip.individualDifferenceEnabled ? this.timeDiff : 0);
            //time = res.Time + this.timeDiff;
        }
        else
        {
            throw new System.NotImplementedException();
        }
        return time;
    }

    /// <summary>
    /// 获取当前动画播放的帧数
    /// </summary>
    /// <returns></returns>
    private int GetFrameIndex()
    {
        float time = GetCurrentTime();
        if (playingClip.length == time)
        {
            //动画播放完毕，获取动画片段最后一帧的索引
            return GetTheLastFrameIndex_WrapMode_Once(playingClip);
        }
        else
        {
            //动画未播放完毕，获取动画片段当前帧的索引
            return GetFrameIndex_WrapMode_Loop(playingClip, time);
        }
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

    private void UpdateJoints(GPUSkinningFrame frame)
    {
        if(joints == null)
        {
            return;
        }

        //fzy delete:never used
        //Matrix4x4[] matrices = frame.matrices;
        GPUSkinningBone[] bones = res.anim.bones;
        int numJoints = joints.Count;
        for(int i = 0; i < numJoints; ++i)
        {
            GPUSkinningPlayerJoint joint = joints[i];
            Transform jointTransform = Application.isPlaying ? joint.Transform : joint.transform;
            if (jointTransform != null)
            {
                // TODO: Update Joint when Animation Blend

                Matrix4x4 jointMatrix = frame.matrices[joint.BoneIndex] * bones[joint.BoneIndex].BindposeInv;
                if(playingClip.rootMotionEnabled && rootMotionEnabled)
                {
                    jointMatrix = frame.RootMotionInv(res.anim.rootBoneIndex) * jointMatrix;
                }

                jointTransform.localPosition = jointMatrix.MultiplyPoint(Vector3.zero);

                Vector3 jointDir = jointMatrix.MultiplyVector(Vector3.right);
                Quaternion jointRotation = Quaternion.FromToRotation(Vector3.right, jointDir);
                jointTransform.localRotation = jointRotation;
            }
            else
            {
                joints.RemoveAt(i);
                --i;
                --numJoints;
            }
        }
    }

    /// <summary>
    /// 构建骨骼关节
    /// joints == null表示尚未创建骨骼关节，此时生成骨骼
    /// Bone.Exposed == true的节点会被生成骨骼
    /// </summary>
    private void ConstructJoints()
    {
        if (joints == null)
        {
            GPUSkinningPlayerJoint[] existingJoints = go.GetComponentsInChildren<GPUSkinningPlayerJoint>();

            GPUSkinningBone[] bones = res.anim.bones;
            int numBones = bones == null ? 0 : bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                GPUSkinningBone bone = bones[i];
                // 该骨骼是否被暴露(在Sampler中设置)
                if (bone.isExposed)
                {
                    if (joints == null)
                    {
                        joints = new List<GPUSkinningPlayerJoint>();
                    }

                    // 是否找到已存在的骨骼节点
                    bool inTheExistingJoints = false;
                    if (existingJoints != null)
                    {
                        for (int j = 0; j < existingJoints.Length; ++j)
                        {
                            if(existingJoints[j] != null && existingJoints[j].BoneGUID == bone.guid)
                            {
                                // 找到对应的骨骼，初始化骨骼信息
                                if (existingJoints[j].BoneIndex != i)
                                {
                                    existingJoints[j].Init(i, bone.guid);
                                    GPUSkinningUtil.MarkAllScenesDirty();
                                }
                                joints.Add(existingJoints[j]);
                                existingJoints[j] = null;
                                inTheExistingJoints = true;
                                break;
                            }
                        }
                    }

                    //未找到已存在的对应骨骼，新创建骨骼节点（对象）
                    if(!inTheExistingJoints)
                    {
                        GameObject jointGo = new GameObject(bone.name);
                        jointGo.transform.parent = go.transform;
                        jointGo.transform.localPosition = Vector3.zero;
                        jointGo.transform.localScale = Vector3.one;

                        GPUSkinningPlayerJoint joint = jointGo.AddComponent<GPUSkinningPlayerJoint>();
                        joints.Add(joint);
                        joint.Init(i, bone.guid);
                        GPUSkinningUtil.MarkAllScenesDirty();
                    }
                }
            }

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
                DelayCall = () => 
                {
                    UnityEditor.EditorApplication.delayCall -= DelayCall;
                    //删除所有无效节点
                    DeleteInvalidJoints(existingJoints);
                };
                UnityEditor.EditorApplication.delayCall += DelayCall;
#endif
            }
            else
            {
                DeleteInvalidJoints(existingJoints);
            }
        }
    }

    private void DeleteInvalidJoints(GPUSkinningPlayerJoint[] joints)
    {
        if (joints != null)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                if (joints[i] != null)
                {
                    for (int j = 0; j < joints[i].transform.childCount; ++j)
                    {
                        Transform child = joints[i].transform.GetChild(j);
                        child.parent = go.transform;
                        child.localPosition = Vector3.zero;
                    }
                    Object.DestroyImmediate(joints[i].transform.gameObject);
                    GPUSkinningUtil.MarkAllScenesDirty();
                }
            }
        }
    }
}

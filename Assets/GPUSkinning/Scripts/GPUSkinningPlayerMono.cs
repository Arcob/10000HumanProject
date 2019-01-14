using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[ExecuteInEditMode]
public class GPUSkinningPlayerMono : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private GPUSkinningAnimation anim = null;

    [HideInInspector]
    [SerializeField]
    private Material mtrl = null;

    [HideInInspector]
    [SerializeField]
    private Mesh mesh = null;

    [HideInInspector]
    [SerializeField]
    private TextAsset textureRawData = null;

    [HideInInspector]
    [SerializeField]
    private int defaultPlayingClipIndex = 0;

    [HideInInspector]
    [SerializeField]
    private bool rootMotionEnabled = false;

    [HideInInspector]
    [SerializeField]
    private bool lodEnabled = true;

    [HideInInspector]
    [SerializeField]
    private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;

    private static GPUSkinningPlayerMonoManager playerManager = new GPUSkinningPlayerMonoManager();

    private GPUSkinningPlayer player = null;
    public GPUSkinningPlayer Player
    {
        get
        {
            return player;
        }
    }

    /// <summary>
    /// fzy adda:
    /// </summary>
    private MeshFilter mf = null;
    [HideInInspector]
    public MeshRenderer mr = null;
    [HideInInspector]
    public GPUSkinningPlayerResources myRes = null;
    [HideInInspector]
    public GPUSkinningMaterial currMtrl = null;
    [HideInInspector]
    public MaterialPropertyBlock mpb = null;

    //供SamplerEditor调用，preview采集到的模型网格、动画等数据
    public void Init(GPUSkinningAnimation anim, Mesh mesh, Material mtrl, TextAsset textureRawData)
    {
        if(player != null)
        {
            return;
        }

        this.anim = anim;
        this.mesh = mesh;
        this.mtrl = mtrl;
        this.textureRawData = textureRawData;
        Init();
    }

    /// <summary>
    /// 初始化PlayerMono设置：
    /// 创建/查询对应的GPUSkinningPlayerResources，通过GPUSkinningPlayerManager管理
    /// fzy remark：初始化存在潜在的开销问题，在首次生成PlayerMono时，Creating CullingBounds会产生较大的开销
    /// </summary>
    public void Init()
    {
        //player != null 表示已经初始化，return结束执行
        if(player != null)
        {
            return;
        }

        GPUSkinningPlayerResources res = null;
        //所有属性设置完毕，开始初始化
        if (anim != null && mesh != null && mtrl != null && textureRawData != null)
        {
            //运行时，注册蒙皮网格、动画、材质等数据
            if (Application.isPlaying)
            {
                playerManager.Register(anim, mesh, mtrl, textureRawData, this, out res);
            }
            else
            {
                //编辑时，创建蒙皮网格资源实例，保存数据
                res = new GPUSkinningPlayerResources();
                res.anim = anim;
                res.mesh = mesh;
                res.texture = GPUSkinningUtil.CreateTexture2D(textureRawData, anim);
                res.texture.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                //初始化Material（一共有6种Material）
                //创建的Material和Texture不保存（游戏运行时在Project视图无法找到索引，游戏运行后销毁）
                res.InitMaterial(mtrl, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
            }

            player = new GPUSkinningPlayer(gameObject, res);
            // 非运行状态设置为false（preview）
            player.RootMotionEnabled = Application.isPlaying ? rootMotionEnabled : false;
            player.LODEnabled = Application.isPlaying ? lodEnabled : false;
            // 动画片段播放的设置
            player.CullingMode = cullingMode;

            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
            }
        }

        // fzy add:
        myRes = res;
        mf = GetComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        mr = GetComponent<MeshRenderer>();
        SetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        mpb = new MaterialPropertyBlock();
    }

#if UNITY_EDITOR
    public void DeletePlayer()
    {
        player = null;
    }

    public void Update_Editor(float deltaTime)
    {
        if(player != null && !Application.isPlaying)
        {
            player.Update_Editor(deltaTime);
        }
    }

    /// <summary>
    /// Editor Called Only：
    /// 脚本被添加或者脚本在Inspector参数被修改时调用
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            //Init();
            Update_Editor(0);
        }
    }
#endif
    
    private void Start()
    {
        //运行时初始化
        Init();
#if UNITY_EDITOR
        Update_Editor(0); 
#endif
    }

    /// <summary>
    /// [ExecuteInEditMode] 在Editor模式下也会调用Update函数 在脚本发生变化时（参数修改、重新编译）以一定频率调用
    /// </summary>
    /*
    private void Update()
    {
        if (player != null)
        {
#if UNITY_EDITOR
            if(Application.isPlaying)
            {
                player.Update(Time.deltaTime);
            }
            else
            {
                player.Update_Editor(0);
            }
#else
            player.Update(Time.deltaTime);
#endif
        }
    }*/

    private void OnDestroy()
    {
        player = null;
        anim = null;
        mesh = null;
        mtrl = null;
        textureRawData = null;

        //销毁时解绑
        if (Application.isPlaying)
        {
            playerManager.Unregister(this);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            //释放未使用资源？
            Resources.UnloadUnusedAssets();
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
        }
#endif
    }

    /// <summary>
    /// 获取并赋值Material
    /// </summary>
    public void SetMaterial(GPUSkinningPlayerResources.MaterialState ms)
    {
        if (myRes == null)
            Debug.LogWarning("myRes is null");
        currMtrl = myRes.GetMaterial(ms);
        if (mr.sharedMaterial != currMtrl.material)
        {
            mr.sharedMaterial = currMtrl.material;
        }
    }

    //private GPUSkinningExecuteOncePerFrame executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();
    public void UpdateMaterial(float deltaTime)
    {
        //多个Player可能共享同一个Resources，保证每个Resources在同一帧内只执行一次
        /*fzy delete：实际上每帧只执行一次
        if (executeOncePerFrame.CanBeExecute())
        {
            executeOncePerFrame.MarkAsExecuted();
            //fzy delete:注释以后不播放动画（猜测：未更新CullingBounds信息，默认判断模型动画出了Camera视锥区域）
            //更新CullingBounds信息（与LOD、Camera Culling相关）
            //UpdateCullingBounds();
        }
        else
        {
            Debug.Log("UpdateMaterial calls again");
        }*/

        //Material的更新在一帧内调用多次
        if (currMtrl.executeOncePerFrame.CanBeExecute())
        {
            currMtrl.executeOncePerFrame.MarkAsExecuted();
            myRes.SetTexture(currMtrl);
        }
    }
    public void UpdatePlayingData(
        GPUSkinningClip playingClip, int frameIndex, GPUSkinningFrame frame, bool rootMotionEnabled,
        GPUSkinningClip lastPlayedClip, int frameIndex_crossFade, float crossFadeTime, float crossFadeProgress)
    {
        //Profiler.BeginSample("PlayerMono.UpdatePlayingData");
        myRes.UpdatePlayingData(mpb, playingClip, frameIndex, frame, rootMotionEnabled,
                lastPlayedClip, frameIndex_crossFade, crossFadeTime, crossFadeProgress);
        //Profiler.EndSample();

        //Profiler.BeginSample("PlayerMono.SetPropertyBlock");
        mr.SetPropertyBlock(mpb);
        //Profiler.EndSample();
    }
}

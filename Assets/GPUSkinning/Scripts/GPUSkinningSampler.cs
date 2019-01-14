using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler : MonoBehaviour 
{
#if UNITY_EDITOR
    [HideInInspector]
    [SerializeField]
	public string animName = null;

    [HideInInspector]
    [System.NonSerialized]
	public AnimationClip animClip = null;

    [HideInInspector]
    [SerializeField]
    public AnimationClip[] animClips = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinningWrapMode[] wrapModes = null;

    [HideInInspector]
    [SerializeField]
    public int[] fpsList = null;

    [HideInInspector]
    [SerializeField]
    public bool[] rootMotionEnabled = null;

    [HideInInspector]
    [SerializeField]
    public bool[] individualDifferenceEnabled = null;

    [HideInInspector]
    [SerializeField]
    public Mesh[] lodMeshes = null;

    [HideInInspector]
    [SerializeField]
    public float[] lodDistances = null;

    [HideInInspector]
    [SerializeField]
    private float sphereRadius = 1.0f;

    [HideInInspector]
    [SerializeField]
    public bool createNewShader = false;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingClipIndex = -1;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingMeshIndex = -1;

    /// <summary>
    /// 存储模型动画数据采样数据的TextAsset
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public TextAsset texture = null;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningQuality skinQuality = GPUSkinningQuality.Bone2;

    [HideInInspector]
    [SerializeField]
	public Transform rootBoneTransform = null;

    
    /// <summary>
    /// 保存的模型动画
    /// </summary>
    /// anim初始为空
    /// 创建后会保存（SerializeField）
    /// 数据用于创建的Anim.asset文件
    [HideInInspector]
    [SerializeField]
    public GPUSkinningAnimation anim = null;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningShaderType shaderType = GPUSkinningShaderType.Unlit;

	[HideInInspector]
	[System.NonSerialized]
	public bool isSampling = false;

    /// <summary>
    /// 保存的模型网格
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public Mesh savedMesh = null;

    /// <summary>
    /// 保存的模型材质
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public Material savedMtrl = null;

    /// <summary>
    /// 新创建的模型着色器（用户选择是否创建）
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public Shader savedShader = null;

    [HideInInspector]
    [SerializeField]
    public bool updateOrNew = true;

    private Animation animationComponent = null;

	private Animator animator = null;
    private RuntimeAnimatorController runtimeAnimatorController = null;

	private SkinnedMeshRenderer smr = null;

    /// <summary>
    /// 所有采样的动画片段数据
    /// </summary>
    private GPUSkinningAnimation gpuSkinningAnimation = null;

    /// <summary>
    /// 当前采样的动画片段
    /// </summary>
    private GPUSkinningClip gpuSkinningClip = null;

    private Vector3 rootMotionPosition;

    private Quaternion rootMotionRotation;

    /// <summary>
    /// 模型包含的所有网格
    /// </summary>
    private SkinnedMeshRenderer[] smrs = null;

    /// <summary>
    /// 采集的网格个数
    /// </summary>
    [HideInInspector]
    [System.NonSerialized]
    public int samplingMeshCount = 0;

    [HideInInspector]
	[System.NonSerialized]
	public int samplingTotalFrams = 0;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingFrameIndex = 0;

	public const string TEMP_SAVED_ANIM_PATH = "GPUSkinning_Temp_Save_Anim_Path";
	public const string TEMP_SAVED_MTRL_PATH = "GPUSkinning_Temp_Save_Mtrl_Path";
	public const string TEMP_SAVED_MESH_PATH = "GPUSkinning_Temp_Save_Mesh_Path";
    public const string TEMP_SAVED_SHADER_PATH = "GPUSkinning_Temp_Save_Shader_Path";
    public const string TEMP_SAVED_TEXTURE_PATH = "GPUSkinning_Temp_Save_Texture_Path";

    //fzy add：保存路径
    private string savePath = null;

    //Sample结束/中途终止后调用，-1表示未在Sample
    public void EndSample()
    {
        samplingClipIndex = -1;
        samplingMeshIndex = -1;
    }

    public bool IsSamplingProgress()
    {
        return samplingClipIndex != -1
            //&& samplingMeshIndex != -1
            ;
    }

    public bool IsAnimatorOrAnimation()
    {
        return animator != null; 
    }

    //Sample前调用，初始化Sample AnimationClip的索引
    public void BeginSample()
    {
        //获取网格的所有模型
        smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
        samplingMeshCount = smrs.Length;
        
        savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), "");

        samplingClipIndex = 0;
        samplingMeshIndex = 0;
    }

    #region fzy remark
    /// <summary>
    /// 开始采样，采样前的初始化工作
    /// </summary>
    public void StartSample()
	{
        if (isSampling)
        {
            return;
        }
        //Trim:剔除string首尾的空格和制表符（不包括中间出现的）
        if (string.IsNullOrEmpty(animName.Trim()))
        {
            ShowDialog("Animation name is empty.");
            return;
        }

        if (rootBoneTransform == null)
        {
            ShowDialog("Please set Root Bone.");
            return;
        }

        if (animClips == null || animClips.Length == 0)
        {
            ShowDialog("Please set Anim Clips.");
            return;
        }

        //采样的动画片段
        animClip = animClips[samplingClipIndex];
        if (animClip == null)
		{
            isSampling = false;
			return;
		}
        // 动画片段总帧数 = 动画片段采样率（每秒帧数） * 动画片段时间
        int numFrames = (int)(GetClipFPS(animClip, samplingClipIndex) * animClip.length);
        if (numFrames == 0)
        {
            isSampling = false;
            return;
        }

        //smr = GetComponentInChildren<SkinnedMeshRenderer>();
        smr = smrs[samplingMeshIndex];
		if(smr == null)
		{
			ShowDialog("Cannot find SkinnedMeshRenderer.");
			return;
		}
		if(smr.sharedMesh == null)
		{
			ShowDialog("Cannot find SkinnedMeshRenderer.mesh.");
			return;
		}

		Mesh mesh = smr.sharedMesh;
		if(mesh == null)
		{
			ShowDialog("Missing Mesh");
			return;
		}

        //是否初始化（没有生成对应的Anim.asset时，anim为null）
		gpuSkinningAnimation = anim == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : anim;

        if(anim == null)
        {
            //分配唯一标识符
            gpuSkinningAnimation.animName = animName + "_" + smr.name;
            gpuSkinningAnimation.guid = System.Guid.NewGuid().ToString();
        }

        //bones_result:引用变量，存储骨骼信息
        List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
        //从骨骼根节点（Root）开始处理
		CollectBones(bones_result, smr.bones, mesh.bindposes, null, rootBoneTransform, 0);
        GPUSkinningBone[] newBones = bones_result.ToArray();
        GenerateBonesGUID(newBones);
        // 初始化时anim为空，不调用
        if (anim != null)
            RestoreCustomBoneData(anim.bones, newBones);
        gpuSkinningAnimation.bones = newBones;
        for(int i=0;i< newBones.Length;i++)
            if(newBones[i].transform == smr.rootBone)
            {
                gpuSkinningAnimation.rootBoneIndex = i;
                Debug.Log(i);
                break;
            }
        //gpuSkinningAnimation.rootBoneIndex = 0;

        //查询该动画是否已经被采样
        int numClips = gpuSkinningAnimation.clips == null ? 0 : gpuSkinningAnimation.clips.Length;
        int overrideClipIndex = -1;
        for (int i = 0; i < numClips; ++i)
        {
            if (gpuSkinningAnimation.clips[i].name == animClip.name)
            {
                overrideClipIndex = i;
                break;
            }
        }

        //创建采集的动画信息实例（GPUSkinningClip）
        gpuSkinningClip = new GPUSkinningClip();
        gpuSkinningClip.name = animClip.name;
        gpuSkinningClip.fps = GetClipFPS(animClip, samplingClipIndex);
        gpuSkinningClip.length = animClip.length;
        gpuSkinningClip.wrapMode = wrapModes[samplingClipIndex];
        //只分配内存，未赋值，在Update采样中赋值
        gpuSkinningClip.frames = new GPUSkinningFrame[numFrames];
        gpuSkinningClip.rootMotionEnabled = rootMotionEnabled[samplingClipIndex];
        gpuSkinningClip.individualDifferenceEnabled = individualDifferenceEnabled[samplingClipIndex];

        //初始化GPUSkinningAnimation的动画片段
        if(gpuSkinningAnimation.clips == null)
        {
            gpuSkinningAnimation.clips = new GPUSkinningClip[] { gpuSkinningClip };
        }
        else
        {
            //未找到该动画
            if (overrideClipIndex == -1)
            {
                //将该动画放到GPUSkinningAnimation.clips列表的最后
                List<GPUSkinningClip> clips = new List<GPUSkinningClip>(gpuSkinningAnimation.clips);
                clips.Add(gpuSkinningClip);
                gpuSkinningAnimation.clips = clips.ToArray();
            }
            else
            {
                GPUSkinningClip overridedClip = gpuSkinningAnimation.clips[overrideClipIndex];
                //更新动画片段信息（动画事件）
                RestoreCustomClipData(overridedClip, gpuSkinningClip);
                gpuSkinningAnimation.clips[overrideClipIndex] = gpuSkinningClip;
            }
        }

        SetCurrentAnimationClip();
        PrepareRecordAnimator();

        //Update函数开始Sample，初始化samplingFrameIndex为0
        samplingFrameIndex = 0;
        isSampling = true;
        //fzy add：
        isThisSampleDone = true;
    }
    #endregion

    // 获取Editor中设置的动画片段采样率，如果为0，表示使用动画片段自身的采样率
    private int GetClipFPS(AnimationClip clip, int clipIndex)
    {
        return fpsList[clipIndex] == 0 ? (int)clip.frameRate : fpsList[clipIndex];
    }

    /// <summary>
    /// 更新GPUSkinning动画片段的自定义数据（动画片段事件）
    /// </summary>
    /// <param name="src"> 初始动画片段 </param>
    /// <param name="dest"> 更新动画片段 </param>
    private void RestoreCustomClipData(GPUSkinningClip src, GPUSkinningClip dest)
    {
        //src的动画片段时间赋给dest
        if(src.events != null)
        {
            int totalFrames = (int)(dest.length * dest.fps);
            dest.events = new GPUSkinningAnimEvent[src.events.Length];
            for(int i = 0; i < dest.events.Length; ++i)
            {
                GPUSkinningAnimEvent evt = new GPUSkinningAnimEvent();
                evt.eventId = src.events[i].eventId;
                evt.frameIndex = Mathf.Clamp(src.events[i].frameIndex, 0, totalFrames - 1);
                dest.events[i] = evt;
            }
        }
    }
    /// <summary>
    /// anim初始化后，保存了上次Sample的信息
    /// </summary>
    /// <param name="bonesOrig"> Anim 中的bones</param>
    /// <param name="bonesNew"> 根据smr.bones生成的GPUSkinningBones</param>
    private void RestoreCustomBoneData(GPUSkinningBone[] bonesOrig, GPUSkinningBone[] bonesNew)
    {
        for(int i = 0; i < bonesNew.Length; ++i)
        {
            for(int j = 0; j < bonesOrig.Length; ++j)
            {
                if(bonesNew[i].guid == bonesOrig[j].guid)
                {
                    bonesNew[i].isExposed = bonesOrig[j].isExposed;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 生成骨骼的唯一标识符
    /// </summary>
    /// <param name="bones"> 所有骨骼的引用 </param>
    private void GenerateBonesGUID(GPUSkinningBone[] bones)
    {
        int numBones = bones == null ? 0 : bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            string boneHierarchyPath = GPUSkinningUtil.BoneHierarchyPath(bones, i);
            string guid = GPUSkinningUtil.MD5(boneHierarchyPath);
            bones[i].guid = guid;
        }
    }

    /// <summary>
    /// 准备记录Animator，用于播放动画片段并采样
    /// </summary>
    private void PrepareRecordAnimator()
    {
        if (animator != null)
        {
            int numFrames = (int)(gpuSkinningClip.fps * gpuSkinningClip.length);

            animator.applyRootMotion = gpuSkinningClip.rootMotionEnabled;
            animator.Rebind();
            //开始的时间
            animator.recorderStartTime = 0;
            //录制开始指定的总帧数
            animator.StartRecording(numFrames);
            for (int i = 0; i < numFrames; ++i)
            {
                animator.Update(1.0f / gpuSkinningClip.fps);
            }
            //停止动画录制
            animator.StopRecording();
            //动画重放
            animator.StartPlayback();
        }
    }
    /// <summary>
    /// 使用Animator组件，设置当前采样的动画片段
    /// </summary>
    private void SetCurrentAnimationClip()
    {
        // ？ 为什么不是animator != null
        if (animationComponent == null)
        {
            AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
            AnimationClip[] clips = runtimeAnimatorController.animationClips;
            AnimationClipPair[] pairs = new AnimationClipPair[clips.Length];
            for (int i = 0; i < clips.Length; ++i)
            {
                AnimationClipPair pair = new AnimationClipPair();
                pairs[i] = pair;
                pair.originalClip = clips[i];
                pair.overrideClip = animClip;
            }
            // 设置animtor.runtimeAnimatorController,AnimatorOverrideController:
            animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
            animatorOverrideController.clips = pairs;
            animator.runtimeAnimatorController = animatorOverrideController;
        }
    }

    /// <summary>
    /// 创建LOD模型网格
    /// </summary>
    /// <param name="bounds"> 模型网格包围体 </param>
    /// <param name="dir"> 保存路径 </param>
    private void CreateLODMeshes(Bounds bounds, string dir)
    {
        gpuSkinningAnimation.lodMeshes = null;
        gpuSkinningAnimation.lodDistances = null;
        gpuSkinningAnimation.sphereRadius = sphereRadius;

        if(lodMeshes != null)
        {
            List<Mesh> newMeshes = new List<Mesh>();
            List<float> newLodDistances = new List<float>();
            for (int i = 0; i < lodMeshes.Length; ++i)
            {
                Mesh lodMesh = lodMeshes[i];
                if(lodMesh != null)
                {
                    //新创建的Mesh包围体需要重新计算？
                    Mesh newMesh = CreateNewMesh(lodMesh, "GPUSkinning_Mesh_LOD" + (i + 1));
                    newMesh.bounds = bounds;
                    string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + "_LOD" + (i + 1) + ".asset";
                    AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                    newMeshes.Add(newMesh);
                    newLodDistances.Add(lodDistances[i]);
                }
            }
            gpuSkinningAnimation.lodMeshes = newMeshes.ToArray();

            newLodDistances.Add(9999);
            gpuSkinningAnimation.lodDistances = newLodDistances.ToArray();
        }

        //将目标对象标记为Dirty，方便查找更改
        EditorUtility.SetDirty(gpuSkinningAnimation);
    }

    /// <summary>
    /// 创建新的Mesh网格
    /// </summary>
    /// <param name="mesh"> Origin Mesh </param>
    /// <param name="meshName"> Mesh的名称 </param>
    /// <returns></returns>
    private Mesh CreateNewMesh(Mesh mesh, string meshName)
    {
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Color[] colors = mesh.colors;
        Vector2[] uv = mesh.uv;

        Mesh newMesh = new Mesh();
        newMesh.name = meshName;
        newMesh.vertices = mesh.vertices;
        if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
        if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
        //大部分模型没有Colors
        if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
        if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

        int numVertices = mesh.vertexCount;
        BoneWeight[] boneWeights = mesh.boneWeights;
        Vector4[] uv2 = new Vector4[numVertices];
		Vector4[] uv3 = new Vector4[numVertices];
        //获取bones Transform的引用
        Transform[] smrBones = smr.bones;

        //在Mesh中保存骨骼权重信息
        for(int i = 0; i < numVertices; ++i)
        {
            BoneWeight boneWeight = boneWeights[i];

			BoneWeightSortData[] weights = new BoneWeightSortData[4];
            //骨骼原本按照递减顺序排列
			weights[0] = new BoneWeightSortData(){ index=boneWeight.boneIndex0, weight=boneWeight.weight0 };
			weights[1] = new BoneWeightSortData(){ index=boneWeight.boneIndex1, weight=boneWeight.weight1 };
			weights[2] = new BoneWeightSortData(){ index=boneWeight.boneIndex2, weight=boneWeight.weight2 };
			weights[3] = new BoneWeightSortData(){ index=boneWeight.boneIndex3, weight=boneWeight.weight3 };
            //按weight从大到小排列
			System.Array.Sort(weights);

			GPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
			GPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
			GPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
			GPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

            //uv2存储较大的两个骨骼权重信息（骨骼索引+骨骼权重）
            //uv2可能用于保存lightmap信息（是否会出现冲突？）
            Vector4 skinData_01 = new Vector4();
			skinData_01.x = GetBoneIndex(bone0);
			skinData_01.y = weights[0].weight;
			skinData_01.z = GetBoneIndex(bone1);
			skinData_01.w = weights[1].weight;
			uv2[i] = skinData_01;

            //uv3存储较小的两个骨骼权重信息（骨骼索引+骨骼权重）
            Vector4 skinData_23 = new Vector4();
			skinData_23.x = GetBoneIndex(bone2);
			skinData_23.y = weights[2].weight;
			skinData_23.z = GetBoneIndex(bone3);
			skinData_23.w = weights[3].weight;
			uv3[i] = skinData_23;
        }
        newMesh.SetUVs(1, new List<Vector4>(uv2));
		newMesh.SetUVs(2, new List<Vector4>(uv3));

        newMesh.triangles = mesh.triangles;
        return newMesh;
    }

    /// <summary>
    /// 自定义BoneWeight数据结构
    /// 可比较大小（根据骨骼权重weight）
    /// </summary>
	private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
	{
		public int index = 0;

		public float weight = 0;
        
		public int CompareTo(BoneWeightSortData b)
		{
			return weight > b.weight ? -1 : 1;
		}
	}

    /// <summary>收集骨骼信息
    /// bones_result：使用自定义数据结构GPUSkinningBone 存储骨骼信息
    /// bones_smr：smr中存储的骨骼信息
    /// bindpose：smr中存储的骨骼绑定信息
    /// parentBone：当前骨骼的父骨骼信息（GPUSkinningBone，在该函数之前的迭代中收集完毕）
    /// currentBoneTransform：当前骨骼
    /// currentBoneIndex：当前骨骼在父骨骼子节点的索引（GPUSkinningBone）
    /// </summary>
	private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, 
        GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
	{
		GPUSkinningBone currentBone = new GPUSkinningBone();
		bones_result.Add(currentBone);

		int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
		currentBone.transform = currentBoneTransform;
		currentBone.name = currentBone.transform.name;
        //bones_smr中是否能找到该骨骼（网格的计算可能未用到部分骨骼）
		currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
		currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);
        
		if(parentBone != null)
		{
			parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
		}

		int numChildren = currentBone.transform.childCount;
        if (numChildren > 0)
        {
            currentBone.childrenBonesIndices = new int[numChildren];
            for (int i = 0; i < numChildren; ++i)
            {
                // 深度优先遍历
                CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
            }
        }
	}

    /// <summary>
    /// 将动画片段集合数据转换为Texture纹理前的初始化操作
    /// </summary>
    /// <param name="gpuSkinningAnim"> 需要转换的动画片段集合 </param>
    private void SetSthAboutTexture(GPUSkinningAnimation gpuSkinningAnim)
    {
        int numPixels = 0;

        GPUSkinningClip[] clips = gpuSkinningAnim.clips;
        int numClips = clips.Length;
        for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
        {
            GPUSkinningClip clip = clips[clipIndex];
            //设置动画数据对应的初始纹理像素位置
            clip.pixelSegmentation = numPixels;

            //获取动画的帧数
            GPUSkinningFrame[] frames = clip.frames;
            int numFrames = frames.Length;
            
            //每根骨头、每帧 需要3个像素点表示（CreateTexureMatrix中）
            numPixels += gpuSkinningAnim.bones.Length * 3/*treat 3 pixels as a float3x4*/ * numFrames;
        }
        
        CalculateTextureSize(numPixels, out gpuSkinningAnim.textureWidth, out gpuSkinningAnim.textureHeight);
    }

    /// <summary>
    /// 使用纹理存储动画数据（重要）
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="gpuSkinningAnim"></param>
    private void CreateTextureMatrix(string dir, GPUSkinningAnimation gpuSkinningAnim)
    {
        // 创建2D纹理
        // false：不重新计算纹理映射（纹理仅用于表示动画，不需要计算纹理映射）
        // true：线性彩色空间（linear，not sRGB）
        Texture2D texture = new Texture2D(gpuSkinningAnim.textureWidth, gpuSkinningAnim.textureHeight, TextureFormat.RGBAHalf, false, true);
        Color[] pixels = texture.GetPixels();
        int pixelIndex = 0;
        for (int clipIndex = 0; clipIndex < gpuSkinningAnim.clips.Length; ++clipIndex)
        {
            GPUSkinningClip clip = gpuSkinningAnim.clips[clipIndex];
            GPUSkinningFrame[] frames = clip.frames;
            int numFrames = frames.Length;
            for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
            {
                GPUSkinningFrame frame = frames[frameIndex];
                Matrix4x4[] matrices = frame.matrices;
                int numMatrices = matrices.Length;
                for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                {
                    Matrix4x4 matrix = matrices[matrixIndex];
                    //3个float4 -> 3个Color（RGBA）存储
                    pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                    pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                    pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                    //最后一维(matrix.m30, matrix.m31, matrix.m32, matrix.m33) = matrix(0,0,0,1)，无需保存
                }
            }
        }
        texture.SetPixels(pixels);
        // SetPixel后调用Apply,应用修改
        // Apply is a potentially expensive operation, so you'll want to change as many pixels as possible between Apply calls.
        // Alternatively, if you don't need to access the pixels on the CPU, you could use Graphics.CopyTexture for fast GPU-side texture data copies.
        texture.Apply();

        string savedPath = dir + "/GPUSKinning_Texture_" + animName + ".bytes";
        using (FileStream fileStream = new FileStream(savedPath, FileMode.Create))
        {
            //获取任意Texture类型的Data（包括Compressed）
            byte[] bytes = texture.GetRawTextureData();
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
        }
        WriteTempData(TEMP_SAVED_TEXTURE_PATH, savedPath);
    }

    /// <summary>
    /// 根据纹理个数计算纹理宽高
    /// </summary>
    /// <param name="numPixels"> 总像素个数 </param>
    /// <param name="texWidth"> 纹理宽度 </param>
    /// <param name="texHeight"> 纹理高度 </param>
    private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
    {
        texWidth = 1;
        texHeight = 1;
        while (true)
        {
            if (texWidth * texHeight >= numPixels) break;
            texWidth *= 2;
            if (texWidth * texHeight >= numPixels) break;
            texHeight *= 2;
        }
    }

    
    public void MappingAnimationClips()
    {
        if(animationComponent == null)
        {
            return;
        }

        List<AnimationClip> newClips = null;
        AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
        if (clips != null)
        {
            for (int i = 0; i < clips.Length; ++i)
            {
                AnimationClip clip = clips[i];
                if (clip != null)
                {
                    if (animClips == null || System.Array.IndexOf(animClips, clip) == -1)
                    {
                        if (newClips == null)
                        {
                            newClips = new List<AnimationClip>();
                        }
                        newClips.Clear();
                        if (animClips != null) newClips.AddRange(animClips);
                        newClips.Add(clip);
                        animClips = newClips.ToArray();
                    }
                }
            }
        }

        if(animClips != null && clips != null)
        {
            for(int i = 0; i < animClips.Length; ++i)
            {
                AnimationClip clip = animClips[i];
                if (clip != null)
                {
                    if(System.Array.IndexOf(clips, clip) == -1)
                    {
                        if(newClips == null)
                        {
                            newClips = new List<AnimationClip>();
                        }
                        newClips.Clear();
                        newClips.AddRange(animClips);
                        newClips.RemoveAt(i);
                        animClips = newClips.ToArray();
                        --i;
                    }
                }
            }
        }
    }

    private void InitTransform()
    {
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
    }

    private void Awake()
	{
        animationComponent = GetComponent<Animation>();
		animator = GetComponent<Animator>();
        if (animator == null && animationComponent == null)
        {
            DestroyImmediate(this);
            ShowDialog("Cannot find Animator Or Animation Component");
            return;
        }
        if(animator != null && animationComponent != null)
        {
            DestroyImmediate(this);
            ShowDialog("Animation is not coexisting with Animator");
            return;
        }
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                DestroyImmediate(this);
                ShowDialog("Missing RuntimeAnimatorController");
                return;
            }
            if (animator.runtimeAnimatorController is AnimatorOverrideController)
            {
                DestroyImmediate(this);
                ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
                return;
            }
            runtimeAnimatorController = animator.runtimeAnimatorController;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            InitTransform();
            return;
        }
        if(animationComponent != null)
        {
            MappingAnimationClips();
            animationComponent.Stop();
            animationComponent.cullingType = AnimationCullingType.AlwaysAnimate;
            InitTransform();
            return;
        }
	}


    /// <summary>
    /// fzy add：用于检测采样是否完成
    /// 在StartSample函数中，与isSampling一起初始化
    /// </summary>
    private bool isThisSampleDone;


    // Sample Animation
    private void Update()
    {
        if (!isSampling)
		{
			return;
		}

        int totalFrams = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
		samplingTotalFrams = totalFrams;

        //一个动画片段采样结束，准备保存
        if (samplingFrameIndex >= totalFrams)
        {
            //Animator停止回放
            if (animator != null)
            {
                animator.StopPlayback();
            }

            #region author version（现已放入BeginSample函数中处理）
            /*
            string savePath = null;
            if (anim == null)
            {
                //anim为空，表示未设置Anim.Asset的路径设置，弹出保存窗口设置保存路径
                savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), animName);
            }
            else
            {
                //获取Asset文件的路径字符串
                string animPath = AssetDatabase.GetAssetPath(anim);
                //将路径字符串中的"\\"替换为"/"
                savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
            }*/
            #endregion

            Debug.Log(savePath);
            if (!string.IsNullOrEmpty(savePath))
			{
                //必须保存在Project Asset目录中
				if(!savePath.Contains(Application.dataPath.Replace('\\', '/')))
				{
					ShowDialog("Must select a directory in the project's Asset folder.");
				}
				else
				{
                    //存储地址本地持久化
					SaveUserPreferDir(savePath);
                    //从“.../Assets/”开始提取保存路径
                    string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

                    //fzy add：每个模型单独设置一个文件夹
                    dir = dir + "/" + samplingMeshIndex.ToString() + smrs[samplingMeshIndex].name;
                    Directory.CreateDirectory(dir);
                    Debug.Log("create");
                        
                    //保存路径
                    //asset：可以表示任一的数据
					string savedAnimPath = dir + "/GPUSKinning_Anim_" + animName + "_" + samplingMeshIndex.ToString() + ".asset";
                    //计算纹理的像素点个数、宽高等基本数据
                    SetSthAboutTexture(gpuSkinningAnimation);

                    //SetDirty->标记该对象已发生修改，需要在Disk上保存更新
                    EditorUtility.SetDirty(gpuSkinningAnimation);

                    // 不等于表示该模型的动画数据是新的保存数据，需要重新创建资源（第一个为该模型采集动画数据，且是该模型的第一个动画片段）
                    if (anim != gpuSkinningAnimation)
                    {
                        Debug.Log("CreateAsset");
                        AssetDatabase.CreateAsset(gpuSkinningAnimation, savedAnimPath);
                    }
                    //本地数据持久化，保存GPUSkinningAnim的保存地址
                    WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
                    
                    // 赋值引用，anim和gpuSkinningAnimation建立联系的唯一代码
                    anim = gpuSkinningAnimation;

                    // 读取anim存储的骨骼变换矩阵，存储到Texture中，使用Pixel.Color（RGBA）保存
                    CreateTextureMatrix(dir, anim);

                    //如果本次采集的动画片段是第一个动画片段，需要保存Mesh和Material的数据
                    //之后的动画片段采样结束后，无需再次保存（使用同一个Mesh和Material数据）
                    if (samplingClipIndex == 0)
                    {
                        Mesh newMesh = CreateNewMesh(smr.sharedMesh, "GPUSkinning_Mesh");
                        if (savedMesh != null)
                        {
                            newMesh.bounds = savedMesh.bounds;
                        }
                        string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + ".asset";
                        AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                        WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
                        savedMesh = newMesh;

                        CreateShaderAndMaterial(dir);

                        CreateLODMeshes(newMesh.bounds, dir);
                    }

					AssetDatabase.Refresh();
                    //与SetDirty相关：
                    //SetDirty标记修改过的资源文件
                    //SaveAssets根据Dirty信息，重新保存这些修改的资源
					AssetDatabase.SaveAssets();
				}
			}
            isSampling = false;
            return;
        }

        //fzy add：如果上次采样未结束
        if (isThisSampleDone == false) return;
        //当前采样帧对应的动画片段播放时间
        float time = gpuSkinningClip.length * ((float)samplingFrameIndex / totalFrams);
        GPUSkinningFrame frame = new GPUSkinningFrame();
        gpuSkinningClip.frames[samplingFrameIndex] = frame;
        //该动画影响的每个骨骼（实际上是模型的所有骨骼）变换矩阵
        frame.matrices = new Matrix4x4[gpuSkinningAnimation.bones.Length];
        //Animator OR Animation Component
        if (animationComponent == null)
        {
            //设置Animator播放time时间的动画片段姿态
            animator.playbackTime = time;
            //模拟、播放该姿态的动画
            animator.Update(0);
        }
        else
        {
            animationComponent.Stop();
            AnimationState animState = animationComponent[animClip.name];
            if(animState != null)
            {
                animState.time = time;
                animationComponent.Sample();
                animationComponent.Play();
            }
        }
        //fzy add：开始采样前初始化变量
        isThisSampleDone = false;
        StartCoroutine(SamplingCoroutine(frame, totalFrams));
        //Debug.Log(samplingFrameIndex);
    }

    private IEnumerator SamplingCoroutine(GPUSkinningFrame frame, int totalFrames)
    {
        //等待当前帧结束（动画片段姿态更新完毕）
		yield return new WaitForEndOfFrame();
        //Debug.Log("Start Sample "+samplingFrameIndex);
        GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
        int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            //多此一举？fzy modify：
            //Transform boneTransform = bones[i].transform;
            //GPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
            GPUSkinningBone currentBone = bones[i];

            frame.matrices[i] = currentBone.bindpose;
            //累乘从初始位置切换到当前位置的变换矩阵（包括父骨骼的变换）
            do
            {
                Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                frame.matrices[i] = mat * frame.matrices[i];
                if (currentBone.parentBoneIndex == -1)
                {
                    break;
                }
                else
                {
                    currentBone = bones[currentBone.parentBoneIndex];
                }
            }
            while (true);
        }

        //第0帧数据，初始化Rootmotion的Position和Rotation（RootMotion使用）
        if(samplingFrameIndex == 0)
        {
            rootMotionPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
            rootMotionRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
        }
        else
        {
            //计算之后和初始位置朝向的变化
            Vector3 newPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
            Quaternion newRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
            Vector3 deltaPosition = newPosition - rootMotionPosition;
            //frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);

            //Quaternion.Euler(transform.forward) = (0.0,0.0,0.0,1.0) = Quaternion.Inverse(Quaternion.Euler(transform.forward))
            frame.rootMotionDeltaPositionQ =
                Quaternion.Inverse(Quaternion.Euler(transform.forward))
                * Quaternion.Euler(deltaPosition.normalized);
            //位移的大小
            frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
            //先转到rootMotion，再转到newRotation
            frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
            rootMotionPosition = newPosition;
            rootMotionRotation = newRotation;

            //第0帧的初始化（与第1帧相同）
            if(samplingFrameIndex == 1)
            {
                gpuSkinningClip.frames[0].rootMotionDeltaPositionQ = gpuSkinningClip.frames[1].rootMotionDeltaPositionQ;
                gpuSkinningClip.frames[0].rootMotionDeltaPositionL = gpuSkinningClip.frames[1].rootMotionDeltaPositionL;
                gpuSkinningClip.frames[0].rootMotionDeltaRotation = gpuSkinningClip.frames[1].rootMotionDeltaRotation;
            }
        }
        //fzy Test:yield return new WaitForSeconds(1.0f);
        //fzy add：采样结束，可以开始下一次采样
        isThisSampleDone = true;
        ++samplingFrameIndex;
    }

    /// <summary>
    /// 为采样模型创建新的Shader和Material
    /// </summary>
    /// <param name="dir"> Shader和Material的保存地址 </param>
	private void CreateShaderAndMaterial(string dir)
	{
        Shader shader = null;
        //创建新的Shader
        if (createNewShader)
        {
            string shaderTemplate =
                shaderType == GPUSkinningShaderType.Unlit ? "GPUSkinningUnlit_Template" :
                shaderType == GPUSkinningShaderType.StandardSpecular ? "GPUSkinningSpecular_Template" :
                shaderType == GPUSkinningShaderType.StandardMetallic ? "GPUSkinningMetallic_Template" : string.Empty;

            //获取Shader代码模板（在Resources目录下）
            string shaderStr = ((TextAsset)Resources.Load(shaderTemplate)).text;
            shaderStr = shaderStr.Replace("_$AnimName$_", animName);
            //根据BoneQuality设置Material（Shader）
            shaderStr = SkinQualityShaderStr(shaderStr);
            string shaderPath = dir + "/GPUSKinning_Shader_" + animName + ".shader";
            File.WriteAllText(shaderPath, shaderStr);
            WriteTempData(TEMP_SAVED_SHADER_PATH, shaderPath);
            //加载新创建的Shader
            AssetDatabase.ImportAsset(shaderPath);
            shader = AssetDatabase.LoadMainAssetAtPath(shaderPath) as Shader;
        }
        else
        {
            string shaderName =
                shaderType == GPUSkinningShaderType.Unlit ? "GPUSkinning/GPUSkinning_Unlit_Skin" :
                shaderType == GPUSkinningShaderType.StandardSpecular ? "GPUSkinning/GPUSkinning_Specular_Skin" :
                shaderType == GPUSkinningShaderType.StandardMetallic ? "GPUSkinning_Metallic_Skin" : string.Empty;
            shaderName +=
                skinQuality == GPUSkinningQuality.Bone1 ? 1 :
                skinQuality == GPUSkinningQuality.Bone2 ? 2 :
                skinQuality == GPUSkinningQuality.Bone4 ? 4 : 1;
            shader = Shader.Find(shaderName);
            WriteTempData(TEMP_SAVED_SHADER_PATH, AssetDatabase.GetAssetPath(shader));
        }

		Material mtrl = new Material(shader);
		if(smr.sharedMaterial != null)
		{
			mtrl.CopyPropertiesFromMaterial(smr.sharedMaterial);
		}
		string savedMtrlPath = dir + "/GPUSKinning_Material_" + animName + ".mat";
		AssetDatabase.CreateAsset(mtrl, savedMtrlPath);
        WriteTempData(TEMP_SAVED_MTRL_PATH, savedMtrlPath);
	}

	private string SkinQualityShaderStr(string shaderStr)
	{
		GPUSkinningQuality removalQuality1 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone2 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone1 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone1 : GPUSkinningQuality.Bone1;

		GPUSkinningQuality removalQuality2 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone2 : GPUSkinningQuality.Bone1;

		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality1 + @"[\s\S]*" + removalQuality1 + @"\$_", string.Empty);
		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality2 + @"[\s\S]*" + removalQuality2 + @"\$_", string.Empty);
		shaderStr = shaderStr.Replace("_$" + skinQuality, string.Empty);
		shaderStr = shaderStr.Replace(skinQuality + "$_", string.Empty);

		return shaderStr;
	}

    //根据Transform获取GPUSkinningBone
    private GPUSkinningBone GetBoneByTransform(Transform transform)
	{
		GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
		int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            if(bones[i].transform == transform)
            {
                return bones[i];
            }
        }
        return null;
	}

    private int GetBoneIndex(GPUSkinningBone bone)
    {
        return System.Array.IndexOf(gpuSkinningAnimation.bones, bone);
    }

	public static void ShowDialog(string msg)
	{
		EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
	}

	private void SaveUserPreferDir(string dirPath)
	{
		PlayerPrefs.SetString("GPUSkinning_UserPreferDir", dirPath);
	}

	private string GetUserPreferDir()
	{
		return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
	}

    /// <summary>
    /// 本地数据持久化
    /// 主要用于存储文件的保存路径
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value">主要是文件保存路径</param>
    public static void WriteTempData(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static string ReadTempData(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public static void DeleteTempData(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }
#endif
}

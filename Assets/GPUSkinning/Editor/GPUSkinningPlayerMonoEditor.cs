using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinningPlayerMono))]
public class GPUSkinningPlayerMonoEditor : Editor
{
    // 当前现实时刻（realtimeSinceStartup）
    private float time = 0;

    private string[] clipsName = null;

    public override void OnInspectorGUI()
    {
        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;
        if (player == null)
        {
            return;
        }

        #region 属性设置后更新
        //设置属性后，调用Init函数初始化
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            player.DeletePlayer();
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mtrl"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            player.DeletePlayer();
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            player.DeletePlayer();
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureRawData"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            player.DeletePlayer();
            player.Init();
        }
        #endregion

        //运行时修改RootMotion属性
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rootMotionEnabled"), new GUIContent("Apply Root Motion"));
        if(EditorGUI.EndChangeCheck())
        {
            if(Application.isPlaying)
            {
                player.Player.RootMotionEnabled = serializedObject.FindProperty("rootMotionEnabled").boolValue;
            }
        }

        //运行时修改LOD属性
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lodEnabled"), new GUIContent("LOD Enabled"));
        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                player.Player.LODEnabled = serializedObject.FindProperty("lodEnabled").boolValue;
            }
        }

        //运行时修改Animator的CullingMode属性
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingMode"), new GUIContent("Culling Mode"));
        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                player.Player.CullingMode = 
                    serializedObject.FindProperty("cullingMode").enumValueIndex == 0 ? GPUSKinningCullingMode.AlwaysAnimate :
                    serializedObject.FindProperty("cullingMode").enumValueIndex == 1 ? GPUSKinningCullingMode.CullUpdateTransforms : GPUSKinningCullingMode.CullCompletely;
            }
        }

        #region 根据defaultPlayingClipIndex（索引）获取默认播放的动画
        GPUSkinningAnimation anim = serializedObject.FindProperty("anim").objectReferenceValue as GPUSkinningAnimation;
        SerializedProperty defaultPlayingClipIndex = serializedObject.FindProperty("defaultPlayingClipIndex");
        //生成anim.clips.name数组
        if (clipsName == null && anim != null)
        {
            List<string> list = new List<string>();
            for(int i = 0; i < anim.clips.Length; ++i)
            {
                list.Add(anim.clips[i].name);
            }
            clipsName = list.ToArray();
            //限定范围
            defaultPlayingClipIndex.intValue = Mathf.Clamp(defaultPlayingClipIndex.intValue, 0, anim.clips.Length);
        }
        if (clipsName != null)
        {
            EditorGUI.BeginChangeCheck();
            //绘制下拉框（属性名称、索引号、下拉框选项名称）
            defaultPlayingClipIndex.intValue = EditorGUILayout.Popup("Default Playing", defaultPlayingClipIndex.intValue, clipsName);
            if (EditorGUI.EndChangeCheck())
            {
                player.Player.Play(clipsName[defaultPlayingClipIndex.intValue]);
            }
        }
        #endregion
        //应用属性修改（一定要调用）
        serializedObject.ApplyModifiedProperties();
    }

    private void Awake()
    {
        time = Time.realtimeSinceStartup;
        //EditorApplication.update += UpdateHandler;

        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;

        if (player != null)
        {
            player.Init();
        }
    }

    private void OnDestroy()
    {
        EditorApplication.update -= UpdateHandler;
    }

    // EditorApplication.update添加的委托，该脚本绑定的游戏对象被选中后以一定频率调用
    private void UpdateHandler()
    {
        float deltaTime = Time.realtimeSinceStartup - time;
        time = Time.realtimeSinceStartup;

        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;
        if (player != null)
        {
            player.Update_Editor(deltaTime);
        }

        //场景视图重新绘制？
        //动画播放后，场景视图重新绘制
        foreach(var sceneView in SceneView.sceneViews)
        {
            if (sceneView is SceneView)
            {
                (sceneView as SceneView).Repaint();
            }
        }
    }

    private void BeginBox()
    {
        EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
        EditorGUILayout.Space();
    }

    private void EndBox()
    {
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
    }
}

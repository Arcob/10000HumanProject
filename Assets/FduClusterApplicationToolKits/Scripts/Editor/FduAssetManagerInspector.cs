/*
 * FduAssetManagerInspector
 * 
 * 简介：FduClusterAssetManager所对应的Editor类
 * 负责显示和相关操作
 * 
 * 最后修改时间: Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduClusterAssetManager))]
public class FduAssetManagerInspector : Editor {

    SerializedProperty m_assestList;
    //绘制窗口
    public override void OnInspectorGUI()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("============Asset List============", style);
        string name;
        for (int i = 0; i < m_assestList.arraySize; ++i)
        {
            try
            {
                name = ((GameObject)m_assestList.GetArrayElementAtIndex(i).objectReferenceValue).name;
                EditorGUILayout.LabelField("Assetid: " + i, name);
            }
            catch (System.NullReferenceException)
            {
                EditorGUILayout.HelpBox("Asset List changed. Please press refresh!", MessageType.Error);  
            }
        }
        if (GUILayout.Button("Refresh"))
        {
            _refreshAssetList();
        }
        if (GUILayout.Button("Clear"))
        {
            ClearData();
        }
    }

    void OnEnable()
    {
        initAttribute();
    }
    void initAttribute()
    {
        m_assestList = serializedObject.FindProperty("gameObjectAssetList");
    }
    //刷新所有携带clusterView组件的预制体
    public void _refreshAssetList()
    {
        var list = Resources.FindObjectsOfTypeAll<FduClusterView>();
        m_assestList.ClearArray();
        int i = 0;
        foreach (FduClusterView view in list)
        {
            if (EditorUtility.IsPersistent(view.gameObject))
            {
                m_assestList.InsertArrayElementAtIndex(m_assestList.arraySize);
                m_assestList.GetArrayElementAtIndex(m_assestList.arraySize - 1).objectReferenceValue = view.gameObject;

                Editor e = Editor.CreateEditor(view);
                e.serializedObject.FindProperty("AssetId").intValue = i;
                e.serializedObject.ApplyModifiedProperties();
                i++;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
    //删除所有数据
    public void ClearData()
    {
        var list = Resources.FindObjectsOfTypeAll<FduClusterView>();
        m_assestList.ClearArray();
        int i = 0;
        foreach (FduClusterView view in list)
        {
            if (EditorUtility.IsPersistent(view.gameObject))
            {
                Editor e = Editor.CreateEditor(view);
                e.serializedObject.FindProperty("AssetId").intValue = -1;
                e.serializedObject.ApplyModifiedProperties();
                i++;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
    //由菜单栏调用的静态刷新数据函数
    public static void refreshAssetList(){

        var list = Resources.FindObjectsOfTypeAll<FduClusterAssetManager>();
        if (list != null)
        {
            foreach (FduClusterAssetManager _instance in list)
            {
                if (EditorUtility.IsPersistent(_instance.gameObject)) //第一轮 找到预制体并刷新资源
                {
                    FduAssetManagerInspector ins = (FduAssetManagerInspector)Editor.CreateEditor(_instance);
                    ins._refreshAssetList();
                    ins.serializedObject.ApplyModifiedProperties();
                    break;
                }
            }
            foreach (FduClusterAssetManager _instance in list)
            {
                if (!EditorUtility.IsPersistent(_instance.gameObject)) //第二轮 将场景中的预制体重置
                {
                    PrefabUtility.ResetToPrefabState(_instance.gameObject);
                }
            }
            EditorUtility.DisplayDialog("FduClusterToolKit", "Asset Refresh Completed", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("FduClusterToolKit", "Can not find Fdu Cluster Asset Manager!", "OK");
        }
    }
}

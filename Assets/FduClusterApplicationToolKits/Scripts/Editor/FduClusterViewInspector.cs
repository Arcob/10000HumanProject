/*
 * FduClusterViewInspector
 * 
 * 简介：FduClusterView的editor类
 * 提供了cluster view在inspector中的显示和操作的相关功能
 * 
 * 最后修改时间：Hayate 2017.07.09
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduClusterView))]
public class FduClusterViewInspector : Editor {


    //监控列表
    SerializedProperty m_observerList;
    //这个View所对应gameObject的assetId 用于创建
    SerializedProperty m_assetId;

    //子view列表
    SerializedProperty m_subViewList;
    //父亲view
    SerializedProperty m_parentView;


    bool _advaceSettingFold = false; 

    FduClusterView m_target;

    void OnEnable()
    {
        InitAttribute();
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        m_target = (FduClusterView)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("ViewID", m_target.ViewId.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("ViewID","Set at run time");
        }

        if (EditorUtility.IsPersistent(m_target.gameObject) && m_target.gameObject.transform.parent == null)
        {
            EditorGUILayout.LabelField("SubviewCount", m_subViewList.arraySize.ToString());
            for (int i = 0; i < m_subViewList.arraySize; ++i)
            {
                EditorGUILayout.LabelField("sub view name", m_subViewList.GetArrayElementAtIndex(i).objectReferenceValue.name);
            }
            EditorGUILayout.BeginHorizontal();
            m_target.IsObservingCreation = EditorGUILayout.Toggle("Observe Creation", m_target.IsObservingCreation);
            if (m_target.IsObservingCreation)
            {
                EditorGUILayout.LabelField("Asset Id", m_assetId.intValue.ToString());
                if (m_assetId.intValue < 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("This Prefab is not added to AssetManager. Please find FduClutserAssetManager and press refresh.", MessageType.Error);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.BeginHorizontal();
        m_target.IsObservingDestruction = EditorGUILayout.Toggle("Observe Destruction", m_target.IsObservingDestruction);
        m_target.IsObservingActiveState = EditorGUILayout.Toggle("Observe Active State*", m_target.IsObservingActiveState);
        EditorGUILayout.EndHorizontal();


        var bold = new GUIStyle();
        bold.fontStyle = FontStyle.Bold;
        _advaceSettingFold = EditorGUILayout.Foldout(_advaceSettingFold, "Advance Setting");
       
        if (_advaceSettingFold)
        {
            EditorGUILayout.BeginHorizontal();
            m_target.IsImmediatelyDeserialize = EditorGUILayout.Toggle("Is ImmediatelyDeserialize", m_target.IsImmediatelyDeserialize);
            m_target.IsAutomaticllySend = EditorGUILayout.Toggle("Is Automaticlly Send", m_target.IsAutomaticllySend);
            EditorGUILayout.EndHorizontal();
            m_target.resendPriority = (FDUObjectSync.Message.ResendPriority)EditorGUILayout.EnumPopup("Data Resend Priority", m_target.resendPriority);
        }

       

        if (m_parentView.objectReferenceValue!=null)
            EditorGUILayout.LabelField("ParentView", m_parentView.objectReferenceValue.name);

        if (m_target.IsObservingActiveState)
        {
            EditorGUILayout.HelpBox("When you are using observing active state,please review considerations in the document.",MessageType.Info);
        }

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField(new GUIContent("Observer List",FduEditorGUI.getObserverIcon()), style);
        if (Application.isPlaying) //程序运行时不允许额外操作
        {

            List<FduObserverBase>.Enumerator enumerator = m_target.getObservers();
            int i = 0;
            while (enumerator.MoveNext())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i+" "+enumerator.Current.gameObject.name + "(" + enumerator.Current.GetType().Name + ")");
                EditorGUILayout.EndHorizontal();
                i++;
            }
        }
        else 
        {
            SerializedProperty property;
            bool hintFlag = false;
            for (int i = 0; i < m_observerList.arraySize; ++i)
            {
                property = m_observerList.GetArrayElementAtIndex(i);
                if (property.objectReferenceValue == null || !m_target.Equals(((FduObserverBase)(property.objectReferenceValue)).GetClusterView())) //如果obsever列表中的值为空 或者对应observer所属的view不是本view 则需要重新刷新
                    hintFlag = true;
                else
                    EditorGUILayout.LabelField(((FduObserverBase)property.objectReferenceValue).gameObject.name + "(" + ((FduObserverBase)property.objectReferenceValue).GetType().FullName + ")");

            }
            for (int i = 0; i < m_subViewList.arraySize; ++i)
            {
                property = m_subViewList.GetArrayElementAtIndex(i);
                if (property.objectReferenceValue == null)
                    hintFlag = true;
            }
            if (hintFlag)
            {
                Refresh();
            }
            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
        }
        
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
    //移除view
    void RemoveView()
    {
        if (m_parentView.objectReferenceValue != null)
        {
            var ed = Editor.CreateEditor(m_parentView.objectReferenceValue);
            var prop = ed.serializedObject.FindProperty("subViewList");
            int index = -1;
            for (index = 0; index < prop.arraySize; ++index)
            {
                if (prop.GetArrayElementAtIndex(index).objectReferenceValue.Equals(m_target))
                {
                    break;
                }
            }
            if (index > 0 && index < prop.arraySize)
            {
                prop.DeleteArrayElementAtIndex(index);
            }
            ed.serializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            DestroyImmediate(m_target);
        }
    }
    //刷新数据
    public void Refresh()
    {
        refreshObsevrverList();
        refreshSubViewList();
        serializedObject.ApplyModifiedProperties();
    }
    //刷新所有Observer数据 搜索所有子节点中有哪些observer还没有所属的view 则将这些observer收归自己管理
    void refreshObsevrverList()
    {
        var list = m_target.GetComponentsInChildren<FduObserverBase>(true);
        while (m_observerList.arraySize > 0)
        {
            m_observerList.DeleteArrayElementAtIndex(0);
        }
        foreach (FduObserverBase ob in list)
        {
            if (ob.GetClusterView() == null)
            {
                Editor ed = Editor.CreateEditor(ob);
                ed.serializedObject.FindProperty("_viewInstance").objectReferenceValue = m_target;
                ed.serializedObject.ApplyModifiedProperties();

                m_observerList.InsertArrayElementAtIndex(0);
                m_observerList.GetArrayElementAtIndex(0).objectReferenceValue = ob;
            }
            else if (ob.GetClusterView().Equals(m_target))
            {
                m_observerList.InsertArrayElementAtIndex(0);
                m_observerList.GetArrayElementAtIndex(0).objectReferenceValue = ob;
            }
        }
    }
    //刷新子view列表 只有在预制体的根节点上才有用
    void refreshSubViewList()
    {
        if (EditorUtility.IsPersistent(m_target.gameObject) && m_target.gameObject.transform.parent == null) //确保其是prefab并且是根节点
        {
            var subViews = m_target.GetComponentsInChildren<FduClusterView>(true);
            while (m_subViewList.arraySize > 0)
                m_subViewList.DeleteArrayElementAtIndex(0);
            foreach(FduClusterView subview in subViews)
            {
                if (!subview.Equals(m_target))
                {
                    m_subViewList.InsertArrayElementAtIndex(0);
                    m_subViewList.GetArrayElementAtIndex(0).objectReferenceValue = subview;
                    var e = Editor.CreateEditor(subview);
                    e.serializedObject.FindProperty("parentView").objectReferenceValue = m_target;
                    e.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
    //初始化变量
    void InitAttribute()
    {
        m_observerList = serializedObject.FindProperty("observerList");
        m_assetId = serializedObject.FindProperty("AssetId");
        m_subViewList = serializedObject.FindProperty("subViewList");
        m_parentView = serializedObject.FindProperty("parentView");

    }


}

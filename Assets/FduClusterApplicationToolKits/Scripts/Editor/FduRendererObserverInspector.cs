/*
 * FduRendererObserverInspector
 * 
 * 简介：FduRendererObserver对应的editor类
 * 负责选择render组件是否监控enbale状态，选择监控哪些shader属性
 * 注意这个Inspector操作，在选择了不同shader属性后，不能直接触发unity的数据变更保存提示（例如场景的层级视图中，有属性变化会出来一个*标识）
 * 需要点击Confirm键 再Control+s 才能保存
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using UnityEditor;
[CustomEditor(typeof(FduRendererObserver))]
public class FduRendererObserverInspector : FduMultiAttrObserverInspectorBase {

    FduRendererObserver m_target;
    //Shader属性
    List<FduShaderSyncAttr> attrList;
    //BitArray列表 一个BItarray代表一个shader中属性监控保存的值
    List<BitArray> bitList = new List<BitArray>();
    //每一个代表是否折叠起这个shader的参数列表
    List<bool> foldList = new List<bool>();
    SerializedProperty m_forceSaveFlag;


    public override string[] getAttributeList()
    {
        return FduRendererObserver.attributeList;
    }

    void OnEnable()
    {
        initAttribute();
        m_target = (FduRendererObserver)target;
        m_forceSaveFlag = serializedObject.FindProperty("forceSaveFlag");
        attrList = m_target.getShaderSyncAttrList();
        if (attrList == null)
        {
            m_target.refreshData();
            attrList = m_target.getShaderSyncAttrList();
            serializedObject.ApplyModifiedProperties();
        }

        refreshBitListData();
        refreshFoldData();
    }
    //刷新位数组列表
    void refreshBitListData()
    {
        if (attrList == null)
            return;
        bitList.Clear();
        foreach (FduShaderSyncAttr attr in attrList)
        {
            if (attr.bitarr == null)
            {
                bitList.Add(new BitArray(attr.propertyType.Count + 1, false));
            }
            else
            {
                bitList.Add(new BitArray(attr.bitarr));
            }
        }
    }
    //刷新折叠数据
    void refreshFoldData()
    {
        if (attrList == null)
            return;
        foldList.Clear();
        foreach (FduShaderSyncAttr attr in attrList)
        {
            foldList.Add(false);
        }
    }
    //绘制面板
    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAttributeField();
        DrawShaderSyncAttributeField();
        if (GUILayout.Button("Refresh"))
        {
            m_target.refreshData();
            attrList = m_target.getShaderSyncAttrList();
            serializedObject.ApplyModifiedProperties();
            refreshBitListData();
            refreshFoldData();
        }
        if (GUILayout.Button("Confirm"))
        {
            m_forceSaveFlag.boolValue = !m_forceSaveFlag.boolValue;
        }
        OnGUIChanged();
    }
    //绘制所有shader对应的属性 以及选择是否监控该属性的开关
    void DrawShaderSyncAttributeField()
    {
        bool btemp = false;
        bool changeFlag = false;
        if (attrList == null)
        {
            return;
        }
        var style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Shader Properties", style);
        for (int i = 0; i < attrList.Count; ++i)
        {
            foldList[i] = EditorGUILayout.Foldout(foldList[i],"Material "+i +": " + attrList[i].materialName);
            if (!foldList[i])
                continue;
            string propertyMsg = "";
            for (int j = 0; j < attrList[i].propertyType.Count; ++j)
            {
                propertyMsg = "Property Name:" +attrList[i].propertyName[j] + " Type:" + ((ShaderUtil.ShaderPropertyType)attrList[i].propertyType[j]);
                if ((ShaderUtil.ShaderPropertyType)attrList[i].propertyType[j] == ShaderUtil.ShaderPropertyType.TexEnv)
                    propertyMsg += " (Not Support Yet)";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(propertyMsg);
                btemp = EditorGUILayout.Toggle(bitList[i][j]);
                if (btemp != bitList[i][j])
                {
                    bitList[i][j] = btemp;
                    changeFlag = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (changeFlag)
            {
                m_target.setObState(i, bitList[i]);
                changeFlag = false;
            }
        }
    }

}

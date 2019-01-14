/*
 * FduUIInputFieldObserverInspector
 * 
 * 简介：FduUIInputFieldObserver对应的Editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduUIInputFieldObserver))]
public class FduUIInputFieldObserverInspector :FduMultiAttrObserverInspectorBase {
    void OnEnable()
    {
        initAttribute();
    }

    public override string[] getAttributeList()
    {
        return FduUIInputFieldObserver.attrList;
    }

    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAttributeField();

        DrawRemoveFuncField();

        OnGUIChanged();
    }
    //选择是否移除数据框上的一些回调函数(只在从节点上会移除)
    void DrawRemoveFuncField()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("=======Remove Call back Option======", style);
        states[FduGlobalConfig.BIT_MASK[29]] = EditorGUILayout.Toggle("Remove On Value Change CallBack On Slave", states[FduGlobalConfig.BIT_MASK[29]]);
        states[FduGlobalConfig.BIT_MASK[30]] = EditorGUILayout.Toggle("Remove On End Edit CallBack On Slave", states[FduGlobalConfig.BIT_MASK[30]]);
    }
}

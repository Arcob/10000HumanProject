/*
 * FduUISliderObserverInspector
 * 
 * 简介：FduUISliderObserver对应的Editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduUISliderObserver))]
public class FduUISliderObserverInspector : FduMultiAttrObserverInspectorBase {

    void OnEnable()
    {
        initAttribute();
    }
    public override string[] getAttributeList()
    {
        return FduUISliderObserver.attrList;
    }
    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAttributeField();

        DrawRemoveFuncField();
        OnGUIChanged();
    }
    //选择是否移除滑动条的callback（只在从节点上移除）
    void DrawRemoveFuncField()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("=======Remove Call back Option======", style);
        states[FduGlobalConfig.BIT_MASK[30]] = EditorGUILayout.Toggle("Remove On Value Change CallBack On Slave", states[FduGlobalConfig.BIT_MASK[30]]);
    }

}

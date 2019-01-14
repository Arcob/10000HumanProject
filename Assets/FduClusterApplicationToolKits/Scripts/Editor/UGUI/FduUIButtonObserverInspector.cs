/*
 * FduUIButtonObserverInspector
 * 
 * 简介：FduUIButtonObserver对应的Editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduUIButtonObserver))]
public class FduUIButtonObserverInspector : FduMultiAttrObserverInspectorBase {

    void OnEnable()
    {
        initAttribute();
    }

    public override string[] getAttributeList()
    {
        return FduUIButtonObserver.attrList;
    }
    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAttributeField();

        DrawRemoveFuncField();
        OnGUIChanged();
    }
    //选择是否移除按钮的CallBack（只会在从节点上移除）
    void DrawRemoveFuncField()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("=======Remove Call back Option======", style);
        states[FduGlobalConfig.BIT_MASK[30]] = EditorGUILayout.Toggle("Remove OnClick CallBack On Slave", states[FduGlobalConfig.BIT_MASK[30]]);
    }


}

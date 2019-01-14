/*
 * FduTransformObserver_EXInspector
 * 
 * 简介：FduTransformObserver_EX对应的Editor类
 * transform监控器加强版的editor
 * 
 * 最后修改时间: Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduTransformObserver_Ex))]
public class FduTransform_ExObserverInspector : FduMultiAttrObserverInspectorBase
{


    void OnEnable()
    {
        initAttribute();
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAttributeField();
        OnGUIChanged();
    }

    public override string[] getAttributeList()
    {
        return FduTransformObserver_Ex.attributeList;
    }
}

/*
 * FduUIRectTransformObserverInspector
 * 
 * 简介：FduUIRectTransformObserver对应的Editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduUIRectTransformObserver))]
public class FduUIRectTransformObserverInspector : FduMultiAttrObserverInspectorBase {

    void OnEnable()
    {
        initAttribute();
    }
    public override string[] getAttributeList()
    {
        return FduUIRectTransformObserver.attrList;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

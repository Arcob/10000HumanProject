/*
 * FduUIImageObserverInspector
 * 
 * 简介：FduUIImageObserver对应的Editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;

[CustomEditor(typeof(FduUIImageObserver))]
public class FduUIImageObserverInspector : FduMultiAttrObserverInspectorBase {

    void OnEnable()
    {
        initAttribute();
    }
    public override string[] getAttributeList()
    {
        return FduUIImageObserver.attrList;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

}

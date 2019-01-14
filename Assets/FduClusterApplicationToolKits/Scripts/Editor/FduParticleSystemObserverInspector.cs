/*
 * FduParticleSystemObserverInspector
 * 
 * 简介：FduParticleSystemObserver对应的editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduParticleSystemObserver))]
public class FduParticleSystemObserverInspector :FduMultiAttrObserverInspectorBase {

    void OnEnable()
    {
        initAttribute();
    }

    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        OnGUIChanged();
    }

}

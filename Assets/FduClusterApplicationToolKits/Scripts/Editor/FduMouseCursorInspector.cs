/*
 * FduMouseCursorInspector
 * 
 * 简介：FduMouseCursor对应的editor类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduMouseCursor))]
public class FduMouseCursorInspector : Editor {

    //两种获取鼠标坐标的模式 一种是screenSpace 则对世界坐标直接赋值；另一种是WorldSpace，则需要通过射线的方式定位鼠标位置
    enum mouseCousorPositionType
    {
        ScreenSpace,WorldSpace
    }

    SerializedProperty m_positionType;
    SerializedProperty m_rayDistance;

    mouseCousorPositionType mcpt;
    int rayDis;

    void OnEnable()
    {
        m_positionType = serializedObject.FindProperty("positionType");
        m_rayDistance = serializedObject.FindProperty("rayDistance");
        mcpt = (mouseCousorPositionType)m_positionType.intValue;
        rayDis = m_rayDistance.intValue;
    }
    //绘制窗口
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        mcpt = (mouseCousorPositionType)EditorGUILayout.EnumPopup("Canvas Render mode",mcpt);

        if (mcpt == mouseCousorPositionType.WorldSpace)
            rayDis = EditorGUILayout.IntField("RayDistance",rayDis);

        if (GUI.changed)
        {
            m_positionType.intValue = (int)mcpt;
            m_rayDistance.intValue = rayDis;
            serializedObject.ApplyModifiedProperties();
        }

    }

}

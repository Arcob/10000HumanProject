/*
 * FduAnimatorObserverInspector
 * 
 * 简介：FduAnimatorObserver对应的editor类
 * 负责刷新和显示对应animator组件的参数 所有动画参数都默认传递
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using FDUClusterAppToolKits;
[CustomEditor(typeof(FduAnimatorObserver))]
public class FduAnimatorObserverInspector : FduMultiAttrObserverInspectorBase
{
    Animator animator;
    AnimatorController m_Controller;
    FduAnimatorObserver m_target;

    void OnEnable()
    {
        initAttribute();
        m_target = (FduAnimatorObserver)target;
        animator = m_target.GetComponent<Animator>();
        
        this.m_Controller = this.GetEffectiveController(this.animator) as AnimatorController;
        refreshAnimatorAttribute();
    }
    //刷新对应animator中的参数 并保存
    void refreshAnimatorAttribute()
    {
        m_target.getParameterList().Clear();
        AnimatorControllerParameter para;
        for (int i = 0; i < GetParameterCount(); ++i)
        {
            para = GetAnimatorControllerParameter(i);
            FduAnimatorParameter param = new FduAnimatorParameter(para.type,para.name);
            m_target.getParameterList().Add(param);
        }
        serializedObject.ApplyModifiedProperties();
    }
    //绘制窗口
    public override void OnInspectorGUI()
    {
        DrawClusterViewField();
        DrawDataTransmitStrategyField();
        DrawAnimatorParameters();
        if (GUILayout.Button("Refresh Animator Parameter"))
        {
            refreshAnimatorAttribute();
        }
        OnGUIChanged();
    }
    //绘制动画控制器所有的参数
    void DrawAnimatorParameters()
    {
        EditorGUILayout.LabelField("Animator Parameters", FduEditorGUI.getTextCenter());
        EditorGUILayout.BeginHorizontal();
        var list = m_target.getParameterList();
        for (int i = 0; i < list.Count; ++i)
        {
            EditorGUILayout.LabelField(list[i].name + "(" +list[i].type + ")");
            if (i % 2 == 1)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    //获取控制器
    private RuntimeAnimatorController GetEffectiveController(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;

        AnimatorOverrideController overrideController = controller as AnimatorOverrideController;
        while (overrideController != null)
        {
            controller = overrideController.runtimeAnimatorController;
            overrideController = controller as AnimatorOverrideController;
        }

        return controller;
    }
    //获取参数个数
    private int GetParameterCount()
    {
        return (this.m_Controller == null) ? 0 : this.m_Controller.parameters.Length;
    }
    //根据index获取动画控制器参数
    private AnimatorControllerParameter GetAnimatorControllerParameter(int i)
    {
        return this.m_Controller.parameters[i];
    }

}

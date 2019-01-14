/*
 * FduMultiAttrObserverInspectorBase
 * 
 * 简介：工具包中所有Observer对应的Editor的基类
 * 基类中包括了所有基础功能：绘制ClusterView功能板，绘制数据传输类功能板，绘制选择监控属性功能板等
 * 
 * 继承自该类可以重新编写OnInspectorGUI函数 自定义编排在Inspector中的显示内容
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using FDUClusterAppToolKits;
public  abstract class FduMultiAttrObserverInspectorBase : Editor {

    //数据传输策略类的枚举变量
    protected EditorDTSEnum dtsEnum = EditorDTSEnum.Direct;
    //数据传输策略类的输入参数
    protected string parameter = "";
    //数据传输策略类的类名
    protected string strategyName = "";

    //数据传输类策略类类名 序列化属性
    protected SerializedProperty m_strategyName;
    //数据传输策略类参数 序列化属性
    protected SerializedProperty m_parameter;
    //监控属性列表
    protected SerializedProperty m_observedState;
    //Clusterview实例 序列化属性
    protected SerializedProperty m_clusterView;
    //位数组（定长）,每一个位代表是否监控该属性
    protected System.Collections.Specialized.BitVector32 states;

    void OnEnable()
    {

    }
    //初始化属性
    protected void initAttribute()
    {
        try
        {
            m_strategyName = serializedObject.FindProperty("dataTransmitStrategyName");
            m_parameter = serializedObject.FindProperty("dataTransmitStrategyParameter");
            m_observedState = serializedObject.FindProperty("observedState_backup");
            m_clusterView = serializedObject.FindProperty("_viewInstance");

            parameter = m_parameter.stringValue;
            strategyName = m_strategyName.stringValue;
            states = new System.Collections.Specialized.BitVector32(m_observedState.intValue);
            dtsEnum = name2DtsEnum(strategyName);
        }
        catch (System.NullReferenceException)
        {
            //Debug.LogWarning(e.Message);
        }
    }
    //获取监控属性列表 派生类可以通过重写此函数 修改最终显示的属性名
    public virtual string[] getAttributeList()
    {
        return null;
    }
    //绘制cluster view功能面板
    protected void DrawClusterViewField()
    {
        string clusterViewName = "Not Registered";

        if (m_clusterView != null && m_clusterView.objectReferenceValue != null)
        {
            clusterViewName = m_clusterView.objectReferenceValue.name + "(" + m_clusterView.objectReferenceValue.GetType().FullName + ")";
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cluster View", clusterViewName);
        if (!Application.isPlaying && GUILayout.Button("Register"))
        {
            if (m_clusterView.objectReferenceValue == null)
            {
                FduClusterView view = ((FduObserverBase)target).findViewInstance();
                if(view !=null)
                {
                    m_clusterView.objectReferenceValue = view;
                    view.registToView(((FduObserverBase)target));
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        if (!Application.isPlaying && GUILayout.Button("Reset"))
        {
            if (m_clusterView.objectReferenceValue != null)
            {
                ((FduObserverBase)target).removeFromClusterView_editor();
                m_clusterView.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.EndHorizontal();
    }
    //绘制数据传输策略类功能面板
    protected void DrawDataTransmitStrategyField()
    {
        if (!Application.isPlaying)
        {
            dtsEnum = (EditorDTSEnum)EditorGUILayout.EnumPopup("Data Transmit Strategy", dtsEnum);

            EditorGUILayout.BeginHorizontal();
            if (dtsEnum == EditorDTSEnum.CustomStrategy)
                strategyName = EditorGUILayout.TextField("Strategy Name", strategyName);
            else
                strategyName = dtsEnum2Name(dtsEnum);

            if (dtsEnum == EditorDTSEnum.EveryNFrame)
            {
                int interval =2;
                string [] paras;
                FduDTS_EveryNFrame.InterpolationOption interOp = FduDTS_EveryNFrame.InterpolationOption.Disable;
                FduDTS_EveryNFrame.ExtrapolationOption extraOp = FduDTS_EveryNFrame.ExtrapolationOption.Disable;
                int cachedPropertyCount = 1;
                int lerpSpeed = 1;
                try
                {
                    paras = parameter.Split('&');
                    interval = int.Parse(paras[0]);
                    interOp = (FduDTS_EveryNFrame.InterpolationOption)int.Parse(paras[1]);
                    extraOp = (FduDTS_EveryNFrame.ExtrapolationOption)int.Parse(paras[2]);
                    cachedPropertyCount = int.Parse(paras[3]);
                    lerpSpeed = int.Parse(paras[4]);
                }
                catch (System.Exception)
                {
                    interval = 2;
                }
                EditorGUILayout.EndHorizontal();
                

                interval = EditorGUILayout.IntSlider("Frame Interval",interval, 2, FduGlobalConfig.EVERY_N_FRAME_MAX_FRAME);
                interOp = (FduDTS_EveryNFrame.InterpolationOption)EditorGUILayout.EnumPopup("Interpolation Option", interOp);
                extraOp = (FduDTS_EveryNFrame.ExtrapolationOption)EditorGUILayout.EnumPopup("Extrapolation Option", extraOp);
                cachedPropertyCount = EditorGUILayout.IntSlider("Cached Property Max Count", cachedPropertyCount, 1, 10);
                if(interOp == FduDTS_EveryNFrame.InterpolationOption.Lerp)
                    lerpSpeed = EditorGUILayout.IntSlider("Lerp Speed", lerpSpeed, 1, 60);
                parameter = interval + "&" + (int)interOp + "&" + (int)extraOp + "&" + cachedPropertyCount + "&" + lerpSpeed;
                EditorGUILayout.BeginHorizontal();
            }
            else{
                if (dtsEnum == EditorDTSEnum.OnClusterCommand)
                {
                    parameter = EditorGUILayout.TextField("Command Name", parameter);
                }
                else if (dtsEnum == EditorDTSEnum.CustomStrategy)
                {
                    parameter = EditorGUILayout.TextField("Custom string Parameter", parameter);
                }
                states[FduGlobalConfig.BIT_MASK[31]] = false;
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            FduObserverBase m_target = (FduObserverBase)target;
            if (m_target.getDataTransmitStrategy() != null)
                EditorGUILayout.LabelField("Strategy Class Name", m_target.getDataTransmitStrategy().GetType().FullName);
            else
                EditorGUILayout.LabelField("Data-transmit-strategy class instance is null");
        }

    }
    //绘制自定义监控属性功能面板
    protected void DrawAttributeField()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField(new GUIContent("Observed Attributes",FduEditorGUI.getAttributeIcon()),style);
        EditorGUILayout.BeginHorizontal();

        if (Application.isPlaying)
            states = new System.Collections.Specialized.BitVector32(((FduMultiAttributeObserverBase)target).getObservedIntData());

        string [] attrList  = getAttributeList();
        if(attrList == null) return;
        for (int i = 1; i < attrList.Length; ++i)
        {
            if (!Application.isPlaying)
                states[FduGlobalConfig.BIT_MASK[i]] = EditorGUILayout.Toggle(attrList[i], states[FduGlobalConfig.BIT_MASK[i]]);
            else
            {
                EditorGUILayout.Toggle(attrList[i], states[FduGlobalConfig.BIT_MASK[i]]);
            }
            
            if (i % 2 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    //视图发生变化时做出的保存操作
    protected void OnGUIChanged()
    {
        if (GUI.changed)
        {
            m_strategyName.stringValue = strategyName;
            m_parameter.stringValue = parameter;
            m_observedState.intValue = states.Data;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
    //一般性的绘制函数，可以直接使用，也可以覆盖重写
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawClusterViewField();

        DrawDataTransmitStrategyField();

        DrawAttributeField();

        OnGUIChanged();

    }
    //根据数据传输策略类的枚举变量获取数据传输策略类的类名
    protected string dtsEnum2Name(EditorDTSEnum dtsEnum)
    {
        return FduGlobalConfig.editorDTSEnum2nameMap[dtsEnum];
    }
    //根据数据传输策略类的类名获取数据传输策略类的枚举变量
    protected EditorDTSEnum name2DtsEnum(string dtsName)
    {
        var query = from d in FduGlobalConfig.editorDTSEnum2nameMap
                    where d.Value == dtsName
                    select d.Key;
        if (query.Count() < 0)
            return EditorDTSEnum.CustomStrategy;
        else
            return query.ToList()[0];
    }



}

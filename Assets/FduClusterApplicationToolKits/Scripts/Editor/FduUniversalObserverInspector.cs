using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using UnityEditor;
using System;
[CustomEditor(typeof(FduUniversalObserver))]
public class FduUniverseObserverInspector : FduMultiAttrObserverInspectorBase {

    List<string> attributeList = new List<string>();

    SerializedProperty m_BitArrayString;

    SerializedProperty m_ComponentProperty;

    BitArray _bitArray;

    void OnEnable()
    {
        
        m_BitArrayString = serializedObject.FindProperty("_bitArrayJson");
        m_ComponentProperty = serializedObject.FindProperty("_ObservedComponent");
        refreshAttribute();

        if (m_BitArrayString.stringValue != null && m_BitArrayString.stringValue.Length>0)
        {
            _bitArray = new BitArray(JsonUtility.FromJson<FduUniversalObserver.BitArrayContainer>(m_BitArrayString.stringValue).arr);
        }
        else
        {
            _bitArray = new BitArray(attributeList.Count);
            _bitArray.SetAll(false);
        }
        
        
        initAttribute();
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();

        int lastId = m_ComponentProperty.objectReferenceInstanceIDValue;

        EditorGUILayout.PropertyField(m_ComponentProperty, new GUIContent("Component"));

        if (m_ComponentProperty.objectReferenceInstanceIDValue != lastId)
        {
            refreshAttribute();
        }

        DrawClusterViewField();
        
        DrawDataTransmitStrategyField();

        DrawPropertiesView();
        //DrawAttributeField();
        
        OnGUIChanged();
    }

    void DrawPropertiesView()
    {

        if (!Application.isPlaying && GUILayout.Button("Refresh"))
        {
            refreshAttribute();
        }

        if (m_ComponentProperty.objectReferenceValue == null) return;

        if (Application.isPlaying)
        {
            _bitArray = ((FduUniversalObserver)target).getBitArray();
        }
        

        var _component = m_ComponentProperty.objectReferenceValue;
        Type type = _component.GetType();
        var list = type.GetProperties();
        EditorGUILayout.BeginHorizontal();
        for (int i = 0,j=0; i < attributeList.Count; ++i)
        {
            if (FduSupportClass.isSendableGenericType(list[i].PropertyType) && list[i].CanRead && list[i].CanWrite)
            {
                if(Application.isPlaying)
                    EditorGUILayout.Toggle(attributeList[i], _bitArray[i]);
                else
                    _bitArray[i] = EditorGUILayout.Toggle(attributeList[i] + "(" + list[i].PropertyType.Name + ")", _bitArray[i]);

                if (j % 2 == 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                j++;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (!Application.isPlaying && GUILayout.Button("Select All"))
        {
            selectAllFunc(list);
        }
        if (!Application.isPlaying && GUILayout.Button("Inverse"))
        {
            inverseAllFunc(list);
        }
        EditorGUILayout.EndHorizontal();

        if (!Application.isPlaying)
        {
            FduUniversalObserver.BitArrayContainer con = new FduUniversalObserver.BitArrayContainer();
            con.arr = new byte[list.Length / 8 + 1];
            _bitArray.CopyTo(con.arr, 0);
            m_BitArrayString.stringValue = JsonUtility.ToJson(con);
            serializedObject.ApplyModifiedProperties();
        }
    }

    void refreshAttribute()
    {
        if (m_ComponentProperty.objectReferenceValue != null)
        {
            var _component = m_ComponentProperty.objectReferenceValue;
            Type type = _component.GetType();
            var list = type.GetProperties();
            _bitArray = new BitArray(list.Length);
            attributeList.Clear();
            for (int i = 0; i < list.Length; ++i)
            {
                attributeList.Add(list[i].Name);
            }
            _bitArray.SetAll(false);
        }
        else
        {
            attributeList.Clear();
            _bitArray = new BitArray(1);
            _bitArray.SetAll(false);
        }
    }

    void selectAllFunc(System.Reflection.PropertyInfo [] list)
    {
        for (int i = 0; i < attributeList.Count; ++i)
        {
            if (FduSupportClass.isSendableGenericType(list[i].PropertyType) && list[i].CanRead && list[i].CanWrite)
            {
                _bitArray[i] = true;
            }
        }
    }
    void inverseAllFunc(System.Reflection.PropertyInfo[] list)
    {
        for (int i = 0; i < attributeList.Count; ++i)
        {
            if (FduSupportClass.isSendableGenericType(list[i].PropertyType) && list[i].CanRead && list[i].CanWrite)
            {
                _bitArray[i] = !_bitArray[i];
            }
        }
    }



}

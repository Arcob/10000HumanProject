using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//useless
//[CustomEditor(typeof(GPUSkinningController))]
public class GPUSkinningControllerEditor : Editor {

    public override void OnInspectorGUI()
    {
        GPUSkinningController controller = target as GPUSkinningController;
        if (controller == null)
        {
            return;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        GPUSkinningAnimation anim = serializedObject.FindProperty("anim").objectReferenceValue as GPUSkinningAnimation;
        serializedObject.ApplyModifiedProperties();
    }
}

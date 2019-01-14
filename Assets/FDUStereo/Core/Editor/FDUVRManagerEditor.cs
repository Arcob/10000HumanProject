using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(FDUVRManager))]
public class FDUVRManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        FDUVRManager _target = (FDUVRManager)target;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("Create", GUILayout.Width(117)))
        {
            _target.Init();
        }
        if (GUILayout.Button("Clear", GUILayout.Width(117)))
        {
            _target.Clear();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("LoadConfig", GUILayout.Width(117)))
        {
            _target.LoadConfig();
        }
        if (GUILayout.Button("SaveConfig", GUILayout.Width(117)))
        {
            _target.SaveConfig();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        base.OnInspectorGUI();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}

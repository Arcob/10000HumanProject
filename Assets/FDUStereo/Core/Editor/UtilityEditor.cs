using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class UtilityEditor : EditorWindow
{
    private static UtilityEditor _instance;

    [MenuItem("SH/UtilityEditor")]
    public static void Init()
    {
        if (_instance == null)
        {
            _instance = GetWindow<UtilityEditor>();
        }
    }

    private string m_strValue = "NewGeometryProxy";
    void OnGUI()
    {
        m_strValue = EditorGUILayout.TextField("Name", m_strValue);
        if (GUILayout.Button("Create"))
        {
            CreateNewGeometryProxyClass(m_strValue);
        }
    }

    private static void CreateNewGeometryProxyClass(string className)
    {
        TextWriter writer = new StreamWriter("Assets/" + className + ".cs");
        writer.WriteLine("using UnityEngine;");
        writer.WriteLine("using System.Collections;");
        writer.WriteLine("");
        writer.WriteLine("#if UNITY_PLUGIN");
        writer.WriteLine("namespace SH");
        
        writer.WriteLine("{");
        writer.WriteLine("[System.Serializable]");
        writer.WriteLine("#else");
        writer.WriteLine("using SH;");
        writer.WriteLine("#endif");
        writer.WriteLine("public class " + className + " : GeometryProxy");
        writer.WriteLine("{");
        writer.WriteLine("}");
        writer.WriteLine("#if UNITY_PLUGIN");
        writer.WriteLine("}");
        writer.WriteLine("#endif");
        writer.Close();

        AssetDatabase.Refresh();
    }
}

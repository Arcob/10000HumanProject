using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
public class testwindow : EditorWindow {

    static testwindow instance;
    Color co = Color.white;
    //[MenuItem("FduCluster/aaa")]
    public static void create()
    {
#if CLUSTER_ENABLE
        if (instance == null)
        {
            instance = (testwindow)EditorWindow.GetWindowWithRect(typeof(testwindow), new Rect(0,0,800,800), false, "test console");
            instance.Show();
        }
#else
        Debug.Log("Cluster is disabled.");
#endif
    }

    void OnGUI()
    {


        //GL.LoadPixelMatrix();
        //GL.PushMatrix();
        //GL.Begin(GL.LINES);
        //GL.Color(Color.red);
        //GL.Vertex3(100, 100, 0);
        //GL.Vertex3(100, 200, 0);
        //GL.Vertex3(200, 200, 0);
        //GL.Vertex3(200, 100, 0);
        //GL.End();
        //GL.PopMatrix();


        GUI.Box(new Rect(100, 100, 100, 100), "");

        co = EditorGUILayout.ColorField("Color", co);

        EditorGUILayout.LabelField(co.ToString());

        
    }

    void OnInspectorUpdate()
    {
        this.Repaint();
    }
}

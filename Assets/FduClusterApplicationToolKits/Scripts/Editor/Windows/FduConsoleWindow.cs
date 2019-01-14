/*
 * FduConsoleWindow
 * 
 * 简介：控制台窗口类，可以在窗口里添加多个子窗口 子窗口需要继承自FduConsoleSubwindowBase
 * 在控制台中可以切换不同的子窗口
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
//集群Debug工具窗口
public class FduConsoleWindow : EditorWindow {

    //静态实例
    static FduConsoleWindow instance;
    //子窗口大小
    public static readonly Rect subWindowRect = new Rect(10, 50, 680, 640);
    //窗口大小
    static readonly Rect windowRect = new Rect(0, 0, 700, 700);
    //子窗口实例
    List<FduConsoleSubwindowBase> subwindows;
    //当前子窗口的下标
    int curSubWindowIndex = 0;
    //上一次子窗口下标
    int oldSubWindowIndex = 0;
    //子窗口名字
    string[] subwindowNames;

    //提示用的Icon
    public Texture hintTexture;
    //警告用的Icon
    public Texture warningTexture;

    void initResources()
    {
        subwindowNames = new string[] { "Profile", "Cluster View", "Cluster Command", "Command Executor","CommandGraph" };

        hintTexture = FduEditorGUI.getHintIcon();
        warningTexture = FduEditorGUI.getWarningIcon();
    }
    //初始化子窗口实例
    static void initSubwindowInstance()
    {
        if (instance != null)
        {
            instance.subwindows = new List<FduConsoleSubwindowBase>();
            //instance.subwindows.Clear();
            instance.subwindows.Add(new FduProfileSubWindow());
            instance.subwindows.Add(new FduClusterViewSubWindow());
            instance.subwindows.Add(new FduClusterCommandSubWindow());
            instance.subwindows.Add(new FduClusterCommandExecutorSubWindow());
            instance.subwindows.Add(new FduClusterCommandGraphSubWindow());
            if (instance.subwindows != null)
            {
                foreach (FduConsoleSubwindowBase sub in instance.subwindows)
                {
                    sub.parentWindow = instance;
                    sub.Awake();
                    sub.OnEnable();
                }
            }
        }
        else
        {
            create();
        }
    }
    //创建窗口
    public static void create()
    {
#if CLUSTER_ENABLE
        if (instance == null)
        {
            instance = (FduConsoleWindow)EditorWindow.GetWindowWithRect(typeof(FduConsoleWindow), windowRect, false, "Cluster Console");
            initSubwindowInstance();
            instance.subwindows[instance.curSubWindowIndex].OnEnter();
            instance.Show();
        }
#else
        EditorUtility.DisplayDialog("FduClusterTool", "Cluster is disabled", "OK");
#endif
    }

    void Awake()
    {
        initResources();
    }


    void OnEnable()
    {
        if (subwindows != null)
        {
            foreach (FduConsoleSubwindowBase sub in subwindows)
            {
                sub.OnEnable();
            }
        }
    }
    void OnDisable()
    {
        if (subwindows != null)
        {
            foreach (FduConsoleSubwindowBase sub in subwindows)
            {
                sub.OnDisable();
            }
        }
    }
    void OnGUI()
    {

#if !CLUSTER_ENABLE
        GUI.Label(windowRect, new GUIContent("Cluster Is Disable", warningTexture), FduEditorGUI.getTitleStyle_LevelOne());
        return;
#endif
        //该控制台是主节点还是从节点的提示label
        string nodeText = "";
        if (ClusterHelper.Instance != null && ClusterHelper.Instance.Server != null)
            nodeText = "-MasterNode";
        if (ClusterHelper.Instance != null && ClusterHelper.Instance.Client != null)
            nodeText = "-SlaveNode";

        GUILayout.Label("Console"+nodeText, FduEditorGUI.getTitleStyle_LevelOne(), GUILayout.Width(windowRect.width - 6));
        curSubWindowIndex = GUILayout.Toolbar(curSubWindowIndex, subwindowNames, GUILayout.Width(windowRect.width - 6));

        GUI.Box(new Rect(subWindowRect.x - 3, subWindowRect.y - 3, subWindowRect.width + 6, subWindowRect.height + 6), "");

        if (subwindows != null)
        {
            //窗口发生切换 触发OnExit和OnEnter函数
            if (oldSubWindowIndex != curSubWindowIndex)
            {
                if (oldSubWindowIndex >= 0)
                    subwindows[oldSubWindowIndex].OnExit();
                subwindows[curSubWindowIndex].OnEnter();
                oldSubWindowIndex = curSubWindowIndex;
            }
            try
            {
                subwindows[curSubWindowIndex].DrawSubWindow();
            }
            catch (System.ArgumentException) { } //由于非运行和运行状态切换时 某几帧会报错 错误原因上不明了 但不影响运行
        }
        else
            initSubwindowInstance();
    }

    //每帧更新
    void Update()
    {
        if (subwindows != null)
        {
            foreach (FduConsoleSubwindowBase sub in subwindows)
            {
                sub.Update();
            }
            if (subwindows[curSubWindowIndex].repaintFrequency == SubWindowRepaintFrequency.everyFrame)
            {
                this.Repaint();
            }
        }

    }

    void OnFocus()
    {
        //Debug.Log("当窗口获得焦点时调用一次");
    }

    void OnLostFocus()
    {
        //Debug.Log("当窗口丢失焦点时调用一次");
    }

    void OnHierarchyChange()
    {
        //Debug.Log("当Hierarchy视图中的任何对象发生改变时调用一次");
    }

    void OnProjectChange()
    {
        //Debug.Log("当Project视图中的资源发生改变时调用一次");
    }
    //每10帧调用 重新绘制
    void OnInspectorUpdate()
    {
        if (subwindows != null)
        {
            foreach (FduConsoleSubwindowBase sub in subwindows)
            {
                sub.OnInspectorUpdate();
            }
            if (subwindows[curSubWindowIndex].repaintFrequency == SubWindowRepaintFrequency.OnInspectorUpdate)
                this.Repaint();
        }
        //Debug.Log("窗口面板的更新");
        //这里开启窗口的重绘，不然窗口信息不会刷新
    }

    void OnSelectionChange()
    {
        //foreach (Transform t in Selection.transforms)
        //{
        //    //有可能是多选，这里开启一个循环打印选中游戏对象的名称
        //    Debug.Log("OnSelectionChange" + t.name);
        //}
    }

    void OnDestroy()
    {
        if (subwindows != null)
        {
            foreach (FduConsoleSubwindowBase sub in subwindows)
            {
                sub.OnDestroy();
            }
        }
    }

}

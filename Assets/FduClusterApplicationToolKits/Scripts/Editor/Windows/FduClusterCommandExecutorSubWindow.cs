/*
 * FduClusterCommandExecutorSubWindow
 * 
 * 简介：控制台集群事件监听器子窗口
 * 在这个子窗口可以查看所有该节点的事件监听器
 * 
 * 最近修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;

public class FduClusterCommandExecutorSubWindow : FduConsoleSubwindowBase {

    //监听器视图当前的滚动位置
    Vector2 CommandScrollPos = Vector2.zero;
    //搜索文本
    string searchText_CommandExecutor = "";
    //是否搜索的flag
    bool searchingFlag = false;
    //提示文本
    string hintText = "";

    Rect ExecutorScroll;
    //绘制子窗口
    public override void DrawSubWindow()
    {
        var leftOffset = new GUIStyle();
        leftOffset.margin.left = 10;

        if (!Application.isPlaying)
        {
            GUI.Label(subWindowRect, new GUIContent("You can get the information of Command Executor at run time", parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }

        //画Box背景
        GUI.Box(ExecutorScroll, "");
        //views = FduClusterViewManager.getClusterViews();


        //==========================================搜索与总数部分Start===================================

        if (ClusterHelper.Instance != null && ClusterHelper.Instance.Server != null)
            hintText = "Master Node Command Executors";
        if (ClusterHelper.Instance != null && ClusterHelper.Instance.Client != null)
            hintText = "Slave Node Command Executors";

        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("Command Executor Count ", FduClusterCommandDispatcher.getExecutorCount().ToString(), leftOffset);
        EditorGUILayout.LabelField(hintText);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));


        searchText_CommandExecutor = EditorGUILayout.TextField("Search", searchText_CommandExecutor, GUILayout.Width(subWindowRect.width * 0.5f), GUILayout.Height(20));
        EditorGUILayout.Space();
        if (GUILayout.Button("Clear", GUILayout.Height(21)))
        {
            searchText_CommandExecutor = "";
            searchingFlag = false;
            EditorGUI.FocusTextInControl("");
        }
        if (GUILayout.Button("Search", GUILayout.Height(21)))
        {
            searchingFlag = true;
            EditorGUI.FocusTextInControl("");
        }
        EditorGUILayout.EndHorizontal();
        //==========================================搜索与总数部分End===================================




        //==========================================Executor列表部分Start===================================
        leftOffset.margin.left += 15;
        var center = new GUIStyle();
        center.alignment = TextAnchor.MiddleCenter;

        float len1, len2, len3, len4, len5;
        len1 = ExecutorScroll.width * 0.3f;
        len2 = ExecutorScroll.width * 0.3f;
        len3 = ExecutorScroll.width * 0.3f;
        len4 = ExecutorScroll.width * 0.43f;
        len5 = ExecutorScroll.width * 0.1f;

        var labelStyle = new GUIStyle();
        labelStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("Executor Info", labelStyle, GUILayout.Width(len1));
        EditorGUILayout.LabelField("Observed Command", labelStyle, GUILayout.Width(len2));
        EditorGUILayout.LabelField("Executor Target", labelStyle, GUILayout.Width(len3));
        EditorGUILayout.EndHorizontal();

        if (FduClusterCommandDispatcher.getExecutorCount() <= 0)
        {
            GUI.Label(ExecutorScroll, new GUIContent("No Registed Command Executor",parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }

        EditorGUILayout.Space();

        CommandScrollPos = EditorGUILayout.BeginScrollView(CommandScrollPos, leftOffset, GUILayout.Width(ExecutorScroll.width - 10), GUILayout.Height(ExecutorScroll.height - 10));

        var Executors = FduClusterCommandDispatcher.getExecutors();
        int listCount = 0;
        while (Executors.MoveNext())
        {
            var subExecutors = Executors.Current.Value.ActionMap.GetEnumerator();
            while (subExecutors.MoveNext())
            {
                EditorGUILayout.BeginHorizontal();
                string exeInfo = string.Format("ID:{0} Name:{1}",subExecutors.Current.Key,subExecutors.Current.Value.Method.Name);
                EditorGUILayout.LabelField(exeInfo, GUILayout.Width(len1));
                EditorGUILayout.LabelField(Executors.Current.Key, GUILayout.Width(len2));
                if(subExecutors.Current.Value.Target!=null)
                    EditorGUILayout.LabelField(subExecutors.Current.Value.Target.ToString(), GUILayout.Width(len3));
                else
                    EditorGUILayout.LabelField("NULL", GUILayout.Width(len3));
                EditorGUILayout.EndHorizontal();
                listCount++;
            }
        }
        //为了强制显示scroll view 的进度条 BeginScrollView里面的alwaysShowVertical参数没用
        for (int i = listCount; i < 35; ++i)
        {
            EditorGUILayout.LabelField("");
        }
        EditorGUILayout.EndScrollView();

        //==========================================Executor列表部分End===================================

    }
    void initRects()
    {
        ExecutorScroll = new Rect(subWindowRect.x + 11, subWindowRect.y + 65, subWindowRect.width - 22, subWindowRect.height * 0.85f);
    }
    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
        EditorGUI.FocusTextInControl("");
    }

    public override void OnEnable() { }

    public override void OnDisable() { }

    public override void Update() { }

    public override void Awake()
    {
        initRects();
    }

    public override void OnDestroy() { }

    bool checkSearchText(string ExecutorName,string CommandName)
    {
        if (searchText_CommandExecutor == "")
            return true;
        if (CommandName.ToUpper().Contains(searchText_CommandExecutor.ToUpper()) || ExecutorName.ToUpper().Contains(searchText_CommandExecutor.ToUpper()))
        {
            return true;
        }
        return false;
    }

}

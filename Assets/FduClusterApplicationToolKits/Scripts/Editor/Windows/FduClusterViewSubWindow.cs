/*
 * FduClusterViewSubWindow
 * 
 * 简介：控制台 Cluster view的子窗口
 * 可以查看和搜索所有view的基本信息
 * 
 * 最后修改时间：Hayate 2017.07.09
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
public class FduClusterViewSubWindow : FduConsoleSubwindowBase
{
    //view面板 滚动的当前位置
    Vector2 viewScrollPos = Vector2.zero;
    //view的detail面板 滚动的当前位置
    Vector2 detailScrollPos = Vector2.zero;
    //搜索文本
    string searchText_view = "";
    //是否在搜索flag
    bool searchingFlag = false;

    Rect viewScroll;
    Rect detailScroll;
    //所有view的映射表
    Dictionary<int, FduClusterView>.Enumerator views;
    //已选择的viewID
    int selectedViewId = -1;
    //绘制子窗口
    public override void DrawSubWindow()
    {
        var leftOffset = new GUIStyle();
        leftOffset.margin.left = 10;
        if (!Application.isPlaying)
        {
            GUI.Label(subWindowRect, new GUIContent("You can get the information of cluster views at run time", parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }

        //画两个Box 上面用于列表 下面用于详细内容
        GUI.Box(viewScroll, "");
        GUI.Box(detailScroll, "");

        views = FduClusterViewManager.getClusterViews();

        FduClusterView view = null;
       
        //==========================================搜索与总数部分Start===================================
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("Total views ", FduClusterViewManager.getViewCount().ToString(), leftOffset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(leftOffset,GUILayout.Width(subWindowRect.width));
        
        
        searchText_view = EditorGUILayout.TextField("Search", searchText_view, GUILayout.Width(subWindowRect.width*0.5f),GUILayout.Height(20));
        EditorGUILayout.Space();
        if (GUILayout.Button("Clear", GUILayout.Height(21)))
        {
            Debug.Log("click clear");
            searchText_view = "";
            searchingFlag = false;
            EditorGUI.FocusTextInControl("");
        }
        if (GUILayout.Button("Search",GUILayout.Height(21)))
        {
            searchingFlag = true;
            EditorGUI.FocusTextInControl("");
        }
        EditorGUILayout.EndHorizontal();
        //==========================================搜索与总数部分End===================================


        //==========================================View列表部分Start===================================
        leftOffset.margin.left += 15;
        //var center = new GUIStyle();
        //center.alignment = TextAnchor.MiddleCenter;
        var labelStyle = new GUIStyle();
        labelStyle.fontStyle = FontStyle.Bold;
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("ViewId", labelStyle, GUILayout.Width(viewScroll.width * 0.07f));
        EditorGUILayout.LabelField("Name", labelStyle, GUILayout.Width(viewScroll.width * 0.25f));
        EditorGUILayout.LabelField("Path", labelStyle, GUILayout.Width(viewScroll.width * 0.5f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        

        var Nonestyle = new GUIStyle();
        Nonestyle.name = "a";

        viewScrollPos = EditorGUILayout.BeginScrollView(viewScrollPos, leftOffset, GUILayout.Width(viewScroll.width - 10), GUILayout.Height(viewScroll.height - 10));
        string id, name, path;

        float len1, len2, len3, len4;
        len1 = viewScroll.width * 0.07f;
        len2 = viewScroll.width * 0.25f;
        len3 = viewScroll.width * 0.5f;
        len4 = viewScroll.width * 0.1f;

        int listCount = 0;
        while (views.MoveNext())
        {
            view = views.Current.Value;
            if (view == null)
            {
                //EditorGUILayout.HelpBox("One view is disappeared but reference still exist in cluster manager!", MessageType.Warning);
            }
            else
            {
                id = view.ViewId.ToString();
                name = view.name;
                path = FduSupportClass.getGameObjectPath(view.gameObject);
                if (!searchingFlag || checkSearchText(id, name, path))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(id, GUILayout.Width(len1));
                    EditorGUILayout.LabelField(name, GUILayout.Width(len2));
                    EditorGUILayout.LabelField(path, GUILayout.Width(len3));
                    if (GUILayout.Button("Detail", GUILayout.Width(len4)))
                    {
                        selectedViewId = views.Current.Value.ViewId;
                    }
                    EditorGUILayout.EndHorizontal();
                    listCount++;
                }
            }
        }
        bool noViewFlag = false;
        if (listCount == 0)
            noViewFlag = true;


        //为了强制显示scroll view 的进度条 BeginScrollView里面的alwaysShowVertical参数没用
        for (int i = listCount; i < 22; ++i)
        {
            EditorGUILayout.LabelField("");
        }
        EditorGUILayout.EndScrollView();

        if(noViewFlag)
            GUI.Label(viewScroll, new GUIContent("No Registed View",parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());


      

        //for (int i = 0; i < 500; ++i)
        //{
        //    EditorGUILayout.BeginHorizontal();
        //    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(len1));
        //    EditorGUILayout.LabelField("tentententententenssssssssssssssssssssss", GUILayout.Width(len2));
        //    EditorGUILayout.LabelField("miao/ssss/wwww/qqqq/aaaa/zzzz/xxxx/ccccddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd", GUILayout.Width(len3));
        //    GUILayout.Button("Yes" + i, GUILayout.Width(len4));
        //    EditorGUILayout.EndHorizontal();
        //}

        
        //==========================================View列表部分End===================================

        var selectedView = FduClusterViewManager.getClusterView(selectedViewId);

        if (selectedView == null)
        {
            GUI.Label(detailScroll, new GUIContent("No Selected View",parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            selectedViewId = -1;
            return;
        }


        //==========================================View detail 部分===================================
        leftOffset.margin.top = 25;
        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos, leftOffset, GUILayout.Width(detailScroll.width - 10), GUILayout.Height(detailScroll.height - 10));
        var pstyle = new GUIStyle();
        pstyle.wordWrap = true;
        pstyle.margin.left = 5;



        EditorGUILayout.LabelField("ViewId: " + selectedView.ViewId + " GameObject Name: " + selectedView.name + " Observer Count: " +selectedView.getObserverCount(), GUILayout.Width(detailScroll.width - 50.0f));
        EditorGUILayout.LabelField("Path: " + FduSupportClass.getGameObjectPath(selectedView.gameObject), pstyle, GUILayout.Width(detailScroll.width - 50.0f));

        if (selectedView.getObserverCount() <= 0)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GameObject Name", GUILayout.Width(detailScroll.width * 0.3f));
        EditorGUILayout.LabelField("Observer Type", GUILayout.Width(detailScroll.width * 0.3f));
        EditorGUILayout.LabelField("DTS Type", GUILayout.Width(detailScroll.width * 0.2f));
        EditorGUILayout.LabelField("DTS Parameter", GUILayout.Width(detailScroll.width * 0.3f));
        EditorGUILayout.EndHorizontal();

        List<FduObserverBase>.Enumerator observers = selectedView.getObservers();
        string dtsName = "NULL";
        string dtsPara = "NULL";

        while (observers.MoveNext())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(observers.Current.name, GUILayout.Width(detailScroll.width * 0.3f));
            EditorGUILayout.LabelField(observers.Current.GetType().FullName, GUILayout.Width(detailScroll.width * 0.3f));
            if (observers.Current.getDataTransmitStrategy() != null)
            {
                dtsName = observers.Current.getDataTransmitStrategy().GetType().FullName;
                dtsPara = getDTSParaInfo(observers.Current.getDataTransmitStrategy());
                //dtsPara = observers.Current.getDataTransmitStrategy().getCustomData().ToString();
            }
            EditorGUILayout.LabelField(dtsName, GUILayout.Width(detailScroll.width * 0.2f));
            EditorGUILayout.LabelField(dtsPara, GUILayout.Width(detailScroll.width * 0.3f));
            EditorGUILayout.EndHorizontal();
        }

        //for (int i = 0; i < 10; ++i)
        //{
        //    EditorGUILayout.BeginHorizontal();
        //    EditorGUILayout.LabelField("Observer GameObject name",GUILayout.Width(detailScroll.width*0.3f));
        //    EditorGUILayout.LabelField("FduTransformObserver", GUILayout.Width(detailScroll.width * 0.3f));
        //    EditorGUILayout.LabelField("FduDTS_Direct", GUILayout.Width(detailScroll.width * 0.2f));
        //    EditorGUILayout.LabelField("OnGetPlayerSetIKEvent", GUILayout.Width(detailScroll.width * 0.3f));
        //    EditorGUILayout.EndHorizontal();
        //}
        EditorGUILayout.EndScrollView();
        //==========================================View detail 部分===================================
    }
    //检查该文本是否通过搜索关键字测试
    bool checkSearchText(string id, string name, string path)
    {
        if (searchText_view == "")
            return true;
        if (id.Contains(searchText_view) || name.ToUpper().Contains(searchText_view.ToUpper()) || path.ToUpper().Contains(searchText_view.ToUpper()))
        {
            return true;
        }
        return false;
    }
    void initRects()
    {
        viewScroll = new Rect(subWindowRect.x+11, subWindowRect.y+65, subWindowRect.width-22, subWindowRect.height*0.6f);
        detailScroll = new Rect(subWindowRect.x+11, viewScroll.y + viewScroll.height+15, subWindowRect.width-22, subWindowRect.height*0.25f);
    }
    public override void OnEnter() {
    }

    public override void OnExit() {
        EditorGUI.FocusTextInControl("");
    }
    public override void OnEnable() { 
        
    }

    public override void OnDisable() { }

    public override void Update() { }

    public override void Awake() {
        initRects();
    }

    public override void OnDestroy() { }
    //根据数据传输策略类获取展示的信息
    string getDTSParaInfo(FduDataTransmitStrategyBase dts)
    {
        string res = "";
        if (dts.GetType().Equals(typeof(FduDTS_EveryNFrame)))
        {
            res = "Interval:" + dts.getCustomData("interval") + " Cur Count:" + dts.getCustomData("curFrameCount");
        }
        else
        {
            res = dts.getCustomData().ToString();
        }
        return res;
    }
}

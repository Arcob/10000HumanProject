/*
 * FduClusterCommandSubWindow
 * 
 * 简介：控制台中的集群事件查看子窗口
 * 可以查看历史上发生了哪些集群事件 以及集群事件包含了哪些参数
 * 
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
public class FduClusterCommandSubWindow : FduConsoleSubwindowBase
{
    //Command窗口当前的滚动位置
    Vector2 CommandScrollPos = Vector2.zero;
    //Command detail窗口当前的滚动位置
    Vector2 detailScrollPos = Vector2.zero;
    //搜索文本
    string searchText_Command = "";
    //是否搜索的flag
    bool searchingFlag = false;

    Rect CommandScroll;
    Rect detailScroll;
    //统计类的实例
    ClusterCommandStatisticClass statistic;
    //Command detail展示用的数据
    ClusterCommandShowData detailData = null;
    //绘制窗口
    public override void DrawSubWindow()
    {
        var leftOffset = new GUIStyle();
        leftOffset.margin.left = 10;

        if (!Application.isPlaying)
        {
            GUI.Label(subWindowRect, new GUIContent("You can get the information of cluster Commands at run time", parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }

        //==========================================搜索与总数部分Start===================================
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("Cached Command Count ", ClusterCommandStatisticClass.instance.getCommandCount().ToString(), leftOffset);
        EditorGUILayout.EndHorizontal();

        //画两个Box 上面用于列表 下面用于详细内容
        GUI.Box(CommandScroll, "");
        GUI.Box(detailScroll, "");

        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));


        searchText_Command = (string)EditorGUILayout.TextField("Search", searchText_Command, GUILayout.Width(subWindowRect.width * 0.5f), GUILayout.Height(20)).Clone();
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Clear", GUILayout.Height(21)))
        {
            Debug.Log("click clear");
            searchText_Command = "";
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


        if (ClusterCommandStatisticClass.instance.getCommandCount() <= 0)
        {
            GUI.Label(CommandScroll, new GUIContent("No Cached Command",parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
        }

        //==========================================Command列表部分Start===================================
        leftOffset.margin.left += 15;

        float len1, len2, len3, len4,len5;
        len1 = CommandScroll.width * 0.25f;
        len2 = CommandScroll.width * 0.07f;
        len3 = CommandScroll.width * 0.07f;
        len4 = CommandScroll.width * 0.43f;
        len5 = CommandScroll.width * 0.1f;

        var labelStyle = new GUIStyle();
        labelStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(leftOffset, GUILayout.Width(subWindowRect.width));
        EditorGUILayout.LabelField("Command Name", labelStyle, GUILayout.Width(len1));
        EditorGUILayout.LabelField("Frame", labelStyle, GUILayout.Width(len2));
        EditorGUILayout.LabelField("Parameter Count And Names", labelStyle, GUILayout.Width(len3 * 5));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        int listCount = 0; 

        CommandScrollPos = EditorGUILayout.BeginScrollView(CommandScrollPos, leftOffset, GUILayout.Width(CommandScroll.width - 10), GUILayout.Height(CommandScroll.height - 10));


        var enu = ClusterCommandStatisticClass.instance.getStatisticCommandData();
        string CommandName;
        while (enu.MoveNext())
        {
            CommandName = enu.Current.e.getCommandName();
            if(!searchingFlag || checkSearchText(CommandName)){

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(CommandName, GUILayout.Width(len1));
                EditorGUILayout.LabelField(enu.Current.frameCount.ToString(), GUILayout.Width(len2));
                EditorGUILayout.LabelField(enu.Current.e.getParameterCount().ToString(), GUILayout.Width(len3));
                EditorGUILayout.LabelField(getCommandParameters(enu.Current.e), GUILayout.Width(len4));
                if (GUILayout.Button("Check", GUILayout.Width(len5)))
                {
                    detailData = new ClusterCommandShowData(enu.Current.e, enu.Current.frameCount);
                }
                EditorGUILayout.EndHorizontal();
                listCount++;
            }
        }

        //为了强制显示scroll view 的进度条 BeginScrollView里面的alwaysShowVertical参数没用
        for (int i = listCount; i < 22; ++i)
        {
            EditorGUILayout.LabelField("");
        }

        EditorGUILayout.EndScrollView();
        //==========================================Command列表部分End===================================

        //==========================================Command detail 部分===================================
        if (detailData == null)
        {
            GUI.Label(detailScroll, new GUIContent("No Selected Command",parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }

        leftOffset.margin.top = 25;
        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos, leftOffset, GUILayout.Width(detailScroll.width - 10), GUILayout.Height(detailScroll.height - 10));


        EditorGUILayout.LabelField("CommandName: " + detailData.e.getCommandName() + " Raised Frame: " + detailData.frameCount + " Parameter Count: " + detailData.e.getParameterCount(), GUILayout.Width(detailScroll.width - 50.0f));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Parameter Name", GUILayout.Width(detailScroll.width * 0.3f));
        EditorGUILayout.LabelField("Parameter Value", GUILayout.Width(detailScroll.width * 0.3f));
        EditorGUILayout.EndHorizontal();

        var paraMap = detailData.e.getMapEnumerator();
        while (paraMap.MoveNext())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(paraMap.Current.Key, GUILayout.Width(detailScroll.width * 0.3f));
            EditorGUILayout.LabelField(paraMap.Current.Value.ToString(), GUILayout.Width(detailScroll.width * 0.3f));
            EditorGUILayout.EndHorizontal();
        }

        var collMap = detailData.e.getCollectionsMapEnumerator();
        while (collMap.MoveNext())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(collMap.Current.Key, GUILayout.Width(detailScroll.width * 0.3f));
            string collectorInfo = getCollectionInfo(collMap.Current.Value);
            EditorGUILayout.LabelField(collectorInfo, FduEditorGUI.getWordWarp(),GUILayout.Width(detailScroll.width * 0.6f));
            EditorGUILayout.EndHorizontal();
        }

        //for (int i = 0; i < 10; ++i)
        //{
        //    EditorGUILayout.BeginHorizontal();
        //    EditorGUILayout.LabelField("Observer GameObject name", GUILayout.Width(detailScroll.width * 0.3f));
        //    EditorGUILayout.LabelField("FduTransformObserver", GUILayout.Width(detailScroll.width * 0.3f));
        //    EditorGUILayout.EndHorizontal();
        //}

        EditorGUILayout.EndScrollView();
        //==========================================Command detail 部分===================================

    }
    void initRects()
    {
        CommandScroll = new Rect(subWindowRect.x + 11, subWindowRect.y + 65, subWindowRect.width - 22, subWindowRect.height * 0.6f);
        detailScroll = new Rect(subWindowRect.x + 11, CommandScroll.y + CommandScroll.height + 15, subWindowRect.width - 22, subWindowRect.height * 0.25f);
    }
    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
        EditorGUI.FocusTextInControl("");
    }
    //启动对应数据统计类运行
    public override void OnEnable() {
        if (Application.isPlaying)
        {
            statistic = ClusterCommandStatisticClass.instance;
            ClusterCommandStatisticClass.isRunnig = true;
        }
    }
    //禁止对应数据统计类的运行
    public override void OnDisable() {

        if (Application.isPlaying)
        {
            ClusterCommandStatisticClass.isRunnig = false;
            ClusterCommandStatisticClass.instance.ClearData();
        }
    }

    public override void Update() { }

    public override void Awake()
    {
        initRects();
    }

    public override void OnDestroy() { }
    //根据集群事件类的实例获取该事件包含的所有参数的格式化显示字符串
    string getCommandParameters(ClusterCommand e)
    {
        string result = "";
        Dictionary<string, object>.Enumerator paras = e.getMapEnumerator();
        if (paras.MoveNext())
        {
            result += paras.Current.Key;
        }
        while (paras.MoveNext())
        {
            result += "," + paras.Current.Key;
        }

        var collparas = e.getCollectionsMapEnumerator();

        while (collparas.MoveNext())
        {
            result += "," + collparas.Current.Key; 
        }
        return result;
    }

    string getCollectionInfo(ClusterCommand.genericData data)
    {
        var keyType = System.Type.GetType(data.keyTypeName);
        var valueType = System.Type.GetType(data.valueTypeName);
        string result = string.Format("Collection Type:{0}, Key Type:{1},Value Type:{2}",data.containerTypeName,keyType.FullName,valueType.FullName);
        return result;
    }

    //检查该事件是否通过搜索关键字检查
    bool checkSearchText(string CommandName)
    {
        if (searchText_Command == "")
            return true;
        if (CommandName.ToUpper().Contains(searchText_Command.ToUpper()))
        {
            return true;
        }
        return false;
    }
}

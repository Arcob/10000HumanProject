/*
 * FduProfileSubWindow
 * 
 * 简介：控制台的Profile子窗口类
 * 可以实时展示数据传输、View、Observer的图表 也可以保存数据 并回放
 * 
 * 最后修改时间：Hayate 2017.07.09
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using FDUClusterAppToolKits;
public class FduProfileSubWindow : FduConsoleSubwindowBase
{
    //图表所需要的数据集
    public class ProfileChartDataSet
    {
        //存储点坐标数据
        public Dictionary<string, List<Vector2>> pointDataSet = new Dictionary<string,List<Vector2>>();
        //存储最大值标签的坐标
        public Vector2 maxValueLabelPoint;
        //存储当前值标签坐标
        public Dictionary<string, Vector2> curValueLabelPoint = new Dictionary<string, Vector2>();

        //存储当前值标签显示的数据
        public Dictionary<string, string> curValueLableString = new Dictionary<string, string>();
        //存储线的颜色
        public Dictionary<string, Color> lineColor = new Dictionary<string, Color>();
        //存储左侧icon的纹理
        public Texture IconTexture;
        //存储该图标的标题
        public string title;
        //存储每个绘制点之间的X轴间隔
        public float XAixsInterval;
        //存储最大值所对应的像素值 一般稍小于窗口的高度 不然看的丑
        public float YAxisTop;
        //X轴各点横坐标数据
        public List<int> XAixsValues;
        //X轴坐标数字所在点的位置Vector2（x,y） x从pointDataSet的x获取 y是一个定值，处于图标的最上方或最下方
        public float XaixsCorordinateHeight;
        //该图标目前达到的最大值 图标中Y轴的值由当前值与最大值的比值得出
        public float maxValue;
        //数据量
        public int maxDataCount;
        //线的颜色显示框
        public Dictionary<string, Texture2D> lineIconColor = new Dictionary<string, Texture2D>();
        //图标的自定义数据
        public Dictionary<string, object> customData = new Dictionary<string, object>();

        public void ClearPointData()
        {
            var enumator = pointDataSet.GetEnumerator();
            while (enumator.MoveNext())
            {
                enumator.Current.Value.Clear();
            }
            XAixsValues.Clear();
        }

    }

    //从文件中读取的数据结构
    public class FileDataSet
    {
        //================模拟ClusterViewStatisticClass中的数据结构==============
        public Queue<int> totalViewData = new Queue<int>();
        public Queue<int> activeViewData = new Queue<int>();
        public Queue<int> totalObserverData = new Queue<int>();
        public Queue<int> activeObserverData = new Queue<int>();
        public Queue<int> viewFrameNumberData = new Queue<int>();
        //===============模拟TransmitDataSizeStatisticClass中的数据结构==========
        public Queue<int> transmitDataSizeData = new Queue<int>();
        public Queue<int> transmitFrameData = new Queue<int>();

        FileStream readfs;
        FileStream writefs;
        BinaryReader br;
        BinaryWriter bw;

        public string savePath = "";

        bool isFileEnd = false;
        //读取前 初始化
        public bool readInit()
        {
            if (!File.Exists(FduEditorGUI.getProfileFilePath()))
            {
                EditorUtility.DisplayDialog("FduClusterTool","Can not find profile data file:" + FduEditorGUI.getProfileFilePath(),"I will check it");
                return false;
            }
            try
            {
                readfs = new FileStream(FduEditorGUI.getProfileFilePath(), FileMode.Open, FileAccess.Read);
                br = new BinaryReader(readfs);
                int code = br.ReadInt32();
                if (code != FduEditorGUI.getProfileDataVerifyCode())
                {
                    EditorUtility.DisplayDialog("FduClusterTool", "Can not load profile data file.The file format is corrupted. " + FduEditorGUI.getProfileFilePath(), "I will check it");
                    finishReading();
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Read Fdu Cluster Profile Data File Failed! " + e.Message);
                finishReading();
                return false;
            }
            return true;
        }
        //写入文件初始化
        public bool writeInit()
        {
            try
            {
                savePath = FduEditorGUI.getNewSavePath();
                writefs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                bw = new BinaryWriter(writefs);
                bw.Write(FduEditorGUI.getProfileDataVerifyCode());
                bw.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Write Fdu Cluster Profile data file Failed! " + e.Message);
                return false;
            }
            return true;
        }
        //从文件中读取一次(1帧)数据
        public void readDataOnce()
        {
            if (br != null && !isFileEnd)
            {
                try
                {
                    int totalView = br.ReadInt32();
                    int acView = br.ReadInt32();
                    int totalOb = br.ReadInt32();
                    int acOb = br.ReadInt32();
                    int viewFrame = br.ReadInt32();

                    int dataSize = br.ReadInt32();
                    int dataFrame = br.ReadInt32();

                    if (totalViewData.Count >= ClusterViewStatisticClass.MAX_FRAME_COUNT)
                        totalViewData.Dequeue();
                    totalViewData.Enqueue(totalView);

                    if (activeViewData.Count >= ClusterViewStatisticClass.MAX_FRAME_COUNT)
                        activeViewData.Dequeue();
                    activeViewData.Enqueue(acView);

                    if (totalObserverData.Count >= ClusterViewStatisticClass.MAX_FRAME_COUNT)
                        totalObserverData.Dequeue();
                    totalObserverData.Enqueue(totalOb);

                    if (activeObserverData.Count >= ClusterViewStatisticClass.MAX_FRAME_COUNT)
                        activeObserverData.Dequeue();
                    activeObserverData.Enqueue(acOb);

                    if (viewFrameNumberData.Count >= ClusterViewStatisticClass.MAX_FRAME_COUNT)
                        viewFrameNumberData.Dequeue();
                    viewFrameNumberData.Enqueue(viewFrame);

                    if (transmitDataSizeData.Count >= TransmitDataSizeStatisticClass.MAX_FRAME_COUNT)
                        transmitDataSizeData.Dequeue();
                    transmitDataSizeData.Enqueue(dataSize);

                    if (transmitFrameData.Count >= TransmitDataSizeStatisticClass.MAX_FRAME_COUNT)
                        transmitFrameData.Dequeue();
                    transmitFrameData.Enqueue(dataFrame);

                }
                catch (System.IO.EndOfStreamException)
                {
                    isFileEnd = true;
                    finishReading();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("An error occured when load cluster profile data from file. Details:"+e.Message);
                }
            }
        }
        //向文件中写入1次（1帧）数据
        public void writeDataOnce(int totalView,int acView,int totalOb,int acOb,int viewFrame,int dataSize,int dataFrame)
        {

            if (bw != null)
            {
                try
                {
                    bw.Write(totalView);
                    bw.Write(acView);
                    bw.Write(totalOb);
                    bw.Write(acOb);
                    bw.Write(viewFrame);

                    bw.Write(dataSize);
                    bw.Write(dataFrame);

                }catch(System.Exception e){

                    Debug.LogError("An error occured when write cluster profile data to file. Details:" + e.Message);
                }
            }
        }
        //将数据flush到文件中
        public void flushData()
        {
            if (bw != null)
                bw.Flush();
        }
        //读取完毕，结束读取操作
        public void finishReading()
        {
            if (br != null)
            {
                br.Close();
                br = null;
            }
            if (readfs != null)
            {
                readfs.Close();
                readfs = null;
            }
        }
        //写入完毕，结束写入操作
        public void finishWriting()
        {
            if (bw != null)
            {
                bw.Flush();
                bw.Close();
                bw = null;
            }
            if (writefs != null)
            {
                writefs.Close();
                writefs = null;
            }
        }
        //清除已读取的数据
        public void clearLoadedData()
        {
            totalViewData.Clear();
            activeViewData.Clear();
            totalObserverData.Clear();
            activeObserverData.Clear();
            viewFrameNumberData.Clear();
            transmitDataSizeData.Clear();
            transmitFrameData.Clear();
        }

    }

    bool inited = false;
    //左侧框的rect
    Rect leftLableBox;
    //右侧图表的Rect
    Rect rightGraphBox;
    //基准点 以基准点为基础做位移
    Vector2[] basePoints;
    //数据集实例
    ProfileChartDataSet viewDataSet;
    ProfileChartDataSet observerDataSet;
    ProfileChartDataSet dataTransmitDataSet;

    int testCount = 0;

    Queue<int> viewCountData =  new Queue<int>();
    Queue<int> activeCountData = new Queue<int>();

    Queue<int> NULLQueueData = new Queue<int>();
    //播放控制的图标
    GUIContent[] playControl = new GUIContent[] { new GUIContent(FduEditorGUI.getPlayIcon()), new GUIContent(FduEditorGUI.getPauseIcon()),new GUIContent(FduEditorGUI.getStopIcon())};

    Vector2[] points;
    //文件来源数据集实例
    FileDataSet fileDataSet = new FileDataSet();

    bool isFileStartLoaded = false;
    bool isFirstPlayRealTime = false;

    enum playControlEnum
    {
        play = 0,pause = 1,stop = 2,disable = 3
    }
    enum dataSourceEnum
    {
        RealTime = 0,File = 1
    }
    playControlEnum playState = playControlEnum.disable;
    dataSourceEnum dataSourceState = dataSourceEnum.RealTime;

    //绘制子窗口
    public override void DrawSubWindow()
    {
        if (!inited)
            return;

        testCount++;
        //FduEditorGUI.BeforGLDrawLine();
        GL.LoadPixelMatrix();
        //画三个灰色背景
        for (int i = 0; i < basePoints.Length; ++i)
        {
            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.99f, 0.99f, 0.99f));
            GL.Vertex3(basePoints[i].x + leftLableBox.width, basePoints[i].y, 0);
            GL.Vertex3(basePoints[i].x + leftLableBox.width + rightGraphBox.width, basePoints[i].y, 0);
            GL.Vertex3(basePoints[i].x + leftLableBox.width + rightGraphBox.width, basePoints[i].y + rightGraphBox.height, 0);
            GL.Vertex3(basePoints[i].x + leftLableBox.width, basePoints[i].y + rightGraphBox.height, 0);
            GL.End();
        }

        string[] names = new string[] { dataSourceEnum.RealTime.ToString(), dataSourceEnum.File.ToString() };

        dataSourceEnum tempSourceState = (dataSourceEnum)GUI.Toolbar(new Rect(10, 60, 200, 25),(int)dataSourceState, names);
        
        if(tempSourceState!=dataSourceState){ //处理发生数据源切换时候的逻辑
            if(!Application.isPlaying) //在非程序运行的时候 切换至realtime时希望表是空的 所以清除一下 不然相当于保留了从文件中读取的snapshot
                ClearPointData();
            if (playState == playControlEnum.pause) //暂停的时候 就更新一下
            {
                dataSourceState = tempSourceState;
                updateViewChartPointData();
                updateObserverChartData();
                updateDataTransmitChartData();
            }
        }
        dataSourceState = tempSourceState;

        playControlEnum tempState = (playControlEnum)GUI.Toolbar(new Rect(270, 60, 200, 25), (int)playState, playControl);
        if (tempState == playControlEnum.stop)
        {
            //处理停止逻辑
            ClearData();
            playState = playControlEnum.disable;
            isFileStartLoaded = false;
        }
        else
            playState = tempState;


        if (playState == playControlEnum.play)
        {
            if (!isFileStartLoaded && dataSourceState == dataSourceEnum.File)
            {
                if (!fileDataSet.readInit())
                {
                    playState = playControlEnum.disable;
                }
                else
                {
                    isFileStartLoaded = true;
                }
            }
            if (!isFirstPlayRealTime && dataSourceState == dataSourceEnum.RealTime && Application.isPlaying)
            {
                fileDataSet.writeInit();
                isFirstPlayRealTime = true;
            }
        }

        if(GUI.Button(new Rect(570, 60, 50, 25), "Clear"))
        {
            ClearData();
        }
        if (GUI.Button(new Rect(630, 60, 50, 25), "Save"))
        {
            
            if (Application.isPlaying)
            {
                var savedPath = fileDataSet.savePath;
                fileDataSet.finishWriting();
                fileDataSet.writeInit();
                EditorUtility.DisplayDialog("FduClusterTool", "Profile data is saved at "+savedPath, "OK");
            }
        }
        DrawObserverChart();
        DrawViewChart();
        DrawDataTransmitChart();
    }
    //绘制view图表
    void DrawViewChart()
    {
        DrawViewLeftInfoBox();
        
        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(basePoints[0].x + leftLableBox.width - 55, basePoints[0].y + rightGraphBox.height - viewDataSet.YAxisTop - 7.5f, 50, 15), ((int)viewDataSet.maxValue).ToString(), yaixLabelStyle);
        GUI.Label(new Rect(basePoints[0].x + leftLableBox.width - 55, basePoints[0].y + rightGraphBox.height - (viewDataSet.YAxisTop * 0.5f) - 7.5f, 50, 15), ((int)(viewDataSet.maxValue * 0.5f)).ToString(), yaixLabelStyle);
        
        FduEditorGUI.BeforGLDrawLine();
        //画最大值的线与中值的线
        if (viewDataSet.pointDataSet["total"].Count > 1)
        {

            GL.Begin(GL.LINES);
            GL.Color(FduEditorGUI.getCoordinateColor());
            GL.Vertex3(basePoints[0].x + leftLableBox.width, basePoints[0].y + rightGraphBox.height - viewDataSet.YAxisTop, 0);
            GL.Vertex3(basePoints[0].x + leftLableBox.width + rightGraphBox.width, basePoints[0].y + rightGraphBox.height - viewDataSet.YAxisTop, 0);

            GL.Vertex3(basePoints[0].x + leftLableBox.width, basePoints[0].y + rightGraphBox.height - (viewDataSet.YAxisTop * 0.5f), 0);
            GL.Vertex3(basePoints[0].x + leftLableBox.width + rightGraphBox.width, basePoints[0].y + rightGraphBox.height - (viewDataSet.YAxisTop * 0.5f), 0);

            GL.End();
        }
        //画横坐标
        var style = new GUIStyle();
        style.alignment = TextAnchor.LowerCenter;
        for (int i = 0; i < viewDataSet.XAixsValues.Count; ++i)
        {
            if (viewDataSet.XAixsValues[i] % 150 == 0)
            {
                FduEditorGUI.BeforGLDrawLine();
                GL.Begin(GL.LINES);
                GL.Color(FduEditorGUI.getCoordinateColor());
                GL.Vertex3(viewDataSet.pointDataSet["total"][i].x, basePoints[0].y + rightGraphBox.height - observerDataSet.YAxisTop, 0);
                GL.Vertex3(viewDataSet.pointDataSet["total"][i].x, viewDataSet.XaixsCorordinateHeight + rightGraphBox.height, 0);
                GL.End();

            }
        }
        //画总view数量的线
        GL.Begin(GL.LINES);
        GL.Color(viewDataSet.lineColor["total"]);
        if (viewDataSet.pointDataSet["total"].Count > 1)
        {
            for (int i = 0; i < viewDataSet.pointDataSet["total"].Count; ++i)
            {
                if (i == 0 || i == viewDataSet.pointDataSet["total"].Count - 1)
                {
                    GL.Vertex3(viewDataSet.pointDataSet["total"][i].x, viewDataSet.pointDataSet["total"][i].y, 0);
                }
                else
                {
                    GL.Vertex3(viewDataSet.pointDataSet["total"][i].x, viewDataSet.pointDataSet["total"][i].y, 0);
                    GL.Vertex3(viewDataSet.pointDataSet["total"][i].x, viewDataSet.pointDataSet["total"][i].y, 0);
                }
            }
        }
        GL.End();


        //画当前激活view数量的线
        GL.Begin(GL.LINES);
        GL.Color(viewDataSet.lineColor["active"]);
        if (viewDataSet.pointDataSet["active"].Count > 1)
        {
            for (int i = 0; i < viewDataSet.pointDataSet["active"].Count; ++i)
            {
                if (i == 0 || i == viewDataSet.pointDataSet["active"].Count - 1)
                {
                    GL.Vertex3(viewDataSet.pointDataSet["active"][i].x, viewDataSet.pointDataSet["active"][i].y, 0);
                }
                else
                {
                    GL.Vertex3(viewDataSet.pointDataSet["active"][i].x, viewDataSet.pointDataSet["active"][i].y, 0);
                    GL.Vertex3(viewDataSet.pointDataSet["active"][i].x, viewDataSet.pointDataSet["active"][i].y, 0);
                }
            }
        }
        GL.End();
    }
    //绘制view图表左侧的信息框
    void DrawViewLeftInfoBox()
    {

        GUI.Box(new Rect(basePoints[0].x, basePoints[0].y, leftLableBox.width, leftLableBox.height), "");
        var labelStyle = new GUIStyle();
        labelStyle.fontSize = 20;
        labelStyle.fontStyle = FontStyle.Normal;

        GUI.Label(new Rect(basePoints[0].x + 10, basePoints[0].y + 10, 150, 150), new GUIContent(" "+viewDataSet.title, viewDataSet.IconTexture), labelStyle);

        GUI.Box(new Rect(basePoints[0].x + 40, basePoints[0].y + 60 + 0 * 20, 15, 15), viewDataSet.lineIconColor["total"]);
        GUI.Label(new Rect(basePoints[0].x + 40 + 20, basePoints[0].y + 60 + 0 * 20, 100, 15), "Total Views");

        GUI.Box(new Rect(basePoints[0].x + 40, basePoints[0].y + 60 + 1 * 20, 15, 15), viewDataSet.lineIconColor["active"]);
        GUI.Label(new Rect(basePoints[0].x + 40 + 20, basePoints[0].y + 60 + 1 * 20, 100, 15), "Active Views");


        var buttonLabelStyle = new GUIStyle();
        buttonLabelStyle.fontSize = 10;
        buttonLabelStyle.alignment = TextAnchor.MiddleCenter;
        buttonLabelStyle.wordWrap = true;
        GUI.Label(new Rect(basePoints[0].x + 3, basePoints[0].y + leftLableBox.height - 33, leftLableBox.width * 0.7f, 30), "Total Count:" + viewDataSet.curValueLableString["total"] + " Active Count " + viewDataSet.curValueLableString["active"], buttonLabelStyle);

        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        yaixLabelStyle.wordWrap = true; 
        //GUI.Label(new Rect(viewDataSet.maxValueLabelPoint.x,viewDataSet.maxValueLabelPoint.y, leftLableBox.width * 0.25f, 15), viewDataSet.maxValueLabelString, yaixLabelStyle);

        //if(totalViewPoints.Count>0)
        //GUI.Label(new Rect(viewDataSet.curValueLabelPoint["total"].x, viewDataSet.curValueLabelPoint["total"].y-12f, leftLableBox.width * 0.25f, 15), viewDataSet.curValueLableString["total"], yaixLabelStyle);
        //GUI.Label(new Rect(viewDataSet.curValueLabelPoint["active"].x, viewDataSet.curValueLabelPoint["active"].y-12f, leftLableBox.width * 0.25f, 15), viewDataSet.curValueLableString["active"], yaixLabelStyle);
        
    }
    //绘制监控器图表
    void DrawObserverChart()
    {
        DrawObserverLeftInfoBox();

        //画纵坐标
        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(basePoints[1].x + leftLableBox.width - 55, basePoints[1].y + rightGraphBox.height - observerDataSet.YAxisTop - 7.5f, 50, 15), ((int)observerDataSet.maxValue).ToString(), yaixLabelStyle);
        GUI.Label(new Rect(basePoints[1].x + leftLableBox.width - 55, basePoints[1].y + rightGraphBox.height - (observerDataSet.YAxisTop * 0.5f) - 7.5f, 50, 15), ((int)(observerDataSet.maxValue *0.5f)).ToString(), yaixLabelStyle);
        
        FduEditorGUI.BeforGLDrawLine();
        //画最大值的线与中值的线
        if (observerDataSet.pointDataSet["total"].Count > 1)
        {

            GL.Begin(GL.LINES);
            GL.Color(FduEditorGUI.getCoordinateColor());
            GL.Vertex3(basePoints[1].x + leftLableBox.width, basePoints[1].y + rightGraphBox.height - observerDataSet.YAxisTop, 0);
            GL.Vertex3(basePoints[1].x + leftLableBox.width + rightGraphBox.width, basePoints[1].y + rightGraphBox.height - observerDataSet.YAxisTop, 0);

            GL.Vertex3(basePoints[1].x + leftLableBox.width, basePoints[1].y + rightGraphBox.height - (observerDataSet.YAxisTop * 0.5f), 0);
            GL.Vertex3(basePoints[1].x + leftLableBox.width + rightGraphBox.width, basePoints[1].y + rightGraphBox.height - (observerDataSet.YAxisTop * 0.5f), 0);

            GL.End();
        }

        //画横坐标
        var style = new GUIStyle();
        style.alignment = TextAnchor.LowerCenter;
        for (int i = 0; i < observerDataSet.XAixsValues.Count; ++i)
        {
            if (observerDataSet.XAixsValues[i] % 150 == 0)
            {
                //GUI.Label(new Rect(observerDataSet.pointDataSet["total"][i].x - 25, observerDataSet.XaixsCorordinateHeight - 10, 50, 10), observerDataSet.XAixsValues[i].ToString(), style);
                FduEditorGUI.BeforGLDrawLine();
                GL.Begin(GL.LINES);
                GL.Color(FduEditorGUI.getCoordinateColor());
                GL.Vertex3(observerDataSet.pointDataSet["total"][i].x, basePoints[1].y + rightGraphBox.height - observerDataSet.YAxisTop, 0);
                GL.Vertex3(observerDataSet.pointDataSet["total"][i].x, observerDataSet.XaixsCorordinateHeight + rightGraphBox.height, 0);
                GL.End();

            }
        }

        //画总observer数量的线
        GL.Begin(GL.LINES);
        GL.Color(observerDataSet.lineColor["total"]);
        if (observerDataSet.pointDataSet["total"].Count > 1)
        {
            for (int i = 0; i < observerDataSet.pointDataSet["total"].Count; ++i)
            {
                if (i == 0 || i == observerDataSet.pointDataSet["total"].Count - 1)
                {
                    GL.Vertex3(observerDataSet.pointDataSet["total"][i].x, observerDataSet.pointDataSet["total"][i].y, 0);
                }
                else
                {
                    GL.Vertex3(observerDataSet.pointDataSet["total"][i].x, observerDataSet.pointDataSet["total"][i].y, 0);
                    GL.Vertex3(observerDataSet.pointDataSet["total"][i].x, observerDataSet.pointDataSet["total"][i].y, 0);
                }
            }
        }
        GL.End();

        //画当前激活observer数量的线
        GL.Begin(GL.LINES);
        GL.Color(observerDataSet.lineColor["active"]);
        if (observerDataSet.pointDataSet["active"].Count > 1)
        {
            for (int i = 0; i < observerDataSet.pointDataSet["active"].Count; ++i)
            {
                if (i == 0 || i == observerDataSet.pointDataSet["active"].Count - 1)
                {
                    GL.Vertex3(observerDataSet.pointDataSet["active"][i].x, observerDataSet.pointDataSet["active"][i].y, 0);
                }
                else
                {
                    GL.Vertex3(observerDataSet.pointDataSet["active"][i].x, observerDataSet.pointDataSet["active"][i].y, 0);
                    GL.Vertex3(observerDataSet.pointDataSet["active"][i].x, observerDataSet.pointDataSet["active"][i].y, 0);
                }
            }
        }
        GL.End();
    }
    //绘制监控器图表左侧的信息框
    void DrawObserverLeftInfoBox()
    {
        GUI.Box(new Rect(basePoints[1].x, basePoints[1].y, leftLableBox.width, leftLableBox.height), "");
        var labelStyle = new GUIStyle();
        labelStyle.fontSize = 20;
        labelStyle.fontStyle = FontStyle.Normal;

        GUI.Label(new Rect(basePoints[1].x + 10, basePoints[1].y + 10, 150, 150), new GUIContent(" " + observerDataSet.title, observerDataSet.IconTexture), labelStyle);

        GUI.Box(new Rect(basePoints[1].x + 40, basePoints[1].y + 60 + 0 * 20, 15, 15), observerDataSet.lineIconColor["total"]);
        GUI.Label(new Rect(basePoints[1].x + 40 + 20, basePoints[1].y + 60 + 0 * 20, 150, 15), "Total Observers");

        GUI.Box(new Rect(basePoints[1].x + 40, basePoints[1].y + 60 + 1 * 20, 15, 15), observerDataSet.lineIconColor["active"]);
        GUI.Label(new Rect(basePoints[1].x + 40 + 20, basePoints[1].y + 60 + 1 * 20, 150, 15), "Active Observers");


        var buttonLabelStyle = new GUIStyle();
        buttonLabelStyle.fontSize = 10;
        buttonLabelStyle.alignment = TextAnchor.MiddleCenter;
        buttonLabelStyle.wordWrap = true;
        GUI.Label(new Rect(basePoints[1].x + 3, basePoints[1].y + leftLableBox.height - 33, leftLableBox.width * 0.7f, 30), "Total Count:" + observerDataSet.curValueLableString["total"] + " Active Count " + observerDataSet.curValueLableString["active"], buttonLabelStyle);

        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        yaixLabelStyle.wordWrap = true;


        //GUI.Label(new Rect(observerDataSet.curValueLabelPoint["total"].x, observerDataSet.curValueLabelPoint["total"].y-12f, leftLableBox.width * 0.25f, 15), observerDataSet.curValueLableString["total"], yaixLabelStyle);
        //GUI.Label(new Rect(observerDataSet.curValueLabelPoint["active"].x, observerDataSet.curValueLabelPoint["active"].y-12f, leftLableBox.width * 0.25f, 15), observerDataSet.curValueLableString["active"], yaixLabelStyle);
    }
    //绘制数据传输图表
    void DrawDataTransmitChart()
    {
        DrawDataTransmitLeftInfoBox();

        //画纵坐标
        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(basePoints[2].x + leftLableBox.width - 55, basePoints[2].y + rightGraphBox.height - dataTransmitDataSet.YAxisTop - 7.5f, 50, 15), FduSupportClass.getDataSizeString((int)dataTransmitDataSet.maxValue), yaixLabelStyle);
        GUI.Label(new Rect(basePoints[2].x + leftLableBox.width - 55, basePoints[2].y + rightGraphBox.height - (dataTransmitDataSet.YAxisTop * 0.5f) - 7.5f, 50, 15), FduSupportClass.getDataSizeString((int)dataTransmitDataSet.maxValue * 0.5f), yaixLabelStyle);

        FduEditorGUI.BeforGLDrawLine();
        //画最大值的线与中值的线
        if (dataTransmitDataSet.pointDataSet["total"].Count > 1)
        {

            GL.Begin(GL.LINES);
            GL.Color(FduEditorGUI.getCoordinateColor());
            GL.Vertex3(basePoints[2].x + leftLableBox.width, basePoints[2].y + rightGraphBox.height - dataTransmitDataSet.YAxisTop, 0);
            GL.Vertex3(basePoints[2].x + leftLableBox.width + rightGraphBox.width, basePoints[2].y + rightGraphBox.height - dataTransmitDataSet.YAxisTop, 0);

            GL.Vertex3(basePoints[2].x + leftLableBox.width, basePoints[2].y + rightGraphBox.height - (dataTransmitDataSet.YAxisTop * 0.5f), 0);
            GL.Vertex3(basePoints[2].x + leftLableBox.width + rightGraphBox.width, basePoints[2].y + rightGraphBox.height - (dataTransmitDataSet.YAxisTop * 0.5f), 0);

            GL.End();
        }

        //画横坐标
        var style = new GUIStyle();
        style.alignment = TextAnchor.LowerCenter;
        for (int i = 0; i < dataTransmitDataSet.XAixsValues.Count; ++i)
        {
            if (dataTransmitDataSet.XAixsValues[i] % 150 == 0)
            {
                GUI.Label(new Rect(dataTransmitDataSet.pointDataSet["total"][i].x - 25, dataTransmitDataSet.XaixsCorordinateHeight - 10, 50, 10), dataTransmitDataSet.XAixsValues[i].ToString(), style);
                FduEditorGUI.BeforGLDrawLine();
                GL.Begin(GL.LINES);
                GL.Color(FduEditorGUI.getCoordinateColor());
                GL.Vertex3(dataTransmitDataSet.pointDataSet["total"][i].x, basePoints[2].y + rightGraphBox.height - dataTransmitDataSet.YAxisTop, 0);
                GL.Vertex3(dataTransmitDataSet.pointDataSet["total"][i].x, dataTransmitDataSet.XaixsCorordinateHeight + rightGraphBox.height, 0);
                GL.End();

            }
        }

        GL.Begin(GL.LINES);
        GL.Color(dataTransmitDataSet.lineColor["total"]);
        if (dataTransmitDataSet.pointDataSet["total"].Count > 1)
        {
            for (int i = 0; i < dataTransmitDataSet.pointDataSet["total"].Count; ++i)
            {
                if (i == 0 || i == dataTransmitDataSet.pointDataSet["total"].Count - 1)
                {
                    GL.Vertex3(dataTransmitDataSet.pointDataSet["total"][i].x, dataTransmitDataSet.pointDataSet["total"][i].y, 0);
                }
                else
                {
                    GL.Vertex3(dataTransmitDataSet.pointDataSet["total"][i].x, dataTransmitDataSet.pointDataSet["total"][i].y, 0);
                    GL.Vertex3(dataTransmitDataSet.pointDataSet["total"][i].x, dataTransmitDataSet.pointDataSet["total"][i].y, 0);
                }
            }
        }
        GL.End();
        //System.IO.StreamReader

    }
    //绘制数据传输图标左侧的信息框
    void DrawDataTransmitLeftInfoBox()
    {
        GUI.Box(new Rect(basePoints[2].x, basePoints[2].y, leftLableBox.width, leftLableBox.height), "");
        var labelStyle = new GUIStyle();
        labelStyle.fontSize = 20;
        labelStyle.fontStyle = FontStyle.Normal;

        GUI.Label(new Rect(basePoints[2].x + 10, basePoints[2].y + 10, 150,150), new GUIContent(" " + dataTransmitDataSet.title, dataTransmitDataSet.IconTexture), labelStyle);

        GUI.Box(new Rect(basePoints[2].x + 40, basePoints[2].y + 60 + 0 * 20, 15, 15), dataTransmitDataSet.lineIconColor["total"]);
        GUI.Label(new Rect(basePoints[2].x + 40 + 20, basePoints[2].y + 60 + 0 * 20, 100, 15), "Data Size Per Frame",FduEditorGUI.getWordWarp());


        var buttonLabelStyle = new GUIStyle();
        buttonLabelStyle.fontSize = 10;
        buttonLabelStyle.alignment = TextAnchor.MiddleLeft;
        //Debug.Log(dataTransmitDataSet.curValueLableString["total"]);
        GUI.Label(new Rect(basePoints[2].x + 3, basePoints[2].y + leftLableBox.height - 48, leftLableBox.width * 0.7f, 20), "Current Data Size:" + FduSupportClass.getDataSizeString(int.Parse(dataTransmitDataSet.curValueLableString["total"])), buttonLabelStyle);
        GUI.Label(new Rect(basePoints[2].x + 3, basePoints[2].y + leftLableBox.height - 28, leftLableBox.width * 0.7f, 20), "Average Data Size:" + FduSupportClass.getDataSizeString((float)dataTransmitDataSet.customData["average"]), buttonLabelStyle);

        var yaixLabelStyle = new GUIStyle();
        yaixLabelStyle.fontSize = 10;
        yaixLabelStyle.alignment = TextAnchor.MiddleRight;
        yaixLabelStyle.wordWrap = true;
        //GUI.Label(new Rect(dataTransmitDataSet.maxValueLabelPoint.x, dataTransmitDataSet.maxValueLabelPoint.y - 12f, leftLableBox.width * 0.25f, 15), FduSupportClass.getDataSizeString((int)dataTransmitDataSet.maxValue), yaixLabelStyle);
        //GUI.Label(new Rect(basePoints[2].x + leftLableBox.width + rightGraphBox.width - 50, dataTransmitDataSet.curValueLabelPoint["total"].y - 12f, 50, 15), dataTransmitDataSet.curValueLableString["total"], yaixLabelStyle);
    }

    //初始化矩形
    void initRects()
    {
       
        leftLableBox = new Rect(0, 0, subWindowRect.width*0.3f, subWindowRect.height * 0.25f);
        rightGraphBox = new Rect(0, 0, subWindowRect.width*0.7f, subWindowRect.height * 0.25f);
        //分开写是因为方便修改
        basePoints = new Vector2[3];
        basePoints[2] = new Vector2(10, 100);
        basePoints[1] = new Vector2(10, basePoints[2].y + leftLableBox.height+5);
        basePoints[0] = new Vector2(10, basePoints[1].y + leftLableBox.height+5);

    }
    //初始化资源
    void initResources()
    {
        viewDataSet = new ProfileChartDataSet();
        observerDataSet = new ProfileChartDataSet();
        dataTransmitDataSet = new ProfileChartDataSet();

        viewDataSet.title = "Views";
        viewDataSet.IconTexture = FduEditorGUI.getViewIcon();
        viewDataSet.lineColor.Add("total", FduEditorGUI.getChartYellowColor());
        viewDataSet.lineColor.Add("active", FduEditorGUI.getChartGreenColor());
        viewDataSet.lineIconColor.Add("total", getLineIconTexture(FduEditorGUI.getChartYellowColor()));
        viewDataSet.lineIconColor.Add("active", getLineIconTexture(FduEditorGUI.getChartGreenColor()));
        viewDataSet.pointDataSet.Add("total",new List<Vector2>());
        viewDataSet.pointDataSet.Add("active", new List<Vector2>());
        viewDataSet.curValueLabelPoint.Add("total", Vector2.zero);
        viewDataSet.curValueLabelPoint.Add("active", Vector2.zero);
        viewDataSet.curValueLableString.Add("total", "");
        viewDataSet.curValueLableString.Add("active", "");
        viewDataSet.XAixsValues = new List<int>();
        viewDataSet.maxDataCount = ClusterViewStatisticClass.MAX_FRAME_COUNT;
        viewDataSet.YAxisTop = 0.8f * rightGraphBox.height;
        viewDataSet.XAixsInterval = rightGraphBox.width / viewDataSet.maxDataCount;
        

        observerDataSet.title = "Observers";
        observerDataSet.IconTexture = FduEditorGUI.getObserverIcon();
        observerDataSet.lineColor.Add("total", FduEditorGUI.getChartBlueColor());
        observerDataSet.lineColor.Add("active", FduEditorGUI.getChartRedColor());
        observerDataSet.lineIconColor.Add("total", getLineIconTexture(FduEditorGUI.getChartBlueColor()));
        observerDataSet.lineIconColor.Add("active", getLineIconTexture(FduEditorGUI.getChartRedColor()));
        observerDataSet.pointDataSet.Add("total", new List<Vector2>());
        observerDataSet.pointDataSet.Add("active", new List<Vector2>());
        observerDataSet.curValueLabelPoint.Add("total", Vector2.zero);
        observerDataSet.curValueLabelPoint.Add("active", Vector2.zero);
        observerDataSet.curValueLableString.Add("total", "");
        observerDataSet.curValueLableString.Add("active", "");
        observerDataSet.XAixsValues = new List<int>();
        observerDataSet.maxDataCount = ClusterViewStatisticClass.MAX_FRAME_COUNT;
        observerDataSet.YAxisTop = 0.8f * rightGraphBox.height;
        observerDataSet.XAixsInterval = rightGraphBox.width / observerDataSet.maxDataCount;

        dataTransmitDataSet.title = "Data";
        dataTransmitDataSet.IconTexture = FduEditorGUI.getDataTransmitIcon();
        dataTransmitDataSet.lineColor.Add("total", FduEditorGUI.getChartYellowColor());
        dataTransmitDataSet.lineIconColor.Add("total", getLineIconTexture(FduEditorGUI.getChartYellowColor()));
        dataTransmitDataSet.pointDataSet.Add("total", new List<Vector2>());
        dataTransmitDataSet.curValueLabelPoint.Add("total", Vector2.zero);
        dataTransmitDataSet.curValueLableString.Add("total", "0");
        dataTransmitDataSet.XAixsValues = new List<int>();
        dataTransmitDataSet.customData.Add("average",0.0f);
        dataTransmitDataSet.maxDataCount = TransmitDataSizeStatisticClass.MAX_FRAME_COUNT;//TODO: 注意修改这里的数据
        dataTransmitDataSet.YAxisTop = 0.8f * rightGraphBox.height;
        dataTransmitDataSet.XAixsInterval = rightGraphBox.width / dataTransmitDataSet.maxDataCount;
        
        inited = true;

    }
    //更新绘制View图表所需要到的坐标数据，绘制函数直接利用这里函数计算好的坐标进行绘制
    void updateViewChartPointData()
    {
        //generateTotalView();
        //generateActiveView();

        Queue<int>.Enumerator views;
        Queue<int>.Enumerator activeViews;
        Queue<int>.Enumerator frames;


        //views = viewCountData.GetEnumerator();
        //activeViews = activeCountData.GetEnumerator();

        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (ClusterViewStatisticClass.instance != null)
            {
                views = ClusterViewStatisticClass.instance.getTotalViewCounts();
                activeViews = ClusterViewStatisticClass.instance.getActiveViewCounts();
                frames = ClusterViewStatisticClass.instance.getFrameNumbers();
            }
            else
            {
                views = NULLQueueData.GetEnumerator();
                activeViews = NULLQueueData.GetEnumerator();
                frames = NULLQueueData.GetEnumerator();
            }
        }
        else
        {
            views = fileDataSet.totalViewData.GetEnumerator();
            activeViews = fileDataSet.activeViewData.GetEnumerator();
            frames = fileDataSet.viewFrameNumberData.GetEnumerator();
        }


        int index = 0;
        float x, y;

        int curTotalValue = 0;
        int curActiveValue = 0;

        float newMaxValue= 0;
        //先统计出本轮的最大值
        while (views.MoveNext())
        {
            newMaxValue = newMaxValue < views.Current ? views.Current : newMaxValue;
        }
        while (activeViews.MoveNext())
        {
            newMaxValue = newMaxValue < activeViews.Current ? activeViews.Current : newMaxValue;
        }

        if (newMaxValue < viewDataSet.maxValue * 0.7f)
            viewDataSet.maxValue = newMaxValue / 0.7f;
        else
            viewDataSet.maxValue = viewDataSet.maxValue < newMaxValue ? newMaxValue : viewDataSet.maxValue;

        viewDataSet.maxValueLabelPoint = new Vector2(basePoints[0].x + leftLableBox.width * 0.7f, (basePoints[0].y + rightGraphBox.height)-viewDataSet.YAxisTop);
        

        //views = viewCountData.GetEnumerator();
        //activeViews = activeCountData.GetEnumerator();

        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (ClusterViewStatisticClass.instance != null)
            {
                views = ClusterViewStatisticClass.instance.getTotalViewCounts();
                activeViews = ClusterViewStatisticClass.instance.getActiveViewCounts();
            }
            else
            {
                views = NULLQueueData.GetEnumerator();
                activeViews = NULLQueueData.GetEnumerator();
            }
        }
        else
        {
            views = fileDataSet.totalViewData.GetEnumerator();
            activeViews = fileDataSet.activeViewData.GetEnumerator();
        }

        
        //然后遍历数据集 生成点的数据
        while (views.MoveNext())
        {
            x = index * viewDataSet.XAixsInterval;
            x = basePoints[0].x + leftLableBox.width + x;
            y = (views.Current * 1.0f / viewDataSet.maxValue) * viewDataSet.YAxisTop;
            y = basePoints[0].y + rightGraphBox.height - y;
            if (viewDataSet.pointDataSet["total"].Count <= index)
            {
                viewDataSet.pointDataSet["total"].Add(new Vector2(x, y));
            }
            else
            {
                viewDataSet.pointDataSet["total"][index] = new Vector2(x, y);
            }
            curTotalValue = views.Current;
            index++;
        }

        index = 0;
        while (activeViews.MoveNext())
        {
            x = index * viewDataSet.XAixsInterval;
            x = basePoints[0].x + leftLableBox.width + x;
            y = (activeViews.Current * 1.0f / viewDataSet.maxValue) * viewDataSet.YAxisTop;
            y = basePoints[0].y + rightGraphBox.height - y;
            if (viewDataSet.pointDataSet["active"].Count <= index)
            {
                viewDataSet.pointDataSet["active"].Add(new Vector2(x, y));
            }
            else
            {
                viewDataSet.pointDataSet["active"][index] = new Vector2(x, y);
            }
            curActiveValue = activeViews.Current;
            index++;
        }


        viewDataSet.curValueLableString["total"] = curTotalValue.ToString();
        viewDataSet.curValueLableString["active"] = curActiveValue.ToString();

        viewDataSet.curValueLabelPoint["total"] = new Vector2(basePoints[0].x + leftLableBox.width * 0.7f, (basePoints[0].y + rightGraphBox.height) - (curTotalValue * 1.0f / viewDataSet.maxValue) * viewDataSet.YAxisTop);
        viewDataSet.curValueLabelPoint["active"] = new Vector2(basePoints[0].x + leftLableBox.width * 0.7f, (basePoints[0].y + rightGraphBox.height) - (curActiveValue * 1.0f / viewDataSet.maxValue) * viewDataSet.YAxisTop);

        index = 0;


        while (frames.MoveNext())
        {
            if (viewDataSet.XAixsValues.Count <= index)
                viewDataSet.XAixsValues.Add(frames.Current);
            else
                viewDataSet.XAixsValues[index] = frames.Current;
            index++;
        }
        viewDataSet.XaixsCorordinateHeight = basePoints[0].y;
    
    }
    //更新绘制Observer图表所需要到的坐标数据，绘制函数直接利用这里函数计算好的坐标进行绘制
    void updateObserverChartData()
    {
        Queue<int>.Enumerator observers;
        Queue<int>.Enumerator activeObservers;
        Queue<int>.Enumerator frames;

        //observers = viewCountData.GetEnumerator();
        //activeObservers = activeCountData.GetEnumerator();



        int index = 0;
        float x, y;

        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (ClusterViewStatisticClass.instance != null)
            {
                observers = ClusterViewStatisticClass.instance.getTotalObserverCounts();
                activeObservers = ClusterViewStatisticClass.instance.getActiveObserverCounts();
                frames = ClusterViewStatisticClass.instance.getFrameNumbers();
            }
            else
            {
                observers = NULLQueueData.GetEnumerator();
                activeObservers = NULLQueueData.GetEnumerator();
                frames = NULLQueueData.GetEnumerator();
            }
        }
        else
        {
            observers = fileDataSet.totalObserverData.GetEnumerator();
            activeObservers = fileDataSet.activeObserverData.GetEnumerator();
            frames = fileDataSet.viewFrameNumberData.GetEnumerator();
        }


        int curTotalValue = 0;
        int curActiveValue = 0;

        float newMaxValue = 0;
        //先统计出本轮的最大值
        while (observers.MoveNext())
        {
            newMaxValue = newMaxValue < observers.Current ? observers.Current : newMaxValue;
        }
        while (activeObservers.MoveNext())
        {
            newMaxValue = newMaxValue < activeObservers.Current ? activeObservers.Current : newMaxValue;
        }

        if (newMaxValue < observerDataSet.maxValue * 0.7f)
            observerDataSet.maxValue = newMaxValue / 0.7f;
        else
            observerDataSet.maxValue = observerDataSet.maxValue < newMaxValue ? newMaxValue : observerDataSet.maxValue;


        observerDataSet.maxValueLabelPoint = new Vector2(basePoints[1].x + leftLableBox.width * 0.7f, (basePoints[1].y + rightGraphBox.height) - observerDataSet.YAxisTop);


        //observers = viewCountData.GetEnumerator();
        //activeObservers = activeCountData.GetEnumerator();
        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (ClusterViewStatisticClass.instance != null)
            {
                observers = ClusterViewStatisticClass.instance.getTotalObserverCounts();
                activeObservers = ClusterViewStatisticClass.instance.getActiveObserverCounts();
            }
            else
            {
                observers = NULLQueueData.GetEnumerator();
                activeObservers = NULLQueueData.GetEnumerator();
            }
        }
        else
        {
            observers = fileDataSet.totalObserverData.GetEnumerator();
            activeObservers = fileDataSet.activeObserverData.GetEnumerator();
        }

        //然后遍历数据集 生成点的数据
        while (observers.MoveNext())
        {
            x = index * observerDataSet.XAixsInterval;
            x = basePoints[1].x + leftLableBox.width + x;
            y = (observers.Current * 1.0f / observerDataSet.maxValue) * observerDataSet.YAxisTop;
            y = basePoints[1].y + rightGraphBox.height - y;
            if (observerDataSet.pointDataSet["total"].Count <= index)
            {
                observerDataSet.pointDataSet["total"].Add(new Vector2(x, y));
            }
            else
            {
                observerDataSet.pointDataSet["total"][index] = new Vector2(x, y);
            }
            curTotalValue = observers.Current;
            index++;
        }

        index = 0;
        while (activeObservers.MoveNext())
        {
            x = index * observerDataSet.XAixsInterval;
            x = basePoints[1].x + leftLableBox.width + x;
            y = (activeObservers.Current * 1.0f / observerDataSet.maxValue) * observerDataSet.YAxisTop;
            y = basePoints[1].y + rightGraphBox.height - y;
            if (observerDataSet.pointDataSet["active"].Count <= index)
            {
                observerDataSet.pointDataSet["active"].Add(new Vector2(x, y));
            }
            else
            {
                observerDataSet.pointDataSet["active"][index] = new Vector2(x, y);
            }
            curActiveValue = activeObservers.Current;
            index++;
        }
        observerDataSet.curValueLableString["total"] = curTotalValue.ToString();
        observerDataSet.curValueLableString["active"] = curActiveValue.ToString();

        observerDataSet.curValueLabelPoint["total"] = new Vector2(basePoints[1].x + leftLableBox.width * 0.7f, (basePoints[1].y + rightGraphBox.height) - (curTotalValue * 1.0f / observerDataSet.maxValue) * observerDataSet.YAxisTop);
        observerDataSet.curValueLabelPoint["active"] = new Vector2(basePoints[1].x + leftLableBox.width * 0.7f, (basePoints[1].y + rightGraphBox.height) - (curActiveValue * 1.0f / observerDataSet.maxValue) * observerDataSet.YAxisTop);

        index = 0;


        while (frames.MoveNext())
        {
            if (observerDataSet.XAixsValues.Count <= index)
                observerDataSet.XAixsValues.Add(frames.Current);
            else
                observerDataSet.XAixsValues[index] = frames.Current;
            index++;
        }
        observerDataSet.XaixsCorordinateHeight = basePoints[1].y;
    
    }
    //更新绘制数据传输图表所需要到的坐标数据，绘制函数直接利用这里函数计算好的坐标进行绘制
    void updateDataTransmitChartData()
    {
        generateTotalView();
        Queue<int>.Enumerator datas;
        Queue<int>.Enumerator frames;
        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (TransmitDataSizeStatisticClass.instance != null)
            {
                datas = TransmitDataSizeStatisticClass.instance.getTransmitDataSize();
                frames = TransmitDataSizeStatisticClass.instance.getFrameNumbers();
            }
            else
            {
                datas = NULLQueueData.GetEnumerator();
                frames = NULLQueueData.GetEnumerator();
            }
        }
        else
        {
            datas = fileDataSet.transmitDataSizeData.GetEnumerator();
            frames = fileDataSet.transmitFrameData.GetEnumerator();
        }

        int index = 0;
        float x, y;

        int curTotalValue = 0;


        float newMaxValue = 0;
        //先统计出本轮的最大值
        while (datas.MoveNext())
            newMaxValue = newMaxValue < datas.Current ? datas.Current : newMaxValue;

        if (newMaxValue < dataTransmitDataSet.maxValue * 0.7f)
            dataTransmitDataSet.maxValue = newMaxValue / 0.7f;
        else
            dataTransmitDataSet.maxValue = dataTransmitDataSet.maxValue < newMaxValue ? newMaxValue : dataTransmitDataSet.maxValue;


        dataTransmitDataSet.maxValueLabelPoint = new Vector2(basePoints[2].x + leftLableBox.width * 0.7f, (basePoints[2].y + rightGraphBox.height) - dataTransmitDataSet.YAxisTop);



        //datas = viewCountData.GetEnumerator();
        if (dataSourceState == dataSourceEnum.RealTime)
        {
            if (TransmitDataSizeStatisticClass.instance != null)
            {
                datas = TransmitDataSizeStatisticClass.instance.getTransmitDataSize();
            }
            else
                datas = NULLQueueData.GetEnumerator();
        }
        else
            datas = fileDataSet.transmitDataSizeData.GetEnumerator();

        int totalSize = 0; 
        //然后遍历数据集 生成点的数据
        while (datas.MoveNext())
        {
            x = index * dataTransmitDataSet.XAixsInterval;
            x = basePoints[2].x + leftLableBox.width + x;
            y = (datas.Current * 1.0f / dataTransmitDataSet.maxValue) * dataTransmitDataSet.YAxisTop;
            y = basePoints[2].y + rightGraphBox.height - y;
            if (dataTransmitDataSet.pointDataSet["total"].Count <= index)
                dataTransmitDataSet.pointDataSet["total"].Add(new Vector2(x, y));
            else
                dataTransmitDataSet.pointDataSet["total"][index] = new Vector2(x, y);
            curTotalValue = datas.Current;
            index++;
            totalSize += curTotalValue;
        }



        dataTransmitDataSet.curValueLableString["total"] = ((int)curTotalValue).ToString();

        dataTransmitDataSet.curValueLabelPoint["total"] = new Vector2(basePoints[2].x + leftLableBox.width * 0.7f, (basePoints[2].y + rightGraphBox.height) - (curTotalValue * 1.0f / dataTransmitDataSet.maxValue) * dataTransmitDataSet.YAxisTop);

        float averageSize = 0.0f;
        if (index > 0)
            averageSize = totalSize / index;
        dataTransmitDataSet.customData.Add_overlay("average",averageSize);


        index = 0;


        while (frames.MoveNext())
        {
            if (dataTransmitDataSet.XAixsValues.Count <= index)
                dataTransmitDataSet.XAixsValues.Add(frames.Current);
            else
                dataTransmitDataSet.XAixsValues[index] = frames.Current;
            index++;
        }
        dataTransmitDataSet.XaixsCorordinateHeight = basePoints[2].y;
        


    }
    //将新的数据写入文件中
    void updateDataToFile()
    {
        int totalView = 0;
        int acView = 0;
        int totalOb = 0;
        int acOb = 0;
        int viewFrame = 0;
        int dataSize = 0;
        int dataFrame = 0;

        if (viewDataSet.curValueLableString.ContainsKey("total"))
            totalView = int.Parse(viewDataSet.curValueLableString["total"]);
        if(viewDataSet.curValueLableString.ContainsKey("active"))
            acView = int.Parse(viewDataSet.curValueLableString["active"]);
        if (observerDataSet.curValueLableString.ContainsKey("total"))
            totalOb = int.Parse(observerDataSet.curValueLableString["total"]);
        if (observerDataSet.curValueLableString.ContainsKey("active"))
            acOb = int.Parse(observerDataSet.curValueLableString["active"]);

        if (viewDataSet.XAixsValues.Count > 0)
            viewFrame = viewDataSet.XAixsValues[viewDataSet.XAixsValues.Count - 1];

        if (dataTransmitDataSet.curValueLableString.ContainsKey("total"))
            dataSize = int.Parse(dataTransmitDataSet.curValueLableString["total"]);
        if (dataTransmitDataSet.XAixsValues.Count > 0)
            dataFrame = dataTransmitDataSet.XAixsValues[dataTransmitDataSet.XAixsValues.Count - 1];

        fileDataSet.writeDataOnce(totalView,acView,totalOb,acOb,viewFrame,dataSize,dataFrame);


    }

    //测试用数据源
    Queue<int>.Enumerator generateTotalView()
    {
        if (viewCountData.Count >= 200)
        {
            viewCountData.Dequeue();
        }
        int random = Random.Range(30, 100);
        viewCountData.Enqueue(random);
        //Debug.Log(viewCountData.Count);

        return viewCountData.GetEnumerator();
    }
    //测试用数据源
    Queue<int>.Enumerator generateActiveView()
    {
        if (activeCountData.Count >= viewDataSet.maxDataCount)
        {
            activeCountData.Dequeue();
        }
        activeCountData.Enqueue(Random.Range(1, 100));

        return activeCountData.GetEnumerator();
    }
    public Texture2D getLineIconTexture(Color color)
    {
        Texture2D result = new Texture2D(50,50);
        for (int i = 0; i < 50; ++i)
        {
            for (int j = 0; j < 50; ++j)
            {
                result.SetPixel(i, j, color);        
            }
        }
        result.Apply();
        return result;
    }


    public override void OnEnter() {
       
    }

    public override void OnExit() {
        EditorGUI.FocusTextInControl("");
    }
    //启动窗口时 要启动对应统计类工作
    public override void OnEnable() {
        ClusterViewStatisticClass.isRunnig = true;
        TransmitDataSizeStatisticClass.isRunning = true;
    }
    //关闭窗口是 要关闭对应统计类的工作
    public override void OnDisable() {
        ClusterViewStatisticClass.isRunnig = false;
        TransmitDataSizeStatisticClass.isRunning = false;
        if (ClusterViewStatisticClass.instance!=null)
            ClusterViewStatisticClass.instance.ClearData();
        if (TransmitDataSizeStatisticClass.instance!=null)
            TransmitDataSizeStatisticClass.instance.ClearData();

        fileDataSet.finishWriting();
        if (Application.isPlaying)
        {
            if (File.Exists(fileDataSet.savePath))
                File.Delete(fileDataSet.savePath);
        }
    }
    //每帧更新 文件读取数据 重新计算图表坐标值 写入文件坐标
    public override void Update() {

        if (playState == playControlEnum.play)
        {
            fileDataSet.readDataOnce();
            updateViewChartPointData();
            updateObserverChartData();
            updateDataTransmitChartData();

            if(Application.isPlaying)
                updateDataToFile();
        }
    }
    public override void OnInspectorUpdate()
    {
        fileDataSet.flushData();
    }

    public override void Awake() {
        points = new Vector2[] { new Vector2(0, 10), new Vector2(10, 30), new Vector2(20, 20), new Vector2(30, 60), new Vector2(40, 10) };
        initRects();
        initResources();
    }
    //清除数据
    void ClearData()
    {
        ClearPointData();
        if (Application.isPlaying)
        {
            TransmitDataSizeStatisticClass.instance.ClearData();
            ClusterViewStatisticClass.instance.ClearData();
        }
        if (dataSourceState == dataSourceEnum.File)
        {
            fileDataSet.clearLoadedData();
        }
    }
    //清除图表的点坐标数据
    void ClearPointData()
    {
        dataTransmitDataSet.ClearPointData();
        viewDataSet.ClearPointData();
        observerDataSet.ClearPointData();
    }


    public override void OnDestroy() { }
}



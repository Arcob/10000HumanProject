/*
 * FduClusterCommandGraphSubWindow
 * 
 * 简介：控制台中将事件数据可视化的窗口
 * 可以直观的看到事件出现的频率和监听次事件监控器的数量
 * 
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;

public class FduClusterCommandGraphSubWindow : FduConsoleSubwindowBase {

    Texture2D[] rectTexture;
    Texture2D colorHintLabelTexture;
    //颜色等级枚举
    enum colorLevel
    {
        level1=0,level2 = 1,level3 =2,level4 = 3,level5  =4,Others = 5,end = 6
    }
    //展示状态 hideMessage是因为节点的信息量太多 无法在对应方块内展示 所以以数字代替
    //MultiNodes是因为子节点数量太多，继续细分会导致面积太小无法分辨，所以递归到一定数值后就不再递归，那么这些节点将会出现在一个显示节点中
    enum displayNodeState
    {
        normal,hideMessage,MultiNodes
    }
    //原始数据 
    class nodeData
    {
        public string CommandName;
        public int invokedTimes = 0;
        public int ExecutorCount = 0;
        public nodeData parent;
        public nodeData leftChild;
        public nodeData rightChild;
    }
    //展示数据
    class displayNode
    {
        public nodeData nodeData;
        public displayNodeState state;
        public Rect position = new Rect(0, 0, 0, 0);
        public colorLevel color = colorLevel.level1;
        public List<nodeData> nodeList;
        public int hideIndex = -1;
        public string getHideInfoString()
        {
            if (state == displayNodeState.MultiNodes)
            {
                if (nodeList != null && nodeList.Count > 0)
                {
                    string result = hideIndex+":";
                    result += nodeList[0].CommandName + " Executor Count:" + nodeList[0].ExecutorCount + " Raised Times:" + nodeList[0].invokedTimes;
                    for (int i = 1; i < nodeList.Count; ++i)
                        result += ", " + nodeList[i].CommandName + " Executor Count:" + nodeList[i].ExecutorCount + " Rasied Times:" + nodeList[i].invokedTimes;
                    return result;
                }
                else
                    return null;
            }
            else if (state == displayNodeState.hideMessage)
            {
                return hideIndex + ":" + nodeData.CommandName + " Executor Count:" + nodeData.ExecutorCount + " Rasied Times:" + nodeData.invokedTimes;
            }
            else
                return null;
        }
    }
    //建立霍夫曼树以后形成的根节点
    nodeData root;
    //原始数据集
    List<nodeData> nodeDataList = new List<nodeData>();
    //建立霍夫曼树所需要的队列
    List<nodeData> buildQueue = new List<nodeData>();
    //展示数据的数据列表
    List<displayNode> displayNodeList = new List<displayNode>();

    //用于颜色计算的相关变量
    float maxWeight = float.MinValue;
    float minWeight = 0;
    List<float> colorStep =  new List<float>();

    //无监听器的事件列表 不进行图形化显示
    List<string> noExecutorCommandList = new List<string>();


    GUIStyle labelStyle = new GUIStyle();
    //所有数据方块的显示区域
    Rect rectsPos;
    //颜色标签的显示区域
    Rect colorHintLabelPos;
    //刷新按钮的显示区域
    Rect refreshButtonPos;
    //细节滚动面显示区域
    Rect detailScrollPos;
    //滚动区域所需要的位置
    Vector2 scrollPos = Vector2.zero;

    bool scaleAnimationTrigger = false;
    //用于缩放动画的缩放值
    float scale = 0.0f;
    //部分方格用数字显示 该变量记录其数量
    int hideIndexCount = 0;
    //细节滚动面板的高度居然要自己求！ 这个就是
    float detailHeight = 0.0f;

    //重新采集数据
    void RefreshData()
    {
        ClearData();
        scale = 0.0f;
        scaleAnimationTrigger = true;

        maxWeight = float.MinValue;
        minWeight = 0;

        Dictionary<string, FduClusterCommandDispatcher.ExecutorData>.Enumerator Executors = FduClusterCommandDispatcher.getExecutors();
        while (Executors.MoveNext())
        {
            nodeData node = new nodeData();
            node.CommandName = Executors.Current.Key;
            node.ExecutorCount = Executors.Current.Value.ActionMap.Count;

            this.nodeDataList.Add(node);
        }
        var invokeData = ClusterCommandStatisticClass.instance.getCommandAccumulatedData();
        while (invokeData.MoveNext())
        {
            nodeData node = nodeDataList.Find(delegate(nodeData n1) { return n1.CommandName == invokeData.Current.Key; });
            if (node != null)
            {
                node.invokedTimes = invokeData.Current.Value;

                maxWeight = maxWeight < node.invokedTimes ? node.invokedTimes : maxWeight;
                minWeight = minWeight > node.invokedTimes ? node.invokedTimes : minWeight;
            }
            else
                noExecutorCommandList.Add(invokeData.Current.Key);
        }

        genTestData();


        root = BuildDataTree();

        if (root != null)
        {
            Divide(root, true, new Vector2(rectsPos.x, rectsPos.y), rectsPos.width, rectsPos.height);
            ProcessDisplayNodeData();
        }

    }
    //清除数据
    void ClearData()
    {
        nodeDataList.Clear();
        buildQueue.Clear();
        noExecutorCommandList.Clear();
        displayNodeList.Clear();
        colorStep.Clear();
    }
    //生成测试用数据
    void genTestData()
    {
        for (int i = 0; i < 15; ++i)
        {
            nodeData node = new nodeData();
            node.CommandName = "Command" + Random.Range(1, 10000000);
            node.ExecutorCount = Random.Range(1, 100);
            node.invokedTimes = Random.Range(0, 50);

            maxWeight = maxWeight < node.invokedTimes ? node.invokedTimes : maxWeight;
            minWeight = minWeight > node.invokedTimes ? node.invokedTimes : minWeight;
            nodeDataList.Add(node);
        }
        for (int i = 0; i < 10; ++i)
        {
            noExecutorCommandList.Add("NoExecutor" + Random.Range(1, 100));
        }
    }
    //分割的递归算法 这里通过生成的霍夫曼树 生成最后需要显示的数据
    void Divide(nodeData node, bool dir, Vector2 pos, float width, float height)
    {
        if ((node.leftChild == null && node.rightChild == null) || (width * height < 2500)) //如果是叶子节点 或者是面积小于某个特定值 则停止继续递归
        {

            if (node.CommandName == null) //证明不是根节点 由于面积限制而进入此逻辑的 因为在霍夫曼树中生成的中间节点是没有CommandName属性的
            {
                displayNode disNode = new displayNode();
                disNode.position = new Rect(pos.x, pos.y, width, height);
                disNode.nodeData = null;
                disNode.state = displayNodeState.MultiNodes;
                disNode.nodeList = new List<nodeData>();
                GetMultiTypeDisplayNode(disNode, node);
                displayNodeList.Add(disNode);
            }
            else //叶子节点
            {
                displayNode disNode = new displayNode();
                disNode.position = new Rect(pos.x, pos.y, width, height);
                disNode.nodeData = node;
                disNode.state = displayNodeState.normal;
                displayNodeList.Add(disNode);
            }
            return;
        }
        if (node.leftChild != null)
        {
            float ratio = node.leftChild.ExecutorCount * 1.0f / node.ExecutorCount;
            if (dir)
            {
                float subWidth = width * ratio;
                Vector2 subV = new Vector2(pos.x, pos.y);
                Divide(node.leftChild, !dir, subV, subWidth, height);
            }
            else
            {
                float subHeight = height * ratio;
                Vector2 subV = new Vector2(pos.x, pos.y);
                Divide(node.leftChild, !dir, subV, width, subHeight);
            }
        }
        if (node.rightChild != null)
        {
            float ratio = node.rightChild.ExecutorCount * 1.0f / node.ExecutorCount;
            if (dir)
            {
                float subWidth = width * ratio;
                Vector2 subV = new Vector2(pos.x + width - subWidth, pos.y);
                Divide(node.rightChild, !dir, subV, subWidth, height);
            }
            else
            {
                float subHeight = height * ratio;
                Vector2 subV = new Vector2(pos.x, pos.y + height - subHeight);
                Divide(node.rightChild, !dir, subV, width, subHeight);
            }
        }
    }
    //停止递归后 该显示节点可能包含多个事件 递归将所有的事件信息保存起来 用于显示
    void GetMultiTypeDisplayNode(displayNode disNode,nodeData node)
    {
        if (node.leftChild == null && node.rightChild == null)
        {
            disNode.nodeList.Add(node);
            return;
        }
        if (node.leftChild != null)
        {
            GetMultiTypeDisplayNode(disNode, node.leftChild);
        }
        if (node.rightChild != null)
        {
            GetMultiTypeDisplayNode(disNode, node.rightChild);
        }
    }
    //对分割后的递归算法进行进一步的数据处理 生成颜色信息
    void ProcessDisplayNodeData()
    {
        GenerateColorStep();
        hideIndexCount = 0;
        foreach (displayNode dis in displayNodeList)
        {
            if (dis.state == displayNodeState.MultiNodes)
            {
                dis.hideIndex = hideIndexCount++;
                dis.color = colorLevel.Others;
            }
            else
            {
                float height = labelStyle.CalcHeight(new GUIContent(getShowMessage(dis.nodeData)), dis.position.width);
                if (height > dis.position.height)
                {
                    dis.state = displayNodeState.hideMessage;
                    dis.hideIndex = hideIndexCount++;
                    dis.color = getColorLevelByWeight(dis.nodeData.invokedTimes);
                }
                else
                {
                    dis.state = displayNodeState.normal;
                    dis.color = getColorLevelByWeight(dis.nodeData.invokedTimes);
                }
            }
        }
    }
    //根据最大最小权值生成颜色列表 注意这里的权值和分割算法的权值不是同一个变量
    void GenerateColorStep()
    {
        if (maxWeight < minWeight) //进入这个逻辑 说明没有发生过Command
        {
            for (int i = 0; i < (int)colorLevel.Others; ++i)
            {
                colorStep.Add(0);
            }
            return;
        }
        float interval = (maxWeight - minWeight) / (int)colorLevel.Others;
        for (int i = 0; i < (int)colorLevel.Others; ++i)
        {
            colorStep.Add(minWeight + (i+1)*interval);
        }
    }
    //需要在GenerateColorStep执行后执行 通过权值获取颜色值
    colorLevel getColorLevelByWeight(float weight)
    {
        colorLevel result = colorLevel.end;
        for (int i = 0; i < colorStep.Count; ++i)
        {
            if (weight <= colorStep[i])
            {
                result = (colorLevel)i;
                break;
            }
        }
        return result;

    }
    string getShowMessage(nodeData node)
    {
        return node.CommandName + "\nExecutor:" + node.ExecutorCount;
    }
    //通过显示数据 画所有内容
    void DrawRects()
    {
        if (displayNodeList.Count < 1)
        {
            GUI.Label(subWindowRect, new GUIContent("No Command Data", parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
        }

        //根据生成的显示数据 画每一个节点
        foreach (displayNode dis in displayNodeList)
        {
            GUI.DrawTexture(new Rect(dis.position.x+dis.position.width*0.5f*(1-scale), dis.position.y + dis.position.height*0.5f*(1-scale),
                dis.position.width * scale,dis.position.height*scale), rectTexture[(int)dis.color]);
            
            if (scale >= 1.0f) //缩放完成后显示Label
            {
                if (dis.state == displayNodeState.normal) //可以正常显示的节点显示信息
                    GUI.Label(dis.position, getShowMessage(dis.nodeData), labelStyle);
                else
                    GUI.Label(dis.position, dis.hideIndex.ToString(), labelStyle);//显示不了的节点显示index 可以由用户通过index查找到对应信息
            }
        }

        //画左侧的颜色标识标签
        GUI.DrawTexture(colorHintLabelPos,colorHintLabelTexture);
        GUI.Label(new Rect(colorHintLabelPos.x, colorHintLabelPos.y-20, 100, 20), "Rasied Times");
        float interval = colorHintLabelPos.height / (int)colorLevel.Others;

        float x = colorHintLabelPos.x + colorHintLabelPos.width + 5;
        float y = colorHintLabelPos.y + colorHintLabelPos.height - interval * 0.5f;
        GUI.Label(new Rect(x, y, 100, 20), "0~" + (int)colorStep[0]);
        //为每一个颜色添加标签
        for (int i = 1; i < (int)colorLevel.Others; ++i)
        {
             x = colorHintLabelPos.x + colorHintLabelPos.width + 5;
             y = colorHintLabelPos.y + colorHintLabelPos.height - interval * 0.5f - i * interval;
             if (((int)colorStep[i - 1]) < ((int)colorStep[i]))
             {
                 GUI.Label(new Rect(x, y, 100, 20), ((int)colorStep[i - 1]) + 1 + "~" + ((int)colorStep[i]));
             }
             else
             {
                 GUI.Label(new Rect(x, y, 100, 20), "None");
             }
        }
        var detailStyle = FduEditorGUI.getWordWarp();
        //画下方的Detail部分
        if (hideIndexCount > 0 && scale >= 1.0f)
        {
            GUI.Box(detailScrollPos, "");
            scrollPos = GUI.BeginScrollView(detailScrollPos, scrollPos, new Rect(0, 0, detailScrollPos.width, detailHeight));
            var content = new GUIContent("");
            detailHeight = 0.0f;
            foreach (displayNode dis in displayNodeList)
            {
                if (dis.state!= displayNodeState.normal)
                {
                    content.text = dis.getHideInfoString();
                    GUI.Label(new Rect(10, detailHeight, detailScrollPos.width-10,15), content, detailStyle); //启动换行了 那么Rect的height形同虚设
                    detailHeight += detailStyle.CalcHeight(content, detailScrollPos.width - 10);
                }
            }
            if (noExecutorCommandList.Count > 0)
            {
                string noExecutorInfo = "No Executor Commands: " + noExecutorCommandList[0];
                for (int i = 1; i < noExecutorCommandList.Count; ++i)
                {
                    noExecutorInfo += ", " + noExecutorCommandList[i];
                }
                content.text = noExecutorInfo;
                GUI.Label(new Rect(10, detailHeight, detailScrollPos.width - 10, 15), content, detailStyle);
                detailHeight += detailStyle.CalcHeight(content, detailScrollPos.width - 10);
            }
            GUI.EndScrollView();
        }
        
    }
    //构建霍夫曼树
    nodeData BuildDataTree()
    {
        buildQueue.Clear();
        foreach (nodeData n in nodeDataList)
        {
            buildQueue.Add(n);
        }
        buildQueue.Sort((nodeData na, nodeData nb) => { return na.ExecutorCount.CompareTo(nb.ExecutorCount); });
        while (buildQueue.Count > 1)
        {
            nodeData n1 = buildQueue[0];
            nodeData n2 = buildQueue[1];

            nodeData n3 = new nodeData();
            n3.leftChild = n1;
            n3.rightChild = n2;
            n1.parent = n3;
            n2.parent = n3;
            n3.ExecutorCount = n1.ExecutorCount + n2.ExecutorCount;

            buildQueue.Add(n3);
            buildQueue.RemoveRange(0, 2);
            buildQueue.Sort((nodeData na, nodeData nb) => { return na.ExecutorCount.CompareTo(nb.ExecutorCount); });
        }
        if (buildQueue.Count == 1)
            return buildQueue[0];
        return null;
    }
    

    public override void DrawSubWindow()
    {
        if (!Application.isPlaying)
        {
            GUI.Label(subWindowRect, new GUIContent("You can get the information of Command graph at run time", parentWindow.hintTexture), FduEditorGUI.getTitleStyle_LevelOne());
            return;
        }
        DrawRects();
        if (GUI.Button(refreshButtonPos, "Refresh"))
        {
            RefreshData();
        }
        
    }
    //初始化纹理数据
    void initRectTexture()
    {
        rectTexture = new Texture2D[(int)colorLevel.end];
        int width = 300; int height = 300;
        int edgeWidth = 1;
        for (int i = 0; i < rectTexture.Length; ++i)
        {
            rectTexture[i] = new Texture2D(width, height);
            for (int j = 0; j < width; ++j)
            {
                for (int k = 0; k < height; ++k)
                {
                    if (j < edgeWidth || j >= width - edgeWidth || k < edgeWidth || k >= height - edgeWidth)
                        rectTexture[i].SetPixel(j, k, Color.black);
                    else
                    {
                        rectTexture[i].SetPixel(j, k, colorLevel2Color((colorLevel)i));
                    }
                }
            }
            rectTexture[i].Apply();
        }
        colorHintLabelTexture = new Texture2D((int)colorHintLabelPos.width,(int)colorHintLabelPos.height);
        float interval = colorHintLabelPos.height / (int)colorLevel.Others;
        for (int i = 0; i < (int)colorHintLabelPos.width; ++i)
        {
            for (int j = 0; j < (int)colorHintLabelPos.height; ++j)
            {
                int index = (int)(j / interval);
                colorHintLabelTexture.SetPixel(i, j, colorLevel2Color((colorLevel)index));
            }
        }
        colorHintLabelTexture.Apply();
    }
    //根据colorLevel获取最终颜色值
    Color colorLevel2Color(colorLevel level)
    {
        Color result = Color.black ;
        switch (level)
        {
            case colorLevel.level1:
                result = new Color(0, 0, 1, 0.5f);
                break;
            case colorLevel.level2:
                result = new Color(0, 0.5f, 0.5f, 0.5f);
                break;
            case colorLevel.level3:
                result = new Color(0, 1, 0, 0.5f);
                break;
            case colorLevel.level4:
                result = new Color(1, 0.92f, 0.016f, 0.5f);
                break;
            case colorLevel.level5:
                result = new Color(1,0, 0, 0.5f);
                break;
            case colorLevel.Others:
                result = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                break;
        }
        return result;
    }
    public override void OnEnter()
    {
      if (Application.isPlaying)
        RefreshData();
    }

    public override void OnExit()
    {
        EditorGUI.FocusTextInControl("");
        scaleAnimationTrigger = false;
    }

    public override void OnEnable() { }

    public override void OnDisable() { }

    public override void Update() {

        if (scaleAnimationTrigger)
        {
            scale += 0.05f;
            if (scale >= 1.0f)
            {
                scale = 1.0f;
                scaleAnimationTrigger = false;
            }
        }
    }
    void initLayoutData()
    {
        rectsPos = new Rect(subWindowRect.x + 130, subWindowRect.y +20, 500, 500);

        colorHintLabelPos = new Rect(subWindowRect.x + 20, subWindowRect.y + 90, 30, 300);

        refreshButtonPos = new Rect(subWindowRect.x + 20, colorHintLabelPos.y + colorHintLabelPos.height+10, 80, 30);

        detailScrollPos = new Rect(subWindowRect.x + 20, rectsPos.y + rectsPos.height +10, subWindowRect.width *0.95f, 100);
    }

    public override void Awake()
    {
        initLayoutData();
        initRectTexture();
        labelStyle.wordWrap = true;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        _repaintFrequency = SubWindowRepaintFrequency.everyFrame;


    }

    public override void OnDestroy() { }


}

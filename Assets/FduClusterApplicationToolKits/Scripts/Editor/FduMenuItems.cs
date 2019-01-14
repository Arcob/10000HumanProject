/*
 * FduMenuItems
 * 
 * 简介：在unity editor中 增添菜单选项的所有函数
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FDUClusterAppToolKits;
public  static class FduMenuItems{

    //打开控制台窗口
    [MenuItem("FduCluster/ConsoleWindow")]
    static void getConsoleWindow()
    {
        FduConsoleWindow.create();
    }
    //打开关于窗口
    [MenuItem("FduCluster/AboutUS")]
    static void getAboutusWindow()
    {
        string message = "";
        message += "Fudan University Cluster Application Tool Kits.\n";
        message += "Author: FuDan University Computer Graphic Laboratory.\n";
        message += "Tool Kits Version:" + FduGlobalConfig.toolKitVersion + ".\n";
        EditorUtility.DisplayDialog("FduClusterToolKit", message, "OK");
    }
    //刷新资源
    [MenuItem("FduCluster/Refresh/Refresh Cluster Assets")]
    static void refreshAsset()
    {
        FduAssetManagerInspector.refreshAssetList();
    }


}

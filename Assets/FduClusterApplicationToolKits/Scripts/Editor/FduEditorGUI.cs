/*
 * FduEditorGUI
 * 
 * 简介：工具包中editor GUI相关的通用函数
 * 或者通用的资源获取接口
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using FDUClusterAppToolKits;
public static  class FduEditorGUI {

    static Texture viewIcon;
    static Texture observerIcon;
    static Texture dataTransmitIcon;
    static Texture attributeIcon;
    static Texture hintIcon;
    static Texture warningIcon;

    static Texture playIcon;
    static Texture pauseIcon;
    static Texture stopIcon;

    static Material lineMaterial;
    //获取换行style
    public static GUIStyle getWordWarp()
    {
        GUIStyle s = new GUIStyle();
        s.wordWrap = true;
        return s;
    }
    //获取文本居中style
    public static GUIStyle getTextCenter()
    {
        GUIStyle s = new GUIStyle();
        s.alignment = TextAnchor.MiddleCenter;
        return s;
    }
    //获取一级标题style
    public static GUIStyle getTitleStyle_LevelOne()
    {
        GUIStyle s = new GUIStyle();
        s.alignment = TextAnchor.MiddleCenter;
        s.fontStyle = FontStyle.Bold;
        s.fontSize = 15;
        return s;
    }
    //获取二级标题style
    public static GUIStyle getTitleStyle_LevelTwo()
    {
        GUIStyle s = new GUIStyle();
        s.alignment = TextAnchor.MiddleCenter;
        s.fontStyle = FontStyle.Bold;
        s.fontSize = 10;
        return s;
    }
    //获取view对应的图标
    public static Texture getViewIcon(){
        if(viewIcon == null)
            viewIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_views.png", typeof(Texture)) as Texture;
        return viewIcon;
    }
    //获取数据传输图标
    public static Texture getDataTransmitIcon()
    {
        if(dataTransmitIcon == null)
            dataTransmitIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_dataTransmit.png", typeof(Texture)) as Texture;
        return dataTransmitIcon;
    }
    //获取监控器图标
    public static Texture getObserverIcon()
    {
        if (observerIcon == null)
            observerIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_observer.png", typeof(Texture)) as Texture;
        return observerIcon;
    }
    //获取提示图标
    public static Texture getHintIcon()
    {
        if (hintIcon == null)
            hintIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/hint.png", typeof(Texture)) as Texture;
        return hintIcon;
    }
    //获取警告图标
    public static Texture getWarningIcon()
    {
        if (warningIcon == null)
            warningIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/warning.png", typeof(Texture)) as Texture;
        return warningIcon;
    }
    //获取监控属性图标
    public static Texture getAttributeIcon()
    {
        if(attributeIcon == null)
            attributeIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/attribute.png", typeof(Texture)) as Texture;
        return attributeIcon;
    }
    //获取播放图标
    public static Texture getPlayIcon()
    {
        if (playIcon == null)
            playIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_play.png", typeof(Texture)) as Texture;
        return playIcon;
    }
    //获取暂停图标
    public static Texture getPauseIcon()
    {
        if (pauseIcon == null)
            pauseIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_pause.png", typeof(Texture)) as Texture;
        return pauseIcon;
    }
    //获取停止图标
    public static Texture getStopIcon()
    {
        if (stopIcon == null)
            stopIcon = AssetDatabase.LoadAssetAtPath("Assets/FduClusterApplicationToolKits/Icons/profile_stop.png", typeof(Texture)) as Texture;
        return stopIcon;
    }
    //获取profile中保存文件的路径
    public static string getProfileFilePath()
    {
        return Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "profileData.fdp";
    }
    //获取一个新的保存profile文件路径
    public static string getNewSavePath()
    {
        return Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "profileData_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".fdp";
    }
    //获取profile文件验证码
    public static int getProfileDataVerifyCode()
    {
        return 441688437;
    }
    //获取图表中的绿色
    public static Color getChartGreenColor()
    {
        return new Color(0.286f, 0.721f, 0.433f,0.8f);
    }
    //获取图表中的黄色
    public static Color getChartYellowColor()
    {
        return new Color(0.813f, 0.875f, 0.232f,0.8f);
    }
    //获取图表中的青色
    public static Color getChartCyanColor()
    {
        return new Color(0.128f,0.756f,0.919f,0.8f);
    }
    //获取图表中的蓝色
    public static Color getChartBlueColor()
    {
        return new Color(0.236f,0.764f,0.846f,0.8f);
    }
    //获取图表中的红色
    public static Color getChartRedColor()
    {
        return new Color(0.794f,0.257f,0.424f,0.8f);
    }
    //获取图表中的橙色
    public static Color getChartOrangeColor()
    {
        return new Color(0.897f,0.419f,0.204f,0.8f);
    }
    //获取图表中的粉色
    public static Color getPurpleColor()
    {
        return new Color(0.246f,0.238f,0.456f,0.8f);
    }
    //获取坐标轴颜色
    public static Color getCoordinateColor()
    {
        return new Color(1.0f, 1.0f, 1.0f, 0.2f);
    }
    //划线前准备函数 设置shader 的pass
    public static void  BeforGLDrawLine(){
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("FduCluster/Colored Blended"));

            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;

        }
        lineMaterial.SetPass(0);
    }

}

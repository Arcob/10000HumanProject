/*
 * FduConsoleSubwindowBase
 * 
 * 简介：控制台中添加子窗口的基类
 * 子窗口需要继承自此类
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
//子窗口重新绘制的频率
public enum SubWindowRepaintFrequency
{
    everyFrame, OnInspectorUpdate
}
//在consolewindow中显示的子窗口基类
public abstract class FduConsoleSubwindowBase{

    //父窗口实例
    public FduConsoleWindow parentWindow;
    //重绘制频率
    protected SubWindowRepaintFrequency _repaintFrequency = SubWindowRepaintFrequency.OnInspectorUpdate;

    public SubWindowRepaintFrequency repaintFrequency { get{ return _repaintFrequency;} }
    //子窗口大小
    protected Rect subWindowRect { get { return FduConsoleWindow.subWindowRect; } }

    //每次重新绘制时调用
    virtual public void DrawSubWindow(){}
    //从别的窗口切换至该窗口时触发
    virtual public void OnEnter() { }
    //切换至别的窗口时触发 先于OnEnter
    virtual public void OnExit() { }

    //启用时触发 同mono
    virtual public void OnEnable() { }
    //禁用时触发 同mono
    virtual public void OnDisable() { }
    //每帧触发
    virtual public void Update() { }
    //子窗口创建时触发一次
    virtual public void Awake() { }
    //摧毁时触发
    virtual public void OnDestroy() { }
    //InspectorUpdat时触发 一般是10帧一次（根据unity文档）
    virtual public void OnInspectorUpdate() { }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingData {

    static SettingData _instance;
    public static SettingData instance{ get{
        if (_instance == null)
        {
            _instance = new SettingData();
            _instance.init();
        }
        return _instance;
    }}

    public DataModel data;

    void init()
    {
        data = XMLUtil.LoadSetting<DataModel>("config\\Setting.xml");
    }

}

public class DataModel
{
    public int humanCount = 0;
    public int threadCount = 4;
    public int delayPercentage = 50;
    public bool isUsingMultiScreen = true;


}
public static class Gen
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("MYGEN/GEN")]
    public static void genData()
    {
        DataModel data = new DataModel();
        data.humanCount = 10000;
        data.isUsingMultiScreen = true;
        XMLUtil.SaveSetting<DataModel>("config\\Setting.xml",data);
    }
#endif
}

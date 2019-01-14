using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
using System.Runtime.InteropServices;
public enum CustomEnum
{
    Enum1,Enum2,Enum3
}

public struct CustomStructData
{
    public Rect CustomRectData;

    public Vector3 CustomVector3Data;

    public Color CustomColorData;

    public CustomEnum CustomEnumData;

    public InsideCustomData InsideData;
}

[System.Serializable]
public class CustomClassData
{
    public string CustomClassStringData;
    public List<int> CustomClassList = new List<int>();
    public Dictionary<string, int> CustomClassDictionaryData = new Dictionary<string, int>();
}

public struct InsideCustomData
{
    public int insideData;
    public InsideCustomData(int value)
    {
        insideData = value;
    }
}


public class CustomObserverSample : FduObserverBase
{
    int IntInstance;

    Vector3 Vector3Instance;

    string StringInstance = "";

    CustomStructData CustomDataInstance;

    CustomClassData CustomClassDataInstance;

    List<InsideCustomData> ListData = new List<InsideCustomData>();

    Dictionary<string, Vector3> DicData = new Dictionary<string, Vector3>();
    
    void Awake()
    {
        setDTSData_Direct();
        fduObserverInit();
    }

    void Start()
    {
        DataRefresh();

        FduClusterCommandDispatcher.AddCommandExecutor("TestCommand", (ClusterCommand c) => { Debug.Log(((CustomClassData)c.getParameter("para1")).CustomClassList.Count); });
    }
    public override void AlwaysUpdate()
    {
        base.AlwaysUpdate();
        RefreshUI();
    }

    public override void OnSendData()
    {
        BufferedNetworkUtilsServer.SendSerializableClass(CustomClassDataInstance);

        BufferedNetworkUtilsServer.SendInt(IntInstance);

        BufferedNetworkUtilsServer.SendVector3(Vector3Instance);

        BufferedNetworkUtilsServer.SendString(StringInstance);

        BufferedNetworkUtilsServer.SendStruct(CustomDataInstance);

        BufferedNetworkUtilsServer.SendList<InsideCustomData>(ListData);

        BufferedNetworkUtilsServer.SendDic<string, Vector3>(DicData);
    }

    public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
    {
        CustomClassDataInstance = (CustomClassData)BufferedNetworkUtilsClient.ReadSerializableClass(ref state);

        IntInstance = BufferedNetworkUtilsClient.ReadInt(ref state);

        Vector3Instance = BufferedNetworkUtilsClient.ReadVector3(ref state);

        StringInstance = BufferedNetworkUtilsClient.ReadString(ref state);

        CustomDataInstance = (CustomStructData)BufferedNetworkUtilsClient.ReadStruct(typeof(CustomStructData), ref state);

        ListData = BufferedNetworkUtilsClient.ReadList<InsideCustomData>(ref state);

        DicData = BufferedNetworkUtilsClient.ReadDic<string, Vector3>(ref state);
    }
    public void DataRefresh()
    {
        IntInstance = Random.Range(0, 50);

        Vector3Instance = Vector3.one * Random.Range(0, 50);

        string all = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ~!@#$%^&*()_+";
        int slength = Random.Range(1, 20);
        StringInstance = "";
        for (int i = 0; i < slength; ++i)
            StringInstance += all[Random.Range(0, all.Length)];

        CustomDataInstance.CustomVector3Data = Vector3.one * Random.Range(0, 50);
        CustomDataInstance.CustomColorData = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        CustomDataInstance.CustomEnumData = (CustomEnum)Random.Range(0,3);
        CustomDataInstance.InsideData.insideData = Random.Range(0, 50);

        ListData.Clear();
        ListData.Add(new InsideCustomData(Random.Range(0, 50)));
        ListData.Add(new InsideCustomData(Random.Range(0, 50)));
        ListData.Add(new InsideCustomData(Random.Range(0, 50)));

        DicData.Clear();
        DicData.Add("Key1", Vector3.one * Random.Range(0, 50));
        DicData.Add("Key2", Vector3.one * Random.Range(0, 50));
        DicData.Add("Key3", Vector3.one * Random.Range(0, 50));

        CustomClassDataInstance = new CustomClassData();
        int len = Random.Range(1, 5);
        CustomClassDataInstance.CustomClassStringData = "";
        for (int i = 0; i < len; ++i)
        {
            CustomClassDataInstance.CustomClassList.Add(Random.Range(0, 50));
            CustomClassDataInstance.CustomClassDictionaryData.Add("Key" + i, Random.Range(0, 50));
            CustomClassDataInstance.CustomClassStringData += all[Random.Range(0, all.Length)];
        }

        ClusterCommand c = ClusterCommand.create("666");
        c.addParameters("123",456,"555","miao");
        c.addDic<string,Vector3>("dicName",DicData);
        FduClusterCommandDispatcher.SendClusterCommand(c);

    }
    void RefreshUI()
    {
        var text = GameObject.Find("InfoText");
        if (text != null)
        {
            string result = "";

            result += "Int Value:" + IntInstance.ToString() +"\n";

            result += "Vector3 Value:" + Vector3Instance.ToString() + "\n";

            result += "String Value:" + StringInstance.ToString() + "\n";

            result += "Custom Struct Data Value: \n";

            result += "    Custom Vector3 Data:" + CustomDataInstance.CustomVector3Data + "\n";

            result += "    Custom Color Data:" + CustomDataInstance.CustomColorData + "\n";

            result += "    Custom Enum Data:" + CustomDataInstance.CustomEnumData + "\n";

            result += "    Custom Rect Data:" + CustomDataInstance.CustomRectData + "\n";

            result += "    Inside Custom Data:" + CustomDataInstance.InsideData.insideData + "\n";

            result += "Custom Serializable Class Data Value: \n";

            result += "    Custom String Data:" + CustomClassDataInstance.CustomClassStringData + "\n";

            result += "    Custom List Data:";

            for (int i = 0; i < CustomClassDataInstance.CustomClassList.Count; ++i)
            {
                result += " " + CustomClassDataInstance.CustomClassList[i].ToString();
            }

            result += "\n";

            result += "    Custom Dicitonary Data:";

            var classDicEnu = CustomClassDataInstance.CustomClassDictionaryData.GetEnumerator();

            while (classDicEnu.MoveNext())
            {
                result += " Key: " + classDicEnu.Current.Key + " Value: " + classDicEnu.Current.Value + " ."; 
            }
            result += "\n";

            result += "List<InsideCustomData> Value: \n";

            if(ListData.Count>0)
            result += "   " + ListData[0].insideData;

            for (int i = 1; i < ListData.Count; ++i)
            {
                result += " , " + ListData[i].insideData;
            }

            result += "\n Dictionary<string,int> Value: \n";


            var enu = DicData.GetEnumerator();
            while (enu.MoveNext())
            {
                result += "    Key: " + enu.Current.Key + " Value:" + enu.Current.Value + "  \n";
            }

            text.GetComponent<UnityEngine.UI.Text>().text = result;
        }
        
    }

}

// If you want to  add a string or array in your struct:

/*
 * public unsafe struct CustomData
{
    public int CustomIntData;

    public float CustomFloatData;

    public Vector3 CustomVector3Data;

    public Color CustomColorData;

    public CustomEnum CustomEnumData;

    public InsideCustomData InsideData;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string CustomStringData;

    public fixed int CustomIntArr[5];
}
*/
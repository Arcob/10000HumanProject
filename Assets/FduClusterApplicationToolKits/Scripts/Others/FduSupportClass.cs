/*
 * FduSupportClass
 * 
 * 简介：静态类 一些通用的函数放在这里
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class FduSupportClass
    {
        //序列化一个可传送的参数
        public static void serializeOneParameter(object para)
        {
            FduSendableParameter typeCode = FduGlobalConfig.getSendableParameterCode(para);
            BufferedNetworkUtilsServer.SendByte((byte)typeCode);
            switch (typeCode)
            {
                case FduSendableParameter.Int:
                    BufferedNetworkUtilsServer.SendInt((int)para);
                    break;
                case FduSendableParameter.IntArray:
                    BufferedNetworkUtilsServer.SendIntArray((int[])para);
                    break;
                case FduSendableParameter.Byte:
                    BufferedNetworkUtilsServer.SendByte((byte)para);
                    break;
                case FduSendableParameter.ByteArray:
                    BufferedNetworkUtilsServer.SendByteArray((byte[])para);
                    break;
                case FduSendableParameter.Float:
                    BufferedNetworkUtilsServer.SendFloat((float)para);
                    break;
                case FduSendableParameter.FloatArray:
                    BufferedNetworkUtilsServer.SendFloatArray((float[])para);
                    break;
                case FduSendableParameter.Bool:
                    BufferedNetworkUtilsServer.SendBool((bool)para);
                    break;
                case FduSendableParameter.BoolArray:
                    bool[] bolarr = (bool[])para;
                    int bollen = bolarr.Length;
                    BufferedNetworkUtilsServer.SendInt(bollen);
                    for (int i = 0; i < bollen; ++i)
                        BufferedNetworkUtilsServer.SendBool(bolarr[i]);
                    break;
                case FduSendableParameter.String:
                    BufferedNetworkUtilsServer.SendString((string)para);
                    break;
                case FduSendableParameter.StringArray:
                    string[] strarr = (string[])para;
                    int strlen = strarr.Length;
                    BufferedNetworkUtilsServer.SendInt(strlen);
                    for (int i = 0; i < strlen; ++i)
                        BufferedNetworkUtilsServer.SendString(strarr[i]);
                    break;
                case FduSendableParameter.Vector2:
                    BufferedNetworkUtilsServer.SendVector2((Vector2)para);
                    break;
                case FduSendableParameter.Vector2Array:
                    Vector2[] v2arr = (Vector2[])para;
                    int v2len = v2arr.Length;
                    BufferedNetworkUtilsServer.SendInt(v2len);
                    for (int i = 0; i < v2len; ++i)
                        BufferedNetworkUtilsServer.SendVector2(v2arr[i]);
                    break;
                case FduSendableParameter.Vector3:
                    BufferedNetworkUtilsServer.SendVector3((Vector3)para);
                    break;
                case FduSendableParameter.Vector3Array:
                    Vector3[] v3arr = (Vector3[])para;
                    int v3len = v3arr.Length;
                    BufferedNetworkUtilsServer.SendInt(v3len);
                    for (int i = 0; i < v3len; ++i)
                        BufferedNetworkUtilsServer.SendVector3(v3arr[i]);
                    break;
                case FduSendableParameter.Vector4:
                    BufferedNetworkUtilsServer.SendVector4((Vector4)para);
                    break;
                case FduSendableParameter.Vector4Array:
                    Vector4[] v4arr = (Vector4[])para;
                    int v4len = v4arr.Length;
                    BufferedNetworkUtilsServer.SendInt(v4len);
                    for (int i = 0; i < v4len; ++i)
                        BufferedNetworkUtilsServer.SendVector3(v4arr[i]);
                    break;
                case FduSendableParameter.Color:
                    BufferedNetworkUtilsServer.SendColor((Color)para);
                    break;
                case FduSendableParameter.ColorArray:
                    Color[] carray = (Color[])para;
                    int clen = carray.Length;
                    BufferedNetworkUtilsServer.SendInt(clen);
                    for (int i = 0; i < clen; ++i)
                        BufferedNetworkUtilsServer.SendColor(carray[i]);
                    break;
                case FduSendableParameter.Quaternion:
                    BufferedNetworkUtilsServer.SendQuaternion((Quaternion)para);
                    break;
                case FduSendableParameter.QuaternionArray:
                    Quaternion[] qarr = (Quaternion[])para;
                    int qlen = qarr.Length;
                    BufferedNetworkUtilsServer.SendInt(qlen);
                    for (int i = 0; i < qlen; i++)
                        BufferedNetworkUtilsServer.SendQuaternion(qarr[i]);
                    break;
                case FduSendableParameter.GameObject:
                    if (((GameObject)para).GetClusterView() != null)
                    {
                        BufferedNetworkUtilsServer.SendBool(true);
                        BufferedNetworkUtilsServer.SendInt(((GameObject)para).GetClusterView().ViewId);
                    }
                    else
                    {
                        BufferedNetworkUtilsServer.SendBool(false);
                        BufferedNetworkUtilsServer.SendString(FduSupportClass.getGameObjectPath((GameObject)para));
                    }
                    break;
                case FduSendableParameter.ClusterView:
                    BufferedNetworkUtilsServer.SendInt(((FduClusterView)para).ViewId);
                    break;
                case FduSendableParameter.Matrix4X4:
                    BufferedNetworkUtilsServer.SendMatrix4x4((Matrix4x4)para);
                    break;
                case FduSendableParameter.Matrix4X4Array:
                    Matrix4x4[] matarr = (Matrix4x4[])para;
                    int mlen = matarr.Length;
                    BufferedNetworkUtilsServer.SendInt(mlen);
                    for (int i = 0; i < mlen; ++i)
                        BufferedNetworkUtilsServer.SendMatrix4x4(matarr[i]);
                    break;
                case FduSendableParameter.Rect:
                    BufferedNetworkUtilsServer.SendRect((Rect)para);
                    break;
                case FduSendableParameter.RectArray:
                    Rect[] rectArr = (Rect[])para;
                    int rectlen = rectArr.Length;
                    BufferedNetworkUtilsServer.SendInt(rectlen);
                    for (int i = 0; i < rectlen; ++i)
                        BufferedNetworkUtilsServer.SendRect(rectArr[i]);
                    break;
                case FduSendableParameter.Struct:
                    BufferedNetworkUtilsServer.SendString(para.GetType().AssemblyQualifiedName);
                    BufferedNetworkUtilsServer.SendStruct(para);
                    break;
                case FduSendableParameter.SerializableClass:
                    BufferedNetworkUtilsServer.SendSerializableClass(para);
                    break;
                case FduSendableParameter.Enum:
                    BufferedNetworkUtilsServer.SendInt(System.Convert.ToInt32(para));
                    break;
                default:
                    throw new InvalidSendableDataException("Such type of data can not be sent. Type name:" + para.GetType().FullName);
            }
        }

        //反序列化一个可传输的参数
        public static object deserializeOneParameter(ref  NetworkState.NETWORK_STATE_TYPE state)
        {
            FduSendableParameter typeCode = (FduSendableParameter)BufferedNetworkUtilsClient.ReadByte(ref state);
            object result;
            switch (typeCode)
            {
                case FduSendableParameter.Int:
                    result = BufferedNetworkUtilsClient.ReadInt(ref state);
                    break;
                case FduSendableParameter.IntArray:
                    result = BufferedNetworkUtilsClient.ReadIntArray(ref state);
                    break;
                case FduSendableParameter.Byte:
                    result = BufferedNetworkUtilsClient.ReadByte(ref state);
                    break;
                case FduSendableParameter.ByteArray:
                    result = BufferedNetworkUtilsClient.ReadByteArray(ref state);
                    break;
                case FduSendableParameter.Float:
                    result = BufferedNetworkUtilsClient.ReadFloat(ref state);
                    break;
                case FduSendableParameter.FloatArray:
                    result = BufferedNetworkUtilsClient.ReadFloatArray(ref state);
                    break;
                case FduSendableParameter.Bool:
                    result = BufferedNetworkUtilsClient.ReadBool(ref state);
                    break;
                case FduSendableParameter.BoolArray:
                    int bollen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    bool[] bolarr = new bool[bollen];
                    for (int i = 0; i < bollen; ++i)
                        bolarr[i] = BufferedNetworkUtilsClient.ReadBool(ref state);
                    result = bolarr;
                    break;
                case FduSendableParameter.String:
                    result = BufferedNetworkUtilsClient.ReadString(ref state);
                    break;
                case FduSendableParameter.StringArray:

                    int strlen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    string[] strarr = new string[strlen];
                    for (int i = 0; i < strlen; ++i)
                        strarr[i] = BufferedNetworkUtilsClient.ReadString(ref state);
                    result = strarr;
                    break;
                case FduSendableParameter.Vector2:
                    result = BufferedNetworkUtilsClient.ReadVector2(ref state);
                    break;
                case FduSendableParameter.Vector2Array:
                    int v2len = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Vector2[] v2arr = new Vector2[v2len];
                    for (int i = 0; i < v2len; ++i)
                        v2arr[i] = BufferedNetworkUtilsClient.ReadVector2(ref state);
                    result = v2arr;
                    break;
                case FduSendableParameter.Vector3:
                    result = BufferedNetworkUtilsClient.ReadVector3(ref state);
                    break;
                case FduSendableParameter.Vector3Array:
                    int v3len = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Vector3[] v3arr = new Vector3[v3len];
                    for (int i = 0; i < v3len; ++i)
                        v3arr[i] = BufferedNetworkUtilsClient.ReadVector3(ref state);
                    result = v3arr;
                    break;
                case FduSendableParameter.Vector4:
                    result = BufferedNetworkUtilsClient.ReadVector4(ref state);
                    break;
                case FduSendableParameter.Vector4Array:
                    int v4len = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Vector4[] v4arr = new Vector4[v4len];
                    for (int i = 0; i < v4len; ++i)
                        v4arr[i] = BufferedNetworkUtilsClient.ReadVector4(ref state);
                    result = v4arr;
                    break;
                case FduSendableParameter.Color:
                    result = BufferedNetworkUtilsClient.ReadColor(ref state);
                    break;
                case FduSendableParameter.ColorArray:
                    int clen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Color[] carray = new Color[clen];
                    for (int i = 0; i < clen; ++i)
                        carray[i] = BufferedNetworkUtilsClient.ReadColor(ref state);
                    result = carray;
                    break;
                case FduSendableParameter.Quaternion:
                    result = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                    break;
                case FduSendableParameter.QuaternionArray:
                    int qlen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Quaternion[] qarr = new Quaternion[qlen];
                    for (int i = 0; i < qlen; i++)
                        qarr[i] = BufferedNetworkUtilsClient.ReadQuaternion(ref state);
                    result = qarr;
                    break;
                case FduSendableParameter.Matrix4X4:
                    result = BufferedNetworkUtilsClient.ReadMatrix4x4(ref state);
                    break;
                case FduSendableParameter.Matrix4X4Array:
                    int matlen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Matrix4x4 [] matArr = new Matrix4x4[matlen];
                    for (int i = 0; i < matlen; ++i)
                        matArr[i] = BufferedNetworkUtilsClient.ReadMatrix4x4(ref state);
                    result = matArr;
                    break;
                case FduSendableParameter.Rect:
                    result = BufferedNetworkUtilsClient.ReadRect(ref state);
                    break;
                case FduSendableParameter.RectArray:
                    int rectlen = BufferedNetworkUtilsClient.ReadInt(ref state);
                    Rect[] rectArr = new Rect[rectlen];
                    for (int i = 0; i < rectlen; ++i)
                        rectArr[i] = BufferedNetworkUtilsClient.ReadRect(ref state);
                    result = rectArr;
                    break;
                case FduSendableParameter.GameObject:
                    bool goType = BufferedNetworkUtilsClient.ReadBool(ref state);
                    if (goType) //有clusterview的物体
                    {
                        var view = FduClusterViewManager.getClusterView(BufferedNetworkUtilsClient.ReadInt(ref state));
                        if (view != null)
                            result = view.gameObject;
                        else
                            result = null;
                    }
                    else //有唯一路径的gameobject
                    {
                        string path = BufferedNetworkUtilsClient.ReadString(ref state);
                        result = FduSupportClass.getGameObjectByPath(path);
                    }
                    break;
                case FduSendableParameter.ClusterView:
                    result = FduClusterViewManager.getClusterView(BufferedNetworkUtilsClient.ReadInt(ref state));
                    break;
                case FduSendableParameter.Struct:
                    try
                    {
                        string typeName = BufferedNetworkUtilsClient.ReadString(ref state);
                        System.Type type = System.Type.GetType(typeName);
                        result = BufferedNetworkUtilsClient.ReadStruct(type, ref state);
                    }
                    catch (System.Exception e) { Debug.LogError("Error occured in reading struct data. Details: " + e.Message); throw; }
                    break;
                case FduSendableParameter.SerializableClass:
                    result = BufferedNetworkUtilsClient.ReadSerializableClass(ref state);
                    break;
                case FduSendableParameter.Enum:
                    result = BufferedNetworkUtilsClient.ReadInt(ref state);
                    break;
                default:
                    throw new InvalidSendableDataException("Received unsendable data type, type code:" + typeCode);
            }
            return result;
        }
        
        public static bool isSendableGenericType(System.Type type)
        {
            if (type.IsValueType)
                return true;
            else if (type == typeof(string))
                return true;
            else
                return false;
        }
        //获取这个GameObject的路径
        public static string getGameObjectPath(GameObject go)
        {
            if (go == null) return null;
            Stack<string> pathStack = new Stack<string>();
            Transform ts = go.transform;
            pathStack.Push(go.name);
            while (ts.parent != null)
            {
                ts = ts.parent;
                pathStack.Push(ts.gameObject.name);
            }
            string path = "";
            while (pathStack.Count > 0)
            {
                path += "/" + pathStack.Pop();
            }
            return path;
        }
        public static string getGameObjectIndexPath(GameObject go)
        {
            if (go == null) return null;
            Stack<string> pathStack = new Stack<string>();
            Transform ts = go.transform;
            pathStack.Push(go.transform.GetSiblingIndex().ToString());
            while (ts.parent != null)
            {
                ts = ts.parent;
                pathStack.Push(ts.GetSiblingIndex().ToString());
            }
            string path = "";
            if (pathStack.Count > 0)
                path = pathStack.Pop();
            while (pathStack.Count > 0)
            {
                path += ";" + pathStack.Pop();
            }
            return path;
        }
        //根据路径获取gameObject
        public static GameObject getGameObjectByPath(string path)
        {
            if (path == null) return null;
            return GameObject.Find(path);
        }
        public static GameObject getGameObjectByIndexPath(string path)
        {
            if (path == null) return null;
            var ss = path.Split(';');
            List<int> indexPath = new List<int>();
            foreach (string s in ss)
            {
                try
                {
                    indexPath.Add(int.Parse(s));
                }
                catch (System.Exception )
                {
                    Debug.LogError("Get GameObject By IndexPath Failed!.");
                    return null;
                }
            }
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject result = null;
            if (scene.isLoaded)
            {
                var allGameObjects = scene.GetRootGameObjects();

                result = allGameObjects[indexPath[0]];

                for (int i = 1; i < indexPath.Count; ++i)
                {
                    if (indexPath[i] < 0 || indexPath[i] >= result.transform.childCount)
                        return null;

                    result = result.transform.GetChild(indexPath[i]).gameObject;
                }
            }
            return result;
        }

        //格式化传输数据 size是字节数
        public static string getDataSizeString(float size)
        {
            string result = "";
            if (size < 1024)
            {
                result = size.ToString() + "B";
            }
            else if (size < 1048576)
            {
                result = (size / 1024.0f).ToString("F2") + "KB";
            }
            else
                result = (size / 1048576.0f).ToString("F2") + "MB";
            return result;
        }
        //是否为master节点
        public static bool isMaster
        {
            get
            {
                if (ClusterHelper.Instance == null)
                    return false;
                else
                    return (ClusterHelper.Instance.Server != null);
            }
        }
        //是否为Slave节点
        public static bool isSlave
        {
            get
            {
                if (ClusterHelper.Instance == null)
                    return false;
                else
                    return (ClusterHelper.Instance.Client != null);
            }
        }
        //集群版本是否已启动
        public static bool clusterEnable
        {
            get
            {
#if CLUSTER_ENABLE
                return true;
#else 
            return false;
#endif
            }
        }

    }
    //非法的传输数据类型异常
    public class InvalidSendableDataException : System.Exception
    {
        public InvalidSendableDataException() { }
        public InvalidSendableDataException(string message) : base(message) { }
    }
}
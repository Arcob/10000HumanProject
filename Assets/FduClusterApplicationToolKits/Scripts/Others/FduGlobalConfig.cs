/*
 * FduGlobalConfig
 * 
 * 简介：本工具集所需要用到的一些只可以访问的全局变量
 * 
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
using System;
namespace FDUClusterAppToolKits
{
    //可传输的参数类型
    public enum FduSendableParameter
    {
        Byte, Int, Float, Bool, String, Vector2, Vector3, Vector4, Color, Quaternion, Matrix4X4,Rect,
        ByteArray, IntArray, FloatArray, BoolArray, StringArray, Vector2Array, Vector3Array, Vector4Array, ColorArray, QuaternionArray,Matrix4X4Array,RectArray,
        GameObject,ClusterView, Struct,Enum,SerializableClass,
        NotImplemented
    }
    //Editor中用到的数据传输类的枚举变量
    public enum EditorDTSEnum
    {
        NULL, Direct, EveryNFrame, OnClusterCommand, CustomStrategy
    }
    public static class FduGlobalConfig
    {
        //工具的版本号
        public static readonly string toolKitVersion = "Test2.0.170925";

        //支持的最大View数量
        public static readonly int MAX_VIEW_COUNT = 9999;

        //2的不同次方数 在位数组中有用到
        public static readonly int[] BIT_MASK = { 
            0,2,4,8,16,32,64,128,256,512,1024,2048,4096,8192,16384,32768,65536,131072,262144,524288,1048576,2097152,4194304,8388608,16777216,
            33554432,67108864,134217728,268435456,536870912,1073741824,-2147483648                              
        };
        //Editor中用到的数据传输类的枚举变量和名称的映射表
        public static readonly Dictionary<EditorDTSEnum, string> editorDTSEnum2nameMap = new Dictionary<EditorDTSEnum, string>()
        {
            {EditorDTSEnum.NULL,"FduDTS_NULL"}, {EditorDTSEnum.Direct,"FduDTS_Direct"},{EditorDTSEnum.EveryNFrame,"FduDTS_EveryNFrame"},{EditorDTSEnum.OnClusterCommand,"FduDTS_OnClusterCommand"},{EditorDTSEnum.CustomStrategy,""}
        };

        public static readonly int EVERY_N_FRAME_MAX_FRAME = 120;

        //类型名称和枚举变量的映射表
        static readonly Dictionary<Type, FduSendableParameter> ParamTypeCodedic = new Dictionary<Type, FduSendableParameter>() { 

            {typeof(int),FduSendableParameter.Int},{typeof(int []), FduSendableParameter.IntArray},
            {typeof(byte), FduSendableParameter.Byte},{typeof(byte[]), FduSendableParameter.ByteArray},
            {typeof(float), FduSendableParameter.Float},{typeof(float[]), FduSendableParameter.FloatArray},
            {typeof(bool), FduSendableParameter.Bool},{typeof(bool[]), FduSendableParameter.BoolArray},
            {typeof(string), FduSendableParameter.String},{typeof(string []), FduSendableParameter.StringArray},
            {typeof(Vector2), FduSendableParameter.Vector2},{typeof(Vector2 []), FduSendableParameter.Vector2Array},
            {typeof(Vector3), FduSendableParameter.Vector3},{typeof(Vector3 []), FduSendableParameter.Vector3Array},
            {typeof(Vector4), FduSendableParameter.Vector4},{typeof(Vector4 []), FduSendableParameter.Vector4Array},
            {typeof(Color), FduSendableParameter.Color},{typeof(Color []), FduSendableParameter.ColorArray},
            {typeof(Quaternion), FduSendableParameter.Quaternion},{typeof(Quaternion []), FduSendableParameter.QuaternionArray},
            {typeof(GameObject),FduSendableParameter.GameObject}, {typeof(FduClusterView),FduSendableParameter.ClusterView},
            {typeof(Matrix4x4),FduSendableParameter.Matrix4X4},{typeof(Matrix4x4[]),FduSendableParameter.Matrix4X4Array},
            {typeof(Rect),FduSendableParameter.Rect},{typeof(Rect[]),FduSendableParameter.RectArray},
        };

        //static readonly Dictionary<Type, FduSendableParameter>

        //根据参数获取该参数的可传递类型枚举变量
        public static FduSendableParameter getSendableParameterCode(object parameter)
        {
            FduSendableParameter _result = FduSendableParameter.NotImplemented;
            if (parameter == null)
            {
                Debug.LogError("[FduSendableParameter]Sendable parameter can not be null!");
                return _result;
            }
            Type _paraType = parameter.GetType();
            if (!ParamTypeCodedic.TryGetValue(_paraType, out _result))
            {
                if (_paraType.IsEnum)
                {
                    _result = FduSendableParameter.Enum;
                }
                else if (_paraType.IsValueType) { 

                    _result = FduSendableParameter.Struct; 
                }
                else if (_paraType.IsSerializable)
                {
                    _result = FduSendableParameter.SerializableClass;
                }
                else
                {
                    _result = FduSendableParameter.NotImplemented;
                    Debug.LogWarning("[FduSendableParameter]Can not send such type of parameter, type name is " + parameter.GetType().FullName + ", Please refer to enum FduSendableParameter or relative doc");
                }
            }
            return _result;
        }
    }
}


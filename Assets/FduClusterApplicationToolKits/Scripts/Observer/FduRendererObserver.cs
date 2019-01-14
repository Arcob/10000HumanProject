/*
 * FduRendererObserver
 * 
 * 简介：监控Renderer的监控器 可以监控renderer的enable状态
 * 可以监控所有对应材质Shader的属性
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    [System.Serializable]
    public class FduShaderSyncAttr
    {
        public string materialName;
        public List<byte> propertyType;
        public List<string> propertyName;
        public byte [] bitarr;
    }

    [RequireComponent(typeof(Renderer))]
    public class FduRendererObserver : FduMultiAttributeObserverBase
    {
        
        public static string[] attributeList = { "NULL","Observe Enable", "Observe Shader State" };

        //材质列表 一个render可以有多个材质 每个材质对应的shader有对应的属性类型
        [SerializeField]
        List<FduShaderSyncAttr> attrList;
        //为了Editor里保存所用到的变量
        [SerializeField]
        bool forceSaveFlag = false;

        public Renderer _renderer;
        //非法检查flag
        bool invalideCheck = false;

        //每个shader对应一个bitarray 一个bitarray的一个位值代表是否监控该属性
        List<BitArray> bitArrList = new List<BitArray>();

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
#if CLUSTER_ENABLE
            fduObserverInit();
            loadObservedState();
            initBitarrayList();
            validateSerializeProperty();
            if (invalideCheck == false)
            {
                Debug.LogError("[FduRendererObserver]Material Property has changed! Please press refresh Button in Inspector window! Object name:" + gameObject.name);
            }
#endif
        }

        public override void OnSendData()
        {
            //是否监控enable状态
            if (getObservedState(1))
            {
                BufferedNetworkUtilsServer.SendBool(_renderer.enabled);
            }
            //是否监控shader状态
            if (getObservedState(2))
            {
                if (!invalideCheck)
                    return;
                for (int i = 0; i < attrList.Count; ++i)
                {
                    for (int j = 0; j < attrList[i].propertyType.Count; ++j)
                    {
                        if (bitArrList[i][j])
                        {
                            SerializeOnePara(attrList[i].propertyType[j], _renderer.materials[i],attrList[i].propertyName[j]);
                        }
                    }
                }
            }
#if UNITY_EDITOR
            BufferedNetworkUtilsServer.SendString("RenderObserverFlag");
#endif
        }

        public override void OnReceiveData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            //是否监控enable状态
            if (getObservedState(1))
            {
                _renderer.enabled = BufferedNetworkUtilsClient.ReadBool(ref state);
            }
            //是否监控shader状态
            if (getObservedState(2))
            {
                if (!invalideCheck)
                    return;
                for (int i = 0; i < attrList.Count; ++i)
                {
                    for (int j = 0; j < attrList[i].propertyType.Count; ++j)
                    {
                        if (bitArrList[i][j])
                        {
                            
                            DeserializeOnePara(attrList[i].propertyType[j], _renderer.materials[i],attrList[i].propertyName[j] , ref state);
                        }
                    }
                }
            }
#if UNITY_EDITOR
            if (BufferedNetworkUtilsClient.ReadString(ref state) != "RenderObserverFlag")
            {
                Debug.LogError("Wrong end");
            }
#endif
        }

        void SerializeOnePara(byte propertyType, Material mat,string  propertyName)
        {
            switch (propertyType)
            {
                case 0://Color
                    BufferedNetworkUtilsServer.SendColor(mat.GetColor(propertyName));
                    break;
                case 1://Vector
                    BufferedNetworkUtilsServer.SendVector4(mat.GetVector(propertyName));
                    break;
                case 2://Float
                    BufferedNetworkUtilsServer.SendFloat(mat.GetFloat(propertyName));
                    break;
                case 3://Range
                    BufferedNetworkUtilsServer.SendFloat(mat.GetFloat(propertyName));
                    break;
                case 4://TexEnv
                    break;
            }
        }
        void DeserializeOnePara(byte propertyType, Material mat, string propertyName,ref NetworkState.NETWORK_STATE_TYPE state) {

            switch (propertyType)
            {
                case 0://Color
                    mat.SetColor(propertyName, BufferedNetworkUtilsClient.ReadColor(ref state));
                    break;
                case 1://Vector
                    mat.SetVector(propertyName, BufferedNetworkUtilsClient.ReadVector4(ref state));
                    break;
                case 2://Float
                    mat.SetFloat(propertyName, BufferedNetworkUtilsClient.ReadFloat(ref state));
                    break;
                case 3://Range
                    mat.SetFloat(propertyName, BufferedNetworkUtilsClient.ReadFloat(ref state));
                    break;
                case 4://TexEnv
                    break;
            }
        }
        //初始化bitarray列表
        void initBitarrayList()
        {
            if (attrList == null)
                return;
            bitArrList.Clear();
            for (int i = 0; i < attrList.Count; ++i)
            {
                bitArrList.Add(new BitArray(attrList[i].bitarr));
            }
        }
        //验证shader属性是否存在 因为有可能shader更新了 而没有在Inspector中更新
        void validateSerializeProperty()
        {
            invalideCheck = true;
            if (_renderer.materials.Length != attrList.Count)
                invalideCheck = false;
            else
            {
                for (int i = 0; i <attrList.Count; ++i)
                {
                    for(int j=0;j<attrList[i].propertyName.Count;++j)
                    {
                        if (!_renderer.materials[i].HasProperty(attrList[i].propertyName[j]))
                        {
                            invalideCheck = false;
                            break;
                        }
                    }
                    
                }
            }
        }

#if UNITY_EDITOR
        //刷新renderer数据
        public void refreshData()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            if (attrList == null)
                attrList = new List<FduShaderSyncAttr>();
            else
                attrList.Clear();
            for (int i = 0; i < _renderer.sharedMaterials.Length; ++i)
            {
                FduShaderSyncAttr attr = new FduShaderSyncAttr();
                attr.materialName = _renderer.sharedMaterials[i].name;
                attr.propertyType = new List<byte>();
                attr.propertyName = new List<string>();
                int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount(_renderer.sharedMaterials[i].shader);
                attr.bitarr = new byte[propertyCount / 8 + 1];
                for (int j = 0; j < propertyCount; ++j)
                {
                    attr.propertyType.Add((byte)UnityEditor.ShaderUtil.GetPropertyType(_renderer.sharedMaterials[i].shader, j));
                    attr.propertyName.Add(UnityEditor.ShaderUtil.GetPropertyName(_renderer.sharedMaterials[i].shader, j));
                }
                attrList.Add(attr);
            }
        }
        public List<FduShaderSyncAttr> getShaderSyncAttrList()
        {
            return attrList;
        }
        public void setObState(int materialIndex, BitArray bits)
        {
            if (bits != null)
            {
                attrList[materialIndex].bitarr = new byte[bits.Count / 8 + 1]; 
                bits.CopyTo(attrList[materialIndex].bitarr, 0);
            }
        }

#endif
    }
}

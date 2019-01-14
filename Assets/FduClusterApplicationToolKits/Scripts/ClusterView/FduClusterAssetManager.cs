/*
 * FduClusterAssetManager
 * 简介：集群资源管理类
 * 目前的资源仅仅指的是动态创建的prefab
 * 原理：通过FduClusterAssetManager所对应的editor类
 * 在editor中寻找到所有带着FduClusterView组件的预制体（并且该预制体勾选了监控创建选项）
 * 并将这些GameObject序列化保存起来 当从节点需要创建预制体时，只需要通过保存好的AssetId
 * 访问本类提供的GameObject实例，再通过GameObject.Instantiate创建即可
 * 
 * 最后修改： Hayaye 2017.07.08
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public class FduClusterAssetManager : MonoBehaviour
    {
        //序列化的预制体列表
        [SerializeField]
        List<GameObject> gameObjectAssetList = new List<GameObject>();

        public static FduClusterAssetManager Instance;

        void Awake()
        {
            Instance = this;
        }
        //根据id获取对应实例
        public GameObject getGameObjectFromId(int id)
        {
            if (validateId(id))
            {
                return gameObjectAssetList[id];
            }
            return null;
        }
        public bool validateId(int id)
        {
            if (id < 0 || id >= gameObjectAssetList.Count)
                return false;
            else
                return true;
        }
    }
}

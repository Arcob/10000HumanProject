/*
 * ClusterGameObjectCreator
 * 简介：集群游戏物体创建器 静态类 主要包含了创建集群物体的静态方法
 * 集群物体的创建不需要显式的修改代码API 凡是设置了监控某个cluster view的创建属性
 * 该view对应的物体在start中会向从节点发出创建该物体的命令 从节点收到命令后创建对应的游戏物体
 * 
 * 最后修改时间：2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class ClusterGameObjectCreator
    {

        //简易的创建函数 不支持被创建物体包含多个子view的情况
        public static GameObject createGameObjectWithClusterView(int viewId, int AssetId, Vector3 position, Quaternion rotation, string parentPath)
        {
            GameObject go = FduClusterAssetManager.Instance.getGameObjectFromId(AssetId);
            if (go == null) return null;

            GameObject parent = FduSupportClass.getGameObjectByPath(parentPath);
            GameObject instance;

            if (parent == null)
                instance = GameObject.Instantiate(go, position, rotation);
            else
                instance = GameObject.Instantiate(go, position, rotation, parent.transform);



            if (instance.GetComponent<FduClusterView>() != null)
            {
                instance.GetComponent<FduClusterView>().ObjectID = viewId;
                FduSyncBaseIDManager.ReceiveIdFromMaster(viewId);
            }
            return instance;
        }
        //创建函数 para包含了所有创建该物体所必须的参数 包括位置信息、父节点信息、子节点信息等等
        public static GameObject createGameObjectWithClusterView(FduClusterViewManager.ClusterGameObjectCreatePara para)
        {
            GameObject go = FduClusterAssetManager.Instance.getGameObjectFromId(para.assetId);
            if (go == null) return null;

            GameObject parent = FduSupportClass.getGameObjectByPath(para.parentPath);
            GameObject instance;

            if (parent == null)
                instance = GameObject.Instantiate(go, para.position, para.rotation);
            else
                instance = GameObject.Instantiate(go, para.position, para.rotation, parent.transform);

            FduClusterView view = instance.GetComponent<FduClusterView>();
            if (view != null)
            {
                view.ObjectID = para.viewId;
                FduSyncBaseIDManager.ReceiveIdFromMaster(para.viewId);
            }

            var subViewList = instance.GetClusterView().getSubViews();
            int index = 0;
            foreach (FduClusterView subView in subViewList)
            {
                if (subView != null)
                {
                    subView.ObjectID = para.subViewId[index++];
                    FduSyncBaseIDManager.ReceiveIdFromMaster(subView.ViewId);
                }
                else
                {
                    Debug.LogError("Find Invalid sub view in one FduClusterView.View id :" + view.ViewId + " Object name:" + view.name + ". Please press the Refresh Button in Inspector");
                }
            }
            if (index != subViewList.Count)
                Debug.LogError("[FduClusterGameObjectCreator]Sub View Count Not matched!");

            return instance;
        }

    }
}

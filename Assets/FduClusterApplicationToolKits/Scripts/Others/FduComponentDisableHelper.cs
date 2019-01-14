/*
 * FduComponentDisableHelper
 * 简介：帮助开发者禁用掉从节点组件
 * disableType为0 则将enabled只为false
 * 为1则直接摧毁该组件
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduComponentDisableHelper : MonoBehaviour
    {

        [SerializeField]
        List<Behaviour> disableList = new List<Behaviour>();

        
        [SerializeField]
        byte disableType = 0;

        void Awake()
        {
#if CLUSTER_ENABLE
            if (ClusterHelper.Instance.Server != null)
                return;
            if (disableType == 0)
            {
                foreach (Behaviour mono in disableList)
                {
                    if (mono != null)
                        mono.enabled = false;
                }
            }
            else if (disableType == 1)
            {
                for (int i = 0; i < disableList.Count; ++i)
                {
                    Destroy(disableList[i]);
                }
            }
#else
        Destroy(this);
#endif
        }
    }
}

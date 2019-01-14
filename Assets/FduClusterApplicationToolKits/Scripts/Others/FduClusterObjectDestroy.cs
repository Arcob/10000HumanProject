/*
 * FduClusterObjectDestroy
 * 
 * 简介：如果是集群的版本 则需要摧毁的物体可以添加此脚本
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduClusterObjectDestroy : MonoBehaviour
    {

#if CLUSTER_ENABLE
        void Awake()
        {
            Destroy(this);
        }

#else
    void Awake()
    {
        Destroy(gameObject);
    }
#endif


    }
}

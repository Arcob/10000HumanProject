/*
 FduClusterManagerDontDestoryOnLoad
 * 
 * 简介：不让FduClusterEvent物体摧毁的脚本
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduClusterManagerDontDestoryOnLoad : MonoBehaviour
    {

        static bool isInstantiated = false;
        void Awake()
        {
#if !CLUSTER_ENABLE
        Destroy(gameObject);
#else
            if (!isInstantiated)
            {
                DontDestroyOnLoad(this);
                isInstantiated = true;
            }
#endif
        }

    }
}

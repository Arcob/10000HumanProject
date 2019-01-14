/*
 * FduMouseCursor
 * 
 * 简介：同步鼠标物体所用的组件
 * 同步的数据来自于StandaloneInputMoudleEx
 * 
 * 最后修改时间：2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class FduMouseCursor : MonoBehaviour
    {

        StandaloneInputModuleEx inputModelInstance;

        //获取位置类型 0为直接position赋值 其他值为raycast 取一定距离定位
        [SerializeField]
        byte positionType = 0;
        //raycast时 寻找对应点的距离
        [SerializeField]
        int rayDistance = 100;


        void Awake()
        {
#if !CLUSTER_ENABLE
        gameObject.SetActive(false);
        Destroy(this);
#endif
        }

        void Start()
        {
            var list = Resources.FindObjectsOfTypeAll<StandaloneInputModuleEx>();
            if (list.Length < 1)
            {
                Debug.LogError("[FduMouseCursor]Can not find StandaloneInputModuleEx component! Please add it to your event system.");
            }
            inputModelInstance = list[0];
        }

        void Update()
        {
            if (positionType == 0)
            {
                transform.position = inputModelInstance.getMousePosition();
            }
            else
            {
                transform.position = inputModelInstance.getRaycastFromEventCamera().GetPoint(rayDistance);
            }

        }
    }
}

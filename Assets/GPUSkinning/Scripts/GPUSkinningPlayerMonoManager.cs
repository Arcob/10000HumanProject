using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 仅用于GPUSkinningPlayerMono中
/// </summary>
public class GPUSkinningPlayerMonoManager
{
    //保存已注册的PlayerResources（每个模型对应一个PlayerResources）
    private List<GPUSkinningPlayerResources> items = new List<GPUSkinningPlayerResources>();

    //注册需要渲染的模型种类
    public void Register(GPUSkinningAnimation anim, Mesh mesh, Material originalMtrl, TextAsset textureRawData, GPUSkinningPlayerMono player, out GPUSkinningPlayerResources resources)
    {
        resources = null;

        if (anim == null || originalMtrl == null || textureRawData == null || player == null)
        {
            return;
        }

        // item完成初始化后赋值给resources
        GPUSkinningPlayerResources item = null;

        int numItems = items.Count;
        //查询该anim是否已注册，根据guid（唯一标识符）判断
        for(int i = 0; i < numItems; ++i)
        {
            if(items[i].anim.guid == anim.guid)
            {
                //找到已注册的Resources，赋值给item
                item = items[i];
                break;
            }
        }

        #region 未找到，初始化赋值
        if(item == null)
        {
            item = new GPUSkinningPlayerResources();
            //Debug.Log("new");
            items.Add(item);
        }

        if(item.anim == null)
        {
            item.anim = anim;
        }

        if(item.mesh == null)
        {
            item.mesh = mesh;
        }

        if(item.texture == null)
        {
            item.texture = GPUSkinningUtil.CreateTexture2D(textureRawData, anim);
        }

        item.InitMaterial(originalMtrl, HideFlags.None);

        #endregion

        //为player设置CullingBounds（在CullingGroup中）
        if (!item.players.Contains(player))
        {
            item.players.Add(player);
            // fzy delete:culling
            //item.AddCullingBounds();
        }

        resources = item;
    }

    public void Unregister(GPUSkinningPlayerMono player)
    {
        if(player == null)
        {
            return;
        }

        int numItems = items.Count;
        for(int i = 0; i < numItems; ++i)
        {
            int playerIndex = items[i].players.IndexOf(player);
            if(playerIndex != -1)
            {
                // player和player对应的CullingBounds的索引号相同
                items[i].players.RemoveAt(playerIndex);
                //fzy delete:
                //items[i].RemoveCullingBounds(playerIndex);
                // 如果使用这个Res的Player全部销毁，删除对应的Res
                if(items[i].players.Count == 0)
                {
                    items[i].Destroy();
                    items.RemoveAt(i);
                }
                break;
            }
        }
    }
}

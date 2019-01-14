/*
 * FduExtenison
 * 
 * 简介：拓展类
 * 对原有的类进行方法拓展 更加便捷
 * 
 * 最后修改时间： Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class FduExtenison
    {

        public static FduClusterView[] GetClusterViewsInChildren(this UnityEngine.GameObject go)
        {
            return go.GetComponentsInChildren<FduClusterView>(true) as FduClusterView[];
        }

        public static FduClusterView GetClusterView(this UnityEngine.GameObject go)
        {
            return go.GetComponent<FduClusterView>() as FduClusterView;
        }

        public static FduClusterView GetClusterView(this UnityEngine.MonoBehaviour mono)
        {
            return mono.GetComponent<FduClusterView>() as FduClusterView;
        }

        public static object Rpc(this UnityEngine.MonoBehaviour mono, string methodName, RpcTarget target, params object[] paras)
        {
            FduClusterView _viewInstance = mono.GetClusterView();
            if (_viewInstance == null)
            {
                Debug.LogError("[FduRPC]There is no cluster view component attach to this game object. Game obejct name:" + mono.gameObject.name);
                return null;
            }
            else
            {
                return _viewInstance.Rpc(methodName,target,paras);
            }
        }

        public static object Rpc(this FduObserverBase ob, string methodName, RpcTarget target, params object[] paras)
        {
            FduClusterView _viewInstance = ob.GetClusterView();
            if (_viewInstance == null)
            {
                Debug.LogError("[FduRPC]This observer is not registed to a cluster view yet. Game object name:" + ob.gameObject.name);
                return null;
            }
            else
            {
                return _viewInstance.Rpc(methodName, target, paras);
            }
        }


        public static Dictionary<TKey, TValue> Add_ignore<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key) == false)
                dict.Add(key, value);
            return dict;
        }
        public static Dictionary<TKey, TValue> Add_overlay<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            dict[key] = value;
            return dict;
        }
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }
    }
}

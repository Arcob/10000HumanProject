/*
 * FduClusterLevelLoader
 * 简介：集群系统场景读取类
 * 由于集群系统的特殊性，在读取新场景时需要替换掉原有的场景读取API
 * 原理：当调用场景读取函数时（主节点），主节点会发送读取场景的事件给所有其他从节点（这里的事件不是ClusterEvent)
 * 其他从节点开始读取场景，并且在同一帧 主节点也开始读取场景 再此之前 暂时停止数据的发送 并将timeout值设置到一个很大的值（防止场景读取过长而断开）
 * 由于帧同步 使得主从节点读取场景进度相同
 * 
 * 最后修改时间：2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
using System;

namespace FDUClusterAppToolKits
{
    public class FduClusterLevelLoader : SyncBase
    {

        public static FduClusterLevelLoader Instance;

        private static string curSceneName = "";
        static bool inited = false;
        void Awake()
        {
            ObjectID = FduSyncBaseIDManager.getLevelLoadManagerSyncId();
            curSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Instance = this;
        }
        protected override void Init()
        {
            inited = true;
        }
        protected override void OnClientInited(ObjectSyncSlave client_)
        {
            base.OnClientInited(client_);
            if (_client != null)
            {
                _client.RegisterEvent(new LevelLoadEvent());
            }
        }
        /// <summary>
        /// Start loading a new scene with a sceneName.Only can be called on Master node.
        /// </summary>
        /// <param name="sceneName"></param>
        public void ClusterLoadScene(string sceneName)
        {
            if (!loadScenePrepare(sceneName))
                return;

            StartCoroutine(serverLoadSceneCo(sceneName));
            Debug.Log("[FduClusterLevelLoader]Server end load scene at " + ClusterHelper.Instance.FrameCount);
        }
        /// <summary>
        /// Start loading a new scene with custom function.Only can be called on Master node.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="customLoadSceneFunc"></param>
        public void ClusterLoadScene(string sceneName, Action customLoadSceneFunc)
        {
            if (!loadScenePrepare(sceneName))
                return;

            StartCoroutine(serverLoadSceneCo(sceneName, customLoadSceneFunc));
            Debug.Log("[FduClusterLevelLoader]Server end load scene at " + ClusterHelper.Instance.FrameCount);
        }
        IEnumerator serverLoadSceneCo(string sceneName)
        {
            ObjectSyncMaster.Instance.SendingEnabled = false;//停止发送数据
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);//读取场景
            curSceneName = sceneName;//更新当前场景名
            FduSyncBaseIDManager.OnLevelLoaded();
            yield return 0;
            FduRpcManager.OnLevelLoaded();
            FduActiveSyncManager.OnLevelLoaded();
            ObjectSyncMaster.Instance.SendingEnabled = true;
        }
        IEnumerator serverLoadSceneCo(string sceneName, Action customLoadSceneFunc)
        {
            ObjectSyncMaster.Instance.SendingEnabled = false;
            customLoadSceneFunc();//执行自定义读取场景函数
            curSceneName = sceneName;
            FduSyncBaseIDManager.OnLevelLoaded();
            yield return 0;
            FduRpcManager.OnLevelLoaded();
            FduActiveSyncManager.OnLevelLoaded();
            ObjectSyncMaster.Instance.SendingEnabled = true;
        }
        //读取场景前的准备工作 包括变量判断 以及发送读取场景事件等 这里的事件是GameEvent 不是ClusterEvent
        bool loadScenePrepare(string sceneName)
        {
            if (sceneName == null)
            {
                Debug.LogError("[FduClusterLevelLoader]SceneName can not be null!");
                return false;
            }
            if (!inited)
            {
                Debug.LogError("[FduClusterLevelLoader]Level loader is not set up yet. (Level Loader is set up in the Start func.)");
                return false;
            }
            if (ClusterHelper.Instance.Client != null)
                return false;

            LevelLoadEvent e = new LevelLoadEvent();
            e.levelName = sceneName;

            _server.SendEvent(e);
            Debug.Log("[FduClusterLevelLoader]Server start load scene at " + ClusterHelper.Instance.FrameCount);

            return true;
        }
        //收到读取场景的事件 从节点开始读取场景
        public void _slaveStartLoadScene(string sceneName)
        {
            Debug.Log("[FduClusterLevelLoader]Slave start load scene at " + ClusterHelper.Instance.FrameCount);

            StartCoroutine(slaveLoadSceneCo(sceneName));

            Debug.Log("Slave end laod scene at " + ClusterHelper.Instance.FrameCount);
        }
        IEnumerator slaveLoadSceneCo(string sceneName)
        {
            var syncSlave = ClusterHelper.Instance.GetComponent<FDUIGC_SyncSlave>();
            syncSlave.SendMessage("BlockSwapSync", SendMessageOptions.DontRequireReceiver);
           
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            curSceneName = sceneName;
            FduSyncBaseIDManager.OnLevelLoaded();
            yield return 0;
            syncSlave.SendMessage("ResumeSwapSync", SendMessageOptions.DontRequireReceiver);
        }
        //判断场景是否读取完毕
        public bool isSceneLoaded()
        {
            return (curSceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded;
        }
        static public string getCurSceneName()
        {
            return curSceneName;
        }

    }
}

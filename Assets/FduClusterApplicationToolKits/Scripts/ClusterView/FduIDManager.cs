/*
 * FduSyncBaseIDManager
 * 简介：用于管理和分配SyncBaseId的管理类
 * 每一个SyncBase都需要有一个ObjectID viewId和ObjectID是一样的
 * 
 * 最后修改时间：Hayate 2017.07.08
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class FduSyncBaseIDManager
    {
        //存储所有ID是否已使用的标志位
        static BitArray _IDStates = new BitArray(FduGlobalConfig.MAX_VIEW_COUNT, false);

        static readonly int _allocateBase = 10; //前10个id预留给工具集本身使用

        //当前可分配的最小ID
        static int _minIndex = _allocateBase + 1;
        //是否需要分配场景物体ID
        static bool _allocateFlag = true;

        public static int getCommandManagerSyncId() { return 1; }

        public static int getActiveManagerSyncId() { return 2; }

        public static int getRpcManagerSyncId() { return 3; }

        public static int getLevelLoadManagerSyncId() { return 4; }

        public static int getClusterViewManagerSyncId() { return 5; }

        public static int getClusterInputManagerSyncId() { return 6; }

        public static int getClusterTimeManagerSyncId() { return 7; }

        public static int getClusterRandomManagerSyncId() { return 8; }


        public static int getInvalidSyncId() { return FduGlobalConfig.MAX_VIEW_COUNT; }

        //申请id 
        internal static int ApplyNextAvaliableId()
        {
            while (_minIndex < FduGlobalConfig.MAX_VIEW_COUNT && _IDStates[_minIndex]) { _minIndex++; }
            if (_minIndex < FduGlobalConfig.MAX_VIEW_COUNT)
            {
                _IDStates.Set(_minIndex, true);
                return _minIndex++;
            }
            else
            {
                Debug.LogError("[FduSyncBaseIDManager]Number of ClusterView or SyncBase reached the limit");
                return getInvalidSyncId();
            }
        }
        //回收id
        internal static void RetrieveId(int id)
        {
            if (id < FduGlobalConfig.MAX_VIEW_COUNT && _IDStates[id])
            {
                _IDStates.Set(id, false);
                _minIndex = _minIndex > id ? id : _minIndex;
            }
        }

        /// <summary>
        /// Check whether the manual id can be used.
        /// </summary>
        /// <param name="manualId"></param>
        /// <returns></returns>
        public static bool CheckManuallyIdAvailable(int manualId)
        {
            if (manualId > _allocateBase && manualId < getInvalidSyncId() && !_IDStates[manualId])
                return true;
            else
                return false;
        }
        /// <summary>
        /// Apply this manual id. This manual id can not be used by other cluster view until it is retrieved.
        /// </summary>
        /// <param name="manualId"></param>
        /// <returns>If this manualId is applied succefully.</returns>
        public static bool ApplyManuallyID(int manualId)
        {
            if (CheckManuallyIdAvailable(manualId))
            {
                _IDStates.Set(manualId,true);
                return true;
            }
            return false;
        }


        //主节点分配的id通知从节点
        internal static void ReceiveIdFromMaster(int id)
        {
            if (id < FduGlobalConfig.MAX_VIEW_COUNT)
            {
                if (!_IDStates.Get(id))
                {
                    _IDStates.Set(id, true);
                }
                else
                    Debug.LogError("[FduSyncBaseIDManager]Slave node have already allocate id " + id + " to other views.(Or not retrieved)");
            }
        }
        //测试用 打印所有ID信息
        public static void PrintStates()
        {
            string s = "Min Index:" + _minIndex + " ";
            for (int i = _allocateBase + 1; i < 100; ++i)
            {
                s += "No." + i.ToString() + " State " + _IDStates.Get(i).ToString();
            }
            Debug.Log(s);
        }
        //Executed on both Master node and slave node Independently
        //Called In Start of FduClusterViewManager and LevelLoadManager
        internal static void AllocateSceneObjectViewId()
        {
            Debug.Log("start allocate scene view id");
            Scene s = SceneManager.GetActiveScene();
            if (s.isLoaded)
            {
                Debug.Log("scene name " + s.name);
                var allGameObjects = s.GetRootGameObjects();
                for (int j = 0; j < allGameObjects.Length; j++)
                {
                    var go = allGameObjects[j];
                    foreach (FduClusterView view in (go.GetComponentsInChildren<FduClusterView>(true)))
                    {
                        view.ObjectID = FduSyncBaseIDManager.ApplyNextAvaliableId();
                        FduClusterViewManager.RegistToViewManager(view);
                    };
                }
                _allocateFlag = false;
            }
            else
            {
                Debug.LogWarning("This scene is not loaded yet");
            }
        }
        //判断是否需要分配场景物体ID
        public static bool needsAllocateSceneViewId()
        {
            return _allocateFlag;
        }

        internal static void OnLevelLoaded()
        {
            _allocateFlag = true;
        }
    }
}

/*
 * FduClusterCommandDispatcher
 * 简介：集群事件派发类 这是一个静态类
 * 当ClusterCommandManager每帧收到主节点的事件信息并解析完毕后，通知本类进行事件派发。
 * 派发类中维护了事件监听器的映射表，每一个事件名可以对应若干个事件监听器 事件监听器是一个Action
 * 从节点收到某一事件，将会触发所有该事件名所对应的事件监听器，并以事件实例ClusterCommand作为参数传入
 * 
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public static class FduClusterCommandDispatcher
    {
        public class ExecutorData
        {
            public uint finalIndex = 1;
            public Dictionary<uint, Action<ClusterCommand>> ActionMap = new Dictionary<uint, Action<ClusterCommand>>();
            public uint AddAction(Action<ClusterCommand> instance){
                ActionMap.Add(finalIndex++, instance);
                return finalIndex;
            }
            public bool RemoveAction(uint index)
            {
                if (!ActionMap.ContainsKey(index))
                    return false;
                else
                {
                    ActionMap.Remove(index);
                    return true;
                }
            }
        }

        //Cluster Command manager实例
        static ClusterCommandManager _clusterCommandMgr;

        //事件监听器映射表 键为事件名 值为监听器映射表 监听器映射表的键为该监听器的名字，值为该监听器对应的Action
        static Dictionary<string, ExecutorData> _CommandExecutorMap = new Dictionary<string, ExecutorData>();

        //从节点上由ClusterCommandManager每帧收到信息时调用，主节点上在序列化事件之后立刻调用
        static public void NotifyDispatch()
        {
            processDispatch();
        }
        static public void setClusterCommandManager(ClusterCommandManager mgr)
        {
            _clusterCommandMgr = mgr;
        }

        //获取所有事件实例
        static public List<ClusterCommand>.Enumerator getCommands()
        {
            if (_clusterCommandMgr != null)
                return _clusterCommandMgr.getUnprocessedCommands();
            else
                return new List<ClusterCommand>.Enumerator();
        }
        //处理派发过程 把所有的接收到的事件实例派发给其对应的监听器
        static void processDispatch()
        {
            List<ClusterCommand>.Enumerator CommandNumerator = _clusterCommandMgr.getUnprocessedCommands();
            while (CommandNumerator.MoveNext())
            {
                ClusterCommand _Command = CommandNumerator.Current;
                if (_CommandExecutorMap.ContainsKey(_Command.getCommandName()))
                {
                    Dictionary<uint, Action<ClusterCommand>>.Enumerator dicNumerator = _CommandExecutorMap[_Command.getCommandName()].ActionMap.GetEnumerator();
                    while (dicNumerator.MoveNext())
                    {
                        dicNumerator.Current.Value(_Command);
                    }
                }
            }
        }


        
        /// <summary>
        ///Send ClusterCommand with a cluster Command instance
        /// </summary>
        /// <param name="clusterCommand">Command Instance</param>
        /// <param name="multiCommandAllow">Whether multi Command instances with the same Command Name can be raised.Default value is true.</param>
        static public void SendClusterCommand(ClusterCommand clusterCommand, bool multiCommandAllow = true)
        {
            _clusterCommandMgr.addClusterCommand(clusterCommand, multiCommandAllow);
        }
        /// <summary>
        ///Send Cluster Command with Command parameters and Command name
        /// </summary>
        /// <param name="CommandName">Name of the Command name</param>
        /// <param name="paras">The first element of each Name-Value Pair must be string which is the name of the parameter，then follow the parameter value e.g. RaiseClusterCommand("CommandName","paraName1",1.0f,"paraName2",2.0f)</param>
        static public void SendClusterCommand(string CommandName, params object[] paras)
        {
            _clusterCommandMgr.addClusterCommand(ClusterCommand.create(CommandName, paras));
        }
        /// <summary>
        /// Send Cluster Command with Command parameters and Command name
        /// </summary>
        /// <param name="CommandName">Name of the Command name</param>
        /// <param name="multiCommandAllow">Whether multi Command instances with the same Command Name can be raised.Default value is true.</param>
        /// <param name="paras">The first element of each Name-Value Pair must be string which is the name of the parameter，then follow the parameter value e.g. RaiseClusterCommand("CommandName",true,"paraName1",1.0f,"paraName2",2.0f)</param>
        static public void SendClusterCommand(string CommandName, bool multiCommandAllow, params object[] paras)
        {
            _clusterCommandMgr.addClusterCommand(ClusterCommand.create(CommandName, paras), multiCommandAllow);
        }
        /// <summary>
        /// Regist Command Executor to dispatcher.
        /// </summary>
        /// <param name="CommandName">Name of the observing Command</param>
        /// <param name="ExecutorName">Name of this Executor</param>
        /// <param name="ExecutorCallback">Callback function. Called when the evnet raised.</param>
        /// <param name="exceptMaster">A flag to identify Whether the master node listen this Command.</param>
        static public uint AddCommandExecutor(string CommandName,Action<ClusterCommand> ExecutorCallback, bool exceptMaster = false)
        {
            uint result = 0;

            if (exceptMaster && FduSupportClass.isMaster)
                return 0;

            if (CommandName == null) { Debug.LogError("[CommandDispatcher]AddCommandExecutor:Command name can not be null"); return 0; }
            if (ExecutorCallback == null) { Debug.LogError("[CommandDispatcher]AddCommandExecutor:Executor call back can not be null"); return 0; }

            if (!_CommandExecutorMap.ContainsKey(CommandName))
            {
                ExecutorData _data = new ExecutorData();
                result = _data.AddAction(ExecutorCallback);
                _CommandExecutorMap.Add(CommandName, _data);
            }
            else
            {
                result = _CommandExecutorMap[CommandName].AddAction(ExecutorCallback);
            }
            return result;
        }

        /// <summary>
        /// Remove one specific Executor. Up to 1 Executor will be removed
        /// </summary>
        static public bool RemoveCommandExecutor(string CommandName, uint ExecutorId)
        {
            if (CommandName == null) { Debug.LogError("[CommandDispatcher]RemoveCommandExecutor:Command name can not be null"); return false; }
            if (_CommandExecutorMap.ContainsKey(CommandName))
            {
                bool result = _CommandExecutorMap[CommandName].RemoveAction(ExecutorId);
                if (_CommandExecutorMap[CommandName].ActionMap.Count == 0)
                {
                    _CommandExecutorMap.Remove(CommandName);
                }
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove all the Executors binded to a specific Command. More than 1 Executor may be removed.
        /// </summary>
        /// <param name="CommandName"></param>
        static public void RemoveAllExecutorsByCommandName(string CommandName)
        {
            if (CommandName == null) { Debug.LogError("[CommandDispatcher]RemoveAllExecutorsOfByCommandName:Command name can not be null"); return; }
            if (_CommandExecutorMap.ContainsKey(CommandName))
            {
                _CommandExecutorMap.Remove(CommandName);
            }
        }
        
        /// <summary>
        /// Called when level is loaded. called by cluster level loader
        /// </summary>
        static public void OnLevelLoaded()
        {
            if (_clusterCommandMgr.getWaitingListCount() > 0)
            {
                Debug.LogWarning("Any Command raised at the frame of scene Loading will be removed.");
            }
            _clusterCommandMgr.clearClusterCommands();
        }
        /// <summary>
        /// Get the number of all Command Executors.
        /// </summary>
        /// <returns></returns>
        static public int getExecutorCount()
        {
            int count = 0;
            foreach (KeyValuePair<string, ExecutorData> kvp in _CommandExecutorMap)
            {
                foreach (KeyValuePair<uint, Action<ClusterCommand>> kvp2 in kvp.Value.ActionMap)
                {
                    count++;
                }
            }
            return count;
        }
        /// <summary>
        /// Get all Command Executors instance.
        /// </summary>
        /// <returns></returns>
        static public Dictionary<string, ExecutorData>.Enumerator getExecutors()
        {
            return _CommandExecutorMap.GetEnumerator();
        }
    }
}


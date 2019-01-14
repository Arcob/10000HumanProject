/*
 * ClusterCommand
 * 简介：集群中可以由主节点派发至从节点的事件类 每个事件实例由事件名作为唯一标识
 * 事件中可以包含任意个可传送的数据（int，float，Vector3..）
 * ClusterCommand类本身对开发者不可见 开发者只需要用指定接口设置事件名以及事件参数
 * 最后修改时间：Hayate 2017.08.30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUClusterAppToolKits
{
    public class ClusterCommand
    {
        string _CommandName = "";

        public class genericData {

            public string keyTypeName = "";
            public string valueTypeName = "";
            public string containerTypeName = "";
            public ArrayList valueData = new ArrayList();
        }


        //参数映射表 键为参数名字 值为参数值
        Dictionary<string, object> _paraMap;

        Dictionary<string, genericData> _genericMap;

        public ClusterCommand(string CommandName)
        {
            if (CommandName == null)
            {
                Debug.LogError("[ClusterCommand]CommandName can not be null");
                return;
            }
            _CommandName = CommandName;
            _paraMap = new Dictionary<string, object>();
            _genericMap = new Dictionary<string, genericData>();
        }
        /// <summary>
        /// Set Command Name for this Command instance.
        /// </summary>
        /// <param name="CommandName">the Command name you want to set</param>
        public void setCommandName(string CommandName)
        {
            if (CommandName == null)
            {
                Debug.LogError("[ClusterCommand]CommandName can not be null");
                return;
            }
            _CommandName = CommandName;
        }
        /// <summary>
        /// Get the Command Name of this instance.
        /// </summary>
        /// <returns>Command name</returns>
        public string getCommandName()
        {
            return _CommandName;
        }
        /// <summary>
        /// Add one Parameter to this Command Instance.
        /// It only make sense on Master Node.
        /// When Slave node received a Command, you can use getParameter method to get the parameter Data.
        /// Notice that the parameter Must Be Transmittable.
        /// </summary>
        /// <param name="paraName">Name of this Parameter</param>
        /// <param name="parameter">Value of this Parameter</param>
        public void addParameter(string paraName, object parameter)
        {
            if (paraName == null || parameter == null)
            {
                Debug.LogError("[ClusterCommand]Parameter Name or Parameter of the Cluster Command " + _CommandName + " is null");
                return;
            }
            _paraMap.Add_overlay(paraName, parameter);
        }
        /// <summary>
        /// Add Multi-parameters to this Command Instance
        /// It Only make sense on Master Node
        /// When Slave node received a Command, you can use getParameter method to get the parameter Data
        /// Notice that the parameter Must Be Transmittable
        /// </summary>
        /// <param name="paras">The first element of each Name-Value Pair must be string which is the name of the parameter，then follow the parameter value e.g. addParameters("paraName1",1.0f,"paraName2",2.0f);</param>
        public void addParameters(params object[] paras)
        {
            if (paras == null)
                return;
            if (paras.Length % 2 != 0)
            {
                Debug.LogError("[ClusterCommand]Add parameters to Cluster Command " + _CommandName + " number of names and parameters mismatched!");
                return;
            }
            for (int i = 0; i < paras.Length; i += 2)
            {
                if ((paras[i] == null) || !(paras[i] is string))
                {
                    Debug.LogError("[ClusterCommand]Add parameters to Cluster Command " + _CommandName + " name must be a string!");
                    continue;
                }
                if (paras[i + 1] == null)
                {
                    Debug.LogError("[ClusterCommand]Add parameters to Cluster Command " + _CommandName + " parameter " + paras[i] + " is null!");
                    continue;
                }
                _paraMap.Add_overlay((string)paras[i], paras[i + 1]);
            }
        }
        /// <summary>
        /// Get the parameter value with the given parameter name
        /// </summary>
        /// <param name="parameterName">name of the parameter</param>
        /// <returns>parameter value</returns>
        public object getParameter(string parameterName)
        {
            if (parameterName == null || !_paraMap.ContainsKey(parameterName)) return null;
            return _paraMap[parameterName];
        }
        /// <summary>
        /// Whether the Command instance contains a parameter with a given name.
        /// </summary>
        /// <param name="key">prameter name</param>
        /// <returns></returns>
        public bool containsKey(string key)
        {
            return _paraMap.ContainsKey(key);
        }
        /// <summary>
        /// Whether the Command instance contains a parameter with a given value.
        /// </summary>
        /// <param name="value">parameter value</param>
        /// <returns></returns>
        public bool containsValue(object value)
        {
            return _paraMap.ContainsValue(value);
        }
        public bool tryGetValue(string key, out object value)
        {
            if (key == null)
            {
                Debug.LogError("[ClusterCommand]Key can not be null");
                value = null;
                return false;
            }
            return _paraMap.TryGetValue(key, out value);
        }
        /// <summary>
        /// Get All non-collections Parameters of this Command instance.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object>.Enumerator getMapEnumerator()
        {
            return _paraMap.GetEnumerator();
        }
        /// <summary>
        /// Get All Collections Parameters of this Command instance.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, genericData>.Enumerator getCollectionsMapEnumerator()
        {
            return _genericMap.GetEnumerator();
        }

        /// <summary>
        /// Get the number of parameters contained in this Command instance.
        /// </summary>
        /// <returns></returns>
        public int getParameterCount()
        {
            return _paraMap.Count + _genericMap.Count;
        }

        public void Send(bool multiCommandAllow = true)
        {
            FduClusterCommandDispatcher.SendClusterCommand(this,multiCommandAllow);
        }

        #region Set&Get Generic Method
        void typeCheck(System.Type type)
        {
            if (!FduSupportClass.isSendableGenericType(type))
            {
                throw new InvalidSendableDataException("[FduClusterCommand]Data must be a struct or string.");
            }
        }
        /// <summary>
        /// Add a dictionary to this Command.You can get This Data with getDic in slave node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Name of this dictionary</param>
        /// <param name="dic">Dictionary Instance</param>
        public void addDic<K, V>(string name, Dictionary<K, V> dic)
        {
            if (name == null) return;
            typeCheck(typeof(K));
            typeCheck(typeof(V));

            genericData _data = new genericData();
            _data.keyTypeName = typeof(K).AssemblyQualifiedName;
            _data.valueTypeName = typeof(V).AssemblyQualifiedName;

            Debug.Log("type1:" + _data.keyTypeName + " type2:" + _data.valueTypeName);

            _data.containerTypeName = "dic";
            var enu = dic.GetEnumerator();
            while (enu.MoveNext())
            {
                _data.valueData.Add(enu.Current.Key);
                _data.valueData.Add(enu.Current.Value);
            }
            _genericMap.Add_overlay(name, _data);
        }



        public void addQueue<T>(string name,Queue<T> que)
        {
            if (name == null) return;
            typeCheck(typeof(T));

            genericData _data = new genericData();
            _data.valueTypeName = typeof(T).AssemblyQualifiedName;
            _data.containerTypeName = "queue";
            var enu = que.GetEnumerator();
            while (enu.MoveNext())
            {
                _data.valueData.Add(enu.Current);
            }
            _genericMap.Add_overlay(name, _data);
        }
        public void addList<T>(string name, List<T> list)
        {
            if (name == null) return;
            typeCheck(typeof(T));

            genericData _data = new genericData();
            _data.valueTypeName = typeof(T).AssemblyQualifiedName;
            _data.containerTypeName = "list";
            var enu = list.GetEnumerator();
            while (enu.MoveNext())
            {
                _data.valueData.Add(enu.Current);
            }
            _genericMap.Add_overlay(name, _data);
        }
        public void addStack<T>(string name, Stack<T> stack)
        {
            if (name == null) return;
            typeCheck(typeof(T));

            genericData _data = new genericData();
            _data.valueTypeName = typeof(T).AssemblyQualifiedName;
            _data.containerTypeName = "stack";
            var enu = stack.GetEnumerator();
            while (enu.MoveNext())
            {
                _data.valueData.Add(enu.Current);
            }
            _genericMap.Add_overlay(name, _data);
        }
        
        public Dictionary<K, V> getDic<K,V>(string name)
        {
            typeCheck(typeof(K));
            typeCheck(typeof(V));
            Dictionary<K,V> result = new Dictionary<K, V>();
            if (name == null)
                return result;
            genericData _data;
            if (_genericMap.TryGetValue(name, out _data))
            {
                try
                {
                    for (int i = 0; i < _data.valueData.Count; i += 2)
                    {
                        result.Add((K)_data.valueData[i], (V)_data.valueData[i + 1]);
                    }
                }
                catch (System.InvalidCastException) { Debug.LogError("InvalidCastException occurs in Cluster Command. CommandName:" + _CommandName + " Generic Container Name:" + name +" Key Type: " + typeof(K).FullName +" Value Type:" + typeof(V).FullName); }
            }
            return result;
        }


        public Queue<T> getQueue<T>(string name)
        {
            typeCheck(typeof(T));
            Queue<T> result = new Queue<T>();
            if (name == null)
                return result;
            genericData _data;
            if (_genericMap.TryGetValue(name, out _data))
            {
                try
                {
                    for (int i = 0; i < _data.valueData.Count; ++i)
                    {
                        result.Enqueue((T)_data.valueData[i]);
                    }
                }
                catch (System.InvalidCastException) { Debug.LogError("InvalidCastException occurs in Cluster Command. CommandName:" + _CommandName + " Generic Container Name:" + name + " Value Type:" + typeof(T).FullName); }
            }
            return result;
        }
        public Stack<T> getStack<T>(string name)
        {
            typeCheck(typeof(T));
            Stack<T> result = new Stack<T>();
            if (name == null)
                return result;
            genericData _data;
            
            if (_genericMap.TryGetValue(name, out _data))
            {
                try
                {
                    for (int i = 0; i < _data.valueData.Count; ++i)
                    {
                        result.Push((T)_data.valueData[i]);
                    }
                }
                catch (System.InvalidCastException) { Debug.LogError("InvalidCastException occurs in Cluster Command. CommandName:" + _CommandName + " Generic Container Name:" + name + " Value Type:" + typeof(T).FullName); }
            }
            return result;
        }
        public List<T> getList<T>(string name)
        {
            typeCheck(typeof(T));
            List<T> result = new List<T>();
            if (name == null)
                return result;
            genericData _data;
            if (_genericMap.TryGetValue(name, out _data))
            {
                try
                {
                    for (int i = 0; i < _data.valueData.Count; ++i)
                    {
                        result.Add((T)_data.valueData[i]);
                    }
                }
                catch (System.InvalidCastException) { Debug.LogError("InvalidCastException occurs in Cluster Command. CommandName:" + _CommandName + " Generic Container Name:" + name + " Value Type:" + typeof(T).FullName); }
            }
            return result;
        }

        #endregion

        #region Serialize&Deserialize Method
        internal void SerializeGenericData()
        {
            var enu = _genericMap.GetEnumerator();
            BufferedNetworkUtilsServer.SendInt(_genericMap.Count);
            while (enu.MoveNext())
            {
                BufferedNetworkUtilsServer.SendString(enu.Current.Key);
                genericData _data = enu.Current.Value;
                BufferedNetworkUtilsServer.SendString(_data.keyTypeName);
                BufferedNetworkUtilsServer.SendString(_data.valueTypeName);
                BufferedNetworkUtilsServer.SendString(_data.containerTypeName);
                int count = _data.valueData.Count;
                BufferedNetworkUtilsServer.SendInt(count);

                bool isStringK = false; bool isStringV = false;
                System.Type typeK = System.Type.GetType(_data.keyTypeName);
                System.Type typeV = System.Type.GetType(_data.valueTypeName);

                if (typeV !=null && typeV.Equals(typeof(string)))
                    isStringV = true;
                if (typeK!=null && typeK.Equals(typeof(string)))
                    isStringK = true;

                if (_data.containerTypeName == "dic")
                {
                    for (int i = 0; i < _data.valueData.Count; i = i + 2)
                    {
                        if (isStringK)
                            BufferedNetworkUtilsServer.SendString((string)_data.valueData[i]);
                        else if (typeK.IsEnum)
                            BufferedNetworkUtilsServer.SendInt(System.Convert.ToInt32(_data.valueData[i]));
                        else
                            BufferedNetworkUtilsServer.SendStruct(_data.valueData[i]);

                        if (isStringV)
                            BufferedNetworkUtilsServer.SendString((string)_data.valueData[i + 1]);
                        else if (typeV.IsEnum)
                            BufferedNetworkUtilsServer.SendInt(System.Convert.ToInt32(_data.valueData[i+1]));
                        else
                            BufferedNetworkUtilsServer.SendStruct(_data.valueData[i + 1]);
                    }

                } else if ( _data.containerTypeName == "queue" || _data.containerTypeName == "list" || _data.containerTypeName == "stack")
                {
                    for (int i = 0; i < _data.valueData.Count; ++i)
                    {
                        if (isStringV)
                            BufferedNetworkUtilsServer.SendString((string)_data.valueData[i]);
                        else if (typeV.IsEnum)
                            BufferedNetworkUtilsServer.SendInt(System.Convert.ToInt32(_data.valueData[i]));
                        else
                            BufferedNetworkUtilsServer.SendStruct(_data.valueData[i]);
                    }
                }
            }
        }
        internal void SerializeParameterData()
        {
            Dictionary<string, object>.Enumerator enumerator = _paraMap.GetEnumerator();
            BufferedNetworkUtilsServer.SendString(_CommandName);   //Name of Command
            BufferedNetworkUtilsServer.SendInt(_paraMap.Count); //Count of Command parameters
            while (enumerator.MoveNext())
            {
                BufferedNetworkUtilsServer.SendString(enumerator.Current.Key); //parameter name
                FduSupportClass.serializeOneParameter(enumerator.Current.Value);//parameter value
            }
        }
        internal void DeserializeParameterData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            _CommandName = BufferedNetworkUtilsClient.ReadString(ref state); //Name of Command
            int parameterCount = BufferedNetworkUtilsClient.ReadInt(ref state);//Count of Command parameters
            for (int j = 0; j < parameterCount; ++j)
            {
                string paraName = BufferedNetworkUtilsClient.ReadString(ref state);//Name of parameter
                object para = FduSupportClass.deserializeOneParameter(ref state);//Value of parameter
                addParameter(paraName, para);
            }
        }
        internal void DeserializeGenericData(ref NetworkState.NETWORK_STATE_TYPE state)
        {
            int genCount = BufferedNetworkUtilsClient.ReadInt(ref state);
            for(int i = 0; i < genCount; ++i)
            {
                string genName = BufferedNetworkUtilsClient.ReadString(ref state);
                string keyTypeName = BufferedNetworkUtilsClient.ReadString(ref state);
                string valueTypeName = BufferedNetworkUtilsClient.ReadString(ref state);
                string containerTypeName = BufferedNetworkUtilsClient.ReadString(ref state);
                System.Type valueType = System.Type.GetType(valueTypeName);
                System.Type keyType = System.Type.GetType(keyTypeName);
                genericData _data = new genericData();
                _data.valueTypeName = valueTypeName;
                _data.containerTypeName = containerTypeName;
                int valueCount = BufferedNetworkUtilsClient.ReadInt(ref state);

                bool isStringK = false; bool isStringV = false;
                
                if (valueType!=null && valueType.Equals(typeof(string)))
                    isStringV = true;

                if (keyType!=null && keyType.Equals(typeof(string)))
                    isStringK = true;

                if (_data.containerTypeName == "dic")
                {
                    for (int j = 0; j < valueCount; j = j + 2)
                    {
                        if (isStringK)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadString(ref state));
                        else if (keyType.IsEnum)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadInt(ref state));
                        else
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadStruct(keyType, ref state));

                        if (isStringV)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadString(ref state));
                        else if (valueType.IsEnum)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadInt(ref state));
                        else
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadStruct(valueType, ref state));
                    }
                }
                else if (_data.containerTypeName == "queue" || _data.containerTypeName == "list" || _data.containerTypeName == "stack")
                {
                    for (int j = 0; j < valueCount; ++j)
                    {
                        if (isStringV)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadString(ref state));
                        else if (valueType.IsEnum)
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadInt(ref state));
                        else
                            _data.valueData.Add(BufferedNetworkUtilsClient.ReadStruct(valueType, ref state));
                    }
                }
                _genericMap.Add(genName, _data);
            }
        }
        #endregion


        static public ClusterCommand create(string CommandName)
        {
            ClusterCommand newCommand = new ClusterCommand(CommandName);
            return newCommand;
        }
        static public ClusterCommand create(string CommandName, params object[] parameters)
        {
            ClusterCommand newCommand = new ClusterCommand(CommandName);
            newCommand.addParameters(parameters);
            return newCommand;
        }
        //用于Debug工具中 游览事件信息 为该事件创建快照并保存起来
        static public ClusterCommand createSnapShot(ClusterCommand e)
        {
            if (e != null)
            {
                ClusterCommand newCommand = new ClusterCommand(e.getCommandName());
                var enu = e._paraMap.GetEnumerator();
                while (enu.MoveNext())
                {
                    newCommand._paraMap.Add(enu.Current.Key, enu.Current.Value.ToString());
                }

                var collEnu = e._genericMap.GetEnumerator();
                while (collEnu.MoveNext())
                {
                    var ins = new genericData();
                    ins.containerTypeName = collEnu.Current.Value.containerTypeName;
                    ins.keyTypeName = collEnu.Current.Value.keyTypeName;
                    ins.valueTypeName = collEnu.Current.Value.valueTypeName;
                    newCommand._genericMap.Add(collEnu.Current.Key, ins);
                }
                return newCommand;
            }
            return null;
        }

    }
}



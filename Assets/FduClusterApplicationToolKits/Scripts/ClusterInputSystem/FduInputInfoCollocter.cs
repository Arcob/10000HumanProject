/*
 * FduUnityInputCollector Unity输入信息收集器
 * 
 * 本类负责通过Input获取各种unity输入信息 然后赋值给FduClusterInputMgr 令其传输到其他节点
 * 
 * 最后修改时间： Hayate 2017.9.11
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;

namespace FDUClusterAppToolKits
{
    public class FduUnityInputCollector
    {

        HashSet<KeyCode> keyboardNames = new HashSet<KeyCode>();

        HashSet<int> mouseNames = new HashSet<int>();

        HashSet<string> axisNames = new HashSet<string>();

        HashSet<string> buttonNames = new HashSet<string>();

        HashSet<string> propertyNames = new HashSet<string>();

        public void refreshInputData()
        {
            var enu = keyboardNames.GetEnumerator();
            while (enu.MoveNext())
            {
                bool newValue;
                KeyCode code = enu.Current;
                newValue = Input.GetKey(code);
                FduClusterInputMgr.SetKey(code, newValue);
            }

            var mouEnu = mouseNames.GetEnumerator();
            while (mouEnu.MoveNext())
            {
                bool newValue;
                newValue = Input.GetMouseButton(mouEnu.Current);
                FduClusterInputMgr.SetMouse(mouEnu.Current,newValue);
            }

            var butEnu = buttonNames.GetEnumerator();
            while (butEnu.MoveNext())
            {
                bool newValue;
                newValue = Input.GetButton(butEnu.Current);
                FduClusterInputMgr.SetButton(butEnu.Current, newValue);
            }


            var axisEnu = axisNames.GetEnumerator();
            while (axisEnu.MoveNext())
            {
                float newVlaue;
                newVlaue = Input.GetAxis(axisEnu.Current);
                FduClusterInputMgr.SetAxis(axisEnu.Current, newVlaue);
            }

            refreshPropertyData();
        }

        public void addKeyboardName(KeyCode code)
        {
            keyboardNames.Add(code);
        }

        public void addMouseName(int num)
        {
            mouseNames.Add(num);
        }

        public void addAxisName(string name)
        {
            if (name == null) return;
            try
            {
               float value = Input.GetAxis(name);
                axisNames.Add(name);
            }
            catch (System.ArgumentException)//如果出现错误 即意味着这个轴的信息并不是unity Input manager中的信息，所以本类不会对该输入进行更新
            {

            }
        }
        public void addButtonName(string name)
        {
            if (name == null) return;
            try
            {
                bool value = Input.GetButton(name);
                buttonNames.Add(name);
            }
            catch (System.ArgumentException)//如果出现错误 即意味着这个轴的信息并不是unity Input manager中的信息，所以本类不会对该输入进行更新
            {

            }
        }

        public void addInputPropertyName(string name)
        {
            if (name == null) return;
            propertyNames.Add(name);
        }

        void refreshPropertyData()
        {
            var enu = propertyNames.GetEnumerator();
            while (enu.MoveNext())
            {
                switch (enu.Current)
                {
                    case "UInput_anyKey":
                        FduClusterInputMgr.SetButton(enu.Current, Input.anyKey);
                        break;
                    case "UInput_anyKeyDown":
                        FduClusterInputMgr.SetButton(enu.Current, Input.anyKeyDown);
                        break;
                    case "UInput_mousePosition":
                        FduClusterInputMgr.SetPosition(enu.Current, Input.mousePosition);
                        break;
                    case "UInput_mouseScrollDelta":
                        FduClusterInputMgr.SetPosition(enu.Current, Input.mouseScrollDelta);
                        break;
                    case "UInput_scaledMousePosition":
                        FduClusterInputMgr.SetPosition(enu.Current, new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height,Input.mousePosition.z));
                        break;
                }
            }
        }
    }
}

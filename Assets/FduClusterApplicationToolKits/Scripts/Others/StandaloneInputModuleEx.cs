/*
 * StandaloneInputModuleEx
 * 
 * 简介：继承自StandaloneInputModule的类 代替原来的StandaloneInputModule
 * 可以传送鼠标的位置和点击数据
 */

#define FIX_DUP_RAYCAST

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

using FDUObjectSync;
using FDUClusterAppToolKits;
namespace FDUObjectSync
{
    public enum InputButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    public enum FramePressState
    {
        Pressed,
        Released,
        PressedAndReleased,
        NotChanged
    }

    public class ButtonState
    {
        private InputButton _button;
        public InputButton button
        {
            get { return _button; }
            set { _button = value; }
        }

        private Vector2 _mousePosition;
        public Vector2 position
        {
            get { return _mousePosition; }
            set { _mousePosition = value; }
        }

        private Vector2 _scrollDelta;
        public Vector2 scrollDelta
        {
            get { return _scrollDelta; }
            set { _scrollDelta = value; }
        }

        private Vector2 _delta;
        public Vector2 delta
        {
            get { return _delta; }
            set { _delta = value; }
        }

        private FramePressState _buttonState;
        public FramePressState buttonState
        {
            get { return _buttonState; }
            set { _buttonState = value; }
        }
    }

    public class MouseState
    {
        private List<ButtonState> _buttonStateList;
        public List<ButtonState> buttonStateList
        {
            get
            {
                return _buttonStateList ?? (_buttonStateList = new List<ButtonState>(){
                            new ButtonState(){
                                button = InputButton.Left,
                            },new ButtonState(){
                                button = InputButton.Right,
                            },new ButtonState(){
                                button = InputButton.Middle,
                            },
                        });
            }
            set { _buttonStateList = value; }
        }
    }
}

public class StandaloneInputModuleEx : StandaloneInputModule, ISerializable
{

    #region ObjectSync Fields

    protected int _objectId;

    protected ObjectSyncMaster _server;
    protected ObjectSyncSlave _client;

    protected FDUObjectSync.MouseState _mouseState;

    public Camera eventCamera;

    
    //2017.3.3 hayate add start
    private Vector3 _modifyPosBuffer;
    private FramePressState _modifyButtonStateBuffer = FramePressState.NotChanged;
    private bool _bufferFlushed = true;

    [SerializeField]
    bool isSendingPressState = true;
    [SerializeField]
    bool isSendingPositionState = true;

    Vector3 mousePositionBuff = Vector3.zero;
    //2017.3.3 hayate add end
    #endregion

    #region MonoBehaviors

    protected override void Awake()
    {
#if !CLUSTER_ENABLE
        GetComponent<StandaloneInputModule>().enabled = true;
        GetComponent<StandaloneInputModuleEx>().enabled = true;
        //this.enabled = false;
        //Destroy(this);
#else
        GetComponent<StandaloneInputModule>().enabled = false;
        this.enabled = true;
#endif
    }

    // Use this for initialization
    protected override void Start()
    {

#if CLUSTER_ENABLE

        _objectId = 0;

        ClusterHelper.Instance.ServerInited += OnServerInited;
        OnServerInited(ClusterHelper.Instance.Server);

        ClusterHelper.Instance.ClientInited += OnClientInited;
        OnClientInited(ClusterHelper.Instance.Client);

        Init();
#endif
    }

    #endregion

    #region Methods

    protected virtual void Init() { }

    protected virtual void OnServerInited(ObjectSyncMaster server_)
    {
        _server = server_;
    }

    protected virtual void OnClientInited(ObjectSyncSlave client_)
    {
        _client = client_;

        if (_client != null)
        {
            _client.RegisterObj(new ObjListEntry()
            {
                objId = _objectId,
                serializeable = this,
            });
        }
    }

    #endregion

    #region Serialization

    public virtual NetworkState.NETWORK_STATE_TYPE Deserialize()
    {
        var state = NetworkState.NETWORK_STATE_TYPE.SUCCESS;
        _mouseState = _mouseState ?? new FDUObjectSync.MouseState();
        _mouseState.buttonStateList.ForEach(buttonState_ =>
        {
            buttonState_.button = (InputButton)BufferedNetworkUtilsClient.ReadInt(ref state);
            buttonState_.position = BufferedNetworkUtilsClient.ReadVector2(ref state);
            buttonState_.delta = BufferedNetworkUtilsClient.ReadVector2(ref state);
            buttonState_.scrollDelta = BufferedNetworkUtilsClient.ReadVector2(ref state);
            buttonState_.buttonState = (FramePressState)BufferedNetworkUtilsClient.ReadInt(ref state);
        });

        return state;
    }

    public virtual void Serialize()
    {
        if (_mouseState != null)
        {
            _mouseState.buttonStateList.ForEach(buttonState_ =>
            {
                BufferedNetworkUtilsServer.SendInt((int)buttonState_.button);
                BufferedNetworkUtilsServer.SendVector2(buttonState_.position);
                BufferedNetworkUtilsServer.SendVector2(buttonState_.delta);
                BufferedNetworkUtilsServer.SendVector2(buttonState_.scrollDelta);
                BufferedNetworkUtilsServer.SendInt((int)buttonState_.buttonState);
            });
        }
    }

    #endregion

    #region InputModule stuffs
    public override void Process()
    {
        bool usedEvent = SendUpdateEventToSelectedObject();

        if (eventSystem.sendNavigationEvents)
        {
            if (!usedEvent)
                usedEvent |= SendMoveEventToSelectedObject();

            if (!usedEvent)
                SendSubmitEventToSelectedObject();
        }

        ProcessMouseEventEx();
    }

    private void ProcessMouseEventEx()
    {
        ProcessMouseEventEx(0);
    }

    private void CopyFromTo(FDUObjectSync.ButtonState from_, PointerInputModule.ButtonState to_,bool copyPosition = true,bool copyState = true)
    {
        var buttonData = to_.eventData.buttonData;
        buttonData.position = from_.position;
        buttonData.delta = from_.delta;
        buttonData.scrollDelta = from_.scrollDelta;

        if(copyState)
            to_.eventData.buttonState = (PointerEventData.FramePressState)from_.buttonState;

        if (copyPosition)
        {
            buttonData.position = new Vector2(
            buttonData.position.x * Screen.width,
            buttonData.position.y * Screen.height);
        }

        buttonData.delta = new Vector2(
            buttonData.delta.x * Screen.width,
            buttonData.delta.y * Screen.height);

        buttonData.scrollDelta = new Vector2(
            buttonData.scrollDelta.x * Screen.width,
            buttonData.scrollDelta.y * Screen.height);
    }

    void GetClientMouseState(MouseState mouseData)
    {
        if (_client != null && ClusterHelper.Instance.UseInputEventPhase)
        {
            //Debug.Log("UpdateInputEvent");
            ClusterHelper.Instance.UpdateInputEvent();
        }

        if (_client != null && _mouseState != null)
        {
            CopyFromTo(_mouseState.buttonStateList[0], mouseData.GetButtonState(PointerEventData.InputButton.Left),isSendingPositionState,isSendingPressState);
            CopyFromTo(_mouseState.buttonStateList[1], mouseData.GetButtonState(PointerEventData.InputButton.Right), isSendingPositionState, isSendingPressState);
            CopyFromTo(_mouseState.buttonStateList[2], mouseData.GetButtonState(PointerEventData.InputButton.Middle), isSendingPositionState, isSendingPressState);

#if FIX_DUP_RAYCAST
            ObjectSyncProfiler.BeginSample("RaycastAll");
            RaycastAll(mouseData);
            ObjectSyncProfiler.EndSample("RaycastAll", true);

#else
            var leftData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData;
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

            var rightData = mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData;
            var middleData = mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData;
            rightData.pointerCurrentRaycast = raycast;
            middleData.pointerCurrentRaycast = raycast;
#endif
        }
    }

    void SendServerMouseState(MouseState mouseData)
    {
        if (_server != null)
        {
            _mouseState = _mouseState ?? new FDUObjectSync.MouseState();

            _mouseState.buttonStateList.ForEach(buttonState_ =>
            {
                var button = mouseData.GetButtonState((PointerEventData.InputButton)buttonState_.button);
                var buttonData = button.eventData.buttonData;
                var buttonState = button.eventData.buttonState;

                buttonState_.position = buttonData.position;
                buttonState_.delta = buttonData.delta;
                buttonState_.scrollDelta = buttonData.scrollDelta;
                buttonState_.buttonState = (FramePressState)buttonState;

                // normalize
                buttonState_.position = new Vector2(
                    buttonState_.position.x / Screen.width,
                    buttonState_.position.y / Screen.height);

                buttonState_.delta = new Vector2(
                    buttonState_.delta.x / Screen.width,
                    buttonState_.delta.y / Screen.height);

                buttonState_.scrollDelta = new Vector2(
                    buttonState_.scrollDelta.x / Screen.width,
                    buttonState_.scrollDelta.y / Screen.height);
            });
            _server.SendState(_objectId, this);
            _server.FlushBuffer();
            if (ClusterHelper.Instance.UseInputEventPhase)
            {
                //Debug.Log("SendEofInputEvent");
                _server.SendEofInputEvent();
            }
        }
    }

    //2017.3.3 Hayate Add 通过输入设备模拟鼠标
    public void setMouseState(Vector3 pos, FramePressState state)
    {
        _modifyPosBuffer = pos;
        _modifyButtonStateBuffer = state;
        _bufferFlushed = false;
    }
    private void processModifyFromOuter(PointerInputModule.MouseState mouseData)
    {
        if (!_bufferFlushed)
        {
            mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData.position = _modifyPosBuffer;
            mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonState = (PointerEventData.FramePressState)_modifyButtonStateBuffer;
            _bufferFlushed = true;
        }
    }

    public bool PressEveryFrame = false;
    public float PressDuration = 1.0f;
    private float _lastTimePressed = 0;
    private int _pressCount = 0;
    private int _frameCount = 0;

    private void TriggerPress(MouseState mouseData_)
    {
        var buttonData = mouseData_.GetButtonState(PointerEventData.InputButton.Left).eventData;
        buttonData.buttonState = PointerEventData.FramePressState.PressedAndReleased;
        _pressCount++;
    }

    void OnApplicationQuit()
    {
#if !CLUSTER_ENABLE
        return;
#endif
        var info = "StandardInputModule: ";
        info += "_pressCount: " + _pressCount;
        info += "_frameCount: " + _frameCount;        
        ObjectSyncProfiler.Log(info, ObjectSyncProfiler.HighLogLevel);
    }

    private void Simulate(MouseState mouseData_)
    {
        if (PressEveryFrame)
        {
            TriggerPress(mouseData_);
        }
        else
        {
            if (Time.realtimeSinceStartup - _lastTimePressed > PressDuration)
            {
                TriggerPress(mouseData_);
                _lastTimePressed = Time.realtimeSinceStartup;
            }
        }
        _frameCount++;
    }

#if FIX_DUP_RAYCAST
    private MouseState _tempMouseState = new MouseState();
    protected override MouseState GetMousePointerEventData(int id)
    {
        // Populate the left button...
        PointerEventData leftData;
        var created = GetPointerData(kMouseLeftId, out leftData, true);

        leftData.Reset();

        if (created)
            leftData.position = Input.mousePosition;

        Vector2 pos = Input.mousePosition;
        leftData.delta = pos - leftData.position;
        leftData.position = pos;
        leftData.scrollDelta = Input.mouseScrollDelta;
        leftData.button = PointerEventData.InputButton.Left;
        //eventSystem.RaycastAll(leftData, m_RaycastResultCache);
        //var raycast = FindFirstRaycast(m_RaycastResultCache);
        //leftData.pointerCurrentRaycast = raycast;
        //m_RaycastResultCache.Clear();

        // copy the apropriate data into right and middle slots
        PointerEventData rightData;
        GetPointerData(kMouseRightId, out rightData, true);
        CopyFromTo(leftData, rightData);
        rightData.button = PointerEventData.InputButton.Right;

        PointerEventData middleData;
        GetPointerData(kMouseMiddleId, out middleData, true);
        CopyFromTo(leftData, middleData);
        middleData.button = PointerEventData.InputButton.Middle;

        _tempMouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
        _tempMouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
        _tempMouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

        return _tempMouseState;
    }
#endif

    private void RaycastAll(PointerInputModule.MouseState mouseData)
    {
        var leftData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData;
        eventSystem.RaycastAll(leftData, m_RaycastResultCache);
        var raycast = FindFirstRaycast(m_RaycastResultCache);
        leftData.pointerCurrentRaycast = raycast;
        m_RaycastResultCache.Clear();

        var rightData = mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData;
        var middleData = mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData;
        rightData.pointerCurrentRaycast = raycast;
        middleData.pointerCurrentRaycast = raycast;
    }

    private void ProcessMouseEventEx(int id_)
    {
        ObjectSyncProfiler.BeginSample("ProcessMouseEventEx");
#if FIX_DUP_RAYCAST
        PointerInputModule.MouseState mouseData = null;
        if (_client != null)
        {
            ObjectSyncProfiler.BeginSample("GetMousePointerEventData");
            mouseData = GetMousePointerEventData(id_);
            ObjectSyncProfiler.EndSample("GetMousePointerEventData", true);

            ObjectSyncProfiler.BeginSample("GetClientMouseState");
            GetClientMouseState(mouseData);
            ObjectSyncProfiler.EndSample("GetClientMouseState", true);
            mousePositionBuff = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData.position;
        }

        if (_server != null)
        {
            ObjectSyncProfiler.BeginSample("GetMousePointerEventData");
            mouseData = GetMousePointerEventData(id_);
            ObjectSyncProfiler.EndSample("GetMousePointerEventData", true);

            //2017.3.3 hayate add start
            processModifyFromOuter(mouseData);
            //2017.3.3 hayate add end
            mousePositionBuff = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData.position;

            ObjectSyncProfiler.BeginSample("SendServerMouseState");
            SendServerMouseState(mouseData);
            ObjectSyncProfiler.EndSample("SendServerMouseState", true);

            ObjectSyncProfiler.BeginSample("RaycastAll");
            RaycastAll(mouseData);
            ObjectSyncProfiler.EndSample("RaycastAll", true);
        }
#else
        var mouseData = GetMousePointerEventData(id_);

        Simulate(mouseData);

        ObjectSyncProfiler.BeginSample("GetClientMouseState");
        GetClientMouseState(mouseData);
        ObjectSyncProfiler.EndSample("GetClientMouseState", true);

        ObjectSyncProfiler.BeginSample("SendServerMouseState");
        SendServerMouseState(mouseData);
        ObjectSyncProfiler.EndSample("SendServerMouseState", true);
#endif

#if FIX_DUP_RAYCAST
        if (mouseData != null)
        {
#endif
            ObjectSyncProfiler.BeginSample("ProcessMouseEvent");
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
            ObjectSyncProfiler.EndSample("ProcessMouseEvent", true);

#if FIX_DUP_RAYCAST
        }
#endif
        ObjectSyncProfiler.EndSample("ProcessMouseEventEx", true);
    }

    #endregion

    #region Public Func

    public Vector3 getMousePosition()
    {
        return mousePositionBuff;
    }

    public Ray getRaycastFromEventCamera()
    {
        if (eventCamera == null)
        {
            Debug.LogError("[StandaloneInputModuleEx]Event camera is not set up yet");
            return new Ray();
        }else
            return eventCamera.ScreenPointToRay(mousePositionBuff);
    }

    #endregion
}

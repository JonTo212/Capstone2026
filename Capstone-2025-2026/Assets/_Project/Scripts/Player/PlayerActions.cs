using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    public enum InputType
    {
        MouseKeyboard,
        Controller
    }

    public InputAction MoveAction { get; set; }
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction lassoAction;
    private InputAction placeTetherAction;
    private InputAction deactivateTetherAction;
    private InputAction activateTetherAction;
    private InputAction dPadForwardAction;
    private InputAction dPadBackwardAction;
    private InputAction dPadRightAction;
    private InputAction dPadLeftAction;
    private InputAction moveAnchorMouseAction;
    private InputAction snapRotateToggleAction;
    private InputAction freeRotateToggleAction;
    private InputAction snapRotateAction;
    private InputAction recallNPCAction;
    private InputAction toolSwitchAction;
    private InputAction grabAction;

    private InputAction[] allActions;
    private InputActionAsset map;

    private InputAction menuAction;
    private InputAction controlAction;
    private InputAction devMenuAction;
    private InputAction respawnAction;

    public InputType CurrentDevice { get; private set; }

    #region Public Accessors
    public Vector2 MoveInput => MoveAction.ReadValue<Vector2>();
    public Vector2 LookInput => lookAction.ReadValue<Vector2>();

    public bool JumpDown => jumpAction.WasPressedThisFrame();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpUp => jumpAction.WasReleasedThisFrame();

    public bool LassoDown => lassoAction.WasPressedThisFrame();
    public bool LassoHeld => lassoAction.IsPressed();
    public bool LassoUp => lassoAction.WasReleasedThisFrame();

    public bool PlaceTetherDown => placeTetherAction.WasPressedThisFrame();
    public bool PlaceTetherHeld => placeTetherAction.IsPressed();
    public bool PlaceTetherUp => placeTetherAction.WasReleasedThisFrame();

    public bool DeactivateTetherDown => deactivateTetherAction.WasPressedThisFrame();
    public bool DeactivateTetherHeld => deactivateTetherAction.IsPressed();
    public bool DeactivateTetherUp => deactivateTetherAction.WasReleasedThisFrame();

    public bool ActivateTetherDown => activateTetherAction.WasPressedThisFrame();
    public bool ActivateTetherHeld => activateTetherAction.IsPressed();
    public bool ActivateTetherUp => activateTetherAction.WasReleasedThisFrame();

    public bool DPadForwardDown => dPadForwardAction.WasPressedThisFrame();
    public bool DPadForwardHeld => dPadForwardAction.IsPressed();
    public bool DPadForwardUp => dPadForwardAction.WasReleasedThisFrame();
    
    public bool DPadBackwardDown => dPadBackwardAction.WasPressedThisFrame();
    public bool DPadBackwardHeld => dPadBackwardAction.IsPressed();
    public bool DPadBackwardUp => dPadBackwardAction.WasReleasedThisFrame();

    public bool DPadRightDown => dPadRightAction.WasPressedThisFrame();
    public bool DPadRightHeld => dPadRightAction.IsPressed();
    public bool DPadRightUp => dPadRightAction.WasReleasedThisFrame();

    public bool DPadLeftDown => dPadLeftAction.WasPressedThisFrame();
    public bool DPadLeftHeld => dPadLeftAction.IsPressed();
    public bool DPadLeftUp => dPadLeftAction.WasReleasedThisFrame();

    public float MoveAnchor => moveAnchorMouseAction.ReadValue<float>();

    public bool SnapRotateToggleDown => snapRotateToggleAction.WasPressedThisFrame();
    public bool SnapRotateToggleHeld => snapRotateToggleAction.IsPressed();
    public bool SnapRotateToggleUp => snapRotateToggleAction.WasReleasedThisFrame();

    public bool FreeRotateToggleDown => freeRotateToggleAction.WasPressedThisFrame();
    public bool FreeRotateToggleHeld => freeRotateToggleAction.IsPressed();
    public bool FreeRotateToggleUp => freeRotateToggleAction.WasReleasedThisFrame();

    public bool RecallNPCDown => recallNPCAction.WasPressedThisFrame();
    public bool RecallNPCHeld => recallNPCAction.IsPressed();
    public bool RecallNPCUp => recallNPCAction.WasReleasedThisFrame();

    public bool MenuDown => menuAction.WasPressedThisFrame();
    public bool MenuHeld => menuAction.IsPressed();
    public bool MenuUp => menuAction.WasReleasedThisFrame();

    public bool ControlDown => controlAction.WasPressedThisFrame();
    public bool ControlHeld => controlAction.IsPressed();
    public bool ControlUp => controlAction.WasReleasedThisFrame();

    public bool DevMenuDown => devMenuAction.WasPressedThisFrame();
    public bool DevMenuHeld => devMenuAction.IsPressed();
    public bool DevMenuUp => devMenuAction.WasReleasedThisFrame();

    public bool RespawnDown => respawnAction.WasPressedThisFrame();
    public bool RespawnHeld => respawnAction.IsPressed();
    public bool RespawnUp => respawnAction.WasReleasedThisFrame();

    public bool toolSwitchDown => toolSwitchAction.WasPressedThisFrame();
    public bool toolSwitchHeld => toolSwitchAction.IsPressed();
    public bool toolSwitchUp => toolSwitchAction.WasReleasedThisFrame();

    public bool grabDown => grabAction.WasPressedThisFrame();
    public bool grabHeld => grabAction.IsPressed();
    public bool grabUp => grabAction.WasReleasedThisFrame();


    #endregion

    private void Awake()
    {
        map = InputSystem.actions;
        MoveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        jumpAction = map.FindAction("Jump");
        lassoAction = map.FindAction("Lasso");
        placeTetherAction = map.FindAction("PlaceTether");
        deactivateTetherAction = map.FindAction("DeactivateTether");
        activateTetherAction = map.FindAction("ActivateTether");
        dPadForwardAction = map.FindAction("DPadForward");
        dPadBackwardAction = map.FindAction("DPadBackward");
        dPadRightAction = map.FindAction("DPadRight");
        dPadLeftAction = map.FindAction("DPadLeft");
        moveAnchorMouseAction = map.FindAction("MoveAnchor");
        snapRotateToggleAction = map.FindAction("SnapRotateToggle");
        freeRotateToggleAction = map.FindAction("FreeRotateToggle");
        snapRotateAction = map.FindAction("SnapRotate");
        recallNPCAction = map.FindAction("RecallNPC");
        menuAction = map.FindAction("Menu");
        controlAction = map.FindAction("Control");
        devMenuAction = map.FindAction("DevMenu");
        respawnAction = map.FindAction("Respawn");
        toolSwitchAction = map.FindAction("ToolSwitch");
        grabAction = map.FindAction("Grab");

        allActions = new InputAction[]
        {
            MoveAction, lookAction, jumpAction, lassoAction,
            placeTetherAction, deactivateTetherAction, activateTetherAction,
            dPadForwardAction, dPadBackwardAction, dPadRightAction, dPadLeftAction,
            moveAnchorMouseAction, snapRotateToggleAction, freeRotateToggleAction,
            snapRotateAction, recallNPCAction, menuAction, controlAction,
            devMenuAction, respawnAction, toolSwitchAction, grabAction
        };

        currentRepeatRate = baseRepeatRate;
    }

    private void OnEnable()
    {
        EnableAllInput();
    }

    private void OnDisable()
    {
        DisableAllInput();
    }

    public void EnableAllInput()
    {
        foreach (var action in allActions)
        {
            if (action == null) continue;

            action.started += OnInputReceived;
            action.performed += OnInputReceived;

            action.Enable();
        }
    }

    public void DisableAllInput(InputAction ignoreAction = null)
    {
        foreach (var action in allActions)
        {
            if (action == null) continue;
            if (action == ignoreAction) continue;

            action.started -= OnInputReceived;
            action.performed -= OnInputReceived;

            action.Disable();
        }
    }

    public void ChangeSpecificInput(string inputActionName, bool enable)
    {
        var map = InputSystem.actions;
        InputAction action = map.FindAction(inputActionName);

        if (enable) action.Enable();
        else action.Disable();
    }

    private void OnInputReceived(InputAction.CallbackContext ctx)
    {
        if (ctx.control.device is Gamepad)
        {
            CurrentDevice = InputType.Controller;
        }
        else if (ctx.control.device is Mouse || ctx.control.device is Keyboard)
        {
            CurrentDevice = InputType.MouseKeyboard;
        }
    }

    //made these numbers up ngl
    float dpadTimer = 0f;
    float baseRepeatRate = 0.125f;
    float dpadHoldCheck = 0.05f;
    float minRepeatRate = 0.00125f;
    float scrollAccel = 0.0125f;
    float currentRepeatRate;
    float repeatTimer = 0f;

    public float GetDPadScrollValue()
    {
        //tap
        if (DPadForwardDown)
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            return +1f;
        }
        if (DPadBackwardDown)
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            return -1f;
        }

        //check if held long enough
        if (DPadForwardHeld || DPadBackwardHeld)
        {
            dpadTimer += Time.deltaTime;

            if (dpadTimer >= dpadHoldCheck)
            {
                repeatTimer -= Time.deltaTime;
                if (repeatTimer <= 0f)
                {
                    currentRepeatRate = Mathf.Clamp(currentRepeatRate - scrollAccel, minRepeatRate, baseRepeatRate);
                    repeatTimer = currentRepeatRate;

                    return DPadForwardHeld ? +1f : -1f;
                }
            }
        }

        //if not use mouse wheel
        else
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            repeatTimer = baseRepeatRate;
            return MoveAnchor;
        }

        return 0f;
    }
}

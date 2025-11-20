using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    private InputAction lookAction;
    private InputAction scrollAction;
    private InputAction dPadUpAction;
    private InputAction dPadDownAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction mainAction;
    private InputAction altAction;
    private InputAction interactAction;
    public InputAction MoveAction { get; set; }
    private InputAction menuAction;

    public Vector2 MoveInput => MoveAction.ReadValue<Vector2>();
    public Vector2 LookInput => lookAction.ReadValue<Vector2>();

    public float ScrollAction => scrollAction.ReadValue<float>();
    public bool DPadUpDown => dPadUpAction.WasPressedThisFrame();
    public bool DPadUpHeld => dPadUpAction.IsPressed();
    public bool DPadUpUp => dPadUpAction.WasReleasedThisFrame();
    public bool DPadDownDown => dPadDownAction.WasPressedThisFrame();
    public bool DPadDownHeld => dPadDownAction.IsPressed();
    public bool DPadDownUp => dPadDownAction.WasReleasedThisFrame();

    public bool JumpDown => jumpAction.WasPressedThisFrame();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpUp => jumpAction.WasReleasedThisFrame();

    public bool CrouchDown => crouchAction.WasPressedThisFrame();
    public bool CrouchHeld => crouchAction.IsPressed();
    public bool CrouchUp => crouchAction.WasReleasedThisFrame();

    public bool SprintDown => sprintAction.WasPressedThisFrame();
    public bool SprintHeld => sprintAction.IsPressed();
    public bool SprintUp => sprintAction.WasReleasedThisFrame();

    public bool MainDown => mainAction.WasPressedThisFrame();
    public bool MainHeld => mainAction.IsPressed();
    public bool MainUp => mainAction.WasReleasedThisFrame();

    public bool AltDown => altAction.WasPressedThisFrame();
    public bool AltHeld => altAction.IsPressed();
    public bool AltUp => altAction.WasReleasedThisFrame();

    public bool InteractDown => interactAction.WasPressedThisFrame();
    public bool InteractHeld => interactAction.IsPressed();
    public bool InteractUp => interactAction.WasReleasedThisFrame();

    public bool MenuDown => menuAction.WasPressedThisFrame();
    public bool MenuHeld => menuAction.IsPressed();
    public bool MenuUp => menuAction.WasReleasedThisFrame();

    private void Awake()
    {
        var map = InputSystem.actions;
        MoveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        scrollAction = map.FindAction("ScrollWheel");
        jumpAction = map.FindAction("Jump");
        crouchAction = map.FindAction("Crouch");
        sprintAction = map.FindAction("Sprint");
        mainAction = map.FindAction("Main");
        altAction = map.FindAction("Alt");
        interactAction = map.FindAction("Interact");
        dPadUpAction = map.FindAction("DPadUp");
        dPadDownAction = map.FindAction("DPadDown");
        currentRepeatRate = baseRepeatRate;
        menuAction = map.FindAction("Menu");
    }

    private void OnEnable()
    {
        MoveAction.Enable();
        lookAction.Enable();
        scrollAction.Enable();
        jumpAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        mainAction.Enable();
        altAction.Enable();
        interactAction.Enable();
        dPadUpAction.Enable();
        dPadDownAction.Enable();
        menuAction.Enable();
    }

    private void OnDisable()
    {
        MoveAction.Disable();
        lookAction.Disable();
        scrollAction.Disable();
        jumpAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        mainAction.Disable();
        altAction.Disable();
        interactAction.Disable();
        dPadUpAction.Disable();
        dPadDownAction.Disable();
        menuAction.Disable();
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
        if (DPadUpDown)
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            return +1f;
        }
        if (DPadDownDown)
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            return -1f;
        }

        //check if held long enough
        if (DPadUpHeld || DPadDownHeld)
        {
            dpadTimer += Time.deltaTime;

            if (dpadTimer >= dpadHoldCheck)
            {
                repeatTimer -= Time.deltaTime;
                if (repeatTimer <= 0f)
                {
                    currentRepeatRate = Mathf.Clamp(currentRepeatRate - scrollAccel, minRepeatRate, baseRepeatRate);
                    repeatTimer = currentRepeatRate;

                    return DPadUpHeld ? +1f : -1f;
                }
            }
        }

        //if not use mouse wheel
        else
        {
            dpadTimer = 0f;
            currentRepeatRate = baseRepeatRate;
            repeatTimer = baseRepeatRate;
            return ScrollAction;
        }

        return 0f;
    }
}

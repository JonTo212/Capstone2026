using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    private InputAction lookAction;
    private InputAction scrollAction;
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
        menuAction.Disable();
    }
}

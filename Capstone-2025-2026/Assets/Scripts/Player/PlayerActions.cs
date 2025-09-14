using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction pullAction;
    private InputAction throwAction;

    public Vector2 MoveInput => moveAction.ReadValue<Vector2>();
    public Vector2 LookInput => lookAction.ReadValue<Vector2>();

    public bool JumpDown => jumpAction.WasPressedThisFrame();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpUp => jumpAction.WasReleasedThisFrame();

    public bool CrouchDown => crouchAction.WasPressedThisFrame();
    public bool CrouchHeld => crouchAction.IsPressed();
    public bool CrouchUp => crouchAction.WasReleasedThisFrame();

    public bool SprintDown => sprintAction.WasPressedThisFrame();
    public bool SprintHeld => sprintAction.IsPressed();
    public bool SprintUp => sprintAction.WasReleasedThisFrame();

    public bool PullDown => pullAction.WasPressedThisFrame();
    public bool PullHeld => pullAction.IsPressed();
    public bool PullUp => pullAction.WasReleasedThisFrame();

    public bool ThrowDown => throwAction.WasPressedThisFrame();
    public bool ThrowHeld => throwAction.IsPressed();
    public bool ThrowUp => throwAction.WasReleasedThisFrame();

    private void Awake()
    {
        var map = InputSystem.actions;
        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        jumpAction = map.FindAction("Jump");
        crouchAction = map.FindAction("Crouch");
        sprintAction = map.FindAction("Sprint");
        pullAction = map.FindAction("Pull");
        throwAction = map.FindAction("Throw");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        pullAction.Enable();
        throwAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        pullAction.Disable();
        throwAction.Disable();
    }
}

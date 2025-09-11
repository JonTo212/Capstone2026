using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerActions : MonoBehaviour
{
    [Header("Input Actions")]
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction pullAction;
    private InputAction throwAction;

    [Header("Input Variables")]
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool crouchInput;
    private bool sprintInput;
    private bool pullInput;

    //Single input events
    public event Action OnJumpPressed;
    public event Action OnThrowPressed;

    [Header("Getters")]
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool CrouchInput => crouchInput;
    public bool SprintInput => sprintInput;
    public bool PullInput => pullInput;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        pullAction = InputSystem.actions.FindAction("Pull");
        throwAction = InputSystem.actions.FindAction("Throw");
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

        //subscribe to callback events (more efficient than constantly checking in update)
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;

        lookAction.performed += OnLookPerformed;
        lookAction.canceled += OnLookCanceled;

        jumpAction.started += OnJumpPerformed;

        crouchAction.performed += OnCrouchPerformed;
        crouchAction.canceled += OnCrouchCanceled;

        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;

        pullAction.performed += OnPullPerformed;
        pullAction.canceled += OnPullCanceled;

        throwAction.performed += OnThrowPerformed;
    }

    private void OnDisable()
    {
        //unsubscribe to make sure input capturing isn't duplicated by accident
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;

        lookAction.performed -= OnLookPerformed;
        lookAction.canceled -= OnLookCanceled;

        jumpAction.performed -= OnJumpPerformed;

        crouchAction.performed -= OnCrouchPerformed;
        crouchAction.canceled -= OnCrouchCanceled;

        sprintAction.performed -= OnSprintPerformed;
        sprintAction.canceled -= OnSprintCanceled;

        pullAction.performed -= OnPullPerformed;
        pullAction.canceled -= OnPullCanceled;

        throwAction.performed -= OnThrowPerformed;

        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        pullAction.Disable();
        throwAction.Disable();
    }

    #region Input callbacks
    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void OnJumpPerformed(InputAction.CallbackContext ctx) => OnJumpPressed?.Invoke();
    private void OnCrouchPerformed(InputAction.CallbackContext ctx) => crouchInput = true;
    private void OnCrouchCanceled(InputAction.CallbackContext ctx) => crouchInput = false;

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => sprintInput = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => sprintInput = false;

    private void OnPullPerformed(InputAction.CallbackContext ctx) => pullInput = true;
    private void OnPullCanceled(InputAction.CallbackContext ctx) => pullInput = false;

    private void OnThrowPerformed(InputAction.CallbackContext ctx) => OnThrowPressed?.Invoke();
    #endregion
}

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class fakeglide : MonoBehaviour
{

    //https://chatgpt.com/share/69263880-8170-8009-b3e3-5b7d35a95c2e copy pasted for testing purposes


    private InputAction holdKAction;
    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        holdKAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/q");
        holdKAction.performed += OnKPressed;
        holdKAction.canceled += OnKReleased;
        holdKAction.Enable();
    }

    private void OnDisable()
    {
        holdKAction.Disable();
    }

    private void OnKPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("K held — activate function!");
        ActivateFunction();
    }

    private void OnKReleased(InputAction.CallbackContext ctx)
    {
        Debug.Log("K released — deactivate function!");
        DeactivateFunction();
    }

    void ActivateFunction()
    {
        rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void DeactivateFunction()
    {
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
    }
}

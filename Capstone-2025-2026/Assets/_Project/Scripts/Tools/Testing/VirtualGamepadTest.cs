using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class VirtualGamepadTest : MonoBehaviour
{
    private Gamepad virtualGamepad;

    void Awake()
    {
        virtualGamepad = InputSystem.AddDevice<Gamepad>();
    }

    void Update()
    {
        // Sin wave creates movement that the Input System must report as a change
        float xValue = Mathf.Sin(Time.time * 5f);

        InputSystem.QueueStateEvent(
            virtualGamepad,
            new GamepadState { leftStick = new Vector2(xValue, 0f) }
        );
    }

    void OnDestroy()
    {
        if (virtualGamepad != null)
            InputSystem.RemoveDevice(virtualGamepad);
    }
}

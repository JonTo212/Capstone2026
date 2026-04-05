using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CinemachineCutscenesLogic : MonoBehaviour
{
    public CinemachineCamera[] cameras;
    public int activePriority = 10;
    public int inactivePriority = 0;

    void Start()
    {
        SetActiveCamera(-1); // all inactive at start
    }

    public void SwitchCamera1(InputAction.CallbackContext context)
    {
        if (context.performed)
            SetActiveCamera(0);
    }

    public void SwitchCamera2(InputAction.CallbackContext context)
    {
        if (context.performed)
            SetActiveCamera(1);
    }

    public void SwitchCamera3(InputAction.CallbackContext context)
    {
        if (context.performed)
            SetActiveCamera(2);
    }

    void SetActiveCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? activePriority : inactivePriority;
        }
    }
}



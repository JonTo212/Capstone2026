using UnityEngine;

public class DevMenu : MonoBehaviour
{
    Camera _camera;
    [SerializeField] private PlayerActions _actions;
    [SerializeField] private RectTransform devUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _camera = Camera.main;
        CloseMenu();
    }

    private void Update()
    {
        if(_actions.ControlHeld)
        {
            if(_actions.DevMenuDown)
            {
                if (!devUI.gameObject.activeInHierarchy)
                {
                    OpenMenu();
                }
                else
                {
                    CloseMenu();
                }
            }
        }
    }

    public void OpenMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        devUI.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        devUI.gameObject.SetActive(false);
    }
}

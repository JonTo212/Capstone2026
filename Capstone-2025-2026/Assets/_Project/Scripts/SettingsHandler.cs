using UnityEngine;

public class SettingsHandler : MonoBehaviour
{
    private static bool initialized = false;

    //this is perfect for menu/settings

    private void Awake()
    {
        if (initialized) return;
        initialized = true;
        DontDestroyOnLoad(gameObject);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
    }
}

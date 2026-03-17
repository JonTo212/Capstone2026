using UnityEngine;

public class DevMenu : MonoBehaviour
{
    public static DevMenu Instance {  get; private set; }

    public bool devMenuOpen;
    [SerializeField] private RectTransform devUI;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    void Start()
    {
        CloseMenu();
    }

    private void Update()
    {
        if(PlayerActions.Instance.ControlHeld)
        {
            if(PlayerActions.Instance.DevMenuDown)
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
        devMenuOpen = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        devUI.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        devMenuOpen = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        devUI.gameObject.SetActive(false);
    }
}

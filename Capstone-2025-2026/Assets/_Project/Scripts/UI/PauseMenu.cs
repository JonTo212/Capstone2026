using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public GameObject pauseMenuScreen;
    public GameObject pauseMenuUI;
    public GameObject settingsUI;
    public GameObject critterUI;
    public GameObject missionImageUI;

    public Button options;
    public int State;

    public GameObject settingsFirst;
    public GameObject menuFirst;

    public PlayerRespawn respawnScript;

    private void Start()
    {
        options = GameObject.Find("Options").GetComponent<Button>();
        pauseMenuScreen.SetActive(false);
        settingsUI.SetActive(false);
        
    }

    void Update()
    {
        if (PlayerActions.Instance.MenuDown)
        {
            if (gameIsPaused)
                Resume();
            else
                Pause();
        }

        if (State == 2 && PlayerActions.Instance.menuBackUp)
        {
            settingsBack();
        }
        else if (State == 1 && PlayerActions.Instance.menuBackUp)
        {
            Resume();
        }

        if(!gameIsPaused && !DevMenu.Instance.devMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    #region PauseDefaults

    public void Resume()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pauseMenuScreen.SetActive(false);
        //critterUI.SetActive(true);
        //missionImageUI.SetActive(true);
        Time.timeScale = 1f;
        gameIsPaused = false;
        PlayerActions.Instance.EnableAllInput();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void Pause()
    {
        State = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseMenuScreen.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
        //critterUI.SetActive(false);
        //missionImageUI.SetActive(false);
        PlayerActions.Instance.DisableAllInput();
        PlayerActions.Instance.ChangeSpecificInput("Menu", true);
        PlayerActions.Instance.ChangeSpecificInput("MenuBack", true);
        EventSystem.current.SetSelectedGameObject(menuFirst);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }

    public void HandleRespawn()
    {
        respawnScript.StartRespawn();
        Resume();
    }

    #endregion

    #region Settings

    public void Settings()
    {
        State = 2;
        pauseMenuUI.SetActive(false);
        settingsUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(settingsFirst);
    }

    public void settingsBack()
    {
        State = 1;
        settingsUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        options.Select();
    }

    public void allBack()
    {
        State = 1;
        settingsUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }

    #endregion
}
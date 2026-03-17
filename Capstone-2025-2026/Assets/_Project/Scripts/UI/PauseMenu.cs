using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public GameObject pauseMenuScreen;
    public GameObject pauseMenuUI;
    public GameObject settingsUI;
    public GameObject critterUI;
    public GameObject missionImageUI;

    public GameObject settingsFirst;
    public GameObject menuFirst;

    private void Start()
    {
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

        if(!gameIsPaused)
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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseMenuScreen.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
        //critterUI.SetActive(false);
        //missionImageUI.SetActive(false);
        PlayerActions.Instance.DisableAllInput();
        EventSystem.current.SetSelectedGameObject(menuFirst);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }

    #endregion

    #region Settings

    public void Settings()
    {
        pauseMenuUI.SetActive(false);
        settingsUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(settingsFirst);
    }

    public void settingsBack()
    {
        settingsUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(menuFirst);
    }

    public void allBack()
    {
        settingsUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }

    #endregion
}
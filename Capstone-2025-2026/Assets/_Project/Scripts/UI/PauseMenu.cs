using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public GameObject pauseMenuScreen;
    public GameObject pauseMenuUI;
    public GameObject settingsUI;

    public PlayerActions playerActions;

    // Update is called once per frame
    void Update()
    {
        if (playerActions.MenuDown)
        {
            if (gameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    #region PauseDefaults
    public void Resume()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pauseMenuScreen.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    public void Pause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseMenuScreen.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    public void Settings()
    {
        pauseMenuUI.SetActive(false);
        settingsUI.SetActive(true);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }
    
    #endregion

    #region Settings

    public void soundBack()
    {
        settingsUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }
    #endregion
}

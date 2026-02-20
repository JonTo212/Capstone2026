using System;
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

        if(!gameIsPaused)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    #region PauseDefaults
    public void Resume()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pauseMenuScreen.SetActive(false);
        critterUI.SetActive(true);
        missionImageUI.SetActive(true);
        Time.timeScale = 1f;
        gameIsPaused = false;
        playerActions.EnableAllInput();
    }

    public void Pause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseMenuScreen.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
        critterUI.SetActive(false);
        missionImageUI.SetActive(false);
        playerActions.DisableAllInput();
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
        settingsUI.SetActive(false) ;
        pauseMenuUI.SetActive(true) ;

        EventSystem.current.SetSelectedGameObject(null);
    }
    #endregion
}

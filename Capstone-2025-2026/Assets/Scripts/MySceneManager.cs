//using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [Header("Scene Index")]
    public string[] SceneNames;

    private string currentScene;

    public void LoadNewScene(int sceneIndex)
    {
        SceneManager.LoadScene(SceneNames[sceneIndex]);
        currentScene = SceneNames[sceneIndex];
    }
    
    public void QuitToDesktop()
    {
        /*
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
        */

        Application.Quit();

    }

    public void ReturnToStartMenu ()
    {
        SceneManager.LoadScene("StartMenu");
        currentScene = "StartMenu";
    }

    public void RestartCurrentLevel()
    {
        SceneManager.LoadScene(currentScene);
    }
}

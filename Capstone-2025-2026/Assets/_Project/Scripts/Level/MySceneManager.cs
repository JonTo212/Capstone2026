//using UnityEditor.SearchService;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [Header("Scene Index")]
    public string[] SceneNames;

    private string currentScene;

    public GameObject goalScreen;

    public void LoadNewScene(int sceneIndex)
    {
        goalScreen.SetActive(true);

        StartCoroutine(switchLevel(sceneIndex));

        /*SceneManager.LoadScene(SceneNames[sceneIndex]);
        currentScene = SceneNames[sceneIndex];*/
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

    IEnumerator switchLevel(int sceneIndex)
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene(SceneNames[sceneIndex]);
        currentScene = SceneNames[sceneIndex];
    }
}

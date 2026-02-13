//using UnityEditor.SearchService;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MySceneManager : MonoBehaviour
{
    [Header("Scene Index")]
    public string[] SceneNames;

    private string currentScene;

    //public GameObject goalScreen;

    public ScreenTransitions transitions;
    private void Start()
    {
        transitions = FindAnyObjectByType<ScreenTransitions>();
    }

    public void LoadNewScene(int sceneIndex)
    {
        //transitions.gameObject.transform.parent = goalScreen.transform;
        //goalScreen.SetActive(true);

        

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

    public void ReturnToStartMenu()
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
        //yield return new WaitForSeconds(3);
        if (transitions != null)
        {
            transitions.FadeToBlack();
        }
        yield return new WaitForSeconds(4);
        SceneManager.LoadScene(SceneNames[sceneIndex]);
        currentScene = SceneNames[sceneIndex];
    }
}

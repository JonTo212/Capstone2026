//using UnityEditor.SearchService;
using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [Header("Scene Index")]
    public string[] SceneNames;

    private string currentScene;
    
    public float loadMinimumTime;
    private bool readyToLoad;
    private bool externalReady;

    //public GameObject goalScreen;

    public ScreenTransitions transitions;

    public GameObject firstButton;

    public LoadingScreenAnimation loadingScreenAnimation;
    private AsyncOperation pendingLoad;
    public AsyncOperation GetPendingLoad() => pendingLoad;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        transitions = FindAnyObjectByType<ScreenTransitions>();
        if(firstButton)EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void LoadNewScene(int sceneIndex)
    {
        //transitions.gameObject.transform.parent = goalScreen.transform;
        //goalScreen.SetActive(true);


        readyToLoad = false;
        StartCoroutine(SwitchLevel(sceneIndex));

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

    public void SetReadyToLoad(bool ready)
    {
        readyToLoad = ready;
    }
    public void SetExternalReady(bool ready)
    {
        externalReady = ready;
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

    IEnumerator SwitchLevel(int sceneIndex)
    {
        //yield return new WaitForSeconds(3);
        if (transitions != null)
        {
            transitions.FadeToBlack();
        }

        pendingLoad = SceneManager.LoadSceneAsync(SceneNames[sceneIndex]);
        pendingLoad.allowSceneActivation = false;

        if (loadingScreenAnimation == null)
        {
            externalReady = true;
            readyToLoad = true;
        }

        //needs to run a minimum duration before officially being completed, even if it loads faster than that
        float elapsed = 0f;
        yield return new WaitUntil(() =>
        {
            elapsed += Time.deltaTime;
            return pendingLoad.progress >= 0.9f && elapsed >= loadMinimumTime && readyToLoad && externalReady;
        });

        pendingLoad.allowSceneActivation = true;
        currentScene = SceneNames[sceneIndex];

        yield return new WaitUntil(() => pendingLoad.isDone);

        if (loadingScreenAnimation != null)
        {
            loadingScreenAnimation.Reset();
            loadingScreenAnimation.gameObject.SetActive(false);
        }
    }
}

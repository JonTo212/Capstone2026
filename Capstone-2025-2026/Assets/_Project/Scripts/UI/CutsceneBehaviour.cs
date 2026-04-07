using UnityEngine;
using UnityEngine.Video;

public class CutsceneBehaviour : MonoBehaviour
{
    public VideoPlayer video;
    public MySceneManager sceneManager;
    private double videoLength;
    private float elapsed;
    private bool startedLoad;

    void Start()
    {
        elapsed = 0;
        startedLoad = false;
        videoLength = video.length;
        sceneManager.SetExternalReady(false);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (!startedLoad && elapsed >= videoLength)
        {
            LoadNextScene();
        }

        if (PlayerActions.Instance.DeactivateTetherDown)
        {
            print("running");
        }

        if (PlayerActions.Instance.DeactivateTetherDown && !startedLoad)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        sceneManager.LoadNewScene(0); // begins async load, no animation yet
        startedLoad = true;
        sceneManager.loadingScreenAnimation.ShowLoadingScreen(); // activates screen AND starts animation
        sceneManager.SetExternalReady(true);
    }
}

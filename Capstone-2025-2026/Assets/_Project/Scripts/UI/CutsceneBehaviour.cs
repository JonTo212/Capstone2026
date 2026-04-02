using UnityEngine;
using UnityEngine.Video;

public class CutsceneBehaviour : MonoBehaviour
{
    public VideoPlayer video;
    public MySceneManager sceneManager;
    private double videoLength;
    private double elapsed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        videoLength = video.length - 4;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= videoLength)
        {
            sceneManager.LoadNewScene(0);
        }

        if (Input.anyKey)
        {
            sceneManager.LoadNewScene(0);
        }
    }
}

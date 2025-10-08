using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [Header("Scene Index")]
    public string[] SceneNames;

    public void LoadNewScene(int sceneIndex)
    {
        SceneManager.LoadScene(SceneNames[sceneIndex]);
    }
    
    public void QuitToDesktop()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }

    }
}

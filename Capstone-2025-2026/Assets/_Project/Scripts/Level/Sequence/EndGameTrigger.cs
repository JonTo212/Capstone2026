using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameTrigger : MonoBehaviour
{

    [SerializeField] CutsceneBase cutscene;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag==("Player"))
        {
            StartCoroutine(CutsceneSequence());
        }

    }

    public IEnumerator CutsceneSequence()
    {
        CameraCutsceneHandler.Instance.StartCutscene(cutscene);
        yield return new WaitForSeconds(cutscene.Duration);
        EndScene();

    }

    public void EndScene()
    {
            //enable cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Gets the current scene's index and adds 1 to load the next one
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            // Check if there is actually a next scene in the build list
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("No more scenes in build order!");
            }
    }
}

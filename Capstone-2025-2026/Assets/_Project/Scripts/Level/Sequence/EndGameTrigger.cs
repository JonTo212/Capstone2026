using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    public MySceneManager sceneManager;

    public void OnTriggerEnter(Collider other)
    {

        if(other.gameObject.CompareTag("Player")){

            sceneManager.LoadNewScene(0);

        }

    }


}

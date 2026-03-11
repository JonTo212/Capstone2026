using System.Linq;
using FMODUnity;
using UnityEngine;

public class EndSequeenceTracker : MonoBehaviour
{

    public GameObject[] Supports;

    // Update is called once per frame
    public void UpdateSupports(int targettedSupport)
    {
        Destroy(Supports[targettedSupport]);
        Supports[targettedSupport] = null;

        bool ready = true;

        foreach(GameObject i in Supports)
        {
            ready = false;
        }

        if (ready)
        {
            EndSequence();
        }
    } 

    public void EndSequence()
    {
        //Play Scene
        Debug.Log("THE END SCENE HAPPENED");
    }

}

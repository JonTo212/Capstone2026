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

        if (Supports[0] == null && Supports[1] == null && Supports[2] == null )
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

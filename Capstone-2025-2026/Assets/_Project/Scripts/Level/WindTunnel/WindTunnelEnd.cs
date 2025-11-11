using UnityEngine;

public class WindTunnelEnd : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EndReached");
        if(other.GetComponent<WindTunnelProp>() != null)
        {
            other.GetComponent<WindTunnelProp>().Respawn();
        }
    }
}

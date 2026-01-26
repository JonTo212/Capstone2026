using UnityEngine;

public class WindTunnelEnd : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<WindTunnelProp>() != null)
        {
            other.GetComponent<WindTunnelProp>().Respawn();
        }
    }
}

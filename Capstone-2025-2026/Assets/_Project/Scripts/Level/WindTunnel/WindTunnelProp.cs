using UnityEngine;

public class WindTunnelProp : MonoBehaviour
{
    [SerializeField] public WindTunnel windTunnelParent { get; private set; }

    public void Init(WindTunnel windTunnelParent)
    {
        this.windTunnelParent = windTunnelParent;
    }

    //respawns back at start after it reaches the end
    public void Respawn()
    {
        windTunnelParent.RespawnObjectInWindtunnel(transform);
    }
}

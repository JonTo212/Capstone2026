using UnityEngine;
using UnityEngine.InputSystem;

public class Teleport : MonoBehaviour
{
    public Transform level1;
    public Transform level2;

    private Transform player;
    private Rigidbody playerRb;


    //https://chatgpt.com/share/691bd4be-b060-8009-98a3-41dbdeefe59f copied just for quick debugging
    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p.transform;
        playerRb = p.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TeleportPlayer(level1.position);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            TeleportPlayer(level2.position);
        }
    }

    void TeleportPlayer(Vector3 pos)
    {
        if (playerRb)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        player.position = pos;
    }
}

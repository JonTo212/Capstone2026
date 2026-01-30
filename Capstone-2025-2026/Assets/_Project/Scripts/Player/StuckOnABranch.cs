using System.Collections;
using UnityEngine;

public class StuckOnABranch : MonoBehaviour
{
    private PlayerMovement playermovement;
    private float timeElapsed = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        playermovement = GetComponent<PlayerMovement>();

        playermovement.PlayerInput.ChangeSpecificInput("Move", false);
        playermovement.EnableGravity(false);
        playermovement.EnableFriction(false);
        StartCoroutine(UnstuckAfterDelay());
    }

    IEnumerator UnstuckAfterDelay()
    {
        yield return new WaitForSeconds(4f);

        playermovement.EnableGravity(true);
        playermovement.EnableFriction(true);
        playermovement.PlayerInput.ChangeSpecificInput("Move", true);
    }
}

using System.Collections;
using UnityEngine;

public class PlayerDive : MonoBehaviour
{
    private PlayerMovement playerController;
    [SerializeField] private Transform forwardRef;

    private Coroutine diveCoroutine;
    [SerializeField] private float diveApexHeight;
    [SerializeField] private float diveApexTime;
    [SerializeField] private float diveDistance;
    [SerializeField] private float slideDuration;
    private float diveGravity;
    private float diveJumpForce;

    public bool IsDiving { get; private set; }
    public bool IsSliding { get; private set; }

    private void Awake()
    {
        playerController = GetComponent<PlayerMovement>();
        diveGravity = 2 * diveApexHeight / Mathf.Pow(diveApexTime, 2);
        diveJumpForce = 2 * diveApexHeight / diveApexTime;
    }

    private void Update()
    {
        if (playerController.PlayerInput.grabDown && diveCoroutine == null)
        {
            StartDive();
        }
    }
    private void StartDive()
    {
        diveCoroutine = StartCoroutine(HandleDive());
    }

    private IEnumerator HandleDive()
    {
        playerController.PlayerInput.ChangeSpecificInput("Move", false);
        playerController.Rb.linearVelocity = Vector3.zero;
        Vector3 dir = playerController.WishDir != Vector3.zero ? playerController.WishDir : forwardRef.forward;
        float diveTimer = 0f;

        playerController.EnableFriction(false);
        playerController.EnableGravity(false);
        Vector3 launchVel = dir.normalized * (diveDistance / diveApexTime) + Vector3.up * diveJumpForce;
        playerController.Rb.AddForce(launchVel, ForceMode.Impulse);
        IsDiving = true;

        while (diveTimer < diveApexTime * 2f)
        {
            diveTimer += Time.fixedDeltaTime;

            Vector3 vel = playerController.Rb.linearVelocity; 
            vel.y -= diveGravity * Time.fixedDeltaTime;
            playerController.Rb.linearVelocity = vel;

            yield return new WaitForFixedUpdate();
        }


        yield return new WaitUntil(() => playerController.IsGrounded());
        IsDiving = false;
        IsSliding = true;

        yield return new WaitForSeconds(slideDuration);
        IsSliding = false;

        playerController.PlayerInput.ChangeSpecificInput("Move", true);
        playerController.EnableGravity(true);
        playerController.EnableFriction(true);
        diveCoroutine = null;
    }
}

using System.Collections;
using UnityEngine;

public class StuckInGround : MonoBehaviour
{
    [SerializeField] private ImageFader fadeToBlackScript;
    [SerializeField] private Animator anim;
    [SerializeField] private float delayBeforeFadeIn = 2f;
    private CapsuleCollider playerCol;
    private bool skipRequested;

    private void Awake()
    {
        playerCol = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        StartCoroutine(StartStuckSequence());
    }

    private void Update()
    {
        if (PlayerActions.Instance.JumpDown) skipRequested = true;
    }

    private IEnumerator StartStuckSequence()
    {
        float timer = 0f;
        while (timer < delayBeforeFadeIn)
        {
            if (skipRequested)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        fadeToBlackScript.FadeOut();
        playerCol.enabled = false;
        PlayerRefData.Instance.PlayerMovement.DisableMovement(true);
        CameraRefData.Instance.ZeldaCameraController.SetRotation(-90f, 0f, true);

        StartCoroutine(WaitForJumpInput());
    }

    private IEnumerator WaitForJumpInput()
    {
        yield return new WaitUntil(() => PlayerActions.Instance.JumpDown);
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(Quaternion.LookRotation(forward), false);
        PlayerRefData.Instance.PlayerMovement.DisableMovement(false);
        PlayerRefData.Instance.PlayerMovement.SetDoubleJumpAvailable(false);
        anim.SetBool("FallCutscene", false);
        anim.SetTrigger("DoubleJump");
        playerCol.enabled = true;
    }
}

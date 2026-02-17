using System.Collections;
using UnityEngine;

public class ThirdPersonAnimatorController : MonoBehaviour
{
    private PlayerMovement _playerController;
    private PlayerActions _playerInput;
    private LassoTetherController _lassoTetherController;
    private JointTetherPlacer _jointTetherPlacer;
    private JointTetherActivator _jointTetherActivator;
    private PlayerLedgeGrab _playerLedgeGrab;
    private Lasso _lasso;
    [SerializeField] private CameraCutsceneHandler _cameraController;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerActions>();
        _lassoTetherController = GetComponent<LassoTetherController>();
        _lasso = GetComponent<Lasso>();
        _jointTetherActivator = GetComponent<JointTetherActivator>();
        _jointTetherPlacer = GetComponent<JointTetherPlacer>();
        _playerLedgeGrab = GetComponent<PlayerLedgeGrab>();

        _lasso.OnObjectHit += SetLassoBool;
        _jointTetherActivator.OnTetherActivated += SetTetherBool;
    }

    private void OnDisable()
    {
        _lasso.OnObjectHit -= SetLassoBool;
        _jointTetherActivator.OnTetherActivated -= SetTetherBool;
    }

    private void Update()
    {
        animator.SetBool("MoveInput", _playerController.WishDir != Vector3.zero);
        animator.SetBool("Jump", _playerInput.JumpDown);
        animator.SetBool("Swinging", _lassoTetherController.CurrentLassoState == LassoState.Swinging);
        animator.SetBool("IsGrounded", _playerController.IsGrounded());
        animator.SetBool("LassoSnared", _lassoTetherController.CurrentLassoState == LassoState.Snared);
        animator.SetBool("TetherStartPointHit", _jointTetherPlacer.didStartPointHit);
        animator.SetBool("TetherEndPointHit", _jointTetherPlacer.didEndPointHit);
        animator.SetBool("IsHanging", _playerLedgeGrab.IsHanging);
        animator.SetBool("Mantling", _playerLedgeGrab._mantleCoroutine != null);
        animator.SetBool("Swinging", _cameraController.IsCutsceneActive);
        animator.SetBool("AttachingToRail", _cameraController.BlendingIn);
    }

    private void SetLassoBool()
    {
        animator.SetBool("LassoStart", true);
        StartCoroutine(ResetBoolNextFrame("LassoStart"));
    }

    private void SetTetherBool()
    {
        animator.SetBool("OnTetherStart", true);
        StartCoroutine(ResetBoolNextFrame("OnTetherStart"));
    }

    private IEnumerator ResetBoolNextFrame(string boolName)
    {
        yield return new WaitForSeconds(0.1f);
        animator.SetBool(boolName, false);
    }
}

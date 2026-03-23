using System.Collections;
using UnityEngine;

public class ThirdPersonAnimatorController : MonoBehaviour
{
    private PlayerRefData _playerRefData;
    [SerializeField] private CameraCutsceneHandler _cameraController;
    [SerializeField] private Animator animator;

    private void Start()
    {
        _playerRefData = GetComponent<PlayerRefData>();

        _playerRefData.Lasso.OnObjectHit += SetLassoStart;
        _playerRefData.JointTetherActivator.OnTetherActivated += SetTetherStart;
    }

    private void OnDisable()
    {
        _playerRefData.Lasso.OnObjectHit -= SetLassoStart;
        _playerRefData.JointTetherActivator.OnTetherActivated -= SetTetherStart;
    }

    private void Update()
    {
        animator.SetBool("MoveInput", _playerRefData.PlayerMovement.WishDir != Vector3.zero && _playerRefData.PlayerMovement.CurrentMovementState != PlayerMoveState.Grabbing);
        animator.SetBool("Jump", PlayerActions.Instance.JumpDown);
        animator.SetBool("Swinging", (_playerRefData.LassoTetherController.CurrentLassoState == LassoState.Swinging) || (_cameraController.IsActive() && _cameraController.CurrentCutscene is RopeSwingCutscene));
        animator.SetBool("IsGrounded", _playerRefData.PlayerMovement.IsGrounded());
        animator.SetBool("LassoSnared", _playerRefData.LassoTetherController.CurrentLassoState == LassoState.Snared);
        if (_playerRefData.LassoTetherController.CurrentLassoState == LassoState.Snared) animator.SetFloat("LassoReel", Mathf.Abs(PlayerActions.Instance.MoveInput.y));
        animator.SetBool("TetherStartPointHit", _playerRefData.JointTetherPlacer.didStartPointHit);
        animator.SetBool("TetherEndPointHit", _playerRefData.JointTetherPlacer.didEndPointHit);
        animator.SetBool("IsHanging", _playerRefData.PlayerLedgeGrab.IsHanging);
        animator.SetBool("Mantling", _playerRefData.PlayerLedgeGrab._mantleCoroutine != null);
        animator.SetBool("AttachingToRail", _cameraController.BlendingIn && _cameraController.CurrentCutscene is RopeSwingCutscene);
    }

    private void SetLassoStart()
    {
        animator.SetTrigger("LassoStart");
    }

    private void SetTetherStart()
    {
        animator.SetTrigger("TetherStart");
    }
}

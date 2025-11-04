using System.Collections;
using UnityEngine;

public class ThirdPersonAnimatorController : MonoBehaviour
{
    private PlayerMovement _playerController;
    private PlayerActions _playerInput;
    private LassoTetherController _lassoTetherController;
    private JointTetherPlacer _jointTetherPlacer;
    private JointTetherActivator _jointTetherActivator;
    private Lasso _lasso;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerActions>();
        _lassoTetherController = GetComponent<LassoTetherController>();
        _lasso = GetComponent<Lasso>();
        _jointTetherActivator = GetComponent<JointTetherActivator>();
        _jointTetherPlacer = GetComponent<JointTetherPlacer>();

        _lasso.OnObjectHit += SetLassoBool;
        _jointTetherPlacer.OnTetherStartHit += SetTetherBool;
        _jointTetherActivator.OnTetherActivated += SetTetherBool;
    }

    private void Update()
    {
        if (_playerController.WishDir != Vector3.zero)
        {
            animator.SetBool("MoveInput", true);
        }
        else
        {
            animator.SetBool("MoveInput", false);
        }

        animator.SetBool("Jump", _playerInput.JumpDown);
        animator.SetBool("Swinging", _lassoTetherController.CurrentLassoState == LassoState.Swinging);
    }

    private void SetLassoBool()
    {
        animator.SetBool("LassoStart", true);
        StartCoroutine(ResetBoolNextFrame("LassoStart"));
    }

    private void SetTetherBool()
    {
        animator.SetBool("TetherStart", true);
        StartCoroutine(ResetBoolNextFrame("TetherStart"));
    }

    private IEnumerator ResetBoolNextFrame(string boolName)
    {
        yield return null;
        animator.SetBool(boolName, false);
    }
}

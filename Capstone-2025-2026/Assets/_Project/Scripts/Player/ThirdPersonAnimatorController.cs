using System.Collections;
using UnityEngine;

public class ThirdPersonAnimatorController : MonoBehaviour
{
    private PlayerMovement _playerController;
    private PlayerActions _playerInput;
    private PlayerMantle _playerMantle;
    private LassoTetherController _lassoTetherController;
    private JointTetherPlacer _jointTetherPlacer;
    private JointTetherActivator _jointTetherActivator;
    private Lasso _lasso;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerActions>();
        _playerMantle = GetComponent<PlayerMantle>();
        _lassoTetherController = GetComponent<LassoTetherController>();
        _lasso = GetComponent<Lasso>();
        _jointTetherActivator = GetComponent<JointTetherActivator>();
        _jointTetherPlacer = GetComponent<JointTetherPlacer>();

        _lasso.OnObjectHit += SetLassoBool;
        _jointTetherActivator.OnTetherActivated += SetTetherBool;
        _playerMantle.OnMantle += SetMantleBool;
    }

    private void OnDisable()
    {
        _lasso.OnObjectHit -= SetLassoBool;
        _jointTetherActivator.OnTetherActivated -= SetTetherBool;
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


        //ground check 
        if (_playerController.IsGrounded())
        {
            animator.SetBool("IsGrounded", true);
        }
        else
        {
            animator.SetBool("IsGrounded", false);
        }

        //check if laso is currently active
        if (_lassoTetherController.CurrentLassoState == LassoState.Snared)
        {
            animator.SetBool("LassoSnared", true);
        }
        else
        {
            animator.SetBool("LassoSnared", false);
        }

        //check tether
        if (_jointTetherPlacer.didStartPointHit)
        {
            animator.SetBool("TetherStartPointHit", true);
        }
        else
        {
            animator.SetBool("TetherStartPointHit", false);
        }

        if (_jointTetherPlacer.didEndPointHit)
        {
            animator.SetBool("TetherEndPointHit", true);
        }
        else
        {
            animator.SetBool("TetherEndPointHit", false);
        }

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

    private void SetMantleBool(bool start)
    {
        animator.SetBool("Mantling", start);
    }

    private IEnumerator ResetBoolNextFrame(string boolName)
    {
        yield return new WaitForSeconds(0.1f);
        animator.SetBool(boolName, false);
    }
}

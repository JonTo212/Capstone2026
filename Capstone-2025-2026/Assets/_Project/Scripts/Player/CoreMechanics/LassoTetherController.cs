using TMPro;
using UnityEngine;

public enum LassoState
{
    Empty,
    Snared,
    Tethering,
    SnaredTether,
    Swinging,
    PlayerYanking,
    ObjectYanking,
    Held,
    Rotating,
    Using
}

public class LassoTetherController : MonoBehaviour
{
    [Header("TEMPORARY - Control UI")]
    [SerializeField] private TMP_Text controlsText;

    [Header("Components")]
    private Lasso playerLasso;
    private PlayerActions playerActions;
    private JointTetherPlacer playerTether;
    private JointTetherActivator playerTetherActivator;

    [Header("Object Manipulation Mode")]
    [SerializeField] private bool useObjectManipulationMode;
    private bool wasUsingPhysicsLasso;
    private Vector3 cachedHitPos;

    [Header("States")]
    [field: SerializeField] public LassoState CurrentLassoState { get; private set; }

    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
        playerActions = GetComponent<PlayerActions>();
        playerTether = GetComponent<JointTetherPlacer>();
        playerTetherActivator = GetComponent<JointTetherActivator>();

        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
        playerLasso.OnSnapFinished += HandleSnapFinish;
        playerTether.OnTetherStartHit += OnTetherStartHit;

        wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;

        TempSetText(LassoState.Empty);
    }

    private void OnDisable()
    {
        playerLasso.OnLassoReleased -= OnLassoReleased;
        playerLasso.OnObjectHit -= OnLassoHit;
        playerTether.OnTetherStartHit -= OnTetherStartHit;
    }

    private void Update()
    {
        switch (CurrentLassoState)
        {
            case LassoState.Empty:
                HandleEmptyControls();
                break;

            case LassoState.Snared:
                HandleSnaredControls();
                break;

            case LassoState.Tethering:
                HandleTetherPlacementControls();
                break;

            case LassoState.SnaredTether:
                HandleSnaredTetherControls();
                break;

            case LassoState.Swinging:
                HandleSwingingControls();
                break;

            case LassoState.Rotating:
                HandleRotatingControls();
                break;
        }
        HandleTetherActivation();
        HandleTetherDestroy();
    }

    private void FixedUpdate()
    {
        if (CurrentLassoState == LassoState.Snared)
        {
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
            playerLasso.AnchorToObject();
        }
        else if(CurrentLassoState == LassoState.Rotating)
        {
            if (playerLasso.useSnapRotation)
            {
                playerLasso.MaintainObjectRotation();
            }
            else
            {
                playerLasso.RotateWithInput(playerActions.LookInput, useObjectManipulationMode);
            }
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
        }
        else if(CurrentLassoState == LassoState.SnaredTether)
        {
            if (playerLasso.Rotated)
            {
                playerLasso.MaintainObjectRotation();
            }
        }
    }

    #endregion

    #region Helper Functions
    private void SwitchLassoState(LassoState newState)
    {
        CurrentLassoState = newState;

        TempSetText(newState);
    }

    private void OnLassoReleased()
    {
        SwitchLassoState(LassoState.Empty);
    }

    private void OnLassoHit()
    {
        if(playerLasso.SnaredObject.TryGetComponent(out SwingPoint swingPoint))
        {
            playerLasso.HandleSwingSetup();
            SwitchLassoState(LassoState.Swinging);
        }
        else
        {
            playerLasso.HandleAnchorStart();
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void OnTetherStartHit()
    {
        SwitchLassoState(LassoState.Tethering);
    }

    #endregion

    #region Empty Controls
    private void HandleEmptyControls()
    {
        if (playerActions.MainDown)
        {
            playerLasso.HandleLassoStart();
        }
        if (playerActions.AltDown)
        {
            playerTether.StartTetherPlacement();
        }
    }
    #endregion

    #region Tether Controls
    private void HandleTetherPlacementControls()
    {
        if (playerActions.AltUp)
        {
            playerTether.EndTetherPlacement(false);
            SwitchLassoState(LassoState.Empty);
        }
    }

    private void HandleTetherActivation()
    {
        if (playerActions.InteractDown)
        {
            playerTetherActivator.StartActivateTether();
        }
        if (playerActions.InteractUp)
        {
            playerTetherActivator.EndActivateTether();
        }
    }

    private void HandleTetherDestroy()
    {
        if (playerActions.CrouchDown)
        {
            playerTetherActivator.StartDestroyTether();
        }
        if (playerActions.CrouchUp)
        {
            playerTetherActivator.EndDestroyTether();
        }

    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
        playerLasso.MoveAnchorPoint(playerActions.GetDPadScrollValue());

        if (playerActions.AltDown)
        {
            playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
            SwitchLassoState(LassoState.SnaredTether);
        }

        if (playerActions.MainUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.HandleObjectReleased();
        }

        if(playerActions.SprintDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.BeginCenterPivot();
                playerLasso.HitPos = playerLasso.SnaredObject.transform.position;
                wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;
                playerLasso.usePhysicsLasso = false;

                if (playerLasso.useSnapRotation)
                {
                    playerLasso.InitializeRotationToClosestSnap();
                }
                else
                {
                    playerLasso.camInputController.enabled = false;
                }
            }
            else
            {
                playerLasso.camInputController.enabled = false;
            }
            SwitchLassoState(LassoState.Rotating);
        }
    }

    #endregion

    #region Snared Tether Controls
    private void HandleSnaredTetherControls()
    {
        if (playerActions.AltUp)
        {
            playerTether.EndTetherPlacement(true);
            playerLasso.HandleObjectReleased();
            SwitchLassoState(LassoState.Empty);
        }
    }
    #endregion

    #region Swinging Controls

    private void HandleSwingingControls()
    {
        if (playerActions.MainDown)
        {
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.JumpDown)
        {
            playerLasso.SwingJumpBoost();
        }
    }

    #endregion

    #region Rotating Controls
    private void HandleRotatingControls()
    {
        playerLasso.MoveAnchorPoint(playerActions.GetDPadScrollValue());

        if (playerLasso.useSnapRotation)
        {
            if (playerActions.InteractDown)
            {
                playerLasso.ApplySnapRotation(Vector3.right);
            }
            if (playerActions.PreviousDown)
            {
                playerLasso.ApplySnapRotation(Vector3.up);
            }

            if (playerActions.SprintDown)
            {
                playerLasso.StartFinishSnap();
            }
        }
        else
        {
            if (playerActions.SprintDown)
            {
                if (useObjectManipulationMode)
                {
                    playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
                    playerLasso.RestorePivot();
                }
                playerLasso.camInputController.enabled = true;
                SwitchLassoState(LassoState.Snared);
            }
        }

        if (playerActions.MainUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.camInputController.enabled = true;
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.AltDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
            SwitchLassoState(LassoState.SnaredTether);
        }
    }

    private void HandleSnapFinish()
    {
        if (CurrentLassoState == LassoState.Rotating)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }

            playerLasso.camInputController.enabled = true;
            playerLasso.RestorePivot();
            SwitchLassoState(LassoState.Snared);
        }
    }
    #endregion

    private void TempSetText(LassoState state)
    {
        if (state == LassoState.Empty)
        {
            controlsText.text = "[LMB]: Start Lasso\nHold [RMB]: Start Tether";
        }

        if (state == LassoState.Tethering)
        {
            controlsText.text = "Release [RMB]: Set Tether End";
        }

        if (state == LassoState.Snared)
        {
            controlsText.text = "Hold [LMB]: Move Object\nRelease [LMB]: Drop Object\n[RMB]: Pull Object\nHold [RMB]: Start Tether";
        }

        if (state == LassoState.SnaredTether)
        {
            controlsText.text = "Release [RMB]: Set Tether End";
        }
    }
}

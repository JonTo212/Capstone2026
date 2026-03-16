using System.Collections;
using TMPro;
using UnityEngine;

public enum LassoState
{
    Empty,
    Snared,
    Tethering,
    SnaredTether,
    TetherMode,
    Swinging,
    ObjectYanking,
    Held,
    FreeRotating,
    Using
}

public class LassoTetherController : MonoBehaviour
{
    [Header("Components")]
    private Lasso playerLasso;
    private PlayerNPCCapture playerNPCCapture;
    private PlayerActions playerActions;
    private JointTetherPlacer playerTether;
    private JointTetherActivator playerTetherActivator;
    private PlayerSwing playerSwing;

    [Header("Object Manipulation Mode")]
    [SerializeField] private bool useObjectManipulationMode;
    private bool wasUsingPhysicsLasso;
    public bool TetherMode = false;

    [Header("States")]
    public bool rodPickedUp = true;
    public bool tetherPickedUp = true;
    [field: SerializeField] public LassoState CurrentLassoState { get; private set; }
    public Lasso Lasso => playerLasso;

    public bool rodEquipped = true;
    public bool CanUseTools { get; private set; }

    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
        playerActions = GetComponent<PlayerActions>();
        //playerInventory = GetComponent<PlayerNPCHolder>();
        playerNPCCapture = GetComponent<PlayerNPCCapture>();
        playerTether = GetComponent<JointTetherPlacer>();
        playerTetherActivator = GetComponent<JointTetherActivator>();
        playerSwing = GetComponent<PlayerSwing>();

        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
        playerTether.OnTetherStartHit += OnTetherStartHit;
        playerNPCCapture.OnObjectYankCompleted += OnObjectYankCompleted;

        wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;
    }

    private void OnDisable()
    {
        playerLasso.OnLassoReleased -= OnLassoReleased;
        playerLasso.OnObjectHit -= OnLassoHit;
        playerTether.OnTetherStartHit -= OnTetherStartHit;
    }

    private void Update()
    {
        if (rodPickedUp)
        {
            float currentRange = rodEquipped ? playerLasso.MaxLassoRange : playerTether.MaxTetherStartRange;
            playerLasso.CheckNearbyTargets(true, currentRange);

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

                case LassoState.TetherMode:
                    HandleTetherModeControls();
                    break;

                case LassoState.Swinging:
                    HandleSwingingControls();
                    break;

                case LassoState.ObjectYanking:
                    HandleYankingControls();
                    break;

                case LassoState.FreeRotating:
                    HandleFreeRotateControls();
                    break;
            }
            HandleTetherActivation();
            HandleTetherDestroy();
        }
    }

    private void FixedUpdate()
    {
        if (CurrentLassoState == LassoState.Snared)
        {
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
        }
        else if (CurrentLassoState == LassoState.FreeRotating)
        {
            playerLasso.RotateWithInput(playerActions.LookInput, useObjectManipulationMode);
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
        }
    }

    #endregion

    #region Helper Functions
    private void SwitchLassoState(LassoState newState)
    {
        CurrentLassoState = newState;
    }

    private void OnLassoReleased()
    {
        SwitchLassoState(LassoState.Empty);
    }

    private void OnLassoHit()
    {
        if (playerLasso.SnaredObject.TryGetComponent(out SwingPoint swingPoint))
        {
            playerLasso.HandleSwingSetup();
            SwitchLassoState(LassoState.Swinging);
        }
        else
        {
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void OnTetherStartHit()
    {
        SwitchLassoState(LassoState.Tethering);
    }

    private void OnObjectYankCompleted()
    {
        playerLasso.HandleHold();
        SwitchLassoState(LassoState.Empty);
    }

    #endregion

    #region Empty Controls
    private void HandleEmptyControls()
    {
        //tool switching
        if ((playerActions.toolSwitchDown) && tetherPickedUp) rodEquipped = !rodEquipped; // toggle state of rodEquipped bool 
        //playerLasso.CheckNearbyTargets(!rodEquipped);

        if (playerActions.LassoDown && rodEquipped)
        {
            playerLasso.HandleLassoStart();
        }
        if (playerActions.LassoDown && !rodEquipped && tetherPickedUp) // temporarily making it check for lasso input so they can use the same button
        {
            playerTether.StartTetherPlacement();
        }
    }
    #endregion

    #region Tether Controls
    private void HandleTetherPlacementControls()
    {
        if (playerActions.LassoUp)
        {
            playerTether.EndTetherPlacement(false);
            SwitchLassoState(LassoState.Empty);
        }
    }

    private void HandleTetherActivation()
    {
        if (playerActions.ActivateTetherDown)
        {
            playerTetherActivator.StartActivateTether();
        }
        if (playerActions.ActivateTetherUp)
        {
            playerTetherActivator.EndActivateTether();
        }
    }

    private void HandleTetherDestroy()
    {
        if (playerActions.DeactivateTetherDown)
        {
            playerTetherActivator.StartDestroyTether();
        }
        if (playerActions.DeactivateTetherUp)
        {
            playerTetherActivator.EndDestroyTether();
        }

    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
        playerLasso.MoveAnchorPointZ(playerActions.GetDPadScrollValue());
        playerLasso.MoveAnchorPointY(playerActions.LookInput.y);

        if (playerActions.toolSwitchDown)
        {
            if (TetherMode)
            {
                playerTether.EnterTetherMode(playerLasso.SnaredObject);
                playerLasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezePosition);
                playerLasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
                SwitchLassoState(LassoState.TetherMode);
            }
            else
            {
                playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
                playerLasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezeAll);
                playerLasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
                SwitchLassoState(LassoState.SnaredTether);
            }
        }

        if (playerActions.LassoUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.FreeRotateToggleDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.BeginCenterPivot();
                playerLasso.HitPos = playerLasso.SnaredObject.transform.position;
                wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;
                playerLasso.usePhysicsLasso = false;
            }
            playerLasso.SnaredObject.DisableJointTemp();
            playerLasso.SetRotating(true);
            SwitchLassoState(LassoState.FreeRotating);
        }
    }

    #endregion

    #region Snared Tether Controls
    private void HandleSnaredTetherControls()
    {
        if (playerActions.LassoUp)
        {
            playerLasso.SnaredObject.SetRigidbodyConstraints(null);
            playerTether.EndTetherPlacement(false);
            playerLasso.HandleObjectReleased();
            SwitchLassoState(LassoState.Empty);
        }
    }

    private void HandleTetherModeControls()
    {
        playerTether.HandleTetherMode();
        if (playerActions.PlaceTetherDown)
        {
            playerTether.TetherModeStartTetherPlacement();
        }
        if (playerActions.PlaceTetherUp)
        {
            playerTether.TetherModeEndTetherPlacement();
        }
        if (playerActions.LassoUp)
        {
            playerLasso.SnaredObject.SetRigidbodyConstraints(null);
            playerTether.ExitTetherMode();
            playerLasso.HandleObjectReleased();
            SwitchLassoState(LassoState.Empty);
        }
    }

    public void ToggleAlowTetherModeActivation()
    {
        TetherMode = !TetherMode;
    }
    #endregion

    #region Swinging Controls

    private void HandleSwingingControls()
    {
        playerLasso.MoveAnchorPointZ(playerActions.GetDPadScrollValue());
        playerSwing.AdjustRopeLength(playerActions.GetDPadScrollValue());

        if (playerActions.LassoUp)
        {
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.JumpDown)
        {
            playerLasso.SwingJumpBoost();
        }
    }

    #endregion

    #region Yanking Controls

    private void HandleYankingControls()
    {
        if (playerActions.LassoDown)
        {
            playerLasso.HandleObjectReleased();
        }
    }

    #endregion

    #region Free Rotate Controls

    private void HandleFreeRotateControls()
    {
        if (playerActions.FreeRotateToggleDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
                playerLasso.RestorePivot();
            }

            if (playerLasso.SnaredObject.IsTetherPulled)
            {
                playerLasso.SnaredObject.UpdateTetherGrabPointsAndLockRotation();
            }

            playerLasso.SnaredObject.EnableJoint();
            //playerLasso.camInputController.enabled = true;
            playerLasso.SetRotating(false);
            SwitchLassoState(LassoState.Snared);
        }

        if (playerActions.LassoUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }

            if (playerLasso.SnaredObject.IsTetherPulled)
            {
                playerLasso.SnaredObject.UpdateTetherGrabPointsAndLockRotation();
            }

            playerLasso.SnaredObject.EnableJoint();
            //playerLasso.camInputController.enabled = true;
            playerLasso.SetRotating(false);
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.PlaceTetherDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }

            if (playerLasso.SnaredObject.IsTetherPulled)
            {
                playerLasso.SnaredObject.UpdateTetherGrabPointsAndLockRotation();
            }

            playerLasso.SnaredObject.EnableJoint();
            playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
            playerLasso.SetRotating(false);
            SwitchLassoState(LassoState.SnaredTether);
        }
    }

    #endregion
}
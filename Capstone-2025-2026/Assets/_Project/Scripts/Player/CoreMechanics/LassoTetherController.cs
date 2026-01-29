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
    PlayerYanking,
    ObjectYanking,
    Held,
    SnapRotating,
    FreeRotating,
    Using
}

public class LassoTetherController : MonoBehaviour
{
    [Header("TEMPORARY - Control UI")]
    [SerializeField] private TMP_Text controlsText;

    [Header("Components")]
    private Lasso playerLasso;
    private PlayerNPCHolder playerInventory;
    private PlayerActions playerActions;
    private JointTetherPlacer playerTether;
    private JointTetherActivator playerTetherActivator;

    [Header("Object Manipulation Mode")]
    [SerializeField] private bool useObjectManipulationMode;
    private bool wasUsingPhysicsLasso;
    private Vector3 cachedHitPos;
    public bool TetherMode = false;

    [Header("States")]
    public bool rodPickedUp = true;
    public bool tetherPickedUp = true;
    [field: SerializeField] public LassoState CurrentLassoState { get; private set; }

    public bool rodEquipped = true;
    //public bool tetherEquipped = true;

    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
        playerActions = GetComponent<PlayerActions>();
        playerInventory = GetComponent<PlayerNPCHolder>();
        playerTether = GetComponent<JointTetherPlacer>();
        playerTetherActivator = GetComponent<JointTetherActivator>();

        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
        playerLasso.OnSnapFinished += HandleSnapFinish;
        playerTether.OnTetherStartHit += OnTetherStartHit;
        playerInventory.OnObjectYankCompleted += OnObjectYankCompleted;

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
        if (rodPickedUp)
        {
            if (playerActions.RecallNPCDown)
            {
                if (playerInventory.CurrentNPC == null) return;

                playerInventory.HandleObjectYank();
                SwitchLassoState(LassoState.ObjectYanking);
                return;
            }

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

                case LassoState.SnapRotating:
                    HandleSnapRotateControls();
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
            playerLasso.AnchorToObject();
        }
        else if(CurrentLassoState == LassoState.SnapRotating)
        {
            playerLasso.MaintainObjectRotation();
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
        }
        else if(CurrentLassoState == LassoState.FreeRotating)
        {
            playerLasso.RotateWithInput(playerActions.LookInput, useObjectManipulationMode);
            playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
        }
        else if (CurrentLassoState == LassoState.SnaredTether)
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
        if ((playerActions.toolSwitchDown) && (tetherPickedUp)) rodEquipped = !rodEquipped; // toggle state of rodEquipped bool 


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
        playerLasso.MoveAnchorPoint(playerActions.GetDPadScrollValue());

        if (playerActions.PlaceTetherHeld)
        {
            if (TetherMode)
            {
                playerTether.EnterTetherMode(playerLasso.SnaredObject);
                playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
                SwitchLassoState(LassoState.TetherMode);
            }
            //else
            //{
            //    playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
            //    playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
            //    SwitchLassoState(LassoState.SnaredTether);
            //}
        }

        if (playerActions.LassoUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.HandleObjectReleased();
        }

        if(playerActions.SnapRotateToggleDown) //fix for free rotate
        {
            if (useObjectManipulationMode)
            {
                playerLasso.BeginCenterPivot();
                playerLasso.HitPos = playerLasso.SnaredObject.transform.position;
                wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;
                playerLasso.usePhysicsLasso = false;
                playerLasso.InitializeRotationToClosestSnap();
            }
            else
            {
                playerLasso.camInputController.enabled = false;
            }
            SwitchLassoState(LassoState.SnapRotating);
        }
        if(playerActions.FreeRotateToggleDown)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.BeginCenterPivot();
                playerLasso.HitPos = playerLasso.SnaredObject.transform.position;
                wasUsingPhysicsLasso = playerLasso.usePhysicsLasso;
                playerLasso.usePhysicsLasso = false;
                playerLasso.camInputController.enabled = false;
            }
            else
            {
                playerLasso.camInputController.enabled = false;
            }
            SwitchLassoState(LassoState.FreeRotating);
        }
    }

    #endregion

    #region Snared Tether Controls
    private void HandleSnaredTetherControls()
    {
        if (playerActions.PlaceTetherUp)
        {
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.None;
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
            Debug.Log("Alt down");
            playerTether.TetherModeStartTetherPlacement();
        }
        if (playerActions.PlaceTetherUp)
        {
            Debug.Log("Alt Up");
            playerTether.TetherModeEndTetherPlacement();
        }
        if (playerActions.LassoUp)
        {
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.None;
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
        if (playerActions.LassoUp)
        {
            playerLasso.SwingJumpBoost();
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

    #region Snap Rotate Controls
    private void HandleSnapRotateControls()
    {
        if (playerActions.DPadForwardDown)
        {
            playerLasso.ApplySnapRotation(Vector3.right);
        }
        if (playerActions.DPadBackwardDown)
        {
            playerLasso.ApplySnapRotation(Vector3.left);
        }
        if (playerActions.DPadRightDown)
        {
            playerLasso.ApplySnapRotation(Vector3.up);
        }
        if (playerActions.DPadLeftDown)
        {
            playerLasso.ApplySnapRotation(Vector3.down);
        }

        if (playerActions.SnapRotateToggleDown)
        {
            playerLasso.StartFinishSnap();
        }
        
        if (playerActions.LassoUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.camInputController.enabled = true;
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.PlaceTetherDown)
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
        if (CurrentLassoState == LassoState.SnapRotating)
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
            playerLasso.camInputController.enabled = true;
            SwitchLassoState(LassoState.Snared);
        }

        if (playerActions.LassoUp)
        {
            if (useObjectManipulationMode)
            {
                playerLasso.usePhysicsLasso = wasUsingPhysicsLasso;
            }
            playerLasso.camInputController.enabled = true;
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.PlaceTetherDown)
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

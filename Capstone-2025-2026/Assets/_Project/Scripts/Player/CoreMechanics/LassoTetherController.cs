using System;
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
    Swinging
}

public class LassoTetherController : MonoBehaviour
{
    [Header("Object Manipulation Mode")]
    public bool TetherMode = false;
    [SerializeField, Range (0,1)] private float reelSpeedMultiplier;

    [Header("States")]
    public bool rodPickedUp = true;
    public bool tetherPickedUp = true;
    public bool RodEnabled { get; private set; }
    public bool TetherEnabled { get; private set; }
    [field: SerializeField] public LassoState CurrentLassoState { get; private set; }

    public bool rodEquipped = true;
    public bool CanUseTools { get; private set; }
    private Coroutine holdDelayCoroutine;

    [Header("Auto-Equip")]
    private bool autoEquippedRod = false;
    private bool previousRodEquipped;

    public event Action OnRodSwap;

    [Header("particles")]
    [SerializeField] private ParticleSystem rodSparkParticles;


    #region Unity Functions
    private void Start()
    {
        PlayerRefData.Instance.Lasso.OnLassoReleased += OnLassoReleased;
        PlayerRefData.Instance.Lasso.OnObjectHit += OnLassoHit;
        PlayerRefData.Instance.JointTetherPlacer.OnTetherStartHit += OnTetherStartHit;
        PlayerRefData.Instance.PlayerNPCCapture.OnObjectYankCompleted += OnObjectYankCompleted;

        TetherEnabled = false;
        RodEnabled = false;
    }

    private void OnDisable()
    {
        PlayerRefData.Instance.Lasso.OnLassoReleased -= OnLassoReleased;
        PlayerRefData.Instance.Lasso.OnObjectHit -= OnLassoHit;
        PlayerRefData.Instance.JointTetherPlacer.OnTetherStartHit -= OnTetherStartHit;
        PlayerRefData.Instance.PlayerNPCCapture.OnObjectYankCompleted -= OnObjectYankCompleted;
    }

    private void Update()
    {
        if (rodPickedUp)
        {
            HandleAutoEquip();

            float currentRange = rodEquipped && RodEnabled ? PlayerRefData.Instance.Lasso.MaxLassoRange : PlayerRefData.Instance.JointTetherPlacer.MaxTetherStartRange;
            PlayerRefData.Instance.Lasso.HandleHighlight(true, currentRange);

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

            }
            HandleTetherActivation();
            HandleTetherDestroy();
        }
    }

    private void FixedUpdate()
    {
        if (CurrentLassoState == LassoState.Snared)
        {
            PlayerRefData.Instance.Lasso.MoveObjectToPos(PlayerRefData.Instance.Lasso.GetAnchoredCenterOfScreen());
        }
    }

    #endregion

    #region Helper Functions
    private void HandleAutoEquip()
    {
        if (CurrentLassoState != LassoState.Empty) return;

        bool inRopeCutscene = CameraRefData.Instance.CameraCutsceneHandler.CurrentCutscene is RopeSwingCutscene && CameraRefData.Instance.CameraCutsceneHandler.IsPlaying();
        bool lookingAtAutoTarget = PlayerRefData.Instance.Lasso.LookingAtAutoEquipTarget || inRopeCutscene;

        if (lookingAtAutoTarget && !autoEquippedRod)
        {
            previousRodEquipped = rodEquipped;
            rodEquipped = true;
            autoEquippedRod = true;
        }
        else if (!lookingAtAutoTarget && autoEquippedRod)
        {
            rodEquipped = previousRodEquipped;
            autoEquippedRod = false;
        }
    }

    private void SwitchLassoState(LassoState newState)
    {
        CurrentLassoState = newState;
    }

    private void OnLassoReleased()
    {
        SwitchLassoState(LassoState.Empty);
    }
    private void OnTetherStartHit()
    {
        SwitchLassoState(LassoState.Tethering);
    }

    private void OnLassoHit()
    {
        if (PlayerRefData.Instance.Lasso.SnaredObject == null) return;

        if (PlayerRefData.Instance.Lasso.SnaredObject.TryGetComponent(out SwingPoint swingPoint))
        {
            PlayerRefData.Instance.Lasso.HandleSwingSetup();
            SwitchLassoState(LassoState.Swinging);
        }
        else
        {
            PlayerRefData.Instance.PlayerMovement.KillVelocity();
            PlayerRefData.Instance.Lasso.SnaredObject.OnPropDestroyed += ClearHold;
            PlayerRefData.Instance.PlayerMovement.SetGrabbing(true);
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void OnObjectYankCompleted()
    {
        PlayerRefData.Instance.Lasso.HandleHold();
        ClearHold();
        SwitchLassoState(LassoState.Empty);
    }

    public void SetTetherState(bool enabled)
    {
        TetherEnabled = enabled;
    }

    public void SetLassoState(bool enabled)
    {
        RodEnabled = enabled;
    }

    public void ClearHold()
    {
        if (HandlePluckoutDelay()) return;

        if (PlayerRefData.Instance.Lasso.SnaredObject != null)
        {
            PlayerRefData.Instance.Lasso.SnaredObject.OnPropDestroyed -= ClearHold;
            PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            PlayerRefData.Instance.Lasso.HandleObjectReleased();
        }

        PlayerRefData.Instance.JointTetherPlacer.EndTetherPlacement(false, true);
        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);

        SwitchLassoState(LassoState.Empty);
    }

    private bool HandlePluckoutDelay()
    {
        bool PluckOut = PlayerRefData.Instance.Lasso.SnaredObject is BreakablePluckupProp;
        if (PluckOut)
        {
            var PluckOutObject = PlayerRefData.Instance.Lasso.SnaredObject as BreakablePluckupProp;

            if (holdDelayCoroutine != null)
                StopCoroutine(holdDelayCoroutine);

            holdDelayCoroutine = StartCoroutine(WaitForHoldDelay(PluckOutObject.pluckDelay));
            return true;
        }
        return false;
    }

    private IEnumerator WaitForHoldDelay(float delay)
    {
        PlayerRefData.Instance.Lasso.SnaredObject.OnPropDestroyed -= ClearHold;
        PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(null);

        PlayerRefData.Instance.Lasso.HandleObjectReleased();
        PlayerRefData.Instance.JointTetherPlacer.EndTetherPlacement(false, true);

        yield return new WaitForSeconds(delay);

        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
    }

    #endregion

    #region Empty Controls
    private void HandleEmptyControls()
    {
        //tool switching
        if (PlayerActions.Instance.toolSwitchDown && tetherPickedUp)
        {
            rodEquipped = !rodEquipped;
            autoEquippedRod = false; // player took manual control, cancel auto-restore
            OnRodSwap?.Invoke();
        }

        //Make rod spark if you havent gotten tether yet
        if (PlayerActions.Instance.toolSwitchDown && !tetherPickedUp)
        {
            if (rodSparkParticles !=null) rodSparkParticles.Play();
            print("SPARK");
        }

        if (PlayerActions.Instance.LassoDown && rodEquipped && RodEnabled)
        {
            PlayerRefData.Instance.Lasso.HandleLassoStart();

        }
        if (PlayerActions.Instance.LassoDown && !rodEquipped && tetherPickedUp && TetherEnabled) // temporarily making it check for lasso input so they can use the same button
        {
            PlayerRefData.Instance.JointTetherPlacer.StartTetherPlacement();
        }
    }
    #endregion

    #region Tether Controls
    private void HandleTetherPlacementControls()
    {
        if (PlayerActions.Instance.LassoUp)
        {
            PlayerRefData.Instance.JointTetherPlacer.EndTetherPlacement(false, false);
            SwitchLassoState(LassoState.Empty);
        }
    }

    private void HandleTetherActivation()
    {
        if (PlayerActions.Instance.ActivateTetherDown)
        {
            PlayerRefData.Instance.JointTetherActivator.StartActivateTether();
        }
        if (PlayerActions.Instance.ActivateTetherUp)
        {
            PlayerRefData.Instance.JointTetherActivator.EndActivateTether();
        }
    }

    private void HandleTetherDestroy()
    {
        if (PlayerActions.Instance.DeactivateTetherDown)
        {
            PlayerRefData.Instance.JointTetherActivator.StartDestroyTether();
        }
        if (PlayerActions.Instance.DeactivateTetherUp)
        {
            PlayerRefData.Instance.JointTetherActivator.EndDestroyTether();
        }

    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
        //PlayerRefData.Instance.LassoMoveAnchorPointZ(PlayerActions.Instance.GetDPadScrollValue());
        PlayerRefData.Instance.Lasso.MoveAnchorPointZ(PlayerActions.Instance.MoveInput.y * 0.25f);
        PlayerRefData.Instance.Lasso.MoveAnchorPointY(PlayerActions.Instance.LookInput.y);
        Vector3 dir = PlayerRefData.Instance.Lasso.SnaredObject.transform.position - transform.position;
        dir.y = 0;
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(Quaternion.LookRotation(dir), true);

        if (PlayerActions.Instance.toolSwitchDown && tetherPickedUp)
        {
            if (TetherMode)
            {
                PlayerRefData.Instance.JointTetherPlacer.EnterTetherMode(PlayerRefData.Instance.Lasso.SnaredObject);
                PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezePosition);
                PlayerRefData.Instance.Lasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
                SwitchLassoState(LassoState.TetherMode);
            }
            else
            {
                PlayerRefData.Instance.JointTetherPlacer.StartTetherPlacement(PlayerRefData.Instance.Lasso.SnaredObject.transform, PlayerRefData.Instance.Lasso.HitPos);
                PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezeAll);
                PlayerRefData.Instance.Lasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
                SwitchLassoState(LassoState.SnaredTether);
            }
        }

        if (PlayerActions.Instance.LassoUp)
        {
            ClearHold();
        }
    }

    #endregion

    #region Snared Tether Controls
    private void HandleSnaredTetherControls()
    {
        if (PlayerActions.Instance.LassoUp)
        {
            if (PlayerRefData.Instance.Lasso.SnaredObject != null)
            {
                PlayerRefData.Instance.Lasso.SnaredObject.OnPropDestroyed -= ClearHold;
                PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(null);
                PlayerRefData.Instance.Lasso.HandleObjectReleased();
            }

            PlayerRefData.Instance.JointTetherPlacer.EndTetherPlacement(false, false);
            PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);

            SwitchLassoState(LassoState.Empty);
        }

        if (PlayerActions.Instance.toolSwitchDown)
        {
            if(PlayerRefData.Instance.Lasso.SnaredObject != null) PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            PlayerRefData.Instance.JointTetherPlacer.EndTetherPlacement(false, true);
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void HandleTetherModeControls()
    {
        PlayerRefData.Instance.JointTetherPlacer.HandleTetherMode();
        if (PlayerActions.Instance.PlaceTetherDown)
        {
            PlayerRefData.Instance.JointTetherPlacer.TetherModeStartTetherPlacement();
        }
        if (PlayerActions.Instance.PlaceTetherUp)
        {
            PlayerRefData.Instance.JointTetherPlacer.TetherModeEndTetherPlacement();
        }
        if (PlayerActions.Instance.LassoUp)
        {
            PlayerRefData.Instance.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            PlayerRefData.Instance.JointTetherPlacer.ExitTetherMode();
            PlayerRefData.Instance.Lasso.HandleObjectReleased();
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
        PlayerRefData.Instance.Lasso.MoveAnchorPointZ(PlayerActions.Instance.GetDPadScrollValue());
        PlayerRefData.Instance.PlayerSwing.AdjustRopeLength(PlayerActions.Instance.GetDPadScrollValue());

        if (PlayerActions.Instance.LassoUp)
        {
            ClearHold();
            PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
        }

        if (PlayerActions.Instance.JumpDown)
        {
            PlayerRefData.Instance.PlayerSwing.SwingJumpBoost();
            PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
        }
    }

    #endregion
}
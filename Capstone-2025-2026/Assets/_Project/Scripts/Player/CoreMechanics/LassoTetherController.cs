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
    private PlayerRefData _playerRefData;

    [Header("Object Manipulation Mode")]
    public bool TetherMode = false;
    [SerializeField, Range (0,1)] private float reelSpeedMultiplier;

    [Header("States")]
    public bool rodPickedUp = true;
    public bool tetherPickedUp = true;
    [field: SerializeField] public LassoState CurrentLassoState { get; private set; }

    public bool rodEquipped = true;
    public bool CanUseTools { get; private set; }

    [Header("Auto-Equip")]
    private bool autoEquippedRod = false;
    private bool previousRodEquipped;

    #region Unity Functions
    private void Awake()
    {
        _playerRefData = GetComponent<PlayerRefData>();

        _playerRefData.Lasso.OnLassoReleased += OnLassoReleased;
        _playerRefData.Lasso.OnObjectHit += OnLassoHit;
        _playerRefData.JointTetherPlacer.OnTetherStartHit += OnTetherStartHit;
        _playerRefData.PlayerNPCCapture.OnObjectYankCompleted += OnObjectYankCompleted;
    }

    private void OnDisable()
    {
        _playerRefData.Lasso.OnLassoReleased -= OnLassoReleased;
        _playerRefData.Lasso.OnObjectHit -= OnLassoHit;
        _playerRefData.JointTetherPlacer.OnTetherStartHit -= OnTetherStartHit;
        _playerRefData.PlayerNPCCapture.OnObjectYankCompleted -= OnObjectYankCompleted;
    }

    private void Update()
    {
        if (rodPickedUp)
        {
            HandleAutoEquip();

            float currentRange = rodEquipped ? _playerRefData.Lasso.MaxLassoRange : _playerRefData.JointTetherPlacer.MaxTetherStartRange;
            _playerRefData.Lasso.HandleHighlight(true, currentRange);

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
            _playerRefData.Lasso.MoveObjectToPos(_playerRefData.Lasso.GetAnchoredCenterOfScreen());
        }
    }

    #endregion

    #region Helper Functions
    private void HandleAutoEquip()
    {
        if (CurrentLassoState != LassoState.Empty) return;

        bool lookingAtAutoTarget = _playerRefData.Lasso.LookingAtAutoEquipTarget;

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
        if (_playerRefData.Lasso.SnaredObject != null && _playerRefData.Lasso.SnaredObject.TryGetComponent(out SwingPoint swingPoint))
        {
            _playerRefData.Lasso.HandleSwingSetup();
            SwitchLassoState(LassoState.Swinging);
        }
        else
        {
            _playerRefData.PlayerMovement.SetGrabbing(true);
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void OnObjectYankCompleted()
    {
        _playerRefData.Lasso.HandleHold();
        SwitchLassoState(LassoState.Empty);
    }

    public void ClearHold()
    {
        if (_playerRefData.Lasso.SnaredObject != null)
        {
            _playerRefData.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            _playerRefData.Lasso.HandleObjectReleased();
        }

        _playerRefData.JointTetherPlacer.EndTetherPlacement(false, true);
        _playerRefData.PlayerMovement.SetGrabbing(false);

        SwitchLassoState(LassoState.Empty);
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
        }

        if (PlayerActions.Instance.LassoDown && rodEquipped)
        {
            _playerRefData.Lasso.HandleLassoStart();

        }
        if (PlayerActions.Instance.LassoDown && !rodEquipped && tetherPickedUp) // temporarily making it check for lasso input so they can use the same button
        {
            _playerRefData.JointTetherPlacer.StartTetherPlacement();
        }
    }
    #endregion

    #region Tether Controls
    private void HandleTetherPlacementControls()
    {
        if (PlayerActions.Instance.LassoUp)
        {
            _playerRefData.JointTetherPlacer.EndTetherPlacement(false, false);
            SwitchLassoState(LassoState.Empty);
        }
    }

    private void HandleTetherActivation()
    {
        if (PlayerActions.Instance.ActivateTetherDown)
        {
            _playerRefData.JointTetherActivator.StartActivateTether();
        }
        if (PlayerActions.Instance.ActivateTetherUp)
        {
            _playerRefData.JointTetherActivator.EndActivateTether();
        }
    }

    private void HandleTetherDestroy()
    {
        if (PlayerActions.Instance.DeactivateTetherDown)
        {
            _playerRefData.JointTetherActivator.StartDestroyTether();
        }
        if (PlayerActions.Instance.DeactivateTetherUp)
        {
            _playerRefData.JointTetherActivator.EndDestroyTether();
        }

    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
        //_playerRefData.LassoMoveAnchorPointZ(PlayerActions.Instance.GetDPadScrollValue());
        _playerRefData.Lasso.MoveAnchorPointZ(PlayerActions.Instance.MoveInput.y * 0.25f);
        _playerRefData.Lasso.MoveAnchorPointY(PlayerActions.Instance.LookInput.y);
        Vector3 dir = _playerRefData.Lasso.SnaredObject.transform.position - transform.position;
        dir.y = 0;
        _playerRefData.PlayerModelRotationHandler.SetNewRotationDir(Quaternion.LookRotation(dir), true);

        if (PlayerActions.Instance.toolSwitchDown && tetherPickedUp)
        {
            if (TetherMode)
            {
                _playerRefData.JointTetherPlacer.EnterTetherMode(_playerRefData.Lasso.SnaredObject);
                _playerRefData.Lasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezePosition);
                _playerRefData.Lasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
                SwitchLassoState(LassoState.TetherMode);
            }
            else
            {
                _playerRefData.JointTetherPlacer.StartTetherPlacement(_playerRefData.Lasso.SnaredObject.transform, _playerRefData.Lasso.HitPos);
                _playerRefData.Lasso.SnaredObject.SetRigidbodyConstraints(RigidbodyConstraints.FreezeAll);
                _playerRefData.Lasso.SnaredObject.Rb.angularVelocity = Vector3.zero;
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
            ClearHold();
        }

        if (PlayerActions.Instance.toolSwitchDown)
        {
            if(_playerRefData.Lasso.SnaredObject != null) _playerRefData.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            _playerRefData.JointTetherPlacer.EndTetherPlacement(false, true);
            _playerRefData.PlayerMovement.SetGrabbing(false);
            SwitchLassoState(LassoState.Snared);
        }
    }

    private void HandleTetherModeControls()
    {
        _playerRefData.JointTetherPlacer.HandleTetherMode();
        if (PlayerActions.Instance.PlaceTetherDown)
        {
            _playerRefData.JointTetherPlacer.TetherModeStartTetherPlacement();
        }
        if (PlayerActions.Instance.PlaceTetherUp)
        {
            _playerRefData.JointTetherPlacer.TetherModeEndTetherPlacement();
        }
        if (PlayerActions.Instance.LassoUp)
        {
            _playerRefData.Lasso.SnaredObject.SetRigidbodyConstraints(null);
            _playerRefData.JointTetherPlacer.ExitTetherMode();
            _playerRefData.Lasso.HandleObjectReleased();
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
        _playerRefData.Lasso.MoveAnchorPointZ(PlayerActions.Instance.GetDPadScrollValue());
        _playerRefData.PlayerSwing.AdjustRopeLength(PlayerActions.Instance.GetDPadScrollValue());

        if (PlayerActions.Instance.LassoUp)
        {
            ClearHold();
            _playerRefData.PlayerMovement.SetGrabbing(false);
        }

        if (PlayerActions.Instance.JumpDown)
        {
            _playerRefData.PlayerSwing.SwingJumpBoost();
            _playerRefData.PlayerMovement.SetGrabbing(false);
        }
    }

    #endregion
}
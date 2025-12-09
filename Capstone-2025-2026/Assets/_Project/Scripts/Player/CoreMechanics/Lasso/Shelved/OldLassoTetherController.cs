using System.Collections;
using TMPro;
using UnityEngine;

/*public enum LassoState
{
    Empty,
    Snared,
    Tethering,
    SnaredTether,
    Swinging,
    PlayerYanking,
    ObjectYanking,
    Held,
    Using
}

public class OldLassoTetherController : MonoBehaviour
{
    [Header("TEMPORARY - Control UI")]
    [SerializeField] private TMP_Text controlsText;

    [Header("Components")]
    private Lasso playerLasso;
    private LassoYank playerYankController;
    private PlayerActions playerActions;
    private JointTetherPlacer playerTether;
    private JointTetherActivator playerTetherActivator;

    [Header("States")]
    public LassoState CurrentLassoState { get; private set; }
    private Coroutine _yankCheckCoroutine;

    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
        playerYankController = GetComponent<LassoYank>();
        playerActions = GetComponent<PlayerActions>();
        playerTether = GetComponent<JointTetherPlacer>();
        playerTetherActivator = GetComponent<JointTetherActivator>();

        playerYankController.OnObjectYankCompleted += OnObjectYankCompleted;
        playerYankController.OnPlayerYankCompleted += OnPlayerYankCompleted;
        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
        playerTether.OnTetherStartHit += OnTetherStartHit;

        TempSetText(LassoState.Empty);
    }

    private void OnDisable()
    {
        playerYankController.OnObjectYankCompleted -= OnObjectYankCompleted;
        playerYankController.OnPlayerYankCompleted -= OnPlayerYankCompleted;
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

            case LassoState.PlayerYanking:
                HandleYankingControls();
                break;

            case LassoState.ObjectYanking:
                HandleYankingControls();
                break;

            case LassoState.Held:
                HandleHeldControls();
                break;

            case LassoState.Using:
                HandleUsingControls();
                break;
        }
        HandleTetherActivation();
        HandleTetherDestroy();
    }

    private void FixedUpdate()
    {
        if (CurrentLassoState == LassoState.Snared)
        {
            playerYankController.HandleSnapback();
            if (playerActions.MainHeld)
            {
                playerLasso.MoveObjectToPos(playerLasso.GetAnchoredCenterOfScreen());
            }
        }
        else if (CurrentLassoState == LassoState.Using)
        {         
            playerLasso.RotateHeldObject();
        }
    }

    #endregion

    #region Helper Functions
    private void SwitchLassoState(LassoState newState)
    {
        CurrentLassoState = newState;

        TempSetText(newState);
    }

    private void CompareWeightsOnSnare()
    {
        var weight = WeightComparison.CompareObjectWeights(gameObject, playerLasso.SnaredObject.gameObject);
        switch (weight)
        {
            case WeightComparisonResult.Object1:
                SwitchLassoState(LassoState.Snared);
                break;

            case WeightComparisonResult.Object2:
                SwitchLassoState(LassoState.Swinging);
                playerLasso.HandleSwingSetup();
                break;

            case WeightComparisonResult.Equal:
                SwitchLassoState(LassoState.Snared);
                break;
        }
    }

    private void CompareWeightsOnYank()
    {
        var weight = WeightComparison.CompareObjectWeights(gameObject, playerLasso.SnaredObject.gameObject);
        switch (weight)
        {
            case WeightComparisonResult.Object1:
                SwitchLassoState(LassoState.ObjectYanking);
                playerYankController.HandleObjectYank();
                break;

            case WeightComparisonResult.Object2:
                SwitchLassoState(LassoState.PlayerYanking);
                playerYankController.HandlePlayerYank();
                break;

            case WeightComparisonResult.Equal:
                SwitchLassoState(LassoState.ObjectYanking);
                playerYankController.HandleObjectYank();
                break;
        }
    }

    private void OnObjectYankCompleted()
    {
        if (playerLasso.SnaredObject == null)
        {
            SwitchLassoState(LassoState.Empty);
            return;
        }

        if (playerLasso.SnaredObject.TryGetComponent(out IActivatable activatable)) SwitchLassoState(LassoState.Using);
        else SwitchLassoState(LassoState.Held);
    }

    private void OnPlayerYankCompleted()
    {
        playerLasso.HandleObjectReleased();
    }

    private void OnLassoReleased()
    {
        SwitchLassoState(LassoState.Empty);
    }

    private void OnLassoHit()
    {
        CompareWeightsOnSnare();
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
        playerLasso.MoveAnchorPoint(playerActions.ScrollAction);

        if (playerActions.AltDown)
        {
            if (_yankCheckCoroutine != null) StopCoroutine(_yankCheckCoroutine);
            _yankCheckCoroutine = StartCoroutine(CheckIfTap());
        }

        if (playerActions.MainUp)
        {
            playerLasso.HandleObjectReleased();
        }
    }

    private IEnumerator CheckIfTap()
    {
        yield return new WaitForSeconds(0.175f);

        if (playerLasso.SnaredObject == null)
        {
            _yankCheckCoroutine = null;
            playerLasso.HandleObjectReleased();
            yield break;
        }

        if (playerActions.AltHeld)
        {
            SwitchLassoState(LassoState.SnaredTether);
            playerTether.StartTetherPlacement(playerLasso.SnaredObject.transform, playerLasso.HitPos);
            playerLasso.SnaredObject.Rb.constraints = RigidbodyConstraints.FreezePosition;
        }
        else
        {
            CompareWeightsOnYank();
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

        if (playerActions.AltDown)
        {
            CompareWeightsOnYank();
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
        if (playerActions.MainDown)
        {
            playerLasso.HandleObjectReleased();
        }
    }

    #endregion

    #region Held Controls

    private void HandleHeldControls()
    {
        if (playerActions.MainDown)
        {
            playerLasso.HandleObjectReleased();
        }

        if (playerActions.AltDown)
        {
            playerLasso.HandleObjectThrow();
        }
    }

    #endregion

    #region Using Controls

    private void HandleUsingControls()
    {
        if (playerLasso.SnaredObject.TryGetComponent(out IActivatable activatable))
        {
            if (activatable.IsActive && playerActions.MainDown)
            {
                activatable.Activate();
            }
            else if (!activatable.IsActive && playerActions.MainDown)
            {
                playerLasso.HandleObjectReleased();
            }
        }

        if (playerActions.AltDown)
        {
            playerLasso.HandleObjectThrow();
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

        if (state == LassoState.Swinging)
        {
            controlsText.text = "[LMB]: Release Lasso\n[RMB]: Yank Player";
        }

        if (state == LassoState.PlayerYanking || state == LassoState.ObjectYanking)
        {
            controlsText.text = "[LMB]: Release Lasso";
        }

        if (state == LassoState.Held)
        {
            controlsText.text = "[LMB]: Drop Object\n[RMB]: Throw Object";
        }

        if (state == LassoState.Using)
        {
            controlsText.text = "[LMB]: Activate Object\n[RMB]: Throw Object";
        }
    }
}*/

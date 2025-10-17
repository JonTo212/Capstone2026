using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
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
    Using
}

public class LassoTetherController : MonoBehaviour
{
    [Header("TEMPORARY - Control UI")]
    [SerializeField] private TMP_Text m0TapText;
    [SerializeField] private TMP_Text m0HoldText;
    [SerializeField] private TMP_Text m0ReleaseText;
    [SerializeField] private TMP_Text m1TapText;
    [SerializeField] private TMP_Text m1HoldText;
    [SerializeField] private TMP_Text m1ReleaseText;

    [Header("Components")]
    private Lasso playerLasso;
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
        playerActions = GetComponent<PlayerActions>();
        playerTether = GetComponent<JointTetherPlacer>();
        playerTetherActivator = GetComponent<JointTetherActivator>();

        playerLasso.OnObjectYankCompleted += OnObjectYankCompleted;
        playerLasso.OnPlayerYankCompleted += OnPlayerYankCompleted;
        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
        playerTether.OnTetherStartHit += OnTetherStartHit;

        TempSetText(LassoState.Empty);
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
            playerLasso.HandleSnapback();
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
                playerLasso.HandleObjectYank();
                break;

            case WeightComparisonResult.Object2:
                SwitchLassoState(LassoState.PlayerYanking);
                playerLasso.HandlePlayerYank();
                break;

            case WeightComparisonResult.Equal:
                SwitchLassoState(LassoState.ObjectYanking);
                playerLasso.HandleObjectYank();
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
        if(playerActions.AltDown)
        {
            playerTether.StartTetherPlacement();
        }
    }
    #endregion

    #region Tether Controls
    private void HandleTetherPlacementControls()
    {
        if(playerActions.AltUp)
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
        if(playerActions.CrouchDown)
        {
            playerTetherActivator.StartDestroyTether();
        }
        if( playerActions.CrouchUp)
        {
            playerTetherActivator.EndDestroyTether();
        }

    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
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
        if(state == LassoState.Empty)
        {
            m0TapText.text = "M0 (tap): Start Lasso";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): N/A";
            m1HoldText.text = "M1 (hold): Start Tether";
            m1ReleaseText.text = "M1 (release): N/A";
        }

        if(state == LassoState.Tethering)
        {
            m0TapText.text = "M0 (tap): N/A";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): N/A";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): Set Tether Target";
        }

        if(state == LassoState.Snared)
        {
            m0TapText.text = "M0 (tap): N/A";
            m0HoldText.text = "M0 (hold): Move Object";
            m0ReleaseText.text = "M0 (release): Release Object";
            m1TapText.text = "M1 (tap): Yank Object";
            m1HoldText.text = "M1 (hold): Start Tether";
            m1ReleaseText.text = "M1 (release): N/A";
        }

        if(state == LassoState.SnaredTether)
        {
            m0TapText.text = "M0 (tap): N/A";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): N/A";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): Set Tether Target";
        }

        if(state == LassoState.Swinging)
        {
            m0TapText.text = "M0 (tap): Release Lasso";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): Yank Player";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): N/A";
        }

        if(state == LassoState.PlayerYanking || state == LassoState.ObjectYanking)
        {
            m0TapText.text = "M0 (tap): Release Lasso";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): N/A";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): N/A";
        }

        if(state == LassoState.Held)
        {
            m0TapText.text = "M0 (tap): Release Object";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): Throw Object";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): N/A";
        }

        if(state == LassoState.Using)
        {
            m0TapText.text = "M0 (tap): Activate Object";
            m0HoldText.text = "M0 (hold): N/A";
            m0ReleaseText.text = "M0 (release): N/A";
            m1TapText.text = "M1 (tap): Throw Object";
            m1HoldText.text = "M1 (hold): N/A";
            m1ReleaseText.text = "M1 (release): N/A";
        }
    }
}

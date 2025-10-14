using System.Collections;
using UnityEngine;

public enum LassoState
{
    Empty,
    Snared,
    Swinging,
    PlayerYanking,
    ObjectYanking,
    Held,
    Using
}

public class LassoTetherController : MonoBehaviour
{
    [Header("Components")]
    private Lasso playerLasso;
    private PlayerActions playerActions;

    [Header("States")]
    public LassoState CurrentLassoState { get; private set; }

    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
        playerActions = GetComponent<PlayerActions>();

        playerLasso.OnObjectYankCompleted += OnObjectYankCompleted;
        playerLasso.OnPlayerYankCompleted += OnPlayerYankCompleted;
        playerLasso.OnLassoReleased += OnLassoReleased;
        playerLasso.OnObjectHit += OnLassoHit;
    }

    private void Update()
    {
        //print(CurrentLassoState);
        switch (CurrentLassoState)
        {
            case LassoState.Empty:
                HandleEmptyControls();
                break;

            case LassoState.Snared:
                HandleSnaredControls();
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
    }

    private void FixedUpdate()
    {
        if (CurrentLassoState == LassoState.Snared)
        {
            playerLasso.HandleSnapback();
            if (playerActions.MainHeld)
            {
                playerLasso.HandleObjectHoldAtDistance();
            }
        }
    }

    #endregion

    #region Helper Functions
    private void SwitchLassoState(LassoState newState)
    {
        CurrentLassoState = newState;
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
        SwitchLassoState(LassoState.Held);
    }
    
    private void OnPlayerYankCompleted()
    {
        playerLasso.HandleObjectReleased();
    }

    private void OnLassoReleased()
    {
        print("Running");
        SwitchLassoState(LassoState.Empty);
    }

    private void OnLassoHit()
    {
        CompareWeightsOnSnare();
    }

    #endregion

    #region Empty Controls
    private void HandleEmptyControls()
    {
        if (playerActions.MainDown)
        {
            playerLasso.HandleLassoStart();
        }
    }
    #endregion

    #region Snared Controls
    private void HandleSnaredControls()
    {
        if(playerActions.AltDown)
        {
            CompareWeightsOnYank();
        }

        if (playerActions.MainUp)
        {
            playerLasso.HandleObjectReleased();
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
        if(playerActions.MainDown)
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
}

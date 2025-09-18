using System.Collections;
using UnityEngine;

public enum TetherState
{
    Empty,
    Lassoing,
    Tethering,
    Held
}

public class TetherController : MonoBehaviour
{
    private PlayerTether playerTether;
    private UpdatedLasso playerLasso;
    private PlayerActions playerActions;
    private TetherState currentTetherState;
    private Coroutine yankCheckCoroutine;

    [SerializeField] private bool separateControls = false;

    private void Awake()
    {
        playerTether = GetComponent<PlayerTether>();
        playerLasso = GetComponent<UpdatedLasso>();
        playerActions = GetComponent<PlayerActions>();
    }

    private void Update()
    {
        switch (currentTetherState)
        {
            case TetherState.Empty:
                HandleEmptyControls();
                break;

            case TetherState.Lassoing:
                HandleLassoControlsCombined();
                break;

            case TetherState.Tethering:
                HandleTetherControls();
                break;

            case TetherState.Held:
                HandleHeldControls();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentTetherState == TetherState.Lassoing)
        {
            if (playerLasso.YankCoroutine == null)
            {
                playerLasso.MoveObjectToLassoPos(playerLasso.GetCenterOfScreen());
            }
        }
    }

    private void SwitchTetherState(TetherState newState)
    {
        currentTetherState = newState;
    }

    #region Empty Controls
    private void HandleEmptyControls()
    {
        if (playerActions.PullDown)
        {
            playerLasso.TryLasso();
        }

        if (playerLasso.HasSnaredObject)
        {
            SwitchTetherState(TetherState.Lassoing);
        }
    }
    #endregion

    #region Lasso Controls
    private void HandleLassoControlsCombined()
    {
        if (playerActions.PullDown)
        {
            playerLasso.ReleaseObject();
            SwitchTetherState(TetherState.Empty);
        }

        if (playerActions.ThrowDown)
        {
            if(yankCheckCoroutine != null)
                StopCoroutine(yankCheckCoroutine);

            yankCheckCoroutine = StartCoroutine(CheckIfTap());
        }
    }
    private IEnumerator CheckIfTap()
    {
        yield return new WaitForSeconds(0.25f);

        if (playerActions.ThrowHeld)
        {
            playerTether.HandleStartTether(playerLasso.SnaredObject);
            SwitchTetherState(TetherState.Tethering);
        }
        else
        {
            playerLasso.YankObject();
            SwitchTetherState(TetherState.Held);
        }
    }

    #endregion

    #region Tether Controls

    private void HandleTetherControls()
    {
        if (playerActions.ThrowHeld)
        {
            playerTether.HandleTetherActive();
        }

        if (playerActions.ThrowUp)
        {
            playerLasso.ReleaseObject();
            playerTether.HandleEndTether();
            SwitchTetherState(TetherState.Empty);
        }
    }

    #endregion

    #region Held Controls

    private void HandleHeldControls()
    {
        playerLasso.MoveObjectToLassoPos(playerLasso.HoldPos.position);

        if (playerActions.PullDown)
        {
            playerLasso.ReleaseObject();
            SwitchTetherState(TetherState.Empty);
        }

        if (playerActions.ThrowDown)
        {
            playerLasso.ThrowObject();
            SwitchTetherState(TetherState.Empty);
        }
    }

    #endregion
}

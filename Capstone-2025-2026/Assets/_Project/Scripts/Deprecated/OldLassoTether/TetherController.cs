using System.Collections;
using UnityEngine;

/*public enum TetherState
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
                playerLasso.MoveObjectToPos(playerLasso.GetCenterOfScreen());
            }
        }
        else if(currentTetherState == TetherState.Held)
        {
            playerLasso.MoveObjectToPos(playerLasso.HoldPos.position);
        }
    }

    private void SwitchTetherState(TetherState newState)
    {
        currentTetherState = newState;
    }

    #region Empty Controls
    private void HandleEmptyControls()
    {
        if (playerActions.MainDown)
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
        if (playerActions.MainDown)
        {
            playerLasso.ReleaseObject();
            SwitchTetherState(TetherState.Empty);
        }

        if (playerActions.AltDown)
        {
            if(yankCheckCoroutine != null)
                StopCoroutine(yankCheckCoroutine);

            yankCheckCoroutine = StartCoroutine(CheckIfTap());
        }
    }
    private IEnumerator CheckIfTap()
    {
        yield return new WaitForSeconds(0.15f);

        if (playerActions.AltHeld)
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
        if (playerActions.AltHeld)
        {
            playerTether.HandleTetherActive();
        }

        if (playerActions.AltUp)
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
        if (playerActions.MainDown)
        {
            playerLasso.ReleaseObject();
            SwitchTetherState(TetherState.Empty);
        }

        if (playerActions.AltDown)
        {
            playerLasso.ThrowObject();
            SwitchTetherState(TetherState.Empty);
        }
    }

    #endregion
}
*/
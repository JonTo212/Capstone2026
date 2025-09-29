using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SeparateTetherController : MonoBehaviour
{
    private PlayerTether playerTether;
    private UpdatedLasso playerLasso;
    private PlayerActions playerActions;
    private TetherState currentTetherState;
    private Coroutine yankCheckCoroutine;
    private Coroutine tetherActivateCheckCoroutine;

    private void Awake()
    {
        playerTether = GetComponent<PlayerTether>();
        playerLasso = GetComponent<UpdatedLasso>();
        playerActions = GetComponent<PlayerActions>();
    }

    private void Update()
    {
        if (playerLasso.GrappleJoint != null)
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

            if (playerActions.InteractDown)
            {
                tetherActivateCheckCoroutine = StartCoroutine(CheckIfInteractTap());
            }

            if (playerActions.InteractUp)
            {
                if (tetherActivateCheckCoroutine != null)
                {
                    StopCoroutine(tetherActivateCheckCoroutine);
                }

                InteractTether();
            }
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

            HandleLassoPull();
        }
        else if (currentTetherState == TetherState.Held)
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
        if (playerActions.PullDown)
        {
            playerLasso.TryLasso();
        }

        if (playerLasso.HasSnaredObject)
        {
            SwitchTetherState(TetherState.Lassoing);
        }

        if(playerActions.ThrowDown)
        {
            playerTether.HandleStartTether(playerLasso.SnaredObject);
            SwitchTetherState(TetherState.Tethering);
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
            playerLasso.YankObject();
            SwitchTetherState(TetherState.Held);
        }
    }

    private void HandleLassoPull()
    {
        playerLasso.UpdateAnchorDistance(playerActions.ScrollAction);
    }
    private IEnumerator CheckIfTap()
    {
        yield return new WaitForSeconds(0.15f);

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

    private void InteractTether()
    {
        playerTether.ActivateSelectedTether();
    }

    private IEnumerator CheckIfInteractTap()
    {
        yield return new WaitForSeconds(1f);

        playerTether.ActivateAllTether();
    }

    #endregion

    #region Held Controls

    private void HandleHeldControls()
    {
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

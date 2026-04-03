using UnityEngine;

public class TetherTutorialCutscene : CameraCutsceneBase
{
    [SerializeField] private Transform playerPos;
    [SerializeField] private CutsceneBase nextCutscene;

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();

        CameraRefData.Instance.ZeldaCameraController.SetFrozen(true);
        HandleScripts(false);
    }

    public override void OnCutsceneStart()
    {
        //move player to position
        PlayerRefData.Instance.PlayerMovement.Rb.position = playerPos.position;
        PlayerRefData.Instance.transform.position = playerPos.position;

        cam.transform.position = startPos.position;
        cam.transform.LookAt (lookAtTarget.position);

        //player rotate to face camera
        Vector3 direction = cam.transform.position - lookAtTarget.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(rotation,true); //stop player model rotation

        PlayerRefData.Instance.PlayerFade.SetFade(false);
    }

    public override void OnCutsceneEnd()
    {
        CameraRefData.Instance.ZeldaCameraController.SetFrozen(false);
        HandleScripts(true);

        //unfreeze player rotation
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(null, false); //stop player model rotation
        PlayerRefData.Instance.LassoTetherController.SetTetherState(true);
        PlayerRefData.Instance.PlayerFade.SetFade(true);

        if (nextCutscene != null)
        {
            CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(nextCutscene);
        }
    }

}

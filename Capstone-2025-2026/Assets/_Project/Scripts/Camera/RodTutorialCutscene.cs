using UnityEngine;

public class RodTutorialCutscene : CameraCutsceneBase
{
    [SerializeField] private Transform playerPos;

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();

        ZeldaCameraController zeldaCam = cam.GetComponent<ZeldaCameraController>();

        if (zeldaCam != null)
        {
            zeldaCam.SetFrozen(true);
        }

        HandleScripts(false);
    }

    public override void OnCutsceneStart()
    {
        PlayerRefData.Instance.PlayerMovement.SetGrabbing(true);

        //move player to position
        PlayerRefData.Instance.PlayerMovement.Rb.position = playerPos.position;
        PlayerRefData.Instance.transform.position = playerPos.position;

        cam.transform.position = startPos.position;
        cam.transform.LookAt (lookAtTarget.position);

        //player rotate to face camera
        Vector3 direction = cam.transform.position - lookAtTarget.position;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(rotation,true); //stop player model rotation

        PlayerRefData.Instance.PlayerFade.SetFade(false);
    }

    public override void OnCutsceneEnd()
    {
        ZeldaCameraController zeldaCam = cam.GetComponent<ZeldaCameraController>();
    
        if (zeldaCam != null)
        {
            zeldaCam.SetFrozen(false);
        }

        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
        HandleScripts(true);

        //unfreeze player rotation
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(null, false); //stop player model rotation
        PlayerRefData.Instance.LassoTetherController.SetLassoState(true);

        PlayerRefData.Instance.PlayerFade.SetFade(true);
    }

}

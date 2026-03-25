using UnityEngine;

public class RodTutorialCutscene : CameraCutsceneBase
{
    [SerializeField] private CutsceneBase nextCutscene;

    public void Configure(Transform start, Transform lookAt)
    {
        startPos = start;
        lookAtTarget = lookAt;  
    }

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
        PlayerActions.Instance.ChangeSpecificInput("Move",false);
        //PlayerActions.Instance.ChangeSpecificInput("Jump", false);
        PlayerRefData.Instance.PlayerMovement.Rb.linearVelocity=(Vector3.zero); //stop players movement
        PlayerRefData.Instance.PlayerMovement.WishDir = (Vector3.zero); //stop players movemen

        cam.transform.position = startPos.position;
        cam.transform.LookAt (lookAtTarget.position);

        //player rotate to face camera
        Vector3 direction = cam.transform.position - lookAtTarget.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(rotation,true); //stop player model rotation

    }

    public override void OnCutsceneEnd()
    {
        ZeldaCameraController zeldaCam = cam.GetComponent<ZeldaCameraController>();
    
        if (zeldaCam != null)
        {
            zeldaCam.SetFrozen(false);
        }

        PlayerActions.Instance.ChangeSpecificInput("Move", true);
        //PlayerActions.Instance.ChangeSpecificInput("Jump", true);

        HandleScripts(true);

        //unfreeze player rotation
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(null, false); //stop player model rotation

        if (nextCutscene != null)
        {
            CameraCutsceneHandler.Instance.StartCutscene(nextCutscene);
        }



    }

}

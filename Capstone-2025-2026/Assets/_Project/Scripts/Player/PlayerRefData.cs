using UnityEngine;

public class PlayerRefData : MonoBehaviour
{
    public PlayerMovement PlayerMovement { get; private set; }
    public PlayerModelRotationHandler PlayerModelRotationHandler { get; private set; }
    public PlayerSwing PlayerSwing { get; private set; }
    public PlayerLedgeGrab PlayerLedgeGrab { get; private set; }
    public PlayerNPCCapture PlayerNPCCapture { get; private set; }
    public PlayerRespawn PlayerRespawn { get; private set; }
    public JointTetherActivator JointTetherActivator { get; private set; }
    public JointTetherPlacer JointTetherPlacer { get; private set; }
    public LassoTetherController LassoTetherController { get; private set; }
    public Lasso Lasso { get; private set; }


    private void Awake()
    {
        PlayerMovement = GetComponent<PlayerMovement>();
        PlayerModelRotationHandler = GetComponent<PlayerModelRotationHandler>();
        PlayerSwing = GetComponent<PlayerSwing>();
        PlayerLedgeGrab = GetComponent<PlayerLedgeGrab>();
        PlayerNPCCapture = GetComponent<PlayerNPCCapture>();
        PlayerRespawn = GetComponent<PlayerRespawn>();
        JointTetherActivator = GetComponent<JointTetherActivator>();
        JointTetherPlacer = GetComponent<JointTetherPlacer>();
        LassoTetherController = GetComponent<LassoTetherController>();
        Lasso = GetComponent<Lasso>();
    }
}

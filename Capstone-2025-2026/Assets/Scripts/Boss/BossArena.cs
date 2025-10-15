using UnityEngine;

public class BossArena : MonoBehaviour
{
    public BossAiScript bossAI;
    private void OnCollisionEnter(Collision collision)
    {
        if ((collision.gameObject.TryGetComponent(out PlayerController controller))){
            bossAI.FightStarted = true;
        }
        
    }
}

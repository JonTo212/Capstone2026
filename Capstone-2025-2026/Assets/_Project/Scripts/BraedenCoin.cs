using UnityEngine;

public class BraedenCoin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //destroy
        Destroy(gameObject);
    }
}

using UnityEngine;

public class VesselHighlightBlocker : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] LassoTetherController playerScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player !=null) playerScript = player.GetComponent<LassoTetherController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerScript!= null)
        {
            if (playerScript.tetherPickedUp == true) Destroy(gameObject);
        }
    }
}

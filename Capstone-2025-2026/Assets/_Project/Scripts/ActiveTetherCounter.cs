using UnityEngine;
using UnityEngine.UI;

public class ActiveTetherCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private JointTetherPlacer jointTetherPlacerScript;

    public int availableTethersValue;

    [SerializeField] private RawImage[] tetherImages; 

    [SerializeField] private Color availableColor = new Color (0, 254, 159);
    [SerializeField] private Color unAvailableColor = new Color(24, 0, 31);


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //references
        player = GameObject.FindWithTag("Player");

        jointTetherPlacerScript = player.GetComponent<JointTetherPlacer>();

    }

    // Update is called once per frame
    void Update()
    {
        availableTethersValue = (jointTetherPlacerScript.maxNumOfTethers - jointTetherPlacerScript.numOfTethersPlaced);

        //update tether sprite color depending on available tethers
        for (int i = 0; i < tetherImages.Length; i++)
        {
            if (i< availableTethersValue)
            {
                tetherImages[i].color = availableColor;
            }
            else
            {
                tetherImages[i].color = unAvailableColor;
            }
        }

    }
}

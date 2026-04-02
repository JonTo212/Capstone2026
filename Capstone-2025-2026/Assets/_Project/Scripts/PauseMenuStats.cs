using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuStats : MonoBehaviour
{
    [SerializeField] CollectibleManager collectibleManagerScript;

    [SerializeField] TextMeshProUGUI collectiblesText;
    [SerializeField] TextMeshProUGUI coinsText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        coinsText.text = CollectibleManager.Instance.coins.ToString();
        collectiblesText.text = CollectibleManager.Instance.critColCount.ToString() + "/" + CollectibleManager.Instance.critCount.ToString();
    }
}

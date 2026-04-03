using UnityEngine;


public class scoreManager : MonoBehaviour
{


    [HideInInspector]
    public float currentTimer;
    [HideInInspector]
    public bool timerIsActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        currentTimer = 0f;
        timerIsActive = true;

    }

    // Update is called once per frame
    void Update()
    {

        if (timerIsActive)
        {
            currentTimer += Time.deltaTime;
        }


    }

    public void EndGameSummary ( int coins, int critters)
    {
        timerIsActive = false;

        GlobalVariables.coins = coins;
        GlobalVariables.critters = critters;
        GlobalVariables.time = currentTimer;

    }
}

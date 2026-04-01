using UnityEngine;
using Unity.UI;
using TMPro;
using System;

public class ScoreResults : MonoBehaviour
{
    private int coins;
    private int critters;
    private float time;

    public int coinMult;
    public int critterMult;
    public Vector2 timeMults;

    public TextMeshProUGUI coin_text;
    public TextMeshProUGUI critter_text;
    public TextMeshProUGUI time_text;
    public TextMeshProUGUI score_text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        coins = GlobalVariables.coins;
        critters = GlobalVariables.critters;
        time = GlobalVariables.time;

        Calculate();
    }

    private void Calculate ()
    {
        int coin_score = coins * coinMult;
        int critters_score = critters * critterMult;
        float time_score = (timeMults.x) * Mathf.Log10(time - 1) + timeMults.y;

        TimeSpan digi_time = TimeSpan.FromSeconds(time);

        int score = coin_score + critters_score + (int)time_score;

        coin_text.text = coins + " x " + coinMult + " = " + coin_score;
        critter_text.text = critters +" x " + critterMult + " = " + critters_score;
        time_text.text = digi_time.Minutes.ToString() + ":" + digi_time.Seconds.ToString() + ":" + digi_time.Milliseconds.ToString();

        score_text.text = score.ToString();

    }

}

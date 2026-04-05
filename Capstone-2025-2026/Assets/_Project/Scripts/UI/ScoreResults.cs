using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ScoreResults : MonoBehaviour
{
    private int coins;
    private int critters;
    private float time;

    public int RankCCeiling;
    public int RankBCeiling;
    public int RankACeiling;

    public int coinMult;
    public int critterMult;
    public Vector2 timeMults;

    public TextMeshProUGUI coin_text;
    public TextMeshProUGUI critter_text;
    public TextMeshProUGUI time_text;
    public TextMeshProUGUI score_text;

    public Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        coins = GlobalVariables.coins;
        critters = GlobalVariables.critters;
        time = GlobalVariables.time;

        FMODUnity.RuntimeManager.PlayOneShot("event:/MenuMusic");

        Calculate();
    }

    private void Calculate()
    {
        int coin_score = coins * coinMult;
        int critters_score = critters * critterMult;
        float time_score = (timeMults.x) * Mathf.Log10(time - 1) + timeMults.y;

        TimeSpan digi_time = TimeSpan.FromSeconds(time);

        int score = coin_score + critters_score + (int)time_score;

        coin_text.text = coins + " x " + coinMult + " = " + coin_score;
        critter_text.text = critters + " x " + critterMult + " = " + critters_score;
        time_text.text = digi_time.Minutes.ToString() + ":" + digi_time.Seconds.ToString() + ":" + digi_time.Milliseconds.ToString();

        score_text.text = score.ToString();


        if (score < RankCCeiling)
        {
            animator.Play("RankV");
            FMODUnity.RuntimeManager.PlayOneShot("event:/C Rank");
        }
        else if (score >= RankCCeiling && score < RankBCeiling)
        {
            animator.Play("RankV");
            FMODUnity.RuntimeManager.PlayOneShot("event:/B Rank");
        }
        else if (score >= RankBCeiling && score < RankACeiling)
        {
            animator.Play("RankV");
            FMODUnity.RuntimeManager.PlayOneShot("event:/A Rank");
        }
        else
        {
            animator.Play("RankV");
            FMODUnity.RuntimeManager.PlayOneShot("event:/V Rank");
        }
    }


}

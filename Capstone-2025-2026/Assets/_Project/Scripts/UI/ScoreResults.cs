using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class ScoreResults : MonoBehaviour
{
    public enum RankAnims
    {
        CRank,
        BRank,
        ARank,
        VRank
    }

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

    public Image current_badge;
    public Sprite[] badges;

    public Animator animator;
    public AnimationClip[] VRankMotions;
    public AnimationClip[] ARankMotions;
    public AnimationClip[] BRankMotions;
    public AnimationClip[] CRankMotions;
    private AnimatorOverrideController overrideController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        coins = GlobalVariables.coins;
        critters = GlobalVariables.critters;
        time = GlobalVariables.time;

        FMODUnity.RuntimeManager.PlayOneShot("event:/MenuMusic");
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
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
        time_text.text = digi_time.Minutes.ToString() + ":" + digi_time.Seconds.ToString() + "." + digi_time.Milliseconds.ToString();

        score_text.text = score.ToString();


        if (score < RankCCeiling)
        {
            SwapClip("CRank", GetRandomClip(RankAnims.CRank));
            animator.Play("CRank");
            current_badge.sprite = GetBadge(RankAnims.CRank);
            FMODUnity.RuntimeManager.PlayOneShot("event:/C Rank");
        }
        else if (score >= RankCCeiling && score < RankBCeiling)
        {
            SwapClip("BRank", GetRandomClip(RankAnims.BRank));
            animator.Play("BRank");
            current_badge.sprite = GetBadge(RankAnims.BRank);
            FMODUnity.RuntimeManager.PlayOneShot("event:/B Rank");
        }
        else if (score >= RankBCeiling && score < RankACeiling)
        {
            SwapClip("ARank", GetRandomClip(RankAnims.ARank));
            animator.Play("ARank");
            current_badge.sprite = GetBadge(RankAnims.ARank);
            FMODUnity.RuntimeManager.PlayOneShot("event:/A Rank");
        }
        else
        {
            SwapClip("VRank", GetRandomClip(RankAnims.VRank));
            animator.Play("VRank");
            current_badge.sprite = GetBadge(RankAnims.VRank);
            FMODUnity.RuntimeManager.PlayOneShot("event:/V Rank");
        }
    }

    private AnimationClip GetRandomClip(RankAnims rank)
    {
        int random = 0;
        AnimationClip selectedMotion = null;

        switch(rank)
        {
            case RankAnims.CRank:
                random = UnityEngine.Random.Range(0, CRankMotions.Length);
                selectedMotion = CRankMotions[random];
                break;

            case RankAnims.BRank:
                random = UnityEngine.Random.Range(0, BRankMotions.Length);
                selectedMotion = BRankMotions[random];
                break;

            case RankAnims.ARank:
                random = UnityEngine.Random.Range(0, ARankMotions.Length);
                selectedMotion = ARankMotions[random];
                break;

            case RankAnims.VRank:
                random = UnityEngine.Random.Range(0, VRankMotions.Length);
                selectedMotion = VRankMotions[random];
                break;
        }

        return selectedMotion;
    }

    private Sprite GetBadge(RankAnims rank)
    {
        Sprite image = null;

        switch (rank)
        {
            case RankAnims.CRank:
                image = badges[0];
                break;

            case RankAnims.BRank:
                image = badges[1];
                break;

            case RankAnims.ARank:
                image = badges[2];
                break;

            case RankAnims.VRank:
                image = badges[3];
                break;
        }

        return image;
    }

    private void SwapClip(string originalClipName, AnimationClip newClip)
    {
        overrideController[originalClipName] = newClip;
    }
}

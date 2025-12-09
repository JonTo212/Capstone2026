using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SimpleAnimaticManager : MonoBehaviour
{

    public Sprite[] frames;
    public float[] duration;
    public AudioClip[] frameFX;

    private Image targetImage;
    private int frameIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        targetImage = GetComponentInChildren<Image>();

        PlayNextFrame();

    }

    private void PlayNextFrame()
    {


        targetImage.sprite = frames[frameIndex];

        //play sound effect
        float curframeDuration = duration[frameIndex];


        StartCoroutine(FrameDuration());



    }

    private IEnumerator FrameDuration()
    {
        yield return new WaitForSeconds(duration[frameIndex]);

        frameIndex++;

        if (frameIndex < frames.Length)
        {
            PlayNextFrame();
        }
        else { EndAnimatic(); }
    }

    // Update is called once per frame
    void Update()
    {
        //if get input
        //end animatic
    }

    private void EndAnimatic()
    {
        SceneManager.LoadScene(2);
    }
}

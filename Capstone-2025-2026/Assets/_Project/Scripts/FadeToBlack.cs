using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FadeToBlack : MonoBehaviour
{
    public Image blackImage; // Assign in inspector
    public Color imageColor;

    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 2f;

    public bool blackOut = false;
    public bool fullBlack = false;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //set color and alpha
        imageColor = Color.black;
        imageColor.a = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //fade to black
        blackImage.color = imageColor;

        //clamp alpha
        imageColor.a = Mathf.Clamp(imageColor.a, 0f, 2f); // alpha only goes to 1 but this is to avoid the teleport triggering early

        if (blackOut) 
        {
            imageColor.a += Time.deltaTime * fadeInSpeed;

            if (imageColor.a >= 2f) fullBlack = true;
        }
        else 
        {
            fullBlack = false;
            imageColor.a -= Time.deltaTime * fadeOutSpeed;
        }


        //print(imageColor.a);
    }
}

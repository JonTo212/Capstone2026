using UnityEngine;

//Scriptable Object holding data that differentiates each critter

[CreateAssetMenu(fileName = "CritterColllectible", menuName = "Scriptable Objects/CritterColllectible")]
public class CritterColllectible : ScriptableObject
{

    public GameObject CritterModelPrefab;
    public Sprite CritterStampSilhouette;
    public Sprite CritterStampImg;

}

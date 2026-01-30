using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class UIImageMovement : MonoBehaviour
{
    public GameObject objects;

    public RawImage RodIcon;
    public RawImage TetherIcon;
    public RawImage SwapIcon;

    public Texture RodSprite; // needed to store the origional sprites
    public Texture TetherSprite; // needed to store the origional sprites

    public LassoTetherController lassoTetherControllerScript;
    public PlayerActions playerActionsScript;
    public Transform location1;
    public Transform location2;

    private float timeToMove = 0.5f; //time in seconds to move between points

    void Update()
    {
        //disable tether icon untill its picked up
        if (!lassoTetherControllerScript.tetherPickedUp)
        {
            objects.SetActive(false);
            return;
        }
        else
        {
            objects.SetActive(true);
            TetherIcon.texture = TetherSprite;
        }

        if (playerActionsScript.toolSwitchDown)
        {
            //playsound
            AudioManager.Instance.PlaySFX(AudioManager.Instance.MenuOk, 10, 1);

            RotateIcon();

        }

        if (lassoTetherControllerScript.rodEquipped == true)
        {
            RodEquip();
        }
        else
        {
            TetherEquip();
        }
    }


    void RodEquip()
    {
        //Equip Rod
        RodIcon.transform.DOMove(location1.position, timeToMove, false);
        RodIcon.transform.DOScale(location1.localScale, timeToMove);
        RodIcon.DOColor(new Color(1, 1, 1), timeToMove);

        //Unequip Tether
        TetherIcon.transform.DOMove(location2.position, timeToMove, false);
        TetherIcon.transform.DOScale(location2.localScale, timeToMove);
        TetherIcon.DOColor(new Color(1, 1, 1, 0.6f), timeToMove);
    }

    void TetherEquip()
    {
        if (lassoTetherControllerScript.tetherPickedUp)
        {
            //Equip Tether
            TetherIcon.transform.DOMove(location1.position, timeToMove, false);
            TetherIcon.transform.DOScale(location1.localScale, timeToMove);
            TetherIcon.DOColor(new Color(1, 1, 1), timeToMove);

            //Unequip Rod
            RodIcon.transform.DOMove(location2.position, timeToMove, false);
            RodIcon.transform.DOScale(location2.localScale, timeToMove);
            RodIcon.DOColor(new Color(1, 1, 1, 0.6f), timeToMove);
        }
        else
        {
            print("No Tether Equipt");
        }

    }

    void RotateIcon()
    {
        //swap icon rotate
        SwapIcon.transform.DORotate(new Vector3(0, 0, SwapIcon.transform.rotation.eulerAngles.z - 360f), timeToMove, RotateMode.FastBeyond360);

        //play different animation if no tether equipped

    }
}

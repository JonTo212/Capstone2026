using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointTetherActivator : MonoBehaviour
{
    [Header("External Components")]
    private Camera _playerCamera;

    [Header("Properties")]
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private float activationRange = 50f;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;
    public List<JointTether> placedTethers = new List<JointTether>();

    [Header("Coroutines")]
    private Coroutine activateAllTethersCoroutine;
    private Coroutine destroyAllTethersCoroutine;

    public AudioManager aManage;

    #region Unity Functions
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        _playerCamera = Camera.main;
        placedTethers = gameObject.GetComponent<JointTetherPlacer>().placedTethers;
    }
    void Update()
    {
        JointTether hoverTether = TryGetTether();
        if (hoverTether != null && hoverTether.isActivated == false)
        {
            hoverTether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorSelected();
        }
        else
        {
            foreach(JointTether tether in placedTethers)
            {
                if (!tether.isActivated) {
                    tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorInactive();
                }
            }
        }
        
    }
    #endregion

    #region Tether Activation
    public void StartActivateTether()
    {
        ActivateSelectedTether();

        if(destroyAllTethersCoroutine == null)
        {
            activateAllTethersCoroutine = StartCoroutine(ActivateAllTether());
        }
    }

    public void EndActivateTether()
    {
        if (activateAllTethersCoroutine == null) { return; }
        StopCoroutine(activateAllTethersCoroutine);
        activateAllTethersCoroutine = null;
    }
    private void ActivateSelectedTether()
    {
        
        JointTether tether = TryGetTether();
        if (tether != null)
        {
            
            aManage.PlaySFX(aManage.TetherTighten, 4, 1f);
            tether.ActivateTether();
            tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
        }
    }

    IEnumerator ActivateAllTether()
    {
        yield return new WaitForSeconds(timeToActivateAllTethers);
        
        
        foreach (JointTether tether in placedTethers)
        {
            aManage.PlaySFX(aManage.TetherTighten, 4, 1f);
            tether.ActivateTether();
            tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
        }
    }
    #endregion

    #region Tether Destroy
    public void StartDestroyTether()
    {
        DestroySelectedTether();

        if(activateAllTethersCoroutine == null )
        {
            destroyAllTethersCoroutine= StartCoroutine(DestroyAllTether());
        }
    }

    public void EndDestroyTether()
    {
        if (destroyAllTethersCoroutine == null) { return; }
        StopCoroutine(destroyAllTethersCoroutine);
        destroyAllTethersCoroutine = null;
    }

    private void DestroySelectedTether()
    {
        JointTether tether = TryGetTether();
        if (tether != null)
        {
            tether.DestroyTether();
        }
    }
    IEnumerator DestroyAllTether()
    {
        yield return new WaitForSeconds(timeToDestroyAllTethers);

        List<JointTether> allPlacedTethers = new List<JointTether>(placedTethers);

        foreach (JointTether tether in allPlacedTethers)
        {
            tether.DestroyTether();
        }
    }
    #endregion

    #region Helper Functions
    private JointTether TryGetTether()
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if(Physics.Raycast(ray, out RaycastHit hit, 1000f, tetherLayerMask, QueryTriggerInteraction.Collide))
        {
            return hit.transform.GetComponent<JointTether>();
        }
        else
        {
            return null;
        }
    }
    #endregion
}

using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointTetherActivator : MonoBehaviour
{
    [Header("External Components")]
    private Camera _playerCamera;
    [SerializeField] private GameObject tetherRetrieveVisialsPrefab;

    [Header("Properties")]
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private float activationRange = 50f;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;
    public List<JointTether> placedTethers = new List<JointTether>();
    [SerializeField] private JointTether currentSelectedTether = null;
    [SerializeField] private JointTether previousTether = null;

    [Header("Coroutines")]
    private Coroutine activateAllTethersCoroutine;
    private Coroutine destroyAllTethersCoroutine;

    //TEMPORARY ANIMATION EVENT
    public event Action OnTetherActivated;

    //MVG BRAEDEN INPUT STUFF
    public bool isLookingAtTether = false;
    public bool isLookingAtActiveTether = false;


    #region Unity Functions
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerCamera = Camera.main;
        placedTethers = gameObject.GetComponent<JointTetherPlacer>().placedTethers;
    }
    void Update()
    {
        currentSelectedTether = TryGetTether();

        if (currentSelectedTether != null)
        {
            isLookingAtTether = true;

            if (currentSelectedTether.isActivated)
            {
                isLookingAtActiveTether = true;
            }
            else
            {
                isLookingAtActiveTether = false;
            }
        }
        else
        {
            // MVG BRAEDEN INPUT STUFF
            if(placedTethers.Count >= 1) currentSelectedTether = placedTethers[placedTethers.Count - 1];
            isLookingAtTether = false;
            isLookingAtActiveTether = false;
        }

        if (currentSelectedTether != null)
        {
            currentSelectedTether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorSelected();

            if(previousTether != null && currentSelectedTether != previousTether)
            {
                previousTether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
            }
        }
        previousTether = currentSelectedTether;

    }
    #endregion

    #region Tether Activation
    public void StartActivateTether()
    {
        /*ActivateSelectedTether();

        if(destroyAllTethersCoroutine == null)
        {
            activateAllTethersCoroutine = StartCoroutine(ActivateAllTether());
        }*/

        ActivateAllTethers(); // TEMP
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

            RuntimeManager.PlayOneShot("event:/TetherActivate", transform.position);
            tether.ActivateTether();

            OnTetherActivated?.Invoke();
            tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
        }
    }

    IEnumerator ActivateAllTether()
    {
        yield return new WaitForSeconds(timeToActivateAllTethers);
        
        
        foreach (JointTether tether in placedTethers)
        {
            RuntimeManager.PlayOneShot("event:/TetherActivate", transform.position);
            tether.ActivateTether();

            OnTetherActivated?.Invoke();
            tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
        }
    }

    private void ActivateAllTethers() // TEMP
    {
        foreach (JointTether tether in placedTethers)
        {
            //RuntimeManager.PlayOneShot("event:/TetherActivate", transform.position);  FOR SOME REASON THIS CAUSES SOUND TO PLAY WHEN YOU DESTROY, NOT ACTIVATE
            tether.ActivateTether();

            OnTetherActivated?.Invoke();
            tether.gameObject.GetComponent<JointTetherVisuals>().SetLineColorActive();
        }
    }

    #endregion

    #region Tether Destroy
    public void StartDestroyTether()
    {
        DestroySelectedTether(currentSelectedTether);
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
            GameObject tetherRetrievalVisuals = Instantiate(tetherRetrieveVisialsPrefab, tether.transform.position, Quaternion.Euler(Vector3.zero));
            tetherRetrievalVisuals.GetComponent<TetherRetrievalEffect>().Init(tether.transform.position, transform);
            tether.DestroyTether();
        }
    }

    public void DestroySelectedTether(JointTether tether)
    {
        if (tether != null)
        {
            RuntimeManager.PlayOneShot("event:/TetherRecall", transform.position);
            GameObject tetherRetrievalVisuals = Instantiate(tetherRetrieveVisialsPrefab, tether.transform.position, Quaternion.Euler(Vector3.zero));
            tetherRetrievalVisuals.GetComponent<TetherRetrievalEffect>().Init(tether.transform.position, transform);
            tether.DestroyTether();
        }
    }

    IEnumerator DestroyAllTether()
    {
        yield return new WaitForSeconds(timeToDestroyAllTethers);

        List<JointTether> allPlacedTethers = new List<JointTether>(placedTethers);

        foreach (JointTether tether in allPlacedTethers)
        {
            DestroySelectedTether(tether);
        }
    }

    private void DestroyMostRecentTether()
    {
        JointTether mostRecentTether = placedTethers[placedTethers.Count - 1];
        DestroySelectedTether(mostRecentTether);
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

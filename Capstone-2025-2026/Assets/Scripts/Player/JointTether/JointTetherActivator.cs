using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointTetherActivator : MonoBehaviour
{
    private Camera _playerCamera;
    private PlayerActions _playerActions;

    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private float activationRange = 50f;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;
    private List<JointTether> placedTethers = new List<JointTether>();

    [Header("Coroutines")]
    private Coroutine activateAllTethersCoroutine;
    private Coroutine destroyAllTethersCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placedTethers = gameObject.GetComponent<JointTetherPlacer>().placedTethers;
        _playerCamera = Camera.main;
        _playerActions = gameObject.GetComponent<PlayerActions>();
    }

    private void Update()
    {
        if(_playerActions.InteractDown)
        {
            StartActivateTether();
        }
        if(_playerActions.InteractUp)
        {
            EndActivateTether();
        }
        if(_playerActions.CrouchDown)
        {
            StartDestroyTether();
        }
        if(_playerActions.CrouchUp)
        {
            EndDestroyTether();
        }
    }

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

    private void ActivateSelectedTether()
    {
        JointTether tether = TryGetTether();
        if(tether != null)
        {
            tether.ActivateTether();
        }
    }

    private void DestroySelectedTether()
    {
        JointTether tether = TryGetTether();
        if (tether != null)
        {
            tether.DestroyTether();
        }
    }

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

    IEnumerator ActivateAllTether()
    {
        yield return new WaitForSeconds(timeToActivateAllTethers);

        foreach (JointTether tether in placedTethers)
        {
            tether.ActivateTether();
        }
    }

    IEnumerator DestroyAllTether()
    {
        yield return new WaitForSeconds(timeToDestroyAllTethers);

        foreach (JointTether tether in placedTethers)
        {
            tether.DestroyTether();
        }

        placedTethers = null;
    }
}

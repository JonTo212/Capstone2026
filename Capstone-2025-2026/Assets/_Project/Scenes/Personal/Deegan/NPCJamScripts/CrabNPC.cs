using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class CrabNPC : MonoBehaviour
{
    private NPCPropBase _prop;
    private NavMeshAgent _agent;
    private Rigidbody _rb;

    public Transform target;

    private bool canReEnableAgent = false;

    Coroutine enableAgentCoroutine = null;
    Coroutine snipAfterDelayCoroutine = null;

    void Start()
    {
        _prop = GetComponent<NPCPropBase>();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        _agent.SetDestination(GetDestinationFromADistance());
    }

    // Update is called once per frame
    void Update()
    {
        if(_prop.IsSnared || _prop.IsHeld || _prop.isTetherPulled)
        {
            if(snipAfterDelayCoroutine == null)
            {
                snipAfterDelayCoroutine = StartCoroutine(SnipAfterDelay(0.3f));
            }
        }

        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if(_prop.IsHeld || _prop.isTetherPulled || _prop.IsSnared)
        {
            DeactivateNavMeshAgent();
        }
        else
        {
            HandleMovementAtEdge();
        }
        
    }

    private void HandleMovementAtEdge()
    {
        if(_agent.isOnNavMesh)
        {
            _agent.SetDestination(GetDestinationFromADistance());

            _agent.FindClosestEdge(out NavMeshHit hit);
            if (Vector3.Distance(hit.position, transform.position) < 0.2f)
            {
                DeactivateNavMeshAgent();

                Vector3 speed = (target.position - transform.position).normalized * _agent.speed;
                _rb.linearVelocity = new Vector3(speed.x, _rb.linearVelocity.y, speed.z);
            }
            else if(Physics.Raycast(transform.position, new Vector3(0, -1, 0), 0.5f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore) == false)
            {
                DeactivateNavMeshAgent();

                Vector3 speed = (target.position - transform.position).normalized * _agent.speed;
                _rb.linearVelocity = new Vector3(speed.x, _rb.linearVelocity.y, speed.z);
            }
        }
        else
        {
            Vector3 speed = (target.position - transform.position).normalized * _agent.speed;
            _rb.linearVelocity = new Vector3(speed.x, _rb.linearVelocity.y, speed.z);

            if(canReEnableAgent)
            {
                if(Physics.Raycast(transform.position, new Vector3(0,-1,0), 0.5f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
                {
                    EnableNavMeshAgent();
                    canReEnableAgent = false;
                }
            }
        }
    }

    private void DeactivateNavMeshAgent()
    {
        _agent.enabled = false;
        _rb.isKinematic = false;

        if(enableAgentCoroutine == null)
        {
            enableAgentCoroutine = StartCoroutine(EnableNavAgentAfterDelay(0.5f));
        }
    }

    private void EnableNavMeshAgent()
    {
        _agent.enabled = true;
        _rb.isKinematic = true;

        _agent.SetDestination(GetDestinationFromADistance());
    }

    IEnumerator EnableNavAgentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        canReEnableAgent = true;
        enableAgentCoroutine = null;
    }

    private Vector3 GetDestinationFromADistance()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        return target.position - direction * 3f;
    }

    IEnumerator SnipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        _prop.DetachAllTether();
        _prop.DetachLasso();
        snipAfterDelayCoroutine = null;
    }
}

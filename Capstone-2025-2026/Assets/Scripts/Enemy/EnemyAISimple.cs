using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;
using UnityEngine.UIElements;

public class EnemyAISimple : MonoBehaviour
{
    public NavMeshAgent agent;

    public Transform player;

    public LayerMask whatIsGround, whatIsPlayer;

    public bool alive = true;
    public GameObject eyes;

    //Patrolling
    public Material chillMat;
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    //Attacking
    public Material exclamationMat;
    public Material spottedMat;
    public Material angryMat;
    public Material deathMat;
    public string attackMode;
    public float timeBetweenAttacks;
    bool alreadyAttacked;
    public Transform projectileSpawnLocation;
    public GameObject projectile;
    public float meleeStrength;
    public BoxCollider boxHit;

    //States
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;
    public string currentState;

    private void Awake() {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(attackMode.ToLower() == "ranged")
        {
            attackRange = 5f;
            timeBetweenAttacks = 3f;
        }
        else if(attackMode.ToLower() == "melee")
        {
            attackRange = 2f;
            timeBetweenAttacks = 1f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Check for sight and attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        if (alive)
        {
            if (!playerInSightRange && !playerInAttackRange) Patroling();
            else if (playerInSightRange && !playerInAttackRange || alreadyAttacked) ChasePlayer();
            else if (playerInSightRange && playerInAttackRange)
            {
                AttackPlayer();
                Invoke(nameof(ResetAttack), timeBetweenAttacks);
            }
        }
        else
        {
            Perish();
        }

        if(currentState == "patrolling")
        {
            GetComponent<MeshRenderer>().material = chillMat;
        }
        else if(currentState == "chase")
        {
            GetComponent<MeshRenderer>().material = spottedMat;
        }
        else
        {
            GetComponent<MeshRenderer>().material = angryMat;
        }
    }

    #region Patrolling State
    private void Patroling() {
        currentState = "patrolling";
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet) agent.SetDestination(walkPoint);

        GetComponent<NavMeshAgent>().speed = 2f;
        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //WalkPoint Reached
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint() {
        //Calculate random point in range

        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if(Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)){
            walkPointSet = true;
        }


    }
    #endregion

    #region Chase State
    private void ChasePlayer() {

        GetComponent<NavMeshAgent>().speed = 5f;
        currentState = "chase";
        agent.SetDestination(player.position);
        transform.LookAt(player);
        
    }

    #endregion

    #region Attack State

    private void AttackPlayer() {
        //Make sure enemy doesn't move
        currentState = "Attacking";

        agent.SetDestination(transform.position);


        if (!alreadyAttacked)
        {
            //Attack Code Here
            if(attackMode.ToLower() == "ranged")
            {
                Ranged();
            }
            else
            {
                Melee();
            }
            //
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack() {
        alreadyAttacked = false;
    }

    private void Melee()
    {
        Debug.Log("Here is the attack fucer");
        Collider[] arrayOfHits = Physics.OverlapBox(boxHit.bounds.center, (boxHit.bounds.max - boxHit.bounds.min) / 2);
        foreach (Collider collider in arrayOfHits) { 
            if(collider.transform == player.transform)
            {
                collider.gameObject.GetComponent<Rigidbody>().AddForce(transform.up * meleeStrength + transform.forward * meleeStrength, ForceMode.Impulse);
                StartCoroutine(ReEnable(collider.gameObject.GetComponent<PlayerActions>()));
            }
        }
    }

    private void Ranged()
    {
        Rigidbody rb = Instantiate(projectile, projectileSpawnLocation.position, Quaternion.identity).GetComponent<Rigidbody>();

        rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
        rb.AddForce(transform.up * 8f, ForceMode.Impulse);
    }

    IEnumerator ReEnable(PlayerActions controller)
    {
        controller.MoveAction.Disable();
        yield return new WaitForSeconds(1f);
        controller.MoveAction.Enable();
    }
    #endregion


    #region Death State

    private void OnCollisionEnter(Collision collision)
    {
                GameObject hitObject = collision.gameObject;
        if (hitObject.TryGetComponent(out Prop prop))
        {
            float speed = hitObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
            if(speed > 1f)
            {
                alive = false;
            }
        }
    }


    private void Perish() {
        if(currentState != "dying")
        {
            GetComponent<MeshRenderer>().enabled = false;
            eyes.GetComponent<CapsuleCollider>().enabled = true;
            eyes.AddComponent<Rigidbody>();
            agent.SetDestination(transform.position);
            currentState = "dying";
            ParticleSystem partSys = GetComponent<ParticleSystem>();
            GetComponent<ParticleSystemRenderer>().material = deathMat;
            partSys.Play();
            Debug.Log("Enemy has Perished");
            Invoke(nameof(DestroyEnemy), 3f);
        }
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
    #endregion

}

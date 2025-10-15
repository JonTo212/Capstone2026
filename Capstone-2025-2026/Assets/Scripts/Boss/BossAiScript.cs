using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class BossAiScript : MonoBehaviour
{

    public Transform player;

    public int health = 5;
    public GameObject eyes;
    public string attackState;

    public float timeBetweenAttacks;
    public float attackTimer;

    public bool FightStarted;

    public GameObject thingThatmMakesTheProjectilesNotGiant;

    //Attack1
    public Material attack1Mat;
    public GameObject projectile;
    public Transform[] projectileSpawnLocation;
    float projLim;

    //Attack2
    public Material attack2Mat;

    //Waiting
    public Material waiting;

    //Death State
    public Material deathMat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        //Freeze Rotation for tilting up
        if(health <= 0)
        {
            Perish();
        }
        if (FightStarted)
        {
            attackTimer += Time.deltaTime;
            transform.LookAt(player);
            //Set Amount of Projectiles to spawn
            if (health == 5)
            {
                projLim = 1;
            }
            else if (health == 4 || health == 3)
            {
                projLim = 3;
            }
            else
            {
                projLim = 5;
            }

            //Check if ready to Attack
            if (attackTimer>timeBetweenAttacks)
            {
                AttackThrow();
                /*
                if (Random.Range(1, 3) == 1)
                {
                    AttackThrow();
                }
                else
                {
                    AttackBeam();
                }*/
                attackTimer = -10;
            }
        }
    }

    
    #region Attack 1

    public void AttackThrow()
    {
        for (int i = 0; i < projLim; i++)
        {
            StartCoroutine(SpawnProjectile(projectileSpawnLocation[i], i));
        }
    }

    IEnumerator SpawnProjectile(Transform location, float timer)
    {
        yield return new WaitForSeconds(timer);
        GameObject spawnedProjectile = Instantiate(projectile, location.position, Quaternion.identity, thingThatmMakesTheProjectilesNotGiant.transform);
        spawnedProjectile.GetComponent<BossProjectile>().bossRef = gameObject;

    }

    #endregion

    #region Attack 2

    public void AttackBeam()
    {
        
    }

    #endregion

    #region Death State

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;
        if (hitObject.TryGetComponent(out Prop prop))
        {
            float speed = hitObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
            if (speed > 5f)
            {
                health--;
                Destroy(hitObject);
                //Insantiate(ParticleEffect);

            }
        }
    }

    private void Perish()
    {
        if (attackState != "dying")
        {
            GetComponent<MeshRenderer>().enabled = false;
            eyes.GetComponent<BoxCollider>().enabled = true;
            eyes.AddComponent<Rigidbody>();
            attackState = "dying";
            ParticleSystem partSys = GetComponent<ParticleSystem>();
            GetComponent<ParticleSystemRenderer>().material = deathMat;
            partSys.Play();
            Debug.Log("Boss has Perished");
            Invoke(nameof(DestroyBoss), 3f);
        }
    }

    private void DestroyBoss()
    {
        Destroy(gameObject);
    }
    #endregion

}

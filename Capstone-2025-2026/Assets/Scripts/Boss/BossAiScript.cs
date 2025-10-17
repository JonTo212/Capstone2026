using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class BossAiScript : MonoBehaviour
{

    public Transform player;

    public int health = 5;
    private int maxHealth;
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

    public Transform[] armorWalls;
    public Transform shield;
    private bool shieldWasUsed = false;

    [Header("Laser")]
    public float timeToChargeLaser = 4f;
    public float timeToStartRotate = 2f;
    public float timeToFullyRotate = 4;
    public ParticleSystem chargeUpVFX;
    public Transform laserPivot;
    public Transform laser;
    public Transform laserBall;
    public bool usedLaser = false;

    public bool musicStarted = false;
    AudioManager aManage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }
    void Start()
    {
        player = GameObject.Find("Player").transform;
        maxHealth = health;
        laserBall.gameObject.SetActive(false);
        laser.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        //Freeze Rotation for tilting up
        if (health <= 0)
        {
            Perish();
        }
        if (FightStarted)
        {
            if (!musicStarted)
            {
                aManage.FadeMusic(aManage.Track2);
                musicStarted = true;
            }
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
            if (attackTimer > timeBetweenAttacks)
            {
                if (health >= 3)
                {
                    AttackThrow();
                }
                else
                {
                    if (!usedLaser)
                    {
                        AttackBeam();
                    }
                    else
                    {
                        shield.gameObject.SetActive(false);
                        AttackThrow();
                    }
                }
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
        attackTimer = -10;
    }

    IEnumerator SpawnProjectile(Transform location, float timer)
    {
        yield return new WaitForSeconds(timer);
        aManage.PlaySFX(aManage.BossSpawn, 2, 1);
        GameObject spawnedProjectile = Instantiate(projectile, location.position, Quaternion.identity, thingThatmMakesTheProjectilesNotGiant.transform);
        spawnedProjectile.GetComponent<BossProjectile>().bossRef = gameObject;

    }

    #endregion

    #region Attack 2

    public void AttackBeam()
    {
        chargeUpVFX.Play();
        aManage.PlaySFX(aManage.BossLaserCharge, 2, 1);
        attackTimer = -timeToChargeLaser - timeToStartRotate - timeToFullyRotate - 3f;
        StartCoroutine(ChargeUpLaser());
        laserBall.gameObject.SetActive(true);
    }

    IEnumerator ChargeUpLaser()
    {
        yield return new WaitForSeconds(timeToChargeLaser);
        StartCoroutine(HoldLaser());
        laser.gameObject.SetActive(true);
        laserBall.gameObject.SetActive(false);
    }

    IEnumerator HoldLaser()
    {
        yield return new WaitForSeconds(timeToStartRotate);
        StartCoroutine(LaserSpin());
    }

    IEnumerator LaserSpin()
    {
        float timePassed = 0;
        aManage.PlaySFX(aManage.BossLaserFire, 2, 1);
        while(timePassed < timeToFullyRotate)
        {
            timePassed += Time.deltaTime;
            laserPivot.transform.localEulerAngles = new Vector3(0, 90 - (timePassed / timeToFullyRotate) * 360, 0);
            Debug.Log(90 - (timePassed / timeToFullyRotate) * 360);
            yield return null;
        }

        laser.gameObject.SetActive(false);
        chargeUpVFX.Stop();
        usedLaser = true;

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
                aManage.PlaySFX(aManage.BossHurt, 1, 1);
                health--;
                Destroy(hitObject);
                //Insantiate(ParticleEffect);
                if (armorWalls[maxHealth-health] != null)
                {
                    armorWalls[maxHealth-health].gameObject.SetActive(true);
                }
            }

            if (health < 3 && !shieldWasUsed && !usedLaser)
            {
                shield.gameObject.SetActive(true);
            }
        }
    }

    private void Perish()
    {
        if (attackState != "dying")
        {
            aManage.FadeMusic(aManage.Track1);
            aManage.PlaySFX(aManage.BossPerish, 1, 1);
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

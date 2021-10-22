using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectileBallistics;
using RootMotion.Dynamics;


public class Enemy : MonoBehaviour
{
    // Start is called before the first frame update
    public float enemyHealth = 100f;
    HitInfo hitObject;
    public GameObject EnemyPrefab;
    public GameObject EnemyHead;

    public PuppetMaster puppetMasta;
    void Start()
    {
        puppetMasta = gameObject.GetComponent<PuppetMaster>();
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    public void Hit(HitInfo hit)
    {

    }

    public void Damage(float InDamage)
    {
        enemyHealth -= InDamage;
        if(enemyHealth <= 0)
        {
            puppetMasta.Kill();
            Destroy(EnemyPrefab, 10);
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.transform.CompareTag("Enemy"))
        {

        }
    }

    public float getHealth()
    {
        return this.enemyHealth;
    }

    public void setHealth(int health)
    {
        enemyHealth = health;
    }

    public GameObject getHead()
    {
        return this.EnemyHead;
    }
}

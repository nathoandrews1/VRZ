using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.Dynamics;

public class ExplodeBarrel : MonoBehaviour
{
    private int hitCount = 0;
    public float explosiveRadius;
    public float explosivePower;
    public GameObject particleObj;
    public GameObject explodedBarrel;
    public AudioSource expSound;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        explodedBarrel.transform.position = this.transform.position;
        explodedBarrel.transform.rotation = this.transform.rotation;
    }


    public void AddHit()
    {
        this.hitCount++;
    }

    public int getHitCount()
    {
        return this.hitCount;
    }
    /*
   private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag.Equals("Bullet"))
        {
            Debug.Log(hitCount++);
            hitCount += 1;
            Destroy(collision.gameObject);
            if(hitCount >= 3)
            {
                this.gameObject.SetActive(false);
                explodedBarrel.SetActive(true);
                Rigidbody rb = explodedBarrel.gameObject.GetComponent<Rigidbody>();
                rb.AddRelativeForce(-transform.up * 15f, ForceMode.Impulse);
                Explode();
            }
        }
    }
   */

    public void Explode()
    {
        GameObject instancedParticlesObj = Instantiate(particleObj, this.transform.position, this.transform.rotation);
        ParticleSystem[] allParticles = instancedParticlesObj.GetComponentsInChildren<ParticleSystem>();

        AudioSource expInstance = Instantiate(expSound, this.transform.position, this.transform.rotation);
        expInstance.Play();
        Destroy(instancedParticlesObj, 2);
        Destroy(expInstance, 2);
        
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, explosiveRadius);
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponentInChildren<Rigidbody>();
            PuppetMaster puppet = hit.gameObject.GetComponentInParent<PuppetMaster>();

            if (rb != null)
            {
                rb.AddExplosionForce(explosivePower, this.transform.position, explosiveRadius, 50.0f);
                puppet.Kill();
            }
         }
    }
}

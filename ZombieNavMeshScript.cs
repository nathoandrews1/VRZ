using UnityEngine;
using System.Collections;
using RootMotion.Dynamics;

namespace RootMotion.Demos
{
    public class ZombieNavMeshScript : MonoBehaviour
    {
        [Header("This is AI Control, Animation and Attack for the Enemy")]
        public BehaviourPuppet puppet;
        public UnityEngine.AI.NavMeshAgent agent;
        [Header("Only set this if it does not find it automatically")]
        [Tooltip("Make sure to use tag 'PlayerZ' on the player")]
        public Transform target;
        public Animator animator;
        public Transform zombie;
        public Enemy enemy;


        //Start of AI   
        public float distanceToTarget = Mathf.Infinity;
        private bool isProvoked = false;
        public float chaseRange = 5f;
        private float attackDelay = 1.4f;

        //Audio Controls below
        public AudioSource attackSound;
        public AudioSource idleSound;
        public AudioSource deathSound;
        public AudioSource footStepSound;
        private float idleWaitTime = 15f;
        private float idleWaitTime2 = 45f;
        private float timer;
        private bool isWalking = false;

        //Private references
        private bool soundPlaying = false;
        private float tempSpeed;
        private IEnumerator updateRotate;
        private float currentTime;
        //End of Variables

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
        private IEnumerator UpdateRotation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            zombie.transform.rotation = agent.transform.rotation;
        }

        void Start()
        {
            //Storing a temp variable so += doesn't increase to the * 2 each time.
            timer = Random.Range(idleWaitTime, idleWaitTime2);
            soundPlaying = false;
            //Ai references that need to be found on startup of game.
            GameObject[] playerPos = GameObject.FindGameObjectsWithTag("PlayerZ");
            target = playerPos[0].transform;
            tempSpeed = agent.speed;
            agent.speed = 0;
            agent.SetDestination(target.position);
            distanceToTarget = Vector3.Distance(target.position, transform.position);
            currentTime = Time.time;
        }

        void FixedUpdate()
        {
            if (isWalking == true && footStepSound.isPlaying == false)
            {
                Walk();
            }
            else if (!isWalking)
            {
                footStepSound.Stop();
            }
            if (Time.time - currentTime >= timer)
            {
                LoopIdleSound();
                timer += Random.Range(idleWaitTime, idleWaitTime2);
                soundPlaying = true;
                currentTime = Time.time; //reset timer
            }
            if (isProvoked == true)
            {
                if (enemy.enemyHealth <= 0) 
                { 
                    if(!deathSound.isPlaying)
                    {
                        deathSound.Play();
                        deathSound.volume = Random.Range(0.8f, 1.2f);
                        deathSound.pitch = Random.Range(0.8f, 1.6f);
                        soundPlaying = true;
                        isWalking = false;
                        enemy.enemyHealth = 9999f;
                        DisableSounds(0.8f);
                        footStepSound.Stop();
                    }
                }
                LoopIdleSound();
                soundPlaying = true;
                timer = 99999f;

            }
        }
        void Update()
        {
            //Track distance to player.
            distanceToTarget = Vector3.Distance(target.position, transform.position);

            // Keep the agent disabled while the puppet is unbalanced
            agent.enabled = puppet.state == BehaviourPuppet.State.Puppet;
            //zombie.transform.position = new Vector3(zombie.transform.position.x, zombie.transform.position.y, agent.transform.position.z);

            // Update agent destination on collision. If not the Agent collider will wander.
            if (isProvoked == true)
            {
                EngageTarget();
                Debug.Log("Engaged");
                isWalking = true;
            }
            else if (distanceToTarget <= chaseRange)
            {
                isProvoked = true;
            }
            else if(enemy.enemyHealth < 99)
            {
                isProvoked = true;
            }
            else
            {
                isProvoked = false;
                isWalking = false;
                agent.speed = 0;
                agent.transform.position = new Vector3(zombie.transform.position.x, zombie.transform.position.y, zombie.transform.position.z);
                StartCoroutine(UpdateRotation(1f));
                animator.SetBool("Attack", false);
            }
        }

        private void EngageTarget()
        {
            if(distanceToTarget >= agent.stoppingDistance)
            {
                ChaseTarget();
            }
            else if (distanceToTarget <= agent.stoppingDistance && enemy.enemyHealth > 0)
            {
                AttackTarget();
            }
        }

        private void AttackTarget()
        {
            agent.speed = 0;
            Debug.Log("1");
            agent.updateRotation.Equals(target);
            Debug.Log("2");
            zombie.transform.rotation = agent.transform.rotation;
            Debug.Log("3");
            isWalking = false;
            footStepSound.Stop();
            animator.SetBool("Attack", true);
            animator.SetBool("Walk", false);
            animator.SetBool("Idle", false);
            Debug.Log("Attacking");
            if (!attackSound.isPlaying && soundPlaying == false)
            {
                attackSound.Play();
                soundPlaying = true;
                attackSound.volume = Random.Range(0.4f, 1.0f);
                attackSound.pitch = Random.Range(0.8f, 2.0f);
            }
            else
            {
                attackSound.Stop();
                soundPlaying = false;
            }
            
        }

        private void ChaseTarget()
        {
            if (agent.enabled == false)
            {
                isWalking = false;
                agent.speed = 0;
                agent.transform.position = new Vector3(zombie.transform.position.x, zombie.transform.position.y, zombie.transform.position.z);
                StartCoroutine(UpdateRotation(1f));
            }

            //While Active track player and animate.
            else if (agent.enabled == true)
            {
                isWalking = true;
                agent.SetDestination(target.position);
                agent.updateRotation.Equals(target.position);
                agent.speed = tempSpeed;

                animator.SetBool("Walk", true);
                animator.SetBool("Idle", false);
                animator.SetBool("Attack", false);
            }
        }
        
        //Use below method to call co routine as it's easier and neater
        private void LoopIdleSound()
        {
            if (!soundPlaying)
            {
                idleSound.Play();
                idleSound.volume = Random.Range(0.5f, 1.0f);
                idleSound.pitch = Random.Range(0.8f, 1.4f);
            }
            else if(!idleSound.isPlaying)
                idleSound.Stop();
        }

        //Use insead of the above. Just for simpler reading.
        private void DisableSounds(float waitTime)
        {
            StartCoroutine(PlayDeath(waitTime));
        }

        public void setProvoked(bool input)
        {
            this.isProvoked = input;
        }
        private IEnumerator PlayDeath(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            idleSound.enabled = false;
            attackSound.enabled = false;
            footStepSound.enabled = false;
        }

        void Walk()
        {
            footStepSound.Play(0);
            footStepSound.volume = Random.Range(0.2f, 0.7f);
            footStepSound.pitch = 0.71f;
        }

        /* private IEnumerator wait(float waitTime, bool finished)
         {
             finished = false;
             yield return new WaitForSeconds(waitTime);
             finished = true;
             yield return finished;
         }

         private bool Wait(float waitTime, bool finished)
         {
             StartCoroutine(wait(waitTime, finished));
             return finished;
         }*/
    }
}

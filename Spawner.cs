using RootMotion.Demos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
        public GameObject[] spawnLocations;
        public GameObject[] fireworks;
        private GameObject[] zombiesAlive;
        public int zombiesAliveInt;
        public GameObject Enemy;
        private float currentTime;
        public int maxRounds = 0;
        public int currentRound = 0;
        public int zombieCount;
        private float roundDelay = 5.5f;
        [Header("Spawn Settings")]
        [Tooltip("Amount of enemies that will spawn")]
        public int SpawnLoopAmount;
        [Tooltip("Spawn delay in seconds")]
        public int spawnDelay;
        private int zombieAmount;

        // Start is called before the first frame update
        void Start()
        {
            currentTime = Time.time;
            spawnLocations = GameObject.FindGameObjectsWithTag("Spawn");
            currentRound = 1;
            fireworks = GameObject.FindGameObjectsWithTag("Fireworks");
            zombieAmount = SpawnLoopAmount;
        }

        // Update is called once per frame
        void Update()
        {
        if (currentRound > 1 && Time.time - currentTime >= roundDelay)
        {
            randomSpawn(SpawnLoopAmount);

        }
        else if(currentRound == 1)
        {
            randomSpawn(SpawnLoopAmount);
        }

            NewRound();
            CountZombies();
        }

        void randomSpawn()
        {
            spawnLocations = GameObject.FindGameObjectsWithTag("Spawn");
            int randomSpot = Random.Range(0, spawnLocations.Length);

            GameObject enemyInstance = Instantiate(Enemy, spawnLocations[randomSpot].transform.position, spawnLocations[randomSpot].transform.rotation);

           ZombieNavMeshScript agent = enemyInstance.transform.GetComponentInChildren<ZombieNavMeshScript>();
           agent.setProvoked(true);

        }

        void randomSpawn(int loop)
        {
            for (int i = 1; i <= loop; i++)
            {
                int randomSpot = Random.Range(0, spawnLocations.Length);

                if (Time.time - currentTime >= spawnDelay)
                {
                    GameObject enemyInstance = Instantiate(Enemy, spawnLocations[randomSpot].transform.position, spawnLocations[randomSpot].transform.rotation);
                    SpawnLoopAmount -= 1;
                    currentTime = Time.time;
                    ZombieNavMeshScript agent = enemyInstance.transform.GetComponentInChildren<ZombieNavMeshScript>();
                    agent.setProvoked(true);
                    zombieCount++;
                }
                else
                {
                    randomSpot = Random.Range(0, spawnLocations.Length);
                }

            }
        }
        
        void PlayFireworks()
        {
            foreach(GameObject firework in fireworks)
            {
                firework.GetComponent<ParticleSystem>().Play();
            }
        }
        
    public void CountZombies()
    {
        zombiesAlive = GameObject.FindGameObjectsWithTag("Enemy");
        zombiesAliveInt = zombiesAlive.Length / 19;
    }

        void NewRound()
        {
            if(currentRound <= maxRounds)
            {
                if (zombiesAliveInt <= 0 && currentRound >= 1)
                {
                    currentRound++;
                    PlayFireworks();
                    zombieAmount += 2;
                    SpawnLoopAmount = zombieAmount;
                    currentTime = Time.time;
                }
            }
            else if(currentRound == maxRounds)
            {
                PlayFireworks();
            }
        }

        void Wait(int wait)
        {
            float currentTime = Time.time;
            if (Time.time - currentTime >= wait)
            {
                currentTime = Time.time;
            }
        }
    }


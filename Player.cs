using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // Player's health at start of game
    private int playerHealth = 25;
    public int healthPickupAmount;
    public int LargeHealthAmount;
    public Text playerHealthUI;

    // Start is called before the first frame update
    void Start()
    {
        playerHealthUI.text = playerHealth.ToString();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Health"))
        {
            if (playerHealth < 100)
            {
                playerHealth += 25;
                playerHealthUI.text = playerHealth.ToString();
                Destroy(other.gameObject);
                if (playerHealth >= 100)
                {
                    playerHealth = 100;
                    playerHealthUI.text = playerHealth.ToString();
                }
            }
        }
    }
}
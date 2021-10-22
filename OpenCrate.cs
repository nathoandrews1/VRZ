using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenCrate : MonoBehaviour
{
    public Animator openAnimation;
    // Start is called before the first frame update
    void Start()
    {
        openAnimation = gameObject.GetComponent<Animator>();
        openAnimation.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerZ"))
        {
            openAnimation.enabled = true;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

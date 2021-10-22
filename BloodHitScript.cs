using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodHitScript : MonoBehaviour
{
    public Vector3 hitPos = new Vector3();
    public GameObject BloodAttach;
    public GameObject[] BloodFX;
    public Transform thisCharacter;
    public Transform nearestBone;
    public int effectIdx;
    // Start is called before the first frame update
    void Start()
    {
        thisCharacter = this.gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        GetNearestBone(this.transform, hitPos);

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag.Equals("Bullet"))
        {
            hitPos = collision.transform.position;

            if (effectIdx == BloodFX.Length) effectIdx = 0;
            float angle = Mathf.Atan2(collision.transform.rotation.x, collision.transform.rotation.z) * Mathf.Rad2Deg + 180;
            var instance = Instantiate(BloodFX[effectIdx], hitPos, Quaternion.Euler(0, angle + 225 , 0));
            effectIdx++;
            Debug.Log(effectIdx);

            var settings = instance.GetComponent<BFX_BloodSettings>();
            //settings.FreezeDecalDisappearance = InfiniteDecal;
            //settings.LightIntensityMultiplier = DirLight.intensity;


            nearestBone = GetNearestBone(collision.transform.root, collision.transform.position);
            if (nearestBone == null) return;

            var attachBloodInstance = Instantiate(BloodAttach);
            var bloodT = attachBloodInstance.transform;
            bloodT.position = hitPos;
            bloodT.localRotation = Quaternion.identity;
            bloodT.LookAt(hitPos + hitPos.normalized, hitPos);
            bloodT.Rotate(90, 0, 0);
            Destroy(attachBloodInstance, 10);
            Destroy(instance, 10);
            Destroy(collision.gameObject);
        }
        
    }

    public Transform GetNearestBone(Transform characterTransform, Vector3 hitPos)
    {
        var closestPos = 10f;
        Transform closestBone = null;
        var childs = characterTransform.GetComponentsInChildren<Transform>();

        foreach (var child in childs)
        {
            var dist = Vector3.Distance(child.position, hitPos);
            if (dist < closestPos)
            {
                closestPos = dist;
                closestBone = child;
            }
        }

        var distRoot = Vector3.Distance(characterTransform.position, hitPos);
        if (distRoot < closestPos)
        {
            closestPos = distRoot;
            closestBone = characterTransform;
        }
        return closestBone;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTargetPosition : MonoBehaviour
{
    public void setRandomPos()
    {
        float randomValue = Random.Range(-200, 200);

        transform.position = new Vector3(randomValue, 0, randomValue);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("yes");
        setRandomPos();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class garbage_collector : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        Destroy(other.gameObject);
    }
}

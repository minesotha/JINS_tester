using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class saw_rotate : MonoBehaviour {

    public float rotateSpeed = 5.0f; //set it in the  inspector

    void Update()
    {
        rotate();
    }


    void rotate()
    {

        transform.Rotate(Vector3.forward * rotateSpeed);
    }
}

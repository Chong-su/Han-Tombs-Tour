using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lookat : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2, 0);
    
    public Transform lookTarget;

    void Start()
    {
        transform.localPosition = offset;
        lookTarget = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.forward = lookTarget.forward;
        
    }
}

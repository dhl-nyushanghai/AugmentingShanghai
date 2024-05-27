using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotate : MonoBehaviour
{
    public Vector3 rotationSpeed; // Rotation speed in degrees per second for X, Y, and Z axes
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         // Calculate rotation for each frame based on speed and time
         var rotation = rotationSpeed * Time.deltaTime;
         // Apply rotation to the object this script is attached to
         transform.Rotate(rotation);
    }
}

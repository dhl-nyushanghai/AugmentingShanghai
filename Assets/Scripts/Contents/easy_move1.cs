using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class easy_move1 : MonoBehaviour
{
    public GameObject objectToMove;
    public Vector3 targetPosition;
    public float duration = 5f;

    private Vector3 startPosition;
    private float elapsedTime = 0f;
    private bool isMoving = false;
    
    // Start is called before the first frame update
    void Start()
    {
        if (objectToMove == null)
        {
            Debug.LogError("Please assign the object to move in the inspector.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            objectToMove.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            if (t >= 1f)
            {
                isMoving = false; // Stop moving once the target is reached
                elapsedTime = 0f; // Reset elapsed time
            }
        }
    }
    
    public void StartMoving()
    {
        if (objectToMove != null)
        {
            startPosition = objectToMove.transform.position;
            isMoving = true;
            elapsedTime = 0f;
        }
        else
        {
            Debug.LogError("No object assigned to move.");
        }
    }

    public void SetTargetPosition(Vector3 newTargetPosition)
    {
        targetPosition = newTargetPosition;
    }
}

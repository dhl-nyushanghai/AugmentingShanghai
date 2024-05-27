using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearMove: MonoBehaviour
{
    public GameObject objectToMove;
    public Vector3 targetPosition;
    public float duration = 5f;

    
    private Vector3 _startPosition;
    private float _elapsedTime = 0f;
    private bool _isMoving = false;
    
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
        if (_isMoving)
        {
            _elapsedTime += Time.deltaTime;
            float t = _elapsedTime / duration;
            objectToMove.transform.position = Vector3.Lerp(_startPosition, targetPosition, t);

            if (t >= 1f)
            {
                _isMoving = false; // Stop moving once the target is reached
                _elapsedTime = 0f; // Reset elapsed time
            }
        }
    }
    
    public void StartMoving()
    {
        if (objectToMove != null)
        {
            _startPosition = objectToMove.transform.position;
            _isMoving = true;
            _elapsedTime = 0f;
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

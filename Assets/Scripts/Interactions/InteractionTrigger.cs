using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionTrigger : MonoBehaviour
{
    #region Variables

    #region Position 

    [Header("Collider Trigger")]
    [SerializeField] private bool useColliderTrigger;
    private Collider _colliderTrigger;
    
    [Space(10)]
    [SerializeField] private UnityEvent onTriggerFirstEnter;
    [SerializeField] private UnityEvent onTriggerEnter;
    [SerializeField] private UnityEvent onTriggerExit;
    
    private bool _firstColliderEnter = true;

    #endregion

    #region Distance

    [Header("Distance Trigger")]
    [SerializeField] private bool useDistanceTrigger;
    [SerializeField] private float distance = 10f;
    /*[Space(5)]
    [SerializeField] private bool useTwoDistance;
    [SerializeField] private float distance2;*/
    
    [Space(10)]
    [SerializeField] private UnityEvent onDistanceFirstEnter;
    [SerializeField] private UnityEvent onDistanceEnter;
    [SerializeField] private UnityEvent onDistanceExit;
    
    /*[Tooltip("If useTwoDistance is enabled, this event will be invoked when the player enters the second distance")]
    [SerializeField] private UnityEvent onDistance2FirstEnter;
    [SerializeField] private UnityEvent onDistance2Enter;
    [SerializeField] private UnityEvent onDistance2Exit;*/
    
    private bool _firstDistanceEnter = true;
    //private bool _firstDistance2Enter = true;
    
    private bool _alreadyInDistance;
    //private bool _alreadyInDistance2;

    #endregion

    #region Lookat

    [Header("LookAt Trigger")]
    [SerializeField] private bool useLookAtTrigger;
    //[SerializeField] private Transform lookAtTarget;
    [SerializeField] private float lookAtAngle = 25f;
    [SerializeField] private float lookAtDistance;
    
    [Space(10)]
    [SerializeField] private UnityEvent onLookAtFirstEnter;
    [SerializeField] private UnityEvent onLookAtEnter;
    [SerializeField,Tooltip("It will be triggered when: LookAt event is triggerred and exit the distance")] 
    private UnityEvent onLookAtDistanceExit;
    
    private bool _firstLookAtEnter = true;
    private bool _alreadyLookAt;
    
    #endregion

    #region Global Variables

    //[Header("Interaction Settings")] [SerializeField] private bool triggerOnlyOnce;
    
    private GameObject _player;
    private Transform _playerTransform;

    private const int CheckRateFreq = 25;

    #endregion

    #endregion


    private void Start()
    {
        CheckAndInitSetting();
        
        _player = GameObject.FindGameObjectWithTag("Player");
        if(_player == null)
            Debug.LogError("No player found in the scene");
        _playerTransform = _player.transform;
    }


    private void Update()
    {
        
        if(useDistanceTrigger)
        {
            if (!CheckRateLimiter(CheckRateFreq)) return; //reduce the number of calculations

            if (InDistance(distance) && !_alreadyInDistance)
            {
                
                // To trigger each event only once when the player enters the distance
                if (_firstDistanceEnter)
                {
                    onDistanceFirstEnter?.Invoke();
                    _firstDistanceEnter = false;
                }

                onDistanceEnter?.Invoke();
                
                _alreadyInDistance = true;
            }
            else if (!InDistance(distance) && _alreadyInDistance)
            {
                onDistanceExit?.Invoke();
                _alreadyInDistance = false;
            }
        }

        if (useLookAtTrigger)
        {
            if (!CheckRateLimiter(CheckRateFreq)) return; //reduce the number of calculations
            //print("Angle: " + Vector3.Angle(_playerTransform.forward, (transform.position - _playerTransform.position).normalized));
            if(InDistance(lookAtDistance) && !_alreadyLookAt)
            {
                if (Vector3.Angle(_playerTransform.forward, (transform.position - _playerTransform.position).normalized) <= lookAtAngle)
                {
                    if (_firstLookAtEnter)
                    {
                        onLookAtFirstEnter?.Invoke();
                        _firstLookAtEnter = false;
                    }

                    onLookAtEnter?.Invoke();
                    
                    _alreadyLookAt = true;
                }
            }

            else if(!InDistance(lookAtDistance) && _alreadyLookAt)
            {
                onLookAtDistanceExit?.Invoke();
                _alreadyLookAt = false;
            }
        }
    }
    
    private bool InDistance(float distance)
    {
        return Vector3.Distance(transform.position, _playerTransform.position) <= distance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!useColliderTrigger) return;
        if(!other.CompareTag("Player")) return;
        
        //In this way, only player can go on and two less indent
        
        if (_firstColliderEnter)
        {
            onTriggerFirstEnter?.Invoke();
            _firstColliderEnter = false;
        }
        
        onTriggerEnter?.Invoke();
        
        
    }

    private void OnTriggerExit(Collider other)
    {
        if(!useColliderTrigger) return;
        if(!other.CompareTag("Player")) return;
        
        onTriggerExit?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        
    }

    private void CheckAndInitSetting()
    {
        /*// Check if there are multiple triggers enabled among the three
        if (useColliderTrigger && useDistanceTrigger || useColliderTrigger && useLookAtTrigger || useDistanceTrigger && useLookAtTrigger)
        {
            Debug.LogError("Multiple triggers enabled. Please enable only one trigger on " + gameObject.name);
            return;
        }*/
        if (useColliderTrigger)
        {
            _colliderTrigger = GetComponent<Collider>();
            if (_colliderTrigger == null)
            {
                Debug.LogError("Collider Trigger is enabled but no collider is attached to " + gameObject.name);
            }
            
            if(onTriggerFirstEnter == null && onTriggerEnter == null && onTriggerExit == null)
                Debug.LogWarning("No events are assigned to Collider Trigger on " + gameObject.name);
        }
        if(useDistanceTrigger)
        {
            if (distance == 0)
            {
                Debug.LogWarning("Distance Trigger is enabled but distance is set to 0 on " + gameObject.name);
            }
            
            if(onDistanceFirstEnter == null && onDistanceEnter == null && onDistanceExit == null)
                Debug.LogWarning("No events are assigned to Distance Trigger on " + gameObject.name);
        }

        if (useLookAtTrigger)
        {
            if(onLookAtFirstEnter == null && onLookAtFirstEnter == null && onLookAtDistanceExit == null)
                Debug.LogWarning("No events are assigned to LookAt Trigger on " + gameObject.name);
        }
        
    }
    
    /// <summary>
    /// Checks if the current frame count is a multiple of the given frequency.
    /// This can be used as a rate limiter in game loops, for example to perform an action every N frames.
    /// </summary>
    /// <param name="frequency">The frequency to check against. This is the number of frames between each "tick".</param>
    /// <returns>True if the current frame count is a multiple of the frequency, false otherwise.</returns>
    public static bool CheckRateLimiter(float frequency)
    {
        return Time.frameCount % frequency == 0;
    }
}



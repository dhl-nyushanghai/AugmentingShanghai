// ==========================================
// PUT THIS SCRIPT ON A GAME OBJECT
// TO TURN THE OBJECT INTO A FLOCKAI ENTITY
// CONTROLLABLE BY FlockAI_Manager.cs
// ==========================================

//  After attaching this script to a game object, add sensor antennae 
//  to the game object and script.  (via a list of child transforms)
//  The more antennae used, the finer tuned the entity's reaction to obstacles can be.
//  Note: Antennae do not necessarily have to be positioned symmetrically.
//     Uneven placement will cause imbalanced (yet consistent) reactions.
//
//  *******************************************************************************
//  Antennae checks extend from the Base Antenna to each antenna in the lists.
//  --> BE SURE THE ANTENNA BASE IS LOCATED **INSIDE** THE ENTITY GAME OBJECT <--
//      Otherwise, the antenna checks (particularly the rear check) can be blocked
//      by the entity's body itself.
//  *******************************************************************************
//
//  Examine one of the example prefabs for details.
//
//  Create a Flock AI Settings asset (a scriptable object)
//  to drop into the FlockAI_Entity_Settings field.  ('entitySettings' below)

using UnityEngine;

namespace FlockAI
{
    public class FlockAI_Entity : MonoBehaviour
    {
        [Tooltip("FlockAI Entity Settings asset")]
        public FlockAI_Entity_Settings entitySettings;

        [HideInInspector] public Rigidbody entityBody;
        [HideInInspector] public int entityInstanceID;
        [HideInInspector] public int controllerID;
        [HideInInspector] public int[] zoneID;
        [HideInInspector] public Renderer entityRenderer;
        [HideInInspector] public Vector3 entity_size;
        [HideInInspector] public Transform target_Transform;
        [HideInInspector] public Transform secondaryTarget_Transform;
        // ---
        [HideInInspector] public Vector3 target;
        [HideInInspector] public int indexOfNearestEntity = -1;
        [HideInInspector] public bool outOfBounds;

        // --- Social Behavior ---
        // If behaviors are not used, the entity will ignore all other entities and chase the flock target or wander.
        [HideInInspector] public bool useSocialBehaviors;
        [HideInInspector] public bool useSocialBehaviors_DEFAULT;
        [HideInInspector] public float attentionRange; // The distance the entity can detect other entities.
        [HideInInspector] public float sqrAttentionRange; // The square distance the entity can detect other entities.
        [HideInInspector] public float personalSpace; // If personal space is violated, entity will flee.
        [HideInInspector] public int overcrowdedAmount; // If too crowded, entity will flee.
        [HideInInspector] public int undercrowdedAmount; // If too few are in range, entity will seek the center location of the entities within range.
        [HideInInspector] public int crowdCount;
        // ---
        [HideInInspector] public bool noEntityNear;
        [HideInInspector] public bool personalSpaceViolated;
        public enum BehaviorStates { TARGETFOCUS_IGNOREOTHERS = -4, WANDERING_IGNOREOTHERS = -3, RETURNINGINBOUNDS = -2, AVOIDING = -1, STATIONARY = 0, WANDERING = 1, CHASING = 2, EVADING = 3, CHASINGCROWD = 4, OVERCROWDED = 6 }
        [HideInInspector] public BehaviorStates BehaviorState; // <---- CONVENIENT TO SHOW IN THE INSPECTOR FOR DEBUGGING -----
        [HideInInspector] public BehaviorStates previousBehaviorState;
        [HideInInspector] public float previousAttractionState;
        [HideInInspector] public bool ignoringFlock = false;

        // --- Target ---
        [HideInInspector] public bool repulsedByTarget;
        [HideInInspector] public float targetSize;
        [HideInInspector] public float targetingInaccuracy;
        [HideInInspector] public float chanceToWander; // The chance to begin wandering around, ignoring other entities. (0 - 1)
        [HideInInspector] public float chanceToWander_checkFrequency; // The frequency to check the chance to begin wandering.  (in seconds)
        [HideInInspector] public float chanceToWander_lastTimeChecked;
        [HideInInspector] public float chanceToWander_wanderDuration; // The duration to wander.  (in seconds)
        [HideInInspector] public float chanceToChaseFlockTarget; // The chance to begin chasing the flock target, ignoring other entities. (0 - 1)
        [HideInInspector] public float chanceToChaseFlockTarget_checkFrequency; // The frequency to check the chance to begin chasing the flock target.  (in seconds)
        [HideInInspector] public float chanceToChaseFlockTarget_lastTimeChecked;
        [HideInInspector] public float chanceToChaseFlockTarget_chaseDuration; // The duration to chase the flock target.  (in seconds)
        [HideInInspector] public float chanceToBeStationary; // The chance to begin chasing the flock target, ignoring other entities. (0 - 1)
        [HideInInspector] public float chanceToBeStationary_checkFrequency; // The frequency to check the chance to begin chasing the flock target.  (in seconds)
        [HideInInspector] public float chanceToBeStationary_lastTimeChecked;
        [HideInInspector] public float chanceToBeStationary_stationaryDuration; // The duration to chase the flock target.  (in seconds)
        [HideInInspector] public bool allowStationaryTurning;
        [HideInInspector] public float attractionState;

        // --- Senses ---
        [HideInInspector] public float lookForwardFactor; // Looks farther ahead based on movement speed.  [Range(0f, 2f)]
        [HideInInspector] public bool lookBehind; // Check behind to look for potential incoming rear-end collisions.
        [HideInInspector] public float lookBehindFactor; // Used to scale the distance to look behind."   [Range(0f, 1f)]
        [HideInInspector] public bool turnWhenBesideSomething; // Check to the sides of the entity's body for impending collisions with things.
        [HideInInspector] public bool turnIfStuck;
        [HideInInspector] public bool reverseIfStuck;
        [HideInInspector] public float frequencyToCheckIfStuck;
        // ---
        [HideInInspector] public float stuckCheck_lastTimeChecked;
        [HideInInspector] public float durationToTryGettingUnstuck;
        [HideInInspector] public float unstuckFix_TimeElapsed = 0f;
        [HideInInspector] public Vector3 entity_stuckCheck_position;
        [HideInInspector] public float stuckFix_RandomTurnDirection;
        [HideInInspector] public float stuckFix_Direction = (float)Polarity.POSITIVE;
        [HideInInspector] public bool entityIsStuck;
        // Antennae ---
        [Tooltip("The base from which the sensor antennae extend." + "\r\n" + "*** PUT THIS INSIDE THE PREFAB'S MESH. ***" +
            "\r\n" + "If not inside the mesh, the entity's body can block raycasts.")]
        public Transform antennae_base; // *** IF NOT INSIDE THE MESH, ENTITY CAN DETECT ITSELF AS ANOTHER ENTITY. ***
        [Tooltip("Antennae are created from the antennae_base to the positions of each transform listed.")]
        public Transform[] antennae_left;
        [Tooltip("Antennae are created from the antennae_base to the positions of each transform listed.")]
        public Transform[] antennae_right;
        // ---
        [HideInInspector] public Vector3[] _antennae_right;
        [HideInInspector] public Vector3[] _antennae_left;
        [HideInInspector] public float[] antennae_right_length;
        [HideInInspector] public float[] antennae_left_length;
        [HideInInspector] public float antennae_left_DistanceToTarget;
        [HideInInspector] public float antennae_right_DistanceToTarget;
        [HideInInspector] public Vector3 antenna_base_position;
        [HideInInspector] public Vector3 _antenna_base_position; // for optimization caching
        // ---
        [HideInInspector] public bool isTouchingSomething; // if the antennae are touching something
        [HideInInspector] public bool somethingIsInFront;
        [HideInInspector] public GameObject somethingIsInFront_Object;
        [HideInInspector] public bool somethingIsBehind;
        [HideInInspector] public GameObject somethingIsBehind_Object;
        [HideInInspector] public bool somethingIsAbove;
        [HideInInspector] public GameObject somethingIsAbove_Object;
        [HideInInspector] public bool somethingIsUnder;
        [HideInInspector] public GameObject somethingIsUnderneath_Object;
        [HideInInspector] public bool somethingIsToTheLeft;
        [HideInInspector] public GameObject somethingIsToTheLeft_Object;
        [HideInInspector] public bool somethingIsToTheRight;
        [HideInInspector] public GameObject somethingIsToTheRight_Object;

        // --- Movement ---
        [HideInInspector] public float speed_preferred;
        [HideInInspector] public float speed_preferred_variance; // Random variance is applied when the entity is created.
        [HideInInspector] public float speed_max; // Top speed is used when fleeing.
        [HideInInspector] public float speed_max_variance; // Random variance is applied when the entity is created.
        [HideInInspector] public float speed_min;
        [HideInInspector] public float speed_min_variance; // Random variance is applied when the entity is created.
        [HideInInspector] public float speed_acceleration;
        [HideInInspector] public float speed_deceleration;
        [HideInInspector] public float speed;
        [HideInInspector] public float turnSpeed;
        [HideInInspector] public float turnSpeed_movingFastest; // The turn speed when the entity is moving its fastest.
        [HideInInspector] public float turnSpeed_movingSlowest; // The turn speed when the entity is moving its slowest.
        // ---
        [HideInInspector] public float yaw;
        [HideInInspector] public float turnDirection;
        [HideInInspector] public float previousTurnDirection;

        // --- Flight ---
        [HideInInspector] public bool flying;
        [HideInInspector] public float preferredElevationOverThings; // The preferred elevation to fly over anything detected underneath.
        [HideInInspector] public float wanderingElevation_max; // Highest possible elevation when wandering.
        [HideInInspector] public float wanderingElevation_min; // Lowest possible elevation when wandering.
        [HideInInspector] public float MatchTargetElevation_tolerance_max; // Variance for matching the entity's elevation compared to its target.
        [HideInInspector] public float MatchTargetElevation_tolerance_min; // Random variance for matching the entity's elevation compared to its target.
        [HideInInspector] public float liftSpeed_movingFastest; // The speed to go up when the entity is moving its fastest.
        [HideInInspector] public float liftSpeed_movingSlowest; // The speed to go up when the entity is moving its slowest.
        [HideInInspector] public float dropSpeed_movingFastest; // The speed to go down when the entity is moving its fastest.
        [HideInInspector] public float dropSpeed_movingSlowest; // The speed to go down when the entity is moving its slowest.
        [HideInInspector] public float pitch_intensityUp; // Controls how agressively pitch changes with changes in elevation.   [Range(0f, 1f)]
        [HideInInspector] public float pitch_intensityDown; // Controls how agressively pitch changes with changes in elevation.   [Range(0f, 1f)]
        [HideInInspector] public float pitch_range; // Min/Max degrees of pitch
        [HideInInspector] public float roll_intensity; // Controls how agressively roll changes with changes in direction.   [Range(0f, 1f)]
        [HideInInspector] public float roll_range; // Min/Max degrees of roll

        [HideInInspector] public float elevationNoiseScale;
        [HideInInspector] public float elevationNoiseSpeed;
        [HideInInspector] public float elevationNoiseTime;

        [HideInInspector] public bool ascendIfStationary;
        [HideInInspector] public bool descendIfStationary;

        [HideInInspector] public float chanceToAscend;
        [HideInInspector] public float chanceToAscend_checkFrequency;
        [HideInInspector] public float chanceToAscend_ascendDuration;
        [HideInInspector] public float chanceToAscend_lastTimeChecked;
        [HideInInspector] public float chanceToDescend;
        [HideInInspector] public float chanceToDescend_checkFrequency;
        [HideInInspector] public float chanceToDescend_descendDuration;
        [HideInInspector] public float chanceToDescend_lastTimeChecked;
        // ---
        [HideInInspector] public float pitch;
        [HideInInspector] public float roll;
        [HideInInspector] public float targetElevationMatchTolerance;
        public enum Polarity { NEUTRAL = 0, POSITIVE = 1, NEGATIVE = -1 }
        [HideInInspector] public Polarity elevationDirection;
        [HideInInspector] public Polarity previousElevationDirection;


        // * State flags used in Coroutines *
        [HideInInspector] public bool ascendIfPossible;
        [HideInInspector] public bool descendIfPossible;

    #if UNITY_EDITOR
            void OnDrawGizmos() 
            {
                if (Application.isPlaying || antennae_base == null) return;
                if (antennae_left != null && antennae_left.Length > 0)
                {
                    for (int i = 0; i < antennae_left.Length; i++)
                    {
                        if (antennae_left[i] == null) continue;
                        Gizmos.DrawLine(antennae_base.transform.position, antennae_left[i].transform.position);
                    }
                }
                if (antennae_right != null && antennae_right.Length > 0)
                {
                    for (int i = 0; i < antennae_right.Length; i++)
                    {
                        if (antennae_right[i] == null) continue;
                        Gizmos.DrawLine(antennae_base.transform.position, antennae_right[i].transform.position);
                    }
                }
            }
    #endif

    }
}

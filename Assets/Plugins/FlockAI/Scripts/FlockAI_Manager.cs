using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;

namespace FlockAI
{
    public class FlockAI_Manager : MonoBehaviour
        {
        bool paused = false;
    
        [Tooltip("General note about the flock." + "\r\n" +
            "NOTE: First line of Note is used for its flock GameObject's Name.")]
        [TextArea(1,4)] public string note;

        [Space]
        [Tooltip("Optional - but helps with initial spawn placement of non-flying/swimming entities.")]
        [SerializeField] GameObject ground;

        [Header("Entities")]
        [Tooltip("Prefab with a FlockAI_Entity script on it.")]
        public FlockAI_Entity flockEntity;
        FlockAI_Entity_Settings entitySettings;
        FlockAI_Entity_Settings defaultEntitySettings;
        bool[] usingDefaultSettings;
        [SerializeField] int initialNumberOfEntites = 50;
        [Tooltip("Determines the amount of memory to reserve.")]
        [SerializeField] int maxAllowableEntities = 100;
        [HideInInspector] public List<GameObject> entities = new List<GameObject>();
        List<FlockAI_Entity> entityAI = new List<FlockAI_Entity>();
        List<Rigidbody> entityBodies = new List<Rigidbody>();
        Transform flockAItransform;

        public enum PerformanceAlgorithm { CheckAgainstAll = 0, DividedIntoZones = 1 }
        [Header("Performance Algorithm")]
        [SerializeField] PerformanceAlgorithm algorithm = PerformanceAlgorithm.CheckAgainstAll;
        [Tooltip("The number of zones to split the boundaries into.")]
        [SerializeField] int zonesWidth = 4;
        [Tooltip("The number of zones to split the boundaries into.")]
        [SerializeField] int zonesDepth = 4;
        int column;
        int row;
        List<int>[,] entitiesInZones;
        int entityManagerControllerID;
        #region
        public int GetZonesWidth() { return zonesWidth; }
        public void SetZonesWidth(int value) { zonesWidth = value; }
        public int GetZonesDepth() { return zonesDepth; }
        public void SetZonesDepth(int value) { zonesDepth = value; }
        #endregion
        bool checkedToTheWest;
        bool checkedToTheEast;
        bool checkedToTheNorth;
        bool checkedToTheSouth;

        [Header("Boundaries")]
        [Tooltip("Show the flock's boundaries in editor.")]
        [SerializeField] bool showBoundaries;
        [Tooltip("The center of the boundaries' floor.")]
        [SerializeField] Transform boundariesBase;
        public Transform GetBoundariesBase() { return boundariesBase; }
        [Tooltip("Entities will stay within these borders - even if fleeing something.")]
        [SerializeField] float boundaryWidth = 40f;
        public float GetBoundaryWidth() { return boundaryWidth; }
        [Tooltip("Entities will stay within these borders - even if fleeing something.")]
        [SerializeField] float boundaryDepth = 60;
        public float GetBoundaryDepth() { return boundaryDepth; }
        [Tooltip("Entities will stay within these borders - even if fleeing something.")]
        [SerializeField] float boundaryHeight = 8f;
        public float GetBoundaryHeight() { return boundaryHeight; }
        float border_top;
        float border_bottom;
        float border_north;
        float border_south;
        float border_west;
        float border_east;
        float zoneSizeX;
        float zoneSizeZ;

        [Header("Flock")]
        [Tooltip("Show the flock radius in editor.")]
        [SerializeField] bool showFlockRadius;
        [Tooltip("Entities have their own emergent flocking behavior, but can be constrained to a limited flocking area within the boundaries' borders.")]
        public float flockRadius = 25f;
        [Tooltip("Center of the constrained flocking area.")]
        public Transform flockTarget;

        [System.Serializable]
        public struct BehaviorTrigger
        {
            public Transform target;
            public float radius;
            public FlockAI_Entity_Settings behavior;
            public Color gizmoColor;
            //public static implicit operator BehaviorTrigger(SerializedProperty v)
            //{
                //throw new System.NotImplementedException();
            //}
        }
        [Header("Behavior Triggers")]
        [Tooltip("Show the behavior triggers in editor.")]
        [SerializeField] bool showTriggers;
        public BehaviorTrigger[] triggers;

        // Misc
        const float arbitraryLargeFloatNumber = 10000000f;
        const float immediateProximityFactor = 3f;
        const float TURN_NOTURN = 0f;  // const float TURN_LEFT = -1f;  const float TURN_RIGHT = 1f;
        const float returnInBoundsTime = 2f;
        public enum Polarity { NEUTRAL = 0, POSITIVE = 1, NEGATIVE = -1 }

        // For Memory and Speed Optimizations:
        Vector3 entity_position;
        Vector3 entity_position_previous;
        float fixedDeltaTime;
        Vector3 antenna_base_position;
        RaycastHit hit;
        RaycastHit hitAbove;
        RaycastHit hitBelow;
        float leftHitDistance;
        float rightHitDistance;
        Vector3 closestTouchHit;
        Vector3 closestTouchHit_LEFT;
        Vector3 closestTouchHit_RIGHT;
        Quaternion rot;
        float lfd; // look forward distance
        int crowdCount;
        Vector3 crowdCenter;
        int controllerIDofNearestEntity;
        float nearestDistance;
        Vector3 _tmpVec3;
        float _tmpFloat;
        float _tmpFloat1;
        float _tmpFloat2;
        float _tmpFloat3;
        int _entitiesCount;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (showBoundaries)
            {
                if (boundariesBase != null)
                {
                    Gizmos.color = new Color(1f, .5f, .5f, .5f);
                    Gizmos.DrawCube(boundariesBase.transform.position + new Vector3(0f, boundaryHeight / 2f, 0f),
                        new Vector3(boundaryWidth, boundaryHeight, boundaryDepth));
                }
            }
            if (showFlockRadius)
            {
                if (flockTarget != null)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, .5f);
                    Gizmos.DrawSphere(flockTarget.position, flockRadius);
                }
            }
            if (showTriggers && triggers.Length > 0)
            {
                for (int i = 0; i < triggers.Length; i++)
                {
                    if (triggers[i].target != null)
                    {
                        //Gizmos.color = new Color(1f, 0f, 1f, .25f);
                        if (triggers[i].gizmoColor.a == 0f &&
                            triggers[i].gizmoColor.r == 0f &&
                            triggers[i].gizmoColor.g == 0f &&
                            triggers[i].gizmoColor.b == 0f)
                            triggers[i].gizmoColor.a = .25f;
                        Gizmos.color = triggers[i].gizmoColor;
                        Gizmos.DrawSphere(triggers[i].target.position, triggers[i].radius);
                    }
                }
            }
        }
#endif

        private void Awake()
        {
            entityAI.Capacity = maxAllowableEntities;
            entities.Capacity = maxAllowableEntities;
            entityBodies.Capacity = maxAllowableEntities;
            defaultEntitySettings = flockEntity.entitySettings;
        }

        private void Start()
        {
            fixedDeltaTime = Time.fixedDeltaTime;

            border_bottom = 0f;
            border_top = boundaryHeight;
            border_north = boundaryDepth / 2f;
            border_south = -1f * boundaryDepth / 2f;
            border_west = -1f * boundaryWidth / 2f;
            border_east = boundaryWidth / 2f;
            zoneSizeX = boundaryWidth / (float)zonesWidth;
            zoneSizeZ = boundaryDepth / (float)zonesDepth;

            GameObject flockAIgroup = new GameObject();
            flockAItransform = flockAIgroup.transform;

            // Distance Check Algorithm Setup
            if (algorithm == PerformanceAlgorithm.DividedIntoZones)
            {
                entitiesInZones = new List<int>[zonesWidth, zonesDepth];
                for (int x = 0; x < zonesWidth; x++)
                {
                    for (int y = 0; y < zonesDepth; y++)
                    {
                        entitiesInZones[x, y] = new List<int>();
                    }
                }
            }

            // Create the initial flock of entities.
            string tmpStr;
            int tmpInt = note.IndexOf('\n');
            if (tmpInt < 0)
            {
                tmpStr = "FlockAI entities";
            }
            else
            {
               tmpStr = "FlockAI " + note.Substring(0, tmpInt) + " entities";
            }
            flockAItransform.name = tmpStr;
            if (initialNumberOfEntites > maxAllowableEntities) initialNumberOfEntites = maxAllowableEntities;
            for (int i = 0; i < initialNumberOfEntites; i++)
            {
                CreateEntity();
            }

            // Setup Behavior Triggers
            if (triggers.Length > 0)
            {
                usingDefaultSettings = new bool[maxAllowableEntities];
                for (int i = 0; i < usingDefaultSettings.Length; i++)
                {
                    usingDefaultSettings[i] = true;
                }
            }
        }

        public void RemoveEntity(int id = -1)
        {
            if (entities.Count < 1) return;
            if (id < 0) id = entities.Count - 1;
            else if (id >= entities.Count) return;
            Destroy(entities[id]);
            entities.RemoveAt(id);
            entityAI.RemoveAt(id);
            entityBodies.RemoveAt(id);
            if (algorithm == PerformanceAlgorithm.DividedIntoZones)
                entitiesInZones[entityAI[id].zoneID[0], entityAI[id].zoneID[1]].Remove(entityAI[id].controllerID);

            if (id < entities.Count)
            {
                // update the entities' controllerIDs
                for (int i = id; i < entityAI.Count; i++)
                {
                    entityAI[i].controllerID = i;
                }
            }
        }

        public void CreateEntity()
        {
            if (entities.Count >= maxAllowableEntities) return;
            // Generate Random Location for Spawning
            FlockAI_Entity flockAI_Entity = flockEntity.gameObject.GetComponentInChildren<FlockAI_Entity>();
            float preferredElevationOverThings = flockAI_Entity.entitySettings.preferredElevationOverThings;
            float height = flockEntity.gameObject.GetComponentInChildren<Renderer>().bounds.size.y;
            float elevationOffset;
            if (flockAI_Entity.entitySettings.flying)
                elevationOffset = 0.5f * height + preferredElevationOverThings;
            else
                elevationOffset = 0.5f * height;
            bool validPlacement = false;
            float y;
            do
            {
                y = Random.Range(border_top, border_bottom + elevationOffset) + boundariesBase.position.y;
                _tmpVec3 = new Vector3(Random.Range(border_west, border_east) + boundariesBase.position.x, y, Random.Range(border_north, border_south) + boundariesBase.position.z);
                if (flockAI_Entity.entitySettings.flying)
                {
                    if (ground && ground.activeSelf)
                    {
                        if (Physics.Raycast(_tmpVec3, Vector3.down, out hit, Mathf.Infinity))
                            validPlacement = true;
                    }
                    else
                    {
                        validPlacement = true;
                    }
                }
                else
                {
                    if (ground && ground.activeSelf)
                    {
                        if (Physics.Raycast(_tmpVec3, Vector3.down, out hit, Mathf.Infinity))
                        {
                            if (hit.transform.gameObject == ground)
                            {
                                y = hit.point.y + elevationOffset;
                                validPlacement = true;
                            }
                        }
                    }
                    else
                    {
                        y = boundariesBase.position.y + border_bottom + elevationOffset;
                        validPlacement = true;
                    }
                }

            } while (!validPlacement);
            _tmpVec3 = new Vector3(_tmpVec3.x, y, _tmpVec3.z);
            
            // Create the Entity
            GameObject go = Instantiate(flockEntity.gameObject, _tmpVec3, Quaternion.identity, flockAItransform);
            flockAI_Entity = go.GetComponent<FlockAI_Entity>();
            // Add the entity to the lists for entities.
            entities.Add(go);
            entityAI.Add(flockAI_Entity);
            entityBodies.Add(go.GetComponent<Rigidbody>());
            // Initialize the entity.
            EntityInitialization(entityAI[entities.Count - 1], entities.Count - 1);
            flockAI_Entity.yaw = Random.Range(-180f, 180f);
        }

        void EntityInitialization(FlockAI_Entity ent, int ID)
        {
            // Entity
            ent.controllerID = ID;
            ent.entityInstanceID = ent.gameObject.GetInstanceID();
            ent.entityRenderer = ent.GetComponent<Renderer>();
            ent.entityBody = ent.GetComponent<Rigidbody>();
            ent.entity_size = ent.entityRenderer.bounds.size;

            // Settings Parameters

            ent.target_Transform = flockTarget;

            ent.BehaviorState = FlockAI_Entity.BehaviorStates.WANDERING;

            ent.useSocialBehaviors = ent.entitySettings.useBehaviors;
            ent.useSocialBehaviors_DEFAULT = ent.entitySettings.useBehaviors;
            ent.attentionRange = ent.entitySettings.attentionRange;
            ent.sqrAttentionRange = ent.entitySettings.attentionRange * ent.entitySettings.attentionRange; // for faster distance checking
            ent.personalSpace = ent.entitySettings.personalSpace;
            ent.overcrowdedAmount = ent.entitySettings.overcrowdedAmount;
            ent.undercrowdedAmount = ent.entitySettings.undercrowdedAmount;

            ent.targetSize = ent.entitySettings.targetSize;
            ent.targetingInaccuracy = ent.entitySettings.targetingInaccuracy;
            ent.repulsedByTarget = ent.entitySettings.repulsedByTarget;

            ent.chanceToWander = ent.entitySettings.chanceToWander;
            ent.chanceToWander_checkFrequency = ent.entitySettings.chanceToWander_checkFrequency;
            ent.chanceToWander_wanderDuration = ent.entitySettings.chanceToWander_wanderDuration;
            if (ent.chanceToWander_checkFrequency < ent.chanceToWander_wanderDuration) ent.chanceToWander_checkFrequency = ent.chanceToWander_wanderDuration;
            ent.chanceToWander_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToWander_checkFrequency);
            ent.chanceToChaseFlockTarget = ent.entitySettings.chanceToChaseFlockTarget;
            ent.chanceToChaseFlockTarget_checkFrequency = ent.entitySettings.chanceToChaseFlockTarget_checkFrequency;
            ent.chanceToChaseFlockTarget_chaseDuration = ent.entitySettings.chanceToChaseFlockTarget_chaseDuration;
            if (ent.chanceToChaseFlockTarget_checkFrequency < ent.chanceToChaseFlockTarget_chaseDuration) ent.chanceToChaseFlockTarget_checkFrequency = ent.chanceToChaseFlockTarget_chaseDuration;
            ent.chanceToChaseFlockTarget_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToChaseFlockTarget_checkFrequency);
            ent.chanceToBeStationary = ent.entitySettings.chanceToBeStationary;
            ent.chanceToBeStationary_checkFrequency = ent.entitySettings.chanceToBeStationary_checkFrequency;
            ent.chanceToBeStationary_stationaryDuration = ent.entitySettings.chanceToBeStationary_stationaryDuration;
            if (ent.chanceToBeStationary_checkFrequency < ent.chanceToBeStationary_stationaryDuration) ent.chanceToBeStationary_checkFrequency = ent.chanceToBeStationary_stationaryDuration;
            ent.chanceToBeStationary_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToBeStationary_checkFrequency);
            ent.allowStationaryTurning = ent.entitySettings.allowStationaryTurning;

            ent.lookForwardFactor = ent.entitySettings.lookForwardFactor;
            ent.lookBehind = ent.entitySettings.lookBehind;
            ent.lookBehindFactor = ent.entitySettings.lookBehindFactor;
            ent.turnWhenBesideSomething = ent.entitySettings.turnWhenBesideSomething;
            ent.turnIfStuck = ent.entitySettings.turnIfStuck;
            ent.reverseIfStuck = ent.entitySettings.reverseIfStuck;
            ent.frequencyToCheckIfStuck = ent.entitySettings.frequencyToCheckIfStuck;
            if (ent.frequencyToCheckIfStuck < ent.durationToTryGettingUnstuck) ent.frequencyToCheckIfStuck = ent.durationToTryGettingUnstuck;
            ent.stuckCheck_lastTimeChecked = Time.time - Random.Range(0f, ent.frequencyToCheckIfStuck);
            ent.durationToTryGettingUnstuck = ent.entitySettings.durationToTryGettingUnstuck;
            ent.speed_preferred = ent.entitySettings.speed_preferred;
            ent.speed_preferred_variance = ent.entitySettings.speed_preferred_variance;
            ent.speed_max = ent.entitySettings.speed_max;
            ent.speed_max_variance = ent.entitySettings.speed_max_variance;
            ent.speed_min = ent.entitySettings.speed_min;
            ent.speed_min_variance = ent.entitySettings.speed_min_variance;
            if (ent.speed_min > ent.speed_max) ent.speed_min = ent.speed_max; // stop dumb values from being used
            ent.speed_acceleration = ent.entitySettings.speed_acceleration;
            ent.speed_deceleration = ent.entitySettings.speed_deceleration;
            ent.turnSpeed_movingFastest = ent.entitySettings.turnSpeed_movingFastest;
            ent.turnSpeed_movingSlowest = ent.entitySettings.turnSpeed_movingSlowest;

            if (!ent.entitySettings.flying && ent.flying) ent.pitch = 0f;
            ent.flying = ent.entitySettings.flying;
            ent.preferredElevationOverThings = ent.entitySettings.preferredElevationOverThings;
            ent.wanderingElevation_max = ent.entitySettings.wanderingElevation_max;
            ent.wanderingElevation_min = ent.entitySettings.wanderingElevation_min;
            if (ent.wanderingElevation_min > ent.wanderingElevation_max) ent.wanderingElevation_min = ent.wanderingElevation_max; // stop dumb values from being used
            ent.MatchTargetElevation_tolerance_max = ent.entitySettings.MatchTargetElevation_tolerance_max;
            ent.MatchTargetElevation_tolerance_min = ent.entitySettings.MatchTargetElevation_tolerance_min;
            if (ent.MatchTargetElevation_tolerance_max < .1f) ent.MatchTargetElevation_tolerance_max = .1f; // stop jitter
            if (ent.MatchTargetElevation_tolerance_min < .1f) ent.MatchTargetElevation_tolerance_min = .1f; // stop jitter
            ent.targetElevationMatchTolerance = Random.Range(ent.MatchTargetElevation_tolerance_min, ent.MatchTargetElevation_tolerance_max);

            ent.liftSpeed_movingFastest = ent.entitySettings.liftSpeed_movingFastest;
            ent.liftSpeed_movingSlowest = ent.entitySettings.liftSpeed_movingSlowest;
            ent.dropSpeed_movingFastest = ent.entitySettings.dropSpeed_movingFastest;
            ent.dropSpeed_movingSlowest = ent.entitySettings.dropSpeed_movingSlowest;
            ent.pitch_intensityUp = ent.entitySettings.pitch_intensityUp;
            ent.pitch_intensityDown = ent.entitySettings.pitch_intensityDown;
            ent.pitch_range = ent.entitySettings.pitch_range;
            ent.roll_intensity = ent.entitySettings.roll_intensity;
            ent.roll_range = ent.entitySettings.roll_range;

            ent.ascendIfStationary = ent.entitySettings.ascendIfStationary;
            ent.descendIfStationary = ent.entitySettings.descendIfStationary;

            ent.elevationNoiseScale = ent.entitySettings.elevationNoiseScale;
            ent.elevationNoiseSpeed = ent.entitySettings.elevationNoiseSpeed;
            ent.elevationNoiseTime = Random.Range(0f, 100f);

            ent.chanceToAscend = ent.entitySettings.chanceToAscend;
            ent.chanceToAscend_checkFrequency = ent.entitySettings.chanceToAscend_checkFrequency;
            ent.chanceToAscend_ascendDuration = ent.entitySettings.chanceToAscend_descendDuration;
            if (ent.chanceToAscend_checkFrequency < ent.chanceToAscend_ascendDuration) ent.chanceToAscend_checkFrequency = ent.chanceToAscend_ascendDuration;
            ent.chanceToAscend_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToAscend_checkFrequency);
            ent.chanceToDescend = ent.entitySettings.chanceToDescend;
            ent.chanceToDescend_checkFrequency = ent.entitySettings.chanceToDescend_checkFrequency;
            ent.chanceToDescend_descendDuration = ent.entitySettings.chanceToDescend_ascendDuration;
            if (ent.chanceToDescend_checkFrequency < ent.chanceToDescend_descendDuration) ent.chanceToDescend_checkFrequency = ent.chanceToDescend_descendDuration;
            ent.chanceToDescend_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToDescend_checkFrequency);

            ent.noEntityNear = true;

            // get Antennae info
            ent._antenna_base_position = ent.antennae_base.position - gameObject.transform.position;
            System.Array.Resize(ref ent.antennae_left_length, ent.antennae_left.Length);
            System.Array.Resize(ref ent.antennae_right_length, ent.antennae_right.Length);
            System.Array.Resize(ref ent._antennae_left, ent.antennae_right.Length);
            System.Array.Resize(ref ent._antennae_right, ent.antennae_right.Length);
            for (int i = 0; i < ent.antennae_left.Length; i++)
            {
                ent.antennae_left_length[i] = Vector3.Distance(ent.antennae_base.position, ent.antennae_left[i].position);
                ent.antennae_right_length[i] = Vector3.Distance(ent.antennae_base.position, ent.antennae_right[i].position);
                ent._antennae_left[i] = ent.antennae_left[i].position - ent.antennae_base.position;
                ent._antennae_right[i] = ent.antennae_right[i].position - ent.antennae_base.position;
            }
            for (int i = 0; i < ent.antennae_left.Length; i++)
            {
                Destroy(ent.antennae_left[i].gameObject);
                Destroy(ent.antennae_right[i].gameObject);
            }
            ent.antennae_left = null;
            ent.antennae_right = null;
            ent.somethingIsInFront = false;
            ent.somethingIsBehind = false;
            ent.somethingIsAbove = false;
            ent.somethingIsUnder = false;

            // set Speed
            ent.speed_preferred += (Random.Range(0f, ent.speed_preferred_variance * 2f) - ent.speed_preferred_variance);
            ent.speed = ent.speed_preferred;
            ent.speed_max += (Random.Range(0f, ent.speed_max_variance * 2) - ent.speed_max_variance);
            ent.speed_min += (Random.Range(0f, ent.speed_min_variance * 2) - ent.speed_min_variance);
            ent.turnSpeed = ent.turnSpeed_movingSlowest;

            // update the stuck position check...
            ent.entity_stuckCheck_position = ent.entityBody.position;

            // update Entity Position Lookup Table
            if (algorithm == PerformanceAlgorithm.DividedIntoZones)
            {
                _tmpFloat = boundariesBase.position.x + border_west;
                _tmpFloat = entity_position.x - _tmpFloat;
                column = (int)(_tmpFloat / zoneSizeX);
                _tmpFloat = boundariesBase.position.z + border_south;
                _tmpFloat = entity_position.z - _tmpFloat;
                row = (int)(_tmpFloat / zoneSizeZ);
                ent.zoneID = new int[2];
                ent.zoneID[0] = column; ent.zoneID[1] = row;
                entitiesInZones[column, row].Add(ent.controllerID);
            }
        }

        public void UpdateEntitiesSettings(FlockAI_Entity_Settings settings)
        {
            entitySettings = settings;
            for (int i = 0; i < entities.Count; i++)
            {
                EntitySettingsUpdate(entityAI[i]);
            }
        }
        public void UpdateEntitySettings(FlockAI_Entity ent, FlockAI_Entity_Settings settings)
        {
            EntitySettingsUpdate(ent, settings);
        }
        void EntitySettingsUpdate(FlockAI_Entity ent, FlockAI_Entity_Settings entitySettings = null)
        {
            if (entitySettings == null) entitySettings = this.entitySettings;

            //ent.target_Transform = flockTarget;
            ent.BehaviorState = FlockAI_Entity.BehaviorStates.WANDERING;
            ent.previousBehaviorState = FlockAI_Entity.BehaviorStates.WANDERING;

            ent.useSocialBehaviors = entitySettings.useBehaviors;
            if (ent.useSocialBehaviors) ent.ignoringFlock = false; else ent.ignoringFlock = true;
            ent.useSocialBehaviors_DEFAULT = entitySettings.useBehaviors;

            ent.attentionRange = entitySettings.attentionRange;
            ent.sqrAttentionRange = entitySettings.attentionRange * entitySettings.attentionRange; // for faster distance checking
            ent.personalSpace = entitySettings.personalSpace;
            ent.overcrowdedAmount = entitySettings.overcrowdedAmount;
            ent.undercrowdedAmount = entitySettings.undercrowdedAmount;

            ent.targetSize = entitySettings.targetSize;
            ent.targetingInaccuracy = entitySettings.targetingInaccuracy;
            ent.repulsedByTarget = entitySettings.repulsedByTarget;

            ent.chanceToWander = entitySettings.chanceToWander;
            ent.chanceToWander_checkFrequency = entitySettings.chanceToWander_checkFrequency;
            ent.chanceToWander_wanderDuration = entitySettings.chanceToWander_wanderDuration;
            if (ent.chanceToWander_checkFrequency < ent.chanceToWander_wanderDuration) ent.chanceToWander_checkFrequency = ent.chanceToWander_wanderDuration;
            ent.chanceToWander_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToWander_checkFrequency);
            ent.chanceToChaseFlockTarget = entitySettings.chanceToChaseFlockTarget;
            ent.chanceToChaseFlockTarget_checkFrequency = entitySettings.chanceToChaseFlockTarget_checkFrequency;
            ent.chanceToChaseFlockTarget_chaseDuration = entitySettings.chanceToChaseFlockTarget_chaseDuration;
            if (ent.chanceToChaseFlockTarget_checkFrequency < ent.chanceToChaseFlockTarget_chaseDuration) ent.chanceToChaseFlockTarget_checkFrequency = ent.chanceToChaseFlockTarget_chaseDuration;
            ent.chanceToChaseFlockTarget_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToChaseFlockTarget_checkFrequency);
            ent.chanceToBeStationary = entitySettings.chanceToBeStationary;
            ent.chanceToBeStationary_checkFrequency = entitySettings.chanceToBeStationary_checkFrequency;
            ent.chanceToBeStationary_stationaryDuration = entitySettings.chanceToBeStationary_stationaryDuration;
            if (ent.chanceToBeStationary_checkFrequency < ent.chanceToBeStationary_stationaryDuration) ent.chanceToBeStationary_checkFrequency = ent.chanceToBeStationary_stationaryDuration;
            ent.chanceToBeStationary_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToBeStationary_checkFrequency);
            ent.allowStationaryTurning = entitySettings.allowStationaryTurning;

            ent.lookForwardFactor = entitySettings.lookForwardFactor;
            ent.lookBehind = entitySettings.lookBehind;
            ent.lookBehindFactor = entitySettings.lookBehindFactor;
            ent.turnWhenBesideSomething = entitySettings.turnWhenBesideSomething;
            ent.turnIfStuck = entitySettings.turnIfStuck;
            ent.reverseIfStuck = entitySettings.reverseIfStuck;
            ent.frequencyToCheckIfStuck = entitySettings.frequencyToCheckIfStuck;
            if (ent.frequencyToCheckIfStuck < ent.durationToTryGettingUnstuck) ent.frequencyToCheckIfStuck = ent.durationToTryGettingUnstuck;
            ent.stuckCheck_lastTimeChecked = Time.time - Random.Range(0f, ent.frequencyToCheckIfStuck);
            ent.durationToTryGettingUnstuck = entitySettings.durationToTryGettingUnstuck;
            ent.speed_preferred = entitySettings.speed_preferred;
            ent.speed_preferred_variance = entitySettings.speed_preferred_variance;
            ent.speed_max = entitySettings.speed_max;
            ent.speed_max_variance = entitySettings.speed_max_variance;
            ent.speed_min = entitySettings.speed_min;
            ent.speed_min_variance = entitySettings.speed_min_variance;
            if (ent.speed_min > ent.speed_max) ent.speed_min = ent.speed_max; // stop dumb values from being used
            ent.speed_acceleration = entitySettings.speed_acceleration;
            ent.speed_deceleration = entitySettings.speed_deceleration;
            ent.turnSpeed_movingFastest = entitySettings.turnSpeed_movingFastest;
            ent.turnSpeed_movingSlowest = entitySettings.turnSpeed_movingSlowest;

            if (!ent.entitySettings.flying && ent.flying) ent.pitch = 0f;
            ent.flying = entitySettings.flying;
            ent.preferredElevationOverThings = entitySettings.preferredElevationOverThings;
            ent.wanderingElevation_max = entitySettings.wanderingElevation_max;
            ent.wanderingElevation_min = entitySettings.wanderingElevation_min;
            if (ent.wanderingElevation_min > ent.wanderingElevation_max) ent.wanderingElevation_min = ent.wanderingElevation_max; // stop dumb values from being used
            ent.MatchTargetElevation_tolerance_max = entitySettings.MatchTargetElevation_tolerance_max;
            ent.MatchTargetElevation_tolerance_min = entitySettings.MatchTargetElevation_tolerance_min;
            if (ent.MatchTargetElevation_tolerance_max < .1f) ent.MatchTargetElevation_tolerance_max = .1f; // stop jitter
            if (ent.MatchTargetElevation_tolerance_min < .1f) ent.MatchTargetElevation_tolerance_min = .1f; // stop jitter
            ent.targetElevationMatchTolerance = Random.Range(ent.MatchTargetElevation_tolerance_min, ent.MatchTargetElevation_tolerance_max);

            ent.liftSpeed_movingFastest = entitySettings.liftSpeed_movingFastest;
            ent.liftSpeed_movingSlowest = entitySettings.liftSpeed_movingSlowest;
            ent.dropSpeed_movingFastest = entitySettings.dropSpeed_movingFastest;
            ent.dropSpeed_movingSlowest = entitySettings.dropSpeed_movingSlowest;
            ent.pitch_intensityUp = entitySettings.pitch_intensityUp;
            ent.pitch_intensityDown = entitySettings.pitch_intensityDown;
            ent.pitch_range = entitySettings.pitch_range;
            ent.roll_intensity = entitySettings.roll_intensity;
            ent.roll_range = entitySettings.roll_range;

            ent.ascendIfStationary = entitySettings.ascendIfStationary;
            ent.descendIfStationary = entitySettings.descendIfStationary;

            ent.elevationNoiseScale = entitySettings.elevationNoiseScale;
            ent.elevationNoiseSpeed = entitySettings.elevationNoiseSpeed;
            ent.elevationNoiseTime = Random.Range(0f, 100f);

            ent.chanceToAscend = entitySettings.chanceToAscend;
            ent.chanceToAscend_checkFrequency = entitySettings.chanceToAscend_checkFrequency;
            ent.chanceToAscend_ascendDuration = entitySettings.chanceToAscend_descendDuration;
            if (ent.chanceToAscend_checkFrequency < ent.chanceToAscend_ascendDuration) ent.chanceToAscend_checkFrequency = ent.chanceToAscend_ascendDuration;
            ent.chanceToAscend_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToAscend_checkFrequency);
            ent.chanceToDescend = entitySettings.chanceToDescend;
            ent.chanceToDescend_checkFrequency = entitySettings.chanceToDescend_checkFrequency;
            ent.chanceToDescend_descendDuration = entitySettings.chanceToDescend_ascendDuration;
            if (ent.chanceToDescend_checkFrequency < ent.chanceToDescend_descendDuration) ent.chanceToDescend_checkFrequency = ent.chanceToDescend_descendDuration;
            ent.chanceToDescend_lastTimeChecked = Time.time - Random.Range(0f, ent.chanceToDescend_checkFrequency);

            // set Speed
            ent.speed_preferred += (Random.Range(0f, ent.speed_preferred_variance * 2f) - ent.speed_preferred_variance);
            ent.speed = ent.speed_preferred;
            ent.speed_max += (Random.Range(0f, ent.speed_max_variance * 2) - ent.speed_max_variance);
            ent.speed_min += (Random.Range(0f, ent.speed_min_variance * 2) - ent.speed_min_variance);
            ent.turnSpeed = ent.turnSpeed_movingSlowest;
        }


        public Vector3 RandomPositionWithinBounderies(FlockAI_Entity ent)
        {
            Vector3 vec = new Vector3(Random.Range(border_west, border_east) + boundariesBase.position.x, Random.Range(border_bottom, border_top) + boundariesBase.position.y, Random.Range(border_north, border_south) + boundariesBase.position.z)
            {
                y = border_bottom + boundariesBase.position.y +
                  Random.Range(ent.wanderingElevation_min, ent.wanderingElevation_max)
            };
            // stops jitter from entity not being able to reach target's elevation, and "bouncing" off floor/ceiling
            if (vec.y < boundariesBase.position.y + border_bottom + ent.preferredElevationOverThings)
                vec.y = boundariesBase.position.y + border_bottom + ent.preferredElevationOverThings;
            else if (vec.y > boundariesBase.position.y + border_top - ent.entity_size.y * 2f)
            {
                vec.y = boundariesBase.position.y + border_top - ent.entity_size.y * 2f;
            }
            return vec;
        }

        public void PauseUnpause()
        {
            paused = !paused;
            if (paused)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].SetActive(true);
                }
            }
        }

        // -------------------------------------------------------------------
        bool _behaviorTriggered;
        private void FixedUpdate()
        {
            _entitiesCount = entities.Count;

            // ENTITY POSITION ALGORITHM UPDATE
            if (algorithm == PerformanceAlgorithm.DividedIntoZones)
            {
                for (int i = 0; i < _entitiesCount; i++)
                {
                    _tmpFloat = boundariesBase.position.x + border_west;
                    _tmpFloat = entity_position.x - _tmpFloat;
                    column = (int)(_tmpFloat / zoneSizeX);
                    column = Mathf.Clamp(column, 0, zonesWidth - 1);
                    _tmpFloat = boundariesBase.position.z + border_south;
                    _tmpFloat = entity_position.z - _tmpFloat;
                    row = (int)(_tmpFloat / zoneSizeZ);
                    row = Mathf.Clamp(column, 0, zonesDepth - 1);
                    if (column != entityAI[i].zoneID[0] || row != entityAI[i].zoneID[1])
                    {
                        entitiesInZones[entityAI[i].zoneID[0], entityAI[i].zoneID[1]].Remove(entityAI[i].controllerID);
                        entitiesInZones[column, row].Add(entityAI[i].controllerID);
                        entityAI[i].zoneID[0] = column; entityAI[i].zoneID[1] = row;
//                        print("ID:" + entityAI[i].controllerID + "  " + column + "  " + row);
                    }
                }
            }

            // CHECK EACH ENTITY'S STATE -------------------------------------

            for (int i = 0; i < _entitiesCount; i++)
            {
                // cached for optimum performance
                entity_position = entityBodies[i].position;
                entity_position_previous = entity_position;
                antenna_base_position = entityAI[i].antennae_base.position;

                // Core Behavior Check against manually created targets (if any)
                if (triggers.Length > 0)
                {
                    _behaviorTriggered = false;
                    for (int ii = 0; ii < triggers.Length; ii++)
                    {
                        if (triggers[ii].target == null) continue;
                        _tmpVec3 = triggers[ii].target.position - entity_position;
                        if (_tmpVec3.sqrMagnitude < triggers[ii].radius * triggers[ii].radius)
                        {
                            if (entityAI[i].entitySettings != triggers[ii].behavior)
                            {
                                UpdateEntitySettings(entityAI[i], triggers[ii].behavior);
                                usingDefaultSettings[i] = false;
                                entityAI[i].secondaryTarget_Transform = triggers[ii].target;
                                _behaviorTriggered = true;
                                break;
                            }
                        }
                    }
                    if (!_behaviorTriggered && usingDefaultSettings[i] == false)
                    {
                        UpdateEntitySettings(entityAI[i], defaultEntitySettings);
                        entityAI[i].secondaryTarget_Transform = null;
                        usingDefaultSettings[i] = true;
                    }
                }

                // Check for Potential Collisions - Takes priority over other movement.
                //   checks each antenna to see if it's touching something
                if (entityAI[i].speed > 0f) CheckAntennae(entityAI[i]);

                // Bounderies Check - Head back to center of bonderies if out-of-bounds.
                if (!entityAI[i].isTouchingSomething) CheckBounderies(entityAI[i]);

                // Check to see if Entity is Stuck - happens sometimes... may trigger in crowds or stuck in a niche.
                if ((entityAI[i].reverseIfStuck || entityAI[i].turnIfStuck) && entityAI[i].BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY) EntityStuckCheck(entityAI[i]);

                // Determine Speed
                DetermineSpeed(entityAI[i]);

                // Fly / Float / Swim  (Vertical Elevation Control)
                if (entityAI[i].flying) Fly(entityAI[i]);

                // Target Acquistion and Response - (horizontal control) (target selection)
                if (!entityAI[i].isTouchingSomething) EntityThoughtProcess(entityAI[i]);

                // --------------------------------------------
                //   Update Entity
                // --------------------------------------------

                // ROTATION ------------------
                entityAI[i].previousTurnDirection = entityAI[i].turnDirection;
                if (!entityAI[i].entityIsStuck)
                {
                    if (entityAI[i].speed != 0f || entityAI[i].allowStationaryTurning)
                    {// Moving...
                     // -- YAW ---
                     // Determine the direction vector
                        if (!entityAI[i].isTouchingSomething)
                        {// uses a target:
                            _tmpVec3 = new Vector3(entityAI[i].target.x, entityBodies[i].transform.position.y, entityAI[i].target.z) - entityBodies[i].transform.position;
                        }
                        else
                        {// rotate away from a potential collision:
                            _tmpVec3 = new Vector3(closestTouchHit.x, entityBodies[i].transform.position.y, closestTouchHit.z) - entityAI[i].entityBody.transform.position;
                        }
                        // Determine the turning direction and angle to rotate
                        _tmpFloat1 = Vector3.SignedAngle(entityBodies[i].transform.forward, _tmpVec3, Vector3.up); // angle to target
                        _tmpFloat2 = Mathf.Sign(_tmpFloat1); // angle sign
                        _tmpFloat1 = Mathf.Abs(_tmpFloat1); // angle with sign removed
                        if (_tmpFloat1 > (entityAI[i].isTouchingSomething ? 0f : entityAI[i].targetingInaccuracy))
                        {
                            entityAI[i].turnDirection = _tmpFloat2 * entityAI[i].attractionState;
                            _tmpFloat = entityAI[i].turnSpeed * fixedDeltaTime; // degrees per second to turn
                            if (_tmpFloat > _tmpFloat1) _tmpFloat = _tmpFloat1; // limit over-steering
                            entityAI[i].yaw += _tmpFloat * entityAI[i].turnDirection;
                            //entityAI[i].yaw %= 360f;
                        }
                        else
                        {
                            entityAI[i].turnDirection = TURN_NOTURN;
                        }
                    }
                    else if (!entityAI[i].allowStationaryTurning)
                    {// Stationary... and turning while stationary is disabled
                        entityAI[i].turnDirection = TURN_NOTURN;
                    }
                }
                else
                {
                    entityAI[i].yaw += entityAI[i].turnSpeed * entityAI[i].stuckFix_RandomTurnDirection * fixedDeltaTime;  // a single step in degrees
                    //entityAI[i].yaw %= 360f;
                }
                rot = Quaternion.Euler(new Vector3(entityAI[i].pitch, entityAI[i].yaw, entityAI[i].roll));
                entityBodies[i].MoveRotation(rot);

                // LOCATION ------------------
                entity_position += entityAI[i].speed * entityAI[i].stuckFix_Direction * fixedDeltaTime * entityBodies[i].transform.forward;
                _tmpFloat = boundariesBase.position.y + border_bottom;
                if (entity_position.y < _tmpFloat)
                {// keep entities from being shoved under ground (and falling if affected by gravity)
                    entity_position.y = _tmpFloat;
                    entityBodies[i].position = entity_position;
                }
                //else entityBodies[i].MovePosition(entity_position);
                entityBodies[i].MovePosition(entity_position);

                // for Debugging...
#if UNITY_EDITOR
                if (entityAI[i].BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS) DebugExtensions.DrawLine(entity_position, entityAI[i].target, new Color(.03f, .07f, .03f));
                else if (entityAI[i].BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING) DebugExtensions.DrawLine(entity_position, entityAI[i].target, new Color(.1f, .2f, .1f));
#endif
            }
        }

        // -------------------------------------------------------------------


        private void CheckAntennae(FlockAI_Entity ent)
        {
            // Check antennae for contact (obstacle avoidance is highest priority)
            RaycastHit hit;
            bool touchingSomething;
            ent.isTouchingSomething = false;
            leftHitDistance = arbitraryLargeFloatNumber;
            rightHitDistance = arbitraryLargeFloatNumber;
            if (ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING)
            {// saved for resetting after collision avoidance
                ent.previousBehaviorState = ent.BehaviorState;
                ent.previousAttractionState = ent.attractionState;
            }

            // Check Left
            for (int i = 0; i < ent._antennae_left.Length; i++)
            {
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.transform.localRotation * ent._antennae_left[i], Color.white);
# endif                
                touchingSomething = Physics.Raycast(antenna_base_position, ent.transform.localRotation * ent._antennae_left[i], out hit, ent.antennae_left_length[i]);
                if (touchingSomething)
                {
                    if (hit.collider.gameObject.GetInstanceID() == ent.entityInstanceID)
                    {
                        // ignore - it is colliding with itself
                    }
                    else
                    {
                        ent.isTouchingSomething = true;
                        if (hit.distance < leftHitDistance)
                        {
                            leftHitDistance = hit.distance;
                            closestTouchHit_LEFT = hit.point;
                        }
                        //Debug.Log("left hit  " + hit.collider.gameObject.name);
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(antenna_base_position, hit.point, Color.red);
#endif
                    }
                }
            }
            // check to the side as well
            if (ent.turnWhenBesideSomething)
            {
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(new Vector3(-ent.entity_size.x * 2f, 0, 0)), Color.white);
#endif
                touchingSomething = Physics.Raycast(antenna_base_position, -ent.antennae_base.transform.right, out hit, ent.entity_size.x * 2f);
                if (touchingSomething)
                {
                    if (hit.collider.gameObject.GetInstanceID() == ent.entityInstanceID)
                    {
                        // ignore - it is colliding with itself
                        ent.somethingIsToTheLeft_Object = null;
                    }
                    else
                    {
                        ent.isTouchingSomething = true;
                        ent.somethingIsToTheLeft = true;
                        ent.somethingIsToTheLeft_Object = hit.transform.gameObject;
                        if (hit.distance < leftHitDistance)
                        {
                            leftHitDistance = hit.distance;
                            closestTouchHit_LEFT = hit.point;
                        }
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(antenna_base_position, hit.point, Color.red);
#endif
                    }
                }
                else ent.somethingIsToTheLeft_Object = null;
            }

            // Check Right
            for (int i = 0; i < ent._antennae_right.Length; i++)
            {
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.transform.localRotation * ent._antennae_right[i], Color.white);
#endif
                touchingSomething = Physics.Raycast(antenna_base_position, ent.transform.localRotation * ent._antennae_right[i], out hit, ent.antennae_left_length[i]);
                if (touchingSomething)
                {
                    if (hit.collider.gameObject.GetInstanceID() == ent.entityInstanceID)
                    {
                        // Ignore:  It is colliding with itself.
                    }
                    else
                    {
                        ent.isTouchingSomething = true;
                        if (hit.distance < rightHitDistance)
                        {
                            rightHitDistance = hit.distance;
                            closestTouchHit_RIGHT = hit.point;
                        }
                        //Debug.Log("right hit  " + hit.collider.gameObject.name);
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(antenna_base_position, hit.point, Color.red);
#endif
                    }
                }
            }
            // check to the side as well
            if (ent.turnWhenBesideSomething)
            {
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(new Vector3(ent.entity_size.x * 2f, 0, 0)), Color.white);
#endif
                touchingSomething = Physics.Raycast(antenna_base_position, ent.antennae_base.transform.right, out hit, ent.entity_size.x * 2f);
                if (touchingSomething)
                {
                    if (hit.collider.gameObject.GetInstanceID() == ent.entityInstanceID)
                    {
                        // ignore - it is colliding with itself
                        ent.somethingIsToTheRight_Object = null;
                    }
                    else
                    {
                        ent.isTouchingSomething = true;
                        ent.somethingIsToTheRight = true;
                        ent.somethingIsToTheRight_Object = hit.transform.gameObject;
                        if (hit.distance < rightHitDistance)
                        {
                            rightHitDistance = hit.distance;
                            closestTouchHit_RIGHT = hit.point;
                        }
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(antenna_base_position, hit.point, Color.red);
#endif
                    }
                }
                else ent.somethingIsToTheRight_Object = null; ;
            }

            // Save info about the closest potential collision
            if (leftHitDistance < rightHitDistance) closestTouchHit = closestTouchHit_LEFT;
            else if (rightHitDistance < leftHitDistance) closestTouchHit = closestTouchHit_RIGHT;

            // Set Behavior State
            if (!ent.isTouchingSomething)
            {
                ent.BehaviorState = ent.previousBehaviorState;
                ent.attractionState = ent.previousAttractionState;
            }
            else
            {
                ent.BehaviorState = FlockAI_Entity.BehaviorStates.AVOIDING;
                ent.attractionState = (float)Polarity.NEGATIVE;
            }
        }


        private void CheckBounderies(FlockAI_Entity ent)
        {   // If outside of the bounderies, set the target to the center of the bounderies.
            if ((entity_position.z > border_north + boundariesBase.position.z) || (entity_position.z < border_south + boundariesBase.position.z) ||
                (entity_position.x < border_west + boundariesBase.position.x) || (entity_position.x > border_east + boundariesBase.position.x) ||
                (entity_position.y > border_top + boundariesBase.position.y) || (entity_position.y < border_bottom + boundariesBase.position.y))
            {// is Outside of Bounds
                if (ent.outOfBounds == false)
                {// just now went out of bounds...
                    ent.outOfBounds = true;
                    if (ent.BehaviorState != FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS) StartCoroutine(ReturnToInsideBounds(ent.controllerID));
                }
            }
            else
            {// is Inside Bounds
                if (ent.outOfBounds == true)
                {// just now came in bounds...
                    ent.outOfBounds = false;
                }
            }
        }


        private void DetermineSpeed(FlockAI_Entity ent)
        {
            #region Check Forward
            ent.somethingIsInFront = Physics.Raycast(antenna_base_position, ent.antennae_base.transform.forward, out hit, ent.speed * ent.lookForwardFactor);
#if UNITY_EDITOR
            DebugExtensions.DrawRay(antenna_base_position, ent.lookForwardFactor * ent.speed * ent.antennae_base.transform.forward, Color.white);
#endif            
            if (ent.somethingIsInFront)
            {
                ent.somethingIsInFront_Object = hit.transform.gameObject;
            }
            else ent.somethingIsInFront_Object = null;
            #endregion

            #region Check Behind
            if (ent.lookBehind)
            {
                ent.somethingIsBehind = Physics.Raycast(antenna_base_position, -ent.antennae_base.transform.forward, out hit, ent.entity_size.z * immediateProximityFactor * ent.lookBehindFactor);
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, immediateProximityFactor * ent.entity_size.z * ent.lookBehindFactor * -ent.antennae_base.transform.forward, Color.white);
#endif
                if (ent.somethingIsBehind)
                {
                    ent.somethingIsBehind_Object = hit.transform.gameObject;
                }
            }
            else ent.somethingIsBehind_Object = null;
            #endregion

            #region --- Adjust Speed --------
            // *** THE ORDER OF THE STATES AND CONDITIONS BEING CHECKED IS IMPORTANT ***

            if (!ent.somethingIsInFront && !ent.somethingIsBehind)
            {// Nothing behind or directly in front of the entity...

                if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.AVOIDING)
                {// Decrease speed to the preferred speed, or leave it at whatever slower speed
                    if (ent.speed > ent.speed_preferred) ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                }

                else if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.TARGETFOCUS_IGNOREOTHERS ||
                    ent.BehaviorState == FlockAI_Entity.BehaviorStates.CHASING ||
                    ent.BehaviorState == FlockAI_Entity.BehaviorStates.CHASINGCROWD)
                {// Set speed to the preferred speed
                    if (ent.speed < ent.speed_preferred)
                    {// Increase speed to the preferred speed
                        ent.speed += ent.speed_acceleration * fixedDeltaTime;
                        if (ent.speed > ent.speed_preferred) ent.speed = ent.speed_preferred;
                    }
                    else if (ent.speed > ent.speed_preferred)
                    {// Decrease speed to the preferred speed
                        ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                        if (ent.speed < ent.speed_preferred) ent.speed = ent.speed_preferred;
                    }
                }

                else if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.EVADING)
                {// Increase speed to the max speed
                    if (ent.speed < ent.speed_max) ent.speed += ent.speed_acceleration * fixedDeltaTime;
                    else ent.speed = ent.speed_max;
                }

                //else if (BehaviorState == BehaviorStates.OVERCROWDED)
                //{
                    // Keep the same speed, but move away from target
                //}

                else if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING ||
                    ent.BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS)
                {// Set speed to the preferred speed
                    if (ent.speed < ent.speed_preferred)
                    {// Increase speed to the preferred speed
                        ent.speed += ent.speed_acceleration * fixedDeltaTime;
                        if (ent.speed > ent.speed_preferred) ent.speed = ent.speed_preferred;
                    }
                    else if (ent.speed > ent.speed_preferred)
                    {// Decrease speed to the preferred speed
                        ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                        if (ent.speed < ent.speed_preferred) ent.speed = ent.speed_preferred;
                    }
                }

                if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY)
                {// Decrease speed to become stationary
                    if (ent.speed > 0f) ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                    if (ent.speed < 0f) ent.speed = 0f;
                }
            }

            else if (ent.somethingIsInFront)
            {   // Decrease Speed
                ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                if (ent.speed < ent.speed_min) ent.speed = ent.speed_min;
            }

            else if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY)
            {   // Decrease Speed
                ent.speed -= ent.speed_deceleration * fixedDeltaTime;
                if (ent.speed < 0f) ent.speed = 0f;
            }

            else if (ent.somethingIsBehind)
            {
                ent.somethingIsBehind_Object = hit.transform.gameObject;
                // Increase Speed  (BUT - don't increase if about ready to bump into something, or becoming stationary)
                if (ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING || ent.BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY)
                {
                    if (ent.speed < ent.speed_max) ent.speed += ent.speed_acceleration * fixedDeltaTime;
                    else ent.speed = ent.speed_max;
                }
            }
            #endregion

            // Adjust Turn Speed based on the movement speed.
            _tmpFloat = ent.speed_max - ent.speed_min;
            if (_tmpFloat <= 0) _tmpFloat = .00001f;  // avoid divide/by/zero/error
            ent.turnSpeed = Mathf.Lerp(ent.turnSpeed_movingSlowest, ent.turnSpeed_movingFastest, (ent.speed - ent.speed_min) / _tmpFloat);
        }


        private void Fly(FlockAI_Entity ent)
        {
            lfd = ent.speed * ent.lookForwardFactor;
            ent.somethingIsAbove = false;
            ent.somethingIsUnder = false;
            ent.previousElevationDirection = ent.elevationDirection;

            #region Elevation Noise  (added first so corrections can be made next)
            if (ent.elevationNoiseScale > 0f && ent.elevationNoiseSpeed > 0f)
            {
                _tmpFloat = ent.elevationNoiseScale;
                _tmpFloat1 = ent.elevationNoiseSpeed;
                _tmpFloat2 = _tmpFloat * Mathf.PerlinNoise(Time.time * _tmpFloat1 + ent.elevationNoiseTime, 0f);
                _tmpFloat2 -= _tmpFloat * .5f;
                if (!entityBodies[ent.controllerID].useGravity) entity_position.y += _tmpFloat2;
                else
                {// If a prefab is affected by gravity, the elevation noise can slam/clip it through a ground level object
                    if (_tmpFloat2 > 0f)
                    {
                        if (Physics.Raycast(entity_position, Vector3.up, out hit, _tmpFloat2 + ent.preferredElevationOverThings))
                        {
                            entity_position.y = hit.point.y - ent.preferredElevationOverThings;
                        }
                        else entity_position.y += _tmpFloat2;
                    }
                    else if (_tmpFloat2 < 0f)
                    {
                        if (Physics.Raycast(entity_position, Vector3.down, out hit, Mathf.Abs(_tmpFloat2) + ent.preferredElevationOverThings))
                        {
                            entity_position.y = hit.point.y + ent.preferredElevationOverThings;
                        }
                        else entity_position.y += _tmpFloat2;
                    }
                }
            }
            #endregion

            #region Check Directly Underneath
#if UNITY_EDITOR
            DebugExtensions.DrawRay(antenna_base_position, -ent.antennae_base.transform.up * ent.preferredElevationOverThings, Color.white);
#endif
            ent.somethingIsUnder = Physics.Raycast(antenna_base_position, -ent.antennae_base.transform.up, out hitBelow, ent.preferredElevationOverThings);
            if (ent.somethingIsUnder) ent.somethingIsUnderneath_Object = hitBelow.transform.gameObject;
            else if (ent.somethingIsInFront)
            {// Check Forward and Underneath (for simple prediction of the situation)
                _tmpVec3 = new Vector3(0, ent.entity_size.y * -1f, lfd);
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), Color.yellow);
#endif 
                ent.somethingIsUnder = Physics.Raycast(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), out hitBelow, lfd);
                if (ent.somethingIsUnder)
                {
                    ent.somethingIsUnderneath_Object = hitBelow.transform.gameObject;
#if UNITY_EDITOR
                    DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), Color.red);
#endif
                }
                else ent.somethingIsUnderneath_Object = null;
            }
            else ent.somethingIsUnderneath_Object = null;
            #endregion

            #region Check Directly Above
#if UNITY_EDITOR
            DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.transform.up * ent.preferredElevationOverThings, Color.white);
#endif
            ent.somethingIsAbove = Physics.Raycast(antenna_base_position, ent.antennae_base.transform.up, out hitAbove, ent.preferredElevationOverThings);
            if (ent.somethingIsAbove) ent.somethingIsAbove_Object = hitAbove.transform.gameObject;
            else if (ent.somethingIsInFront)
            {// Check Forward and Above (for simple prediction of the situation)
                _tmpVec3 = new Vector3(0, ent.entity_size.y, lfd);
#if UNITY_EDITOR
                DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), Color.yellow);
#endif
                ent.somethingIsAbove = Physics.Raycast(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), out hitAbove, lfd);
                if (ent.somethingIsAbove)
                {
                    ent.somethingIsAbove_Object = hitAbove.transform.gameObject;
#if UNITY_EDITOR
                    DebugExtensions.DrawRay(antenna_base_position, ent.antennae_base.TransformDirection(_tmpVec3), Color.red);
#endif 
                }
                else ent.somethingIsAbove_Object = null;
            }
            else ent.somethingIsAbove_Object = null;
            #endregion

            #region Determine Elevation Changes
            _tmpFloat = ent.speed_max - ent.speed_min;  // the range of speed possibilities
            if (_tmpFloat <= 0) _tmpFloat = .00001f;  // avoid divide/by/zero/error
            _tmpFloat1 = Mathf.Abs(ent.target.y - entity_position.y); // difference in elevation
            ent.elevationDirection = FlockAI_Entity.Polarity.NEUTRAL;

            // Check for moving up...
            if ((!ent.somethingIsAbove) &&
                !(ent.descendIfStationary && ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY))
            {
                if ((ent.somethingIsUnder || ent.ascendIfPossible) ||
                   (ent.ascendIfStationary && ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY))
                {// Move Up
                    ent.elevationDirection = FlockAI_Entity.Polarity.POSITIVE;
                    entity_position.y += Mathf.Lerp(ent.liftSpeed_movingSlowest, ent.liftSpeed_movingFastest, (ent.speed - ent.speed_min) / _tmpFloat) * fixedDeltaTime;
                    if (entity_position.y > border_top + boundariesBase.position.y) entity_position.y = border_top + boundariesBase.position.y;
                }
                else if (ent.target.y > entity_position.y && _tmpFloat1 > ent.targetElevationMatchTolerance)
                {// Move Up to target's level
                    ent.elevationDirection = FlockAI_Entity.Polarity.POSITIVE;
                    entity_position.y += Mathf.Lerp(ent.liftSpeed_movingSlowest, ent.liftSpeed_movingFastest, (ent.speed - ent.speed_min) / _tmpFloat) * fixedDeltaTime;
                    if (entity_position.y > ent.target.y) entity_position.y = ent.target.y;
                }
                // do nothing...
            }

            // Check for moving down...
            if ((!ent.somethingIsUnder) &&
                !(ent.ascendIfStationary && ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY))
            {
                if (ent.somethingIsAbove || ent.descendIfPossible ||
                   (ent.descendIfStationary && ent.BehaviorState == FlockAI_Entity.BehaviorStates.STATIONARY))
                {// Move Down
                    ent.elevationDirection = FlockAI_Entity.Polarity.NEGATIVE;
                    entity_position.y -= Mathf.Lerp(ent.dropSpeed_movingSlowest, ent.dropSpeed_movingFastest, (ent.speed - ent.speed_min) / _tmpFloat) * fixedDeltaTime;
                    if (entity_position.y < boundariesBase.position.y + border_bottom) entity_position.y = boundariesBase.position.y + border_bottom;
                    // double-check since moved...
                    ent.somethingIsUnder = Physics.Raycast(antenna_base_position, -ent.antennae_base.transform.up, out hitBelow, ent.preferredElevationOverThings);
                    if (ent.somethingIsUnder)
                    {
                        ent.somethingIsUnderneath_Object = hitBelow.transform.gameObject;
                        entity_position.y = hitBelow.point.y + ent.preferredElevationOverThings;  // stop from overshooting  into "ground" level (causes jitter)
                    }
                }
                else if (ent.target.y < entity_position.y && _tmpFloat1 > ent.targetElevationMatchTolerance)
                {// Move Down to target's level
                    ent.elevationDirection = FlockAI_Entity.Polarity.NEGATIVE;
                    entity_position.y -= Mathf.Lerp(ent.dropSpeed_movingSlowest, ent.dropSpeed_movingFastest, (ent.speed - ent.speed_min) / _tmpFloat) * fixedDeltaTime;
                    if (entity_position.y < ent.target.y) entity_position.y = entity_position_previous.y;  // stop from overshooting if only targeting somethin
                    // double-check since moved...
                    ent.somethingIsUnder = Physics.Raycast(antenna_base_position, -ent.antennae_base.transform.up, out hitBelow, ent.preferredElevationOverThings);
                    if (ent.somethingIsUnder)
                    {
                        ent.somethingIsUnderneath_Object = hitBelow.transform.gameObject;
                        entity_position.y = hitBelow.point.y + ent.preferredElevationOverThings;  // stop from overshooting into "ground" level (causes jitter)
                    }
                }

                // or do nothing...
            }

            #region Random Changes in Elevation (if applicable)
            // Ascend Check
            if (ent.chanceToAscend > 0f &&
                ent.chanceToAscend_checkFrequency > 0f &&
                ent.ascendIfPossible != true &&
                Time.time - ent.chanceToAscend_lastTimeChecked >= ent.chanceToAscend_checkFrequency)
            {// frequency to check has been met
                ent.chanceToAscend_lastTimeChecked = Time.time;
                if (Random.value <= ent.chanceToAscend)
                {
                    StartCoroutine(AscendIfPossible(ent.controllerID));
                }
            }
            // Descend Check
            if (ent.chanceToDescend > 0f &&
            ent.chanceToDescend_checkFrequency > 0f &&
            ent.descendIfPossible != true &&
            Time.time - ent.chanceToDescend_lastTimeChecked >= ent.chanceToDescend_checkFrequency)
            {// frequency to check has been met
                ent.chanceToDescend_lastTimeChecked = Time.time;
                if (Random.value <= ent.chanceToDescend)
                {
                    StartCoroutine(DescendIfPossible(ent.controllerID));
                }
            }
            #endregion
            #endregion

            #region Determine Pitch
            if (ent.elevationDirection != FlockAI_Entity.Polarity.NEUTRAL && ent.elevationDirection == ent.previousElevationDirection)
            {// previousElevationDirection is used to stop micro-changes from being considered as "changes"
                if (ent.elevationDirection == FlockAI_Entity.Polarity.POSITIVE) _tmpFloat = ent.pitch_intensityUp * fixedDeltaTime;  // pitch Amount
                else _tmpFloat = ent.pitch_intensityDown * fixedDeltaTime;  // pitch Amount
                ent.pitch += _tmpFloat * (int)ent.elevationDirection * -1f;
                if (Mathf.Abs(ent.pitch) > ent.pitch_range) { ent.pitch = ent.pitch_range * (int)ent.elevationDirection * -1f; }
            }
            else
            {// level back out
                if (ent.pitch > 0f)
                {
                    ent.pitch -= ent.pitch_intensityDown * fixedDeltaTime;
                    if (ent.pitch < 0f) ent.pitch = 0f;
                }
                else if (ent.pitch < 0f)
                {
                    ent.pitch += ent.pitch_intensityUp * fixedDeltaTime;
                    if (ent.pitch > 0f) ent.pitch = 0f;
                }
            }
            #endregion

            #region Determine Roll
            _tmpFloat = ent.speed_max - ent.speed_min;  // difference in possible speeds
            if (_tmpFloat <= 0f) _tmpFloat = .00001f;  // avoid divide/by/zero/error
            _tmpFloat1 = (ent.speed - ent.speed_min) / _tmpFloat * ent.roll_intensity * fixedDeltaTime;  // rollAmount
            if (ent.turnDirection != TURN_NOTURN && ent.turnDirection == ent.previousTurnDirection)
            {// roll/bank into the turn
                ent.roll += _tmpFloat1 * ent.turnDirection * -1f;
                if (Mathf.Abs(ent.roll) > ent.roll_range) { ent.roll = ent.roll_range * ent.turnDirection * -1f; }
            }
            else
            {// level back out (not turning)
                if (ent.roll > 0f)
                {
                    ent.roll -= _tmpFloat1;
                    if (ent.roll < 0f) ent.roll = 0f;
                }
                else if (ent.roll < 0f)
                {
                    ent.roll += _tmpFloat1;
                    if (ent.roll > 0f) ent.roll = 0f;
                }
            }
            #endregion
        }


        // -------------------------------------------------------------------

        private void EntityThoughtProcess(FlockAI_Entity ent)
        {
            // IN BOUNDS...
            if (!ent.outOfBounds)
            {
                // ------------------------------------------------------------
                // DISREGARD OTHER ENTITIES' PRESSENCE:
                if (!ent.useSocialBehaviors)
                {
                    ent.ignoringFlock = true;
                    // IGNORE OTHER ENTITIES, AND CHASE A DESIGNATED TARGET  <<=====<<<<
                    if (ent.target_Transform != null &&
                        ent.BehaviorState != FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS &&
                        ent.BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY)
                    {
                        ent.BehaviorState = FlockAI_Entity.BehaviorStates.TARGETFOCUS_IGNOREOTHERS;
                        if (!ent.repulsedByTarget)
                            ent.attractionState = (float)Polarity.POSITIVE;
                        else
                            ent.attractionState = (float)Polarity.NEGATIVE;
                        if (ent.secondaryTarget_Transform == null)
                            ent.target = ent.target_Transform.position;
                        else
                            ent.target = ent.secondaryTarget_Transform.position;
                        // Check to see if target has been reached.
                        //distanceToTarget = Vector3.Distance(entity_position, ent.target);
                        //if (distanceToTarget <= wanderTargetSize)
                        //{
                        //}
                    }

                    // IGNORE OTHER ENTITIES, AND WANDER AROUND  <<=====<<<<
                    else
                    {
                        if (ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING &&
                            ent.BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY &&
                            ent.BehaviorState != FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS)
                        {// No target, so Wander around...
                            ent.chanceToWander = 1f;
                            if (ent.chanceToWander_wanderDuration < 3f) ent.chanceToWander_wanderDuration = 3f;
                            if (ent.chanceToWander_checkFrequency < ent.chanceToWander_wanderDuration) ent.chanceToWander_checkFrequency = ent.chanceToWander_wanderDuration;
                            ent.chanceToWander_lastTimeChecked = Time.time;
                            StartCoroutine(Wander_IgnoreFlock(ent.controllerID));
                        }
                        // Check to see if target has been reached
                        if (ent.flying && !entityBodies[ent.controllerID].useGravity)
                        {
                            _tmpFloat = Vector3.Distance(entity_position, ent.target); // distance to target
                            if (_tmpFloat <= ent.targetSize)
                            {// give entity a new target to wander to
                                ent.target = RandomPositionWithinBounderies(ent);
                                ent.attractionState = (float)Polarity.POSITIVE;
                            }
                        }
                        else
                        {
                            _tmpFloat1 = Mathf.Abs(ent.target.x - entity_position.x);
                            _tmpFloat3 = Mathf.Abs(ent.target.z - entity_position.z);
                            if (_tmpFloat1 <= ent.targetSize && _tmpFloat3 <= ent.targetSize)
                            {// give entity a new target to wander to
                                ent.target = RandomPositionWithinBounderies(ent);
                                ent.attractionState = (float)Polarity.POSITIVE;
                            }
                        }
                    }
#if UNITY_EDITOR
                    DebugExtensions.DrawLine(entity_position, ent.target, Color.grey);
#endif                
                }

                // ------------------------------------------------------------
                // RESPOND TO OTHER ENTITIES' PRESSENCE:
                else if (!ent.ignoringFlock)
                {// Use a Target Created Internally by the Behavior Settings
                    // Check if Outside of Flock Radius.
                    _tmpFloat1 = Vector3.Distance(entity_position, ent.target_Transform.position);
                    _tmpFloat2 = _tmpFloat1;
                    if (ent.repulsedByTarget && ent.secondaryTarget_Transform != null)
                        _tmpFloat2 = Vector3.Distance(entity_position, ent.secondaryTarget_Transform.position);
                    if (_tmpFloat1 > flockRadius)
                    {
                        // Outside of the acceptable Flocking Range
                        ent.attractionState = (float)Polarity.POSITIVE;
                        ent.target = ent.target_Transform.position;
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(entity_position, ent.target, new Color(.25f, .25f, .25f));
#endif
                    }
                    else if (ent.repulsedByTarget && _tmpFloat2 < ent.attentionRange)
                    {
                        // Flee from Target
                        ent.attractionState = (float)Polarity.NEGATIVE;
                        if (ent.secondaryTarget_Transform != null)
                            ent.target = ent.secondaryTarget_Transform.position;
                        else
                            ent.target = ent.target_Transform.position;
#if UNITY_EDITOR
                        DebugExtensions.DrawLine(entity_position, ent.target, new Color(0, 0, .5f));
#endif
                    }

                    else
                    {// Find the Closest Entity.  (and Target it)
                        crowdCount = 0;
                        crowdCenter = new Vector3(0, 0, 0);
                        controllerIDofNearestEntity = -1; nearestDistance = arbitraryLargeFloatNumber;

                        #region --- CHECK FOR OTHER ENTITIES ALGORITHM ---

                        #region CHECK AGAINST ALL
                        if (algorithm == PerformanceAlgorithm.CheckAgainstAll)
                        {
                            for (int i = 0; i < _entitiesCount; i++)
                            {
                                if (Mathf.Abs(entity_position.x - entityBodies[i].position.x) <= ent.attentionRange &&
                                    Mathf.Abs(entity_position.z - entityBodies[i].position.z) <= ent.attentionRange &&
                                    Mathf.Abs(entity_position.y - entityBodies[i].position.y) <= ent.attentionRange &&
                                    ent.controllerID != entityAI[i].controllerID)
                                {
                                    crowdCount++;
                                    crowdCenter += entityBodies[i].position;
#if UNITY_EDITOR
                                    DebugExtensions.DrawLine(entity_position, entityBodies[i].position, Color.grey);
#endif
                                    _tmpVec3 = entityBodies[i].position - entity_position;
                                    _tmpFloat = _tmpVec3.sqrMagnitude; // distance
                                    if (_tmpFloat < nearestDistance)
                                    {
                                        nearestDistance = _tmpFloat;
                                        controllerIDofNearestEntity = i;
                                        ent.indexOfNearestEntity = controllerIDofNearestEntity;
                                        ent.target = entityBodies[controllerIDofNearestEntity].position;
                                        ent.BehaviorState = FlockAI_Entity.BehaviorStates.CHASING;
                                        ent.attractionState = (float)Polarity.POSITIVE;  // move towards the closest entity (assumption)
                                    }
                                }
                            }
                        }
                        #endregion

                        #region DIVIDED_INTO_ZONES
                        else if (algorithm == PerformanceAlgorithm.DividedIntoZones)
                        {
                            checkedToTheWest = false;
                            checkedToTheEast = false;
                            checkedToTheNorth = false;
                            checkedToTheSouth = false;
                            ent.noEntityNear = false;
                            // the zone it's in...
                            CheckEntitiesInZone(ent, new int[2] { 0, 0 });
                            // the zone to the west...
                            if (ent.zoneID[0] > 0 &&
                                entity_position.x - ent.zoneID[0] * zoneSizeX <= ent.attentionRange)
                            {
                                CheckEntitiesInZone(ent, new int[2] { -1, 0 });
                                checkedToTheWest = true;
                            }
                            // the zone to the east...
                            if (ent.zoneID[0] < zonesWidth - 1 &&
                                (ent.zoneID[0] + 1) * zoneSizeX - entity_position.x <= ent.attentionRange)
                            { 
                                CheckEntitiesInZone(ent, new int[2] { 1, 0 });
                                checkedToTheEast = true;
                            }
                            // the zone to the south...
                            if (ent.zoneID[1] > 0 &&
                                entity_position.y - ent.zoneID[1] * zoneSizeZ <= ent.attentionRange)
                            { 
                                CheckEntitiesInZone(ent, new int[2] { 0, -1 });
                                checkedToTheSouth = true;
                            }
                            // the zone to the north...
                            if (ent.zoneID[1] < zonesDepth - 1 &&
                                (ent.zoneID[1] + 1) * zoneSizeZ - entity_position.z <= ent.attentionRange)
                            { 
                                CheckEntitiesInZone(ent, new int[2] { 0, 1 });
                                checkedToTheNorth = true;
                            }
                            if (checkedToTheWest && checkedToTheNorth)
                                CheckEntitiesInZone(ent, new int[2] { -1, 1 });
                            if (checkedToTheWest && checkedToTheSouth)
                                CheckEntitiesInZone(ent, new int[2] { -1, -1 });
                            if (checkedToTheEast && checkedToTheNorth)
                                CheckEntitiesInZone(ent, new int[2] { 1, 1 });
                            if (checkedToTheEast && checkedToTheSouth)
                                CheckEntitiesInZone(ent, new int[2] { 1, -1 });
                        }
                        #endregion

                        ent.crowdCount = crowdCount;
                        // update to *real* distance instead of (faster) sqrDistance
                        if (controllerIDofNearestEntity > -1)
                        {
                            nearestDistance = Vector3.Distance(entityBodies[controllerIDofNearestEntity].position, entity_position);
#if UNITY_EDITOR
                            DebugExtensions.DrawLine(antenna_base_position, entityBodies[controllerIDofNearestEntity].position, Color.green);
#endif
                        }
                        #endregion

                        // Determine how to behave based on the crowd level
                        //   (checked in order from most to least likely)

                        // Check for personal space violation
                        if (nearestDistance < ent.personalSpace)
                        {
                            ent.personalSpaceViolated = true;
                        }
                        else
                        {
                            ent.personalSpaceViolated = false;
                        }
                        if (ent.personalSpaceViolated)
                        {// Move away from the violator
                            ent.target = entityBodies[controllerIDofNearestEntity].position; // <------=== OFFENDER!
                            ent.attractionState = (float)Polarity.NEGATIVE;
                            ent.BehaviorState = FlockAI_Entity.BehaviorStates.EVADING;
#if UNITY_EDITOR
                            DebugExtensions.DrawLine(antenna_base_position, ent.target, new Color(1f, 1f, 0f));
#endif
                        }
                        else if (crowdCount >= ent.overcrowdedAmount)
                        {// Move away from the crowd's center
                            ent.target = (crowdCenter + entity_position) / (crowdCount + 1);
                            ent.attractionState = (float)Polarity.NEGATIVE;
                            ent.BehaviorState = FlockAI_Entity.BehaviorStates.OVERCROWDED;
#if UNITY_EDITOR
                            DebugExtensions.DrawLine(antenna_base_position, ent.target, new Color(1f, .35f, .1f));
#endif
                        }
                        else if (crowdCount < 1)
                        {// No entities around, so wander to a random location at the preferred elevation
                            ent.noEntityNear = true;
                            if (ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING &&
                                ent.BehaviorState != FlockAI_Entity.BehaviorStates.WANDERING &&
                                ent.BehaviorState != FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS &&
                                ent.BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY)
                            {// turn on Wander Mode
                                Wander(ent.controllerID);
                            }
                            // Check to see if target has been reached
                            _tmpFloat = Vector3.Distance(entity_position, ent.target); // distance to target
                            if (_tmpFloat <= ent.targetSize)
                            {// give entity a new target to wander to
                                Wander(ent.controllerID);
                            }
#if UNITY_EDITOR
                            DebugExtensions.DrawLine(antenna_base_position, ent.target, new Color(.5f, 1f, .5f));
#endif                            
                        }
                        else if (crowdCount <= ent.undercrowdedAmount)
                        {// Too few entities around (but not zero)
                            ent.target = (crowdCenter + entity_position) / (crowdCount + 1);
                            ent.attractionState = (float)Polarity.POSITIVE;
                            ent.BehaviorState = FlockAI_Entity.BehaviorStates.CHASINGCROWD;
#if UNITY_EDITOR
                            DebugExtensions.DrawLine(antenna_base_position + new Vector3(0f, .05f, 0f), ent.target, new Color(.25f, .75f, 1f));
#endif
                        }

                    }
                }

                // ------------------------------------------------------------
                // CHECK FOR CHANGES TO CORE BEHAVIOR:

                // Check to see if target has been reached if WANDERING
                if (ent.BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING)
                {
                    if (ent.flying && !entityBodies[ent.controllerID].useGravity)
                    {
                        _tmpFloat = Vector3.Distance(entity_position, ent.target); // distance to target
                        if (_tmpFloat <= ent.targetSize)
                        {// give entity a new target to wander to
                            ent.target = RandomPositionWithinBounderies(ent);
                            ent.attractionState = (float)Polarity.POSITIVE;
                        }
                    }
                    else
                    {
                        _tmpFloat1 = Mathf.Abs(ent.target.x - entity_position.x);
                        _tmpFloat3 = Mathf.Abs(ent.target.z - entity_position.z);
                        if (_tmpFloat1 <= ent.targetSize && _tmpFloat3 <= ent.targetSize)
                        {// give entity a new target to wander to
                            ent.target = RandomPositionWithinBounderies(ent);
                            ent.attractionState = (float)Polarity.POSITIVE;
                        }
                    }
                }

                // Check for Random Changes
                #region
                // Random Wandering
                if (ent.chanceToWander > 0f &&
                    ent.chanceToWander_checkFrequency > 0f &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS &&
                    Time.time - ent.chanceToWander_lastTimeChecked >= ent.chanceToWander_checkFrequency)
                {// frequency to check has been met
                    ent.chanceToWander_lastTimeChecked = Time.time;
                    if (Random.value <= ent.chanceToWander)
                    {
                        StartCoroutine(Wander_IgnoreFlock(ent.controllerID));
                    }
                }

                // Randomly start Chasing the Flock Target.
                if (ent.chanceToChaseFlockTarget > 0f &&
                    ent.chanceToChaseFlockTarget_checkFrequency > 0f &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.TARGETFOCUS_IGNOREOTHERS &&
                    Time.time - ent.chanceToChaseFlockTarget_lastTimeChecked >= ent.chanceToChaseFlockTarget_checkFrequency)
                {// frequency to check has been met
                    ent.chanceToChaseFlockTarget_lastTimeChecked = Time.time;
                    if (Random.value <= ent.chanceToChaseFlockTarget)
                    {
                        StartCoroutine(ChaseTarget_IgnoreFlock(ent.controllerID));
                    }
                }

                // Randomly become stationary.
                if (ent.chanceToBeStationary > 0f &&
                    ent.chanceToBeStationary_checkFrequency > 0f &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.AVOIDING &&
                    ent.BehaviorState != FlockAI_Entity.BehaviorStates.STATIONARY &&
                    Time.time - ent.chanceToBeStationary_lastTimeChecked >= ent.chanceToBeStationary_checkFrequency)
                {// frequency to check has been met
                    ent.chanceToBeStationary_lastTimeChecked = Time.time;
                    if (Random.value <= ent.chanceToBeStationary)
                    {
                        StartCoroutine(BecomeStationary(ent.controllerID));
                    }
                }
                #endregion
            }

            // OUT OF BOUNDS !
            else
            {   // Set Target to the Center of the Bonderies
                ent.ignoringFlock = true;
                ent.attractionState = (float)Polarity.POSITIVE;
                ent.target = new Vector3(boundariesBase.position.x + (border_west + border_east) * .5f,
                boundariesBase.position.y + (border_top + border_bottom) * .5f,
                boundariesBase.position.z + (border_north + border_south) * .5f);
#if UNITY_EDITOR
                DebugExtensions.DrawLine(entity_position, ent.target, Color.black);
#endif
            }

        }

        private void CheckEntitiesInZone(FlockAI_Entity ent, int[] offset)
        {
            int x = ent.zoneID[0] + offset[0];
            int z = ent.zoneID[1] + offset[1];
            if (x < 0 ||
                x >= zonesWidth ||
                z < 0 ||
                z >= zonesDepth ||
                entitiesInZones[x,z].Count < 2)
                return;

            for (int i = 0; i < entitiesInZones[x,z].Count; i++)
            {
                entityManagerControllerID = entitiesInZones[x,z][i];
                if (Mathf.Abs(entity_position.x - entityBodies[entityManagerControllerID].position.x) <= ent.attentionRange &&
                    Mathf.Abs(entity_position.z - entityBodies[entityManagerControllerID].position.z) <= ent.attentionRange &&
                    Mathf.Abs(entity_position.y - entityBodies[entityManagerControllerID].position.y) <= ent.attentionRange &&
                    ent.controllerID != entityManagerControllerID)
                {
                    crowdCount++;
                    crowdCenter += entityBodies[entityManagerControllerID].position;
#if UNITY_EDITOR
                    DebugExtensions.DrawLine(entity_position, entityBodies[entityManagerControllerID].position, Color.grey);
#endif
                    _tmpVec3 = entityBodies[entityManagerControllerID].position - entity_position;
                    _tmpFloat = _tmpVec3.sqrMagnitude; // distance
                    if (_tmpFloat < nearestDistance)
                    {
                        nearestDistance = _tmpFloat;
                        controllerIDofNearestEntity = entityManagerControllerID;
                        ent.indexOfNearestEntity = controllerIDofNearestEntity;
                        ent.target = entityBodies[controllerIDofNearestEntity].position;
                        ent.BehaviorState = FlockAI_Entity.BehaviorStates.CHASING;
                        ent.attractionState = (float)Polarity.POSITIVE;  // move towards the closest entity (assumption)
                    }
                }
            }
        }

        private void EntityStuckCheck(FlockAI_Entity ent)
        {
            if (Time.time - ent.stuckCheck_lastTimeChecked >= ent.frequencyToCheckIfStuck)
            {
                ent.stuckCheck_lastTimeChecked = Time.time;
                if ((ent.speed > ent.entity_size.z * .1f) &&
                   (Mathf.Abs(ent.entity_stuckCheck_position.x - entity_position.x) < ent.entity_size.x) &&
                   (Mathf.Abs(ent.entity_stuckCheck_position.y - entity_position.y) < ent.entity_size.y) &&
                   (Mathf.Abs(ent.entity_stuckCheck_position.z - entity_position.z) < ent.entity_size.z))
                {// *** STUCK ***
                    if (!ent.entityIsStuck)
                    {// got stuck just now, so choose a direction to turn for a while
                        if (Random.Range(0f, 1f) < .5f)
                        {
                            ent.stuckFix_RandomTurnDirection = (float)Polarity.POSITIVE;
                        }
                        else
                        {
                            ent.stuckFix_RandomTurnDirection = (float)Polarity.NEGATIVE;
                        }
                        ent.unstuckFix_TimeElapsed = 0f;
                        if (ent.reverseIfStuck)
                        {// go into reverse...
                            ent.stuckFix_Direction = (float)Polarity.NEGATIVE;
                        }
                        //Debug.Log(gameObject.name + " stuck");
                    }
                    ent.entityIsStuck = true;
                }
                else
                {// not stuck ...
                    ent.entityIsStuck = false;
                    ent.stuckFix_Direction = (float)Polarity.POSITIVE;
                }
                ent.entity_stuckCheck_position = entity_position;  // get new position for next check
            }
            if (ent.entityIsStuck)
            {
                ent.unstuckFix_TimeElapsed += fixedDeltaTime;
                if (ent.unstuckFix_TimeElapsed > ent.durationToTryGettingUnstuck)
                {// stop trying to get unstuck (at least for the moment)
                    ent.unstuckFix_TimeElapsed = 0f;
                    ent.entityIsStuck = false;
                    ent.stuckFix_Direction = (float)Polarity.POSITIVE;
                }
            }
        }

        // -------------------------------------------------------------------

        // ------------ BEHAVIOURS ------------------
        #region
        private int ObjectIDtoControllerID(int id)
        {
            for (int i = 0; i < entityAI.Count; i++)
            {
                if (id == entityAI[i].entityInstanceID)
                    return i;
            }
            return -1;
        }

        public void Wander(int index)
        {
            entityAI[index].BehaviorState = FlockAI_Entity.BehaviorStates.WANDERING;
            entityAI[index].attractionState = (float)Polarity.POSITIVE;
            entityAI[index].target = RandomPositionWithinBounderies(entityAI[index]);
        }

        public IEnumerator ReturnToInsideBounds(int index)
        {
            entityAI[index].BehaviorState = FlockAI_Entity.BehaviorStates.RETURNINGINBOUNDS;
            entityAI[index].attractionState = (float)Polarity.POSITIVE;
            entityAI[index].target = new Vector3(
                boundariesBase.position.x + (border_west + border_east) * .5f,
                boundariesBase.position.y + (border_top + border_bottom) * .5f,
                boundariesBase.position.z + (border_north + border_south) * .5f);
            entityAI[index].ignoringFlock = true;
            entityAI[index].useSocialBehaviors = false;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(returnInBoundsTime);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].ignoringFlock = false;
                entityAI[index].useSocialBehaviors = entityAI[index].useSocialBehaviors_DEFAULT;
                Wander(index);
            }
        }

        public IEnumerator Wander_IgnoreFlock(int index)
        {
            entityAI[index].BehaviorState = FlockAI_Entity.BehaviorStates.WANDERING_IGNOREOTHERS;
            entityAI[index].attractionState = (float)Polarity.POSITIVE;
            entityAI[index].target = RandomPositionWithinBounderies(entityAI[index]);
            entityAI[index].ignoringFlock = true;
            entityAI[index].useSocialBehaviors = false;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(entityAI[index].chanceToWander_wanderDuration);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].ignoringFlock = false;
                entityAI[index].useSocialBehaviors = entityAI[index].useSocialBehaviors_DEFAULT;
                Wander(entityAI[index].controllerID);
            }
        }

        public IEnumerator ChaseTarget_IgnoreFlock(int index)
        {
            entityAI[index].BehaviorState = FlockAI_Entity.BehaviorStates.TARGETFOCUS_IGNOREOTHERS;
            entityAI[index].attractionState = (float)Polarity.POSITIVE;
            entityAI[index].ignoringFlock = true;
            entityAI[index].useSocialBehaviors = false;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(entityAI[index].chanceToChaseFlockTarget_chaseDuration);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].ignoringFlock = false;
                entityAI[index].useSocialBehaviors = entityAI[index].useSocialBehaviors_DEFAULT;
                Wander(entityAI[index].controllerID);
            }
        }

        public IEnumerator AscendIfPossible(int index)
        {
            entityAI[index].ascendIfPossible = true;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(entityAI[index].chanceToAscend_ascendDuration);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].ascendIfPossible = false;
                entityAI[index].chanceToAscend_lastTimeChecked = Time.time;
            }
        }
        public IEnumerator DescendIfPossible(int index)
        {
            entityAI[index].descendIfPossible = true;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(entityAI[index].chanceToDescend_descendDuration);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].descendIfPossible = false;
                entityAI[index].chanceToDescend_lastTimeChecked = Time.time;
            }
        }

        public IEnumerator BecomeStationary(int index)
        {
            entityAI[index].BehaviorState = FlockAI_Entity.BehaviorStates.STATIONARY;
            entityAI[index].attractionState = (float)Polarity.POSITIVE;
            entityAI[index].ignoringFlock = true;
            int currentObjectID = entityAI[index].entityInstanceID;
            yield return new WaitForSeconds(entityAI[index].chanceToBeStationary_stationaryDuration);
            // check count in case the entity has been destroyed before yield return "returned" back
            if (index < entityAI.Count)
            {
                // check in case the list of entities has been modified since waiting for yield
                if (currentObjectID != entityAI[index].entityInstanceID)
                {// get updated controller ID
                    index = ObjectIDtoControllerID(index);
                }
            }
            else
                index = ObjectIDtoControllerID(index);
            if (index > -1)
            {
                entityAI[index].ignoringFlock = false;
                entityAI[index].stuckCheck_lastTimeChecked = Time.time;
                Wander(entityAI[index].controllerID);
            }
        }

        #endregion
    
    }
}
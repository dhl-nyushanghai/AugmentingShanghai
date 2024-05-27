using UnityEngine;

namespace FlockAI
{
[CreateAssetMenu(fileName = "FlockAI Entity", menuName = "FlockAI/FlockAI Entity Settings", order = 1)]
public class FlockAI_Entity_Settings : ScriptableObject
{
    public string description;

    //[Header("Social Behavior")]
    [Tooltip("If behaviors are not used, the entity will not interact with other entities.")]
    public bool useBehaviors = true;
    [Tooltip("If personal space is violated, entity will flee.")]
    public float personalSpace = 1f;
    [Tooltip("If too crowded, entity will flee.")]
    public int overcrowdedAmount = 10;
    [Tooltip("If too few are in range, entity will seek the center location of the entities within range.")]
    public int undercrowdedAmount = 4;

    //[Header("Target")]
    [Tooltip("The distance the entity can detect other entities.")]
    public float attentionRange = 10f;
    [Tooltip("Degrees off-center to allow a targeted entity before chasing after it.")]
    [Range(0f, 20f)] public float targetingInaccuracy = 5f;
    [Tooltip("Some behaviour settings can make it hard to collide with a small sized target..." + "\r\n" + "(Useful to stop circular wandering.)")]
    public float targetSize = 2f;
    [Tooltip("Repulsed by the flock target or secondary target.")]
    public bool repulsedByTarget;
    [Tooltip("The chance to begin randomly wandering to a random target, ignoring other entities.  (0 - 1)")]
    [Range(0f, 1f)] public float chanceToWander = 0f;
    [Tooltip("The frequency to check the chance to randomly wander.  (in seconds)")]
    public float chanceToWander_checkFrequency = 15f;
    [Tooltip("The duration to randomly wander.  (in seconds)")]
    public float chanceToWander_wanderDuration = 10f;
    [Tooltip("The chance to begin randomly chasing the flock target, ignoring other entities.  (0 - 1)")]
    [Range(0f, 1f)] public float chanceToChaseFlockTarget = 0f;
    [Tooltip("The frequency to check the chance to randomly chase the flock target.  (in seconds)")]
    public float chanceToChaseFlockTarget_checkFrequency = 15f;
    [Tooltip("The duration to randomly chase the flock target.  (in seconds)")]
    public float chanceToChaseFlockTarget_chaseDuration = 10f;
    [Tooltip("The chance to randomly become stationary.  (0 - 1)")]
    [Range(0f, 1f)] public float chanceToBeStationary = 0f;
    [Tooltip("The frequency to check the random chance to become stationary.  (in seconds)")]
    public float chanceToBeStationary_checkFrequency = 15f;
    [Tooltip("The duration to become randomly stationary.  (in seconds)")]
    public float chanceToBeStationary_stationaryDuration = 5f;

    //[Header("Senses")]
    [Tooltip("Looks farther ahead based on movement speed.")]
    [Range(0f, 2f)] public float lookForwardFactor = 1f;
    [Tooltip("Check behind to look for potential incoming rear-end collisions.")]
    public bool lookBehind = true;
    [Tooltip("Used to scale the distance to look behind.")]
    [Range(0f, 1f)] public float lookBehindFactor = 1f;
    [Tooltip("Check to the sides of the entity's body for impending collisions with things.")]
    public bool turnWhenBesideSomething = true;
    [Tooltip("Sometimes an entity will get stuck by an obstacle or if overcrowded.")]
    public bool turnIfStuck = true;
    [Tooltip("Sometimes an entity will get stuck by an obstacle or if overcrowded.")]
    public bool reverseIfStuck = true;
    // movement stuck check...
    [Tooltip("Frequency to check if the entity is stuck in the same spot.  (in seconds)")]
    public float frequencyToCheckIfStuck = 3f;
    [Tooltip("How long to try and get un-stuck.")]
    public float durationToTryGettingUnstuck = .25f;

    //[Header("Movement")]
    [Tooltip("The entity's preferred speed.  (meters per second)")]
    public float speed_preferred = 5f;
    [Tooltip("Random variance applied to the entity when it's created.  (+/- meters per second)")]
    public float speed_preferred_variance = .1f;
    [Tooltip("Top speed is used when fleeing from other entities.")]
    public float speed_max = 7f;
    [Tooltip("Random variance applied to the entity when it's created.  (+/- meters per second)")]
    public float speed_max_variance = .1f;
    [Tooltip("The minimum speed.  (unless stationary)")]
    public float speed_min = 0f;
    [Tooltip("Random variance applied to the entity when it's created.  (+/- meters per second)")]
    public float speed_min_variance = 0f;
    public float speed_acceleration = 1f;
    public float speed_deceleration = 2f;
    [Tooltip("The turn speed when the entity is moving its fastest.  (degrees per second)")]
    public float turnSpeed_movingFastest = 180f;
    [Tooltip("The turn speed when the entity is moving its slowest.  (degrees per second)")]
    public float turnSpeed_movingSlowest = 360f;
    [Tooltip("Allow stationary turning when speed is 0.")]
    public bool allowStationaryTurning = true;

    //[Header("Flying / Swimming / Floating / Skipping")]
    [Tooltip("Whether or not the entity flies/swims.")]
    public bool flying = true;
    [Tooltip("The preferred elevation to fly over (and under) things.")]
    public float preferredElevationOverThings = 1f;
    [Tooltip("Highest possible elevation when wandering.")]
    public float wanderingElevation_max;
    [Tooltip("Lowest possible elevation when wandering.")]
    public float wanderingElevation_min;
    [Tooltip("Variance (in meters) for matching the entity's elevation compared to its target." + "\r\n" + "(0 tolerance can cause jitter)")]
    public float MatchTargetElevation_tolerance_max = 1f;
    [Tooltip("Variance (in meters) for matching the entity's elevation compared to its target." + "\r\n" + "(0 tolerance can cause jitter)")]
    public float MatchTargetElevation_tolerance_min = .3f;
    [Tooltip("The speed to ascend when the entity is moving its fastest.")]
    public float liftSpeed_movingFastest = 2f;
    [Tooltip("The speed to ascend when the entity is moving its slowest.")]
    public float liftSpeed_movingSlowest = 1f;
    [Tooltip("The speed to descend when the entity is moving its fastest.")]
    public float dropSpeed_movingFastest = 1f;
    [Tooltip("The speed to descend when the entity is moving its slowest.")]
    public float dropSpeed_movingSlowest = 2f;

    [Tooltip("Amount of elevation 'noise'." + "\r\n" + "Very small values are best for most situations.")]
    [Range(0f, 1f)]
    public float elevationNoiseScale = 0f;
    [Tooltip("Oscillation speed of the elevation 'noise'." + "\r\n" + "Very small values are best for most situations.")]
    [Range(0f, 10f)]
    public float elevationNoiseSpeed = 0f;

    [Tooltip("Ascend if in Stationary Mode.")]
    public bool ascendIfStationary;
    [Tooltip("Descend if in Stationary Mode.")]
    public bool descendIfStationary;

    [Tooltip("The chance to randomly ascend.  (0 - 1)")]
    [Range(0f, 1f)] public float chanceToAscend = 0f;
    [Tooltip("The frequency to check the chance to randomly ascend.  (in seconds)")]
    public float chanceToAscend_checkFrequency = 15f;
    [Tooltip("The duration to randomly ascend.  (in seconds)")]
    public float chanceToAscend_descendDuration = 1f;
    [Tooltip("The chance to randomly descend.  (0 - 1)")]
    [Range(0f, 1f)] public float chanceToDescend = 0f;
    [Tooltip("The frequency to check the chance to randomly descend.  (in seconds)")]
    public float chanceToDescend_checkFrequency = 15f;
    [Tooltip("The duration to randomaly descend.  (in seconds)")]
    public float chanceToDescend_ascendDuration = 1f;

    [Tooltip("Controls how agressively pitch changes with changes in elevation.  (degrees per second)")]
    [Range(0f, 360f)] public float pitch_intensityUp = 90f;
    [Tooltip("Controls how agressively pitch changes with changes in elevation.  (degrees per second)")]
    [Range(0f, 360f)] public float pitch_intensityDown = 90f;
    [Tooltip("Min/Max degree of pitch")]
    [Range(0f, 89f)] public float pitch_range = 45f;
    [Tooltip("Controls how agressively roll changes with changes in direction.  (degrees per second)")]
    [Range(0f, 720f)] public float roll_intensity = 180f;
    [Tooltip("Min/Max degree of roll")]
    [Range(0f, 89f)] public float roll_range = 45f;

}
}
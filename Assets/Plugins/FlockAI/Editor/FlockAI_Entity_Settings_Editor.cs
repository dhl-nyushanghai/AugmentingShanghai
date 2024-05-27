using UnityEditor;

namespace FlockAI
{
    [CustomEditor(typeof(FlockAI_Entity_Settings))]
    public class FlockAI_Entity_Settings_Editor : UnityEditor.Editor
    {
        static bool showSocialBehaviorInfo = false;
        static bool showTargetInfo = false;
        static bool showSensesInfo = false;
        static bool showMovementInfo = false;
        static bool showElevationInfo = false;

        SerializedProperty useBehaviors;
        SerializedProperty chanceToWander;
        SerializedProperty chanceToChaseFlockTarget;
        SerializedProperty chanceToBeStationary;
        SerializedProperty lookBehind;
        SerializedProperty turnIfStuck;
        SerializedProperty reverseIfStuck;
        SerializedProperty flying;
        SerializedProperty chanceToAscend;
        SerializedProperty chanceToDescend;
        SerializedProperty ascendIfStationary;
        SerializedProperty descendIfStationary;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));

            showSocialBehaviorInfo = EditorGUILayout.Foldout(showSocialBehaviorInfo, "Social Behavior");
            if (showSocialBehaviorInfo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useBehaviors"));
                useBehaviors = serializedObject.FindProperty("useBehaviors");
                if (useBehaviors.boolValue)
                {

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("personalSpace"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("overcrowdedAmount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("undercrowdedAmount"));
                }
            }

            EditorGUILayout.Space();
            showTargetInfo = EditorGUILayout.Foldout(showTargetInfo, "Target");
            if (showTargetInfo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("repulsedByTarget"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attentionRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetingInaccuracy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToWander"));
                chanceToWander = serializedObject.FindProperty("chanceToWander");
                if (chanceToWander.floatValue > 0f)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToWander_checkFrequency"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToWander_wanderDuration"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToChaseFlockTarget"));
                chanceToChaseFlockTarget = serializedObject.FindProperty("chanceToChaseFlockTarget");
                if (chanceToChaseFlockTarget.floatValue > 0f)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToChaseFlockTarget_checkFrequency"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToChaseFlockTarget_chaseDuration"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToBeStationary"));
                chanceToBeStationary = serializedObject.FindProperty("chanceToBeStationary");
                if (chanceToBeStationary.floatValue > 0f)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToBeStationary_checkFrequency"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToBeStationary_stationaryDuration"));
                }
            }

            EditorGUILayout.Space();
            showSensesInfo = EditorGUILayout.Foldout(showSensesInfo, "Senses");
            if (showSensesInfo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lookForwardFactor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lookBehind"));
                lookBehind = serializedObject.FindProperty("lookBehind");
                if (lookBehind.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lookBehindFactor"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turnWhenBesideSomething"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turnIfStuck"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseIfStuck"));
                turnIfStuck = serializedObject.FindProperty("turnIfStuck");
                reverseIfStuck = serializedObject.FindProperty("reverseIfStuck");
                if (turnIfStuck.boolValue || reverseIfStuck.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("frequencyToCheckIfStuck"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("durationToTryGettingUnstuck"));
                }
            }

            EditorGUILayout.Space();
            showMovementInfo = EditorGUILayout.Foldout(showMovementInfo, "Movement");
            if (showMovementInfo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_preferred"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_preferred_variance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_max"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_max_variance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_min"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_min_variance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_acceleration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed_deceleration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turnSpeed_movingFastest"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turnSpeed_movingSlowest"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("allowStationaryTurning"));
            }

            EditorGUILayout.Space();
            showElevationInfo = EditorGUILayout.Foldout(showElevationInfo, "Flying / Swimming / Floating / Skipping");
            if (showElevationInfo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("flying"));
                flying = serializedObject.FindProperty("flying");
                if (flying.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("preferredElevationOverThings"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("wanderingElevation_max"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("wanderingElevation_min"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MatchTargetElevation_tolerance_max"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MatchTargetElevation_tolerance_min"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("liftSpeed_movingFastest"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("liftSpeed_movingSlowest"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropSpeed_movingFastest"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropSpeed_movingSlowest"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elevationNoiseScale"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elevationNoiseSpeed"));

                    ascendIfStationary = serializedObject.FindProperty("ascendIfStationary");
                    descendIfStationary = serializedObject.FindProperty("descendIfStationary");
                    if (!descendIfStationary.boolValue)
                    { 
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("ascendIfStationary"));
                    }
                    if (!ascendIfStationary.boolValue)
                    { 
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("descendIfStationary"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToAscend"));
                    chanceToAscend = serializedObject.FindProperty("chanceToAscend");
                    if (chanceToAscend.floatValue > 0f)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToAscend_checkFrequency"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToAscend_descendDuration"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToDescend"));
                    chanceToDescend = serializedObject.FindProperty("chanceToDescend");
                    if (chanceToDescend.floatValue > 0f)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToDescend_checkFrequency"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToDescend_ascendDuration"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch_intensityUp"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch_intensityDown"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch_range"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("roll_intensity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("roll_range"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    
    }
}
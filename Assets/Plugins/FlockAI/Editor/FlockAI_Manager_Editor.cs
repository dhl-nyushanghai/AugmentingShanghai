using UnityEngine;
using UnityEditor;

namespace FlockAI
{
    [CustomEditor(typeof(FlockAI_Manager))]
    public class FlockAI_Manager_Editor : UnityEditor.Editor
    {
        FlockAI_Manager manager;
        SerializedProperty performanceType;

        public override void OnInspectorGUI()
        {
            manager = (FlockAI_Manager)target;
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("note"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ground"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flockEntity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialNumberOfEntites"));
            if (Application.isPlaying == false)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAllowableEntities"));
            }

            string help = "White - Entity's antennae." + "\r\n" +
                "Grey - An entity within the entity's attention range." + "\r\n" +
                "Green - The entity that has the entity's attention." + "\r\n" +
                "Dark Green - Wandering... but not ignoring others." + "\r\n" +
                "Grey - Wanderingto point... ignoring other enties." + "\r\n" +
                "Black - Out of bounds.  Returning to center..." + "\r\n" +
                "Dark Grey - Out of flock's range.  Returning..." + "\r\n" +
                "Red - Avoiding the obstacle the line goes to." + "\r\n" +
                "Dark Red - Fleeing the thing the line goes to." + "\r\n" +
                "Orange - Avoiding the area the line goes to.  (over - crowded)" + "\r\n" +
                "Cyan - Going to the location the line goes to.  (under-crowded)";
            GUIContent buttonContent = new GUIContent("Toggle RayCast Gizmos", help);
            if (GUILayout.Button(buttonContent))
            {
                DebugExtensions.showDebugItems = !DebugExtensions.showDebugItems;
            }

            if (Application.isPlaying == false)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("algorithm"));
                performanceType = serializedObject.FindProperty("algorithm");
                if (performanceType.intValue == (int)FlockAI_Manager.PerformanceAlgorithm.DividedIntoZones)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zonesWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zonesDepth"));
                    if (manager.GetZonesWidth() < 2)
                    {
                        manager.SetZonesWidth(2);
                    }
                    else if (manager.GetBoundaryWidth() / manager.GetZonesWidth() <
                        manager.flockEntity.entitySettings.attentionRange)
                    {
                        manager.SetZonesWidth((int)(manager.GetBoundaryWidth() / manager.flockEntity.entitySettings.attentionRange));
                        Debug.Log("Boundary divided into zones smaller than the entities' attention range.");
                    }
                    if (manager.GetZonesDepth() < 2)
                    {
                        manager.SetZonesDepth(2);
                    }
                    else if (manager.GetBoundaryDepth() / manager.GetZonesDepth() <
                        manager.flockEntity.entitySettings.attentionRange)
                    {
                        manager.SetZonesDepth((int)(manager.GetBoundaryDepth() / manager.flockEntity.entitySettings.attentionRange));
                        Debug.Log("Boundary divided into zones smaller than the entities' attention range.");
                    }
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("showBoundaries"));
            if (Application.isPlaying == false)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundariesBase"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundaryWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundaryDepth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundaryHeight"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("showFlockRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flockRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flockTarget"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("showTriggers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggers"));

            serializedObject.ApplyModifiedProperties();
        }
    }

}

 

using UnityEngine;

namespace FlockAI
{
    public class FlockAI_TargetResponse : MonoBehaviour
    {
        FlockAI_Manager manager;
        FlockAI_Entity flockAI_Entity;
        [SerializeField] FlockAI_Entity_Settings outOfRangeSettings;
        [SerializeField] FlockAI_Entity_Settings inRangeSettings;
        float sqrAttentionRange;
        bool isInRange;

        void Start()
        {
            manager = FindObjectOfType<FlockAI_Manager>();
            flockAI_Entity = GetComponent<FlockAI_Entity>();
            sqrAttentionRange = inRangeSettings.attentionRange * inRangeSettings.attentionRange;
        }

        void Update()
        {
            // Chase if target within attention range.
            if (Vector3.SqrMagnitude(transform.position - manager.flockTarget.position) < sqrAttentionRange)
            {
                if (!isInRange)
                {
                    isInRange = true;
                    manager.UpdateEntitySettings(flockAI_Entity, inRangeSettings);
                }
            }
            // Wander if target outside attention range.
            else
            {
                if (isInRange)
                {
                    isInRange = false;
                    manager.UpdateEntitySettings(flockAI_Entity, outOfRangeSettings);
                }
            }
        }
    }
}

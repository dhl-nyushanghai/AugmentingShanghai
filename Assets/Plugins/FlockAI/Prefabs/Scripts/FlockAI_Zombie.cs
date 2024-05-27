using UnityEngine;

namespace FlockAI
{
    public class FlockAI_Zombie : MonoBehaviour
    {
        FlockAI_Manager manager;
        FlockAI_Entity flockAI_Entity;
        [SerializeField] FlockAI_Entity_Settings obliviousSettings;
        [SerializeField] FlockAI_Entity_Settings agroSettings;
        float sqrAttentionRange;
        bool isAggro;

        void Start()
        {
            manager = FindObjectOfType<FlockAI_Manager>();
            flockAI_Entity = GetComponent<FlockAI_Entity>();
            sqrAttentionRange = agroSettings.attentionRange * agroSettings.attentionRange;
        }

        void Update()
        {
            if (Vector3.SqrMagnitude(transform.position - manager.flockTarget.position) < sqrAttentionRange)
            {// Chase if target within attention range.
                if (!isAggro)
                {
                    isAggro = true;
                    manager.UpdateEntitySettings(flockAI_Entity, agroSettings);
                }
            }
            else
            {// Wander if target outside attention range.
                if (isAggro)
                {
                    isAggro = false;
                    manager.UpdateEntitySettings(flockAI_Entity, obliviousSettings);
                }
            }
        }
    }
}

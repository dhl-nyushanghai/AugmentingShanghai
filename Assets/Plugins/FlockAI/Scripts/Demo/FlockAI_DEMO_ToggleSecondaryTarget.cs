using UnityEngine;

namespace FlockAI
{
    public class FlockAI_DEMO_ToggleSecondaryTarget : MonoBehaviour
    {
        FlockAI_Manager flockAI_Manager;
        public Transform secondaryTarget;
        bool usingSecondaryTarget;

        private void Awake()
        {
            flockAI_Manager = FindObjectOfType<FlockAI_Manager>();
        }

        public void ToggleTarget()
        {
            usingSecondaryTarget = !usingSecondaryTarget;
            if (usingSecondaryTarget)
            {
                for (int i = 0; i < flockAI_Manager.entities.Count; i++)
                {
                    flockAI_Manager.entities[i].GetComponent<FlockAI_Entity>().secondaryTarget_Transform = secondaryTarget;
                }
            }
            else
            {
                for (int i = 0; i < flockAI_Manager.entities.Count; ++i)
                {
                    flockAI_Manager.entities[i].GetComponent<FlockAI_Entity>().secondaryTarget_Transform = null;
                }
            }
        }

    }
}
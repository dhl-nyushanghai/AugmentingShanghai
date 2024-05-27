using UnityEngine;

namespace FlockAI
{
    public class FlockAI_MoveFlockTarget : MonoBehaviour
    {
        [SerializeField] FlockAI_Manager flockController;
        [SerializeField] Transform flockTarget_Transform;

        float elapsedTime = 0f;
        float randomTime;
        [SerializeField] float minTime = .5f;
        [SerializeField] float maxTime = 8f;


        void Start()
        {
            randomTime = Random.Range(minTime, maxTime);
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > randomTime)
            {
                elapsedTime = 0f;
                randomTime = Random.Range(minTime, maxTime);
                ChangeTarget();
            }
        }

        void ChangeTarget()
        {
            Vector3 target = new Vector3(Random.Range(0f, flockController.GetBoundaryWidth()) - flockController.GetBoundaryWidth() / 2f,
            Random.Range(0f, flockController.GetBoundaryHeight()),
            Random.Range(0f, flockController.GetBoundaryDepth()) - flockController.GetBoundaryDepth() / 2f);
            flockTarget_Transform.transform.position = .8f * target + flockController.GetBoundariesBase().position;
        }
    }
}
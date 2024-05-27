//  This script is used for demonstrating the FlockAI Manager usage.

using UnityEngine;

namespace FlockAI
{
    public class FlockAI_DEMO_SceneController : MonoBehaviour
    {
        [SerializeField] GameObject barriers;
        bool barriersActive;

        [SerializeField] Transform flockTarget_Transform;
        Rigidbody targetBody;
        bool useGravity;
        Renderer targetRenderer;
        [Tooltip("Respawn if Target gets pushed out of bounds by the entities.")]
        [SerializeField] bool respawnTargetIfOutofBounds;

        [SerializeField] FlockAI_Manager flockController;
        [Tooltip("For changing Entity Settings at runtime or while playing in the editor.")]
        [SerializeField] FlockAI_Entity_Settings entitySettings;
        FlockAI_Entity_Settings previousflockSettings;
        [SerializeField] FlockAI_Entity_Settings[] settings;
        int settingsIndex = 0;

        void Start()
        {
            targetBody = flockTarget_Transform.gameObject.GetComponent<Rigidbody>();
            useGravity = targetBody.useGravity;
            targetRenderer = flockTarget_Transform.gameObject.GetComponent<Renderer>();
            barriersActive = barriers.activeSelf;
            previousflockSettings = entitySettings;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                barriersActive = !barriersActive;
                barriers.SetActive(barriersActive);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                useGravity = !useGravity;
                targetBody.useGravity = useGravity;
            }

            if (Input.GetKeyDown(KeyCode.H)) targetRenderer.enabled = !targetRenderer.enabled;

            if (Input.GetKeyDown(KeyCode.Space)) ChangeFlockTarget();

            if (respawnTargetIfOutofBounds &&
                ((flockTarget_Transform.transform.position.z > flockController.GetBoundariesBase().position.z + flockController.GetBoundaryDepth() / 2f) ||
                (flockTarget_Transform.transform.position.z < flockController.GetBoundariesBase().position.z - flockController.GetBoundaryDepth() / 2f) ||
                (flockTarget_Transform.transform.position.x > flockController.GetBoundariesBase().position.x + flockController.GetBoundaryWidth() / 2f) ||
                (flockTarget_Transform.transform.position.x < flockController.GetBoundariesBase().position.x - flockController.GetBoundaryWidth() / 2f)))
                ChangeFlockTarget();

            if (Input.GetKeyDown(KeyCode.P))
            {
                // The Flock Controller can be paused (Example: when out of range or sight)
                if (flockController.enabled) flockController.PauseUnpause();
                flockController.enabled = !flockController.enabled;
                if (flockController.enabled) flockController.PauseUnpause();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                flockController.CreateEntity();
            }
            if (Input.GetKeyDown(KeyCode.R))

            {
                flockController.RemoveEntity();
            }

            if (previousflockSettings != entitySettings)
            {
                previousflockSettings = entitySettings;
                flockController.UpdateEntitiesSettings(entitySettings);
            }
            if (Input.GetKeyDown(KeyCode.X) && settings.Length > 0)
            {
                settingsIndex++;
                if (settingsIndex >= settings.Length) settingsIndex = 0;
                flockController.UpdateEntitiesSettings(settings[settingsIndex]);
            }

        }

        void ChangeFlockTarget()
        {
            Vector3 target = new Vector3(Random.Range(0f, flockController.GetBoundaryWidth()) - flockController.GetBoundaryWidth() / 2f,
            Random.Range(0f, flockController.GetBoundaryHeight()),
            Random.Range(0f, flockController.GetBoundaryDepth()) - flockController.GetBoundaryDepth() / 2f);
            flockTarget_Transform.transform.position = target + flockController.GetBoundariesBase().position;
        }
    }
}
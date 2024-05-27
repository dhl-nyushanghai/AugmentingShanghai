using UnityEngine;

namespace FlockAI
{
    public class FlockAI_Cow : MonoBehaviour {

        FlockAI_Entity entity;
        AudioSource sound;
        [SerializeField] float chanceToMoo = .25f;
        [SerializeField] float checkFrequency = 3f;
        float elapsedTime;

	    void Start () {
            entity = GetComponent<FlockAI_Entity>();
            sound = GetComponent<AudioSource>();
            elapsedTime = Random.value * checkFrequency;
        }
	
	    void Update () {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > checkFrequency &&
                entity.BehaviorState == FlockAI_Entity.BehaviorStates.WANDERING && !sound.isPlaying &&
                Random.value < chanceToMoo)
            {// Mooooo (chance is when going into WANDER_IGNOREOTHERS mode)
                sound.pitch = Random.Range(.75f, 1f);
                sound.Play();
                elapsedTime = 0f;
            }
	    }
    }
}
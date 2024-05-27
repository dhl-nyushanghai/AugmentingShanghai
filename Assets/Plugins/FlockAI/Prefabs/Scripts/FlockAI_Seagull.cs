using UnityEngine;

namespace FlockAI
{
    public class FlockAI_Seagull : MonoBehaviour {

        FlockAI_Entity entity;
        AudioSource sound;

	    void Start () {
            entity = GetComponent<FlockAI_Entity>();
            sound = GetComponent<AudioSource>();
	    }
	
	    void Update () {
		    if (entity.somethingIsUnder)
            {
                if ((
                        (entity.somethingIsUnderneath_Object != null && entity.somethingIsUnderneath_Object.GetComponent<FlockAI_Seagull>() != null)
                        || 
                        (entity.somethingIsAbove_Object != null && entity.somethingIsAbove_Object.GetComponent<FlockAI_Seagull>() != null) 
                    )
                    && !sound.isPlaying)
                {   // Screech if another seagull (but not some other kind of thing) is directly underneath or above.
                    //   (because we're thieving sky-jerks)
                    sound.pitch = Random.Range(.8f, 1f);
                    sound.Play();
                }
            }
	    }
    }
}

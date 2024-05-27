using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioContent : MonoBehaviour
{
    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if(_audioSource == null)
            Debug.LogError("AudioSource not found in " + gameObject.name);
    }
    
    public void PlayAudio()
    {
        if(!_audioSource.isPlaying)
            _audioSource.Play();
    }
    
    public void StopAudio()
    {
        if(_audioSource.isPlaying)
            _audioSource.Stop();
    }
    
    public void PlayOneShot()
    {
        _audioSource.PlayOneShot(_audioSource.clip);
    }
    
    public void PauseAudio()
    {
        if(_audioSource.isPlaying)
            _audioSource.Pause();
    }
}

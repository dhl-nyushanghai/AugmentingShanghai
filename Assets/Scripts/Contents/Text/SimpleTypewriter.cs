using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using TMPro;

public class SimpleTypewriter : MonoBehaviour
{
    private TMP_Text _text;
    [CanBeNull] private AudioSource _typeAudioSource;
    [TextArea,SerializeField,Header("If you need to use the typewriter effect,\nyou need to set the text here"),Space(10)] 
    private string contentToType;
    [SerializeField,Space(5)] private float TypeSpeed = 0.1f;

    [Header("Setting")]
    [SerializeField] private bool typeOnStart = false;
    [CanBeNull,SerializeField,Header("If you need to play a sound when typing,\nyou need to set the AudioSource here"),Space(10)] 
    private AudioClip typeSound;
    
    private bool _isPlayed = false;

    private void Start()
    {
        _text = GetComponent<TMP_Text>();
        if(_text == null)
            Debug.LogError("TextMeshPro not found in " + gameObject.name);
        
        TryGetComponent(out _typeAudioSource); // Try to get the AudioSource component
        
        if (typeOnStart)
        {
            StartCoroutine(TypeText(contentToType));
        }
    }
    
    public void StartTyping()
    {
        StartCoroutine(TypeText(contentToType));
    }
    
    private IEnumerator TypeText(string textToType)
    {
        if (_isPlayed)
        {
            Debug.LogWarning("Text has been typed on " + gameObject.name);
            yield break;
        }
        _isPlayed = true;
        _text.text = "";
        foreach (var c in textToType)
        {
            _text.text += c;
            if(typeSound != null && _typeAudioSource != null)
                _typeAudioSource.PlayOneShot(typeSound);
            yield return new WaitForSeconds(TypeSpeed);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FadeText : MonoBehaviour
{
    public float fadeDuration;
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if(_text == null)
            Debug.LogError("TextMeshPro not found in " + gameObject.name);
    }
    
    
    public void FadeIn()
    {
        StartCoroutine(TextFade(_text, 1f, fadeDuration));
    }
    
    public void FadeOut()
    {
        StartCoroutine(TextFade(_text, 0f, fadeDuration));
    }
    
    private static IEnumerator TextFade(TMP_Text text, float targetAlpha, float duration) {
        var originalAlpha = text.color.a;
        var elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            var alpha = Mathf.Lerp(originalAlpha, targetAlpha, elapsed / duration);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }
    }
    
}

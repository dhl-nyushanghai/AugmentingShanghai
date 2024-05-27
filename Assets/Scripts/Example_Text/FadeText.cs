using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FadeText : MonoBehaviour
{
    public float fadeDuration;
    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }
    
    
    public void FadeIn()
    {
        StartCoroutine(TextFadeIn(text, fadeDuration));
    }
    
    public void FadeOut()
    {
        StartCoroutine(TextFadeOut(text, fadeDuration));
    }
    
    
    IEnumerator TextFadeIn(TMP_Text text, float duration) {
        float originalAlpha = text.color.a;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalAlpha, 1f, elapsed / duration);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }
    }
    
    IEnumerator TextFadeOut(TMP_Text text, float duration) {
        float originalAlpha = text.color.a;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalAlpha, 0f, elapsed / duration);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }
    }
    
}

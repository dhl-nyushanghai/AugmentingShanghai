using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeMesh : MonoBehaviour
{
    
    public float fadeDuration;
    private MeshRenderer meshRenderer;
    
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Fade mesh in
    public void FadeIn()
    {
        StartCoroutine(MeshFadeIn(meshRenderer, fadeDuration));
    }
    
    // Fade mesh out
    public void FadeOut()
    {
        StartCoroutine(MeshFadeOut(meshRenderer, fadeDuration));
    }
    
    IEnumerator MeshFadeIn(MeshRenderer meshRenderer, float duration) {
        float originalAlpha = meshRenderer.material.color.a;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalAlpha, 1f, elapsed / duration);
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, alpha);
            yield return null;
        }
    }
    
    IEnumerator MeshFadeOut(MeshRenderer meshRenderer, float duration) {
        float originalAlpha = meshRenderer.material.color.a;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalAlpha, 0f, elapsed / duration);
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, alpha);
            yield return null;
        }
    }
    
}

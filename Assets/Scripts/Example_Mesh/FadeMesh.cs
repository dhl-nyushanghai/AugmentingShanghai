using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeMesh : MonoBehaviour
{
    
    public float fadeDuration;
    private MeshRenderer _meshRenderer;
    
    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        if(_meshRenderer == null)
            Debug.LogError("MeshRenderer not found in " + gameObject.name);
    }

    // Fade mesh in
    public void FadeIn()
    {
        StartCoroutine(MeshFade(_meshRenderer, 1f, fadeDuration));
    }
    
    // Fade mesh out
    public void FadeOut()
    {
        StartCoroutine(MeshFade(_meshRenderer, 0f,fadeDuration));
    }
    
    
    private static IEnumerator MeshFade(Renderer meshRenderer, float targetAlpha, float duration) {
        var originalAlpha = meshRenderer.material.color.a;
        var elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            var alpha = Mathf.Lerp(originalAlpha, targetAlpha, elapsed / duration);
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, alpha);
            yield return null;
        }
    }
}

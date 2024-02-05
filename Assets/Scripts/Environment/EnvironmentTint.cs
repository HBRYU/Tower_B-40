using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class EnvironmentTint : MonoBehaviour
{
    public bool applyOnStart;
    public bool applyColor;
    public Color tint;
    [Range(0f, 1f)] public float weight;
    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (applyOnStart)
            Apply();
    }

    public void Apply()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_TintWeight", weight); // Make sure the shader property name matches
        if (applyColor)
            propertyBlock.SetColor("_Color", tint); // Make sure the shader property name matches

        sr.SetPropertyBlock(propertyBlock);
        //print("Applied weight: " + weight + ", allegedly.");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// EnvironmentTint modifies the material properties of a SpriteRenderer at runtime to apply a tint effect.
/// </summary>
/// <remarks>
/// - Attach this script to GameObjects with a SpriteRenderer component that you want to tint.
/// - The script allows for dynamic changes in tint color and weight.
/// - It utilizes MaterialPropertyBlock for efficient material modifications without affecting other instances of the material.
/// 
/// Usage:
/// - Enable 'applyOnStart' to apply the tint when the object is first initialized.
/// - Use 'applyColor' to decide if the tint color should be applied.
/// - Adjust 'tint' to change the color of the tint.
/// - Use 'weight' to control the intensity of the tint effect. 
/// - Call the Apply method to update the tint effect at runtime.
/// </remarks>

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
        if(sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        sr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_TintWeight", weight); // Make sure the shader property name matches
        if (applyColor)
            propertyBlock.SetColor("_Color", tint); // Make sure the shader property name matches

        sr.SetPropertyBlock(propertyBlock);
    }
}

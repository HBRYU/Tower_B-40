using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGParallax handles the parallax effect for background elements in a scene.
/// It moves background objects at different speeds based on their Z position to create a depth illusion.
/// </summary>
/// <remarks>
/// - Attach this script to a GameObject in your scene (preferably the camera or a dedicated manager GameObject).
/// - The script operates on child objects of a specified parent transform.
/// - Each child's parallax effect is determined by its Z position and the specified parallax multiplier.
/// - Optionally, it can also apply a color tint based on the Z position.
/// 
/// Usage:
/// - Set the 'parentFolder' to the transform containing the background objects.
/// - Adjust 'parallaxZMultiplier' to control the depth effect's strength.
/// - If 'zTintMultiplier' is greater than 0, a color tint effect is applied to the objects.
/// - The 'color' field specifies the tint color to be applied.
/// - Set 'xOnly' to true if the parallax should only affect the X-axis.
/// </remarks>

public class BGParallax : MonoBehaviour
{
    public Transform parentFolder;
    [SerializeField] private float parallaxZMultiplier;
    private List<Transform> parallaxTransforms = new List<Transform>();

    private Vector3 prevCameraPos;

    private Transform cameraTransform;

    [SerializeField] private bool xOnly = true;

    [SerializeField] [Tooltip("0 for 'don't apply'")] private float zTintMultiplier;

    [SerializeField] private bool applyColor;

    [SerializeField] private Color color;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < parentFolder.childCount; i++)
        {
            parallaxTransforms.Add(parentFolder.GetChild(i).transform);
            if (zTintMultiplier > 0f)
            {
                ApplyZTint(parentFolder.GetChild(i).GetComponent<EnvironmentTint>(), parentFolder.GetChild(i).position.z);
            }
        }

        cameraTransform = Camera.main.transform;
        prevCameraPos = cameraTransform.position;
    }

    void ApplyZTint(EnvironmentTint environmentTint, float z)
    {
        environmentTint.weight = Mathf.Clamp01(z * zTintMultiplier);
        if (applyColor)
        {
            environmentTint.applyColor = true;
            environmentTint.tint = color;
        }
        environmentTint.Apply();
    }

    void FixedUpdate()
    {
        Vector3 deltaPos = cameraTransform.position - prevCameraPos;
        if (xOnly) deltaPos.y = 0;

        foreach (var objTransform in parallaxTransforms)
        {
            float amount = (objTransform.position.z * parallaxZMultiplier) > 1f
                ? 1f
                : objTransform.position.z * parallaxZMultiplier;
            objTransform.position += deltaPos * amount;
        }

        prevCameraPos = cameraTransform.position;
    }
}

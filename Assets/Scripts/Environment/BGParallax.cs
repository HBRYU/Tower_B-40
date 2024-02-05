using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Update is called once per frame
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

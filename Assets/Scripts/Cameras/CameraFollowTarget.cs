using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float speed;

    // Used for maintaining position.z=-10 
    [SerializeField] private Vector3 offset;

    // basePosition: the camera's aim pivot-point.
    // aimedPosition: the camera's aimed final target position.
    private Vector3 basePosition, aimedPosition;
    // Used for calculating aimedPosition.
    [SerializeField] private float sightDistanceCoeff;

    void Start()
    {
        if (target == null)
            target = GM.GetPlayer().transform;
    }
    
    void FixedUpdate()
    {
        basePosition = target.position + offset;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        aimedPosition = Vector3.Lerp(basePosition, mousePosition, sightDistanceCoeff);

        transform.position =  Vector3.Lerp(transform.position, aimedPosition, speed);
    }
}

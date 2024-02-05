using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public Transform parentFolder;
    private List<Transform> transforms = new List<Transform>();
    public float xSpeed = 0.1f;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < parentFolder.childCount; i++)
        {
            transforms.Add(parentFolder.GetChild(i));
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (var t in transforms)
        {
            var position = t.position;
            position += Vector3.right * (xSpeed / position.z);
            t.position = position;
        }
    }
}

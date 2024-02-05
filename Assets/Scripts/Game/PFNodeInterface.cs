using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[SerializeField]
public class PFNode
{
    public Vector2 position;
    public PFNode[] adjacentNodes;
}

public class PFNodeInterface : MonoBehaviour
{
    public PFNode node = new PFNode();
    public int index;
    public List<Transform> adjacentNodeTransforms;
    public bool drawDebugLines;
    
    // Start is called before the first frame update
    void Start()
    {
        node.position = transform.position;
        node.adjacentNodes = adjacentNodeTransforms.Select(t => t.GetComponent<PFNodeInterface>().node).ToArray();
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugLines)
            return;
        
        // Handles.Label(transform.position, index.ToString());
        foreach (var t in adjacentNodeTransforms)
        {
            Gizmos.DrawLine(transform.position, t.position);
        }
    }
}

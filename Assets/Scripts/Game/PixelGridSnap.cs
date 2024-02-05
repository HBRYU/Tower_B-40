using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelGridSnap : MonoBehaviour
{
    public int pixelsPerUnit = 16;  // PPU of the pixel grid for the sprite to snap to
    public bool useVelocityCeil;    
    public float velocityCeil = 1f; // Max velocity of the sprite to apply snapping

    private Vector3 prevPos;
    void FixedUpdate()
    {
        Vector3 parentPos = transform.parent.position;
        if (((parentPos - prevPos) / Time.fixedDeltaTime).sqrMagnitude <= velocityCeil * velocityCeil || !useVelocityCeil)
        {
            Vector3 mod = new Vector3(Mod(parentPos.x, 1f/pixelsPerUnit), Mod(parentPos.x, 1f/pixelsPerUnit));
            transform.position = parentPos - mod;
        }
        else
        {
            transform.position = parentPos;
        }
        prevPos = parentPos;
    }

    float Mod(float a, float b)
    {
        if (a > 0f)
        {
            return a % b;
        }

        return -(-a % b);
    }
}

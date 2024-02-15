using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Foliage : MonoBehaviour
{
    public Sprite[] windRightSprites, windLeftSprites;  // stronger winds as index increases
    private SpriteRenderer sr;
    public float windSpeed = 1f;
    
    float WindValue(float x)
    {
        // What the fuck to do with t
        float t = Time.realtimeSinceStartup % (10000f * Mathf.PI) * windSpeed * Time.timeScale;
        float globalAvgWind = 0.5f * Mathf.Sin(0.05f * t);
        
        // Some random combination of trigonometric functions to get smooth values between -1 and 1
        return 0.5f * Mathf.Sin(0.1f * x + t) * Mathf.Cos(0.2f * x + 2 * t) * Mathf.Sin(0.05f * x + 0.1f * t) + globalAvgWind;
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        float wind = WindValue(transform.position.x);
        if (wind >= 0f)
        {
            // Right wind
            
            if (wind >= 1f)
                wind = 0.99f;
            int index = Mathf.FloorToInt(wind * windRightSprites.Length);
            sr.sprite = windRightSprites[index];
        }
        else
        {
            // Left wind
            
            if (wind <= -1f)
                wind = -0.99f;
            int index = Mathf.FloorToInt(-wind * windLeftSprites.Length);
            sr.sprite = windLeftSprites[index];
        }
    }
}

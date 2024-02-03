using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWingsAudio : MonoBehaviour
{
    public List<PlayerWing> wings = new List<PlayerWing>();
    public List<AudioSource> audioSourcesSine = new List<AudioSource>();
    public List<AudioSource> audioSourcesSaw = new List<AudioSource>();
    public AudioClip sine, saw;
    public float velocityCap, velocitySmoothing;
    private List<float> prevVelocities = new List<float>();

    private bool playingSine, playingSaw;

    // Update is called once per frame

    public void Setup()
    {
        // Called in PlayerWingsBehaviour.Start()
        foreach (var wing in wings)
        {
            audioSourcesSine.Add(GM.GetAudioManager().Request(sine,
                () => wing.position + transform.position,
                () => FreeWingAudioInstance(wing),
                volume: 0.5f, reverb: 0f, loop: true, spatialBlend: 0.6f, priority: 50).AudioSource);
            audioSourcesSaw.Add(GM.GetAudioManager().Request(saw,
                () => wing.position + transform.position,
                () => FreeWingAudioInstance(wing),
                volume: 0.5f, reverb: 0f, loop: true, spatialBlend: 0.6f, priority: 50).AudioSource);
            prevVelocities.Add(0f);
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < wings.Count; i++)
        {
            var wing = wings[i];
            var velocity = wing.Velocity * (1f-velocitySmoothing) + prevVelocities[i] * velocitySmoothing;
            prevVelocities[i] = velocity;
            float totalVolume = 0.5f, sineMix = 0.5f, sawMix = 2f;
            // print(velocity);
            float sawWeight = Mathf.Clamp01(velocity / (velocityCap + 0.001f));
            float sineWeight = 1 - sawWeight;

            if (playingSaw)
            {
                audioSourcesSine[i].volume = 0f;
                audioSourcesSaw[i].volume = 0f;
            }
            else
            {
                audioSourcesSine[i].volume = totalVolume * sineMix * sineWeight;
                audioSourcesSaw[i].volume = totalVolume * sawMix * sawWeight;
                if (sawWeight > 0.3f)
                {
                    playingSaw = true;
                }
            }
        }

        playingSaw = false;
    }

    bool FreeWingAudioInstance(PlayerWing wing)
    {
        return false;
    }
}

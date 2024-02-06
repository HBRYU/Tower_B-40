using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimicAudio : MonoBehaviour
{
    private AudioManager audioManager;
    public AudioClip alertSFX;
    
    
    // Start is called before the first frame update
    void Start()
    {
        audioManager = GM.GetAudioManager();
    }

    public void PlayAlertSFX()
    {
        audioManager.Request(alertSFX, () => transform.position, null,
            volume: 1f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private AudioManager audioManager;
    private PlayerMovement playerMovement;
    public AudioClip walkSFX, jumpSFX, landSFX, dashSFX;
    
    void Start()
    {
        audioManager = GM.GetAudioManager();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void PlayWalkSFX()
    {
        audioManager.Request(walkSFX,
             () => transform.position,
             () => (Input.GetAxisRaw("Horizontal") ==0 || !playerMovement.OnGround),
             volume:0.7f, reverb:0.25f, loop:true, priority: 100);
    }

    public void PlayJumpSFX()
    {
        audioManager.Request(jumpSFX,
            () => transform.position,
            null,  // Free on clip end
            volume: 1f, loop: false, priority: 100);
    }
    
    public void PlayLandSFX()
    {
        audioManager.Request(landSFX,
            () => transform.position,
            null,  // Free on clip end
            volume: 0.5f, loop: false, priority: 100);
    }

    public void PlayDashSFX()
    {
        audioManager.Request(dashSFX,
            () => transform.position,
            null,  // Free on clip end
            volume: 0.3f, loop: false, priority: 100);
    }
}

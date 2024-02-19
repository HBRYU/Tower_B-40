using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimicAudio : MonoBehaviour
{
    private AudioManager audioManager;
    public AudioClip alertSFX, searchSFX, warningSFX, slashSFX, releaseSFX, deathSFX;

    public AudioClip[] legMoveSFX, legSFX;
    
    
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

    public void PlaySearchSFX()
    {
        audioManager.Request(searchSFX, () => transform.position, null,
            volume: 0.5f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }

    public void PlayLegMoveSFX(Vector3 position, int i = 0, bool random = false)
    {
        if (random)
            i = Random.Range(0, legMoveSFX.Length);
        audioManager.Request(legMoveSFX[i], () => position, null,
            volume: 0.3f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }
    
    public void PlayLegSFX(Vector3 position, int i = 0, bool random = false)
    {
        if (random)
            i = Random.Range(0, legSFX.Length);
        audioManager.Request(legSFX[i], () => position, null,
            volume: 0.3f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }

    public void PlayWarningSFX()
    {
        audioManager.Request(warningSFX, () => transform.position, null,
            volume: 1f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }

    public void PlaySlashSFX(Transform clawTransform)
    {
        audioManager.Request(slashSFX, () => clawTransform.position, null,
            volume: 1f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }

    public void PlayReleaseSFX()
    {
        audioManager.Request(releaseSFX, () => transform.position, null,
            volume: 1f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 50);
    }
    
    public void PlayDeathSFX()
    {
        audioManager.Request(deathSFX, () => transform.position, null,
            volume: 1f, reverb: 0.5f, loop: false, spatialBlend: 0.5f, priority: 100);
    }
}

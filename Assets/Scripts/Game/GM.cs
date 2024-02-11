using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GM : MonoBehaviour
{
    // Singleton design pattern
    public static GM Instance { get; private set; }
    public static GameObject PlayerInstance { get; private set; }
    [SerializeField] private PFManager pfManager;
    [SerializeField] private AudioManager audioManager;
    public static InGameUI InGameUIInstance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        PlayerInstance = GetPlayer();
        pfManager = GetComponent<PFManager>();
        InGameUIInstance = GameObject.FindGameObjectWithTag("InGameUI").GetComponent<InGameUI>();
    }


    public float physicsSpeedMutlitplier = 1f;
    public Tilemap standardAStarTilemap, colliderTilemap;
    public TimerManager timerManager = new ();
    
    // Start is called before the first frame update
    void Start()
    {
        physicsSpeedMutlitplier = Mathf.Max(0f, physicsSpeedMutlitplier);
    }

    private void Update()
    {
        timerManager.UpdateTimers(Time.deltaTime);
        Time.timeScale = physicsSpeedMutlitplier;
    }

    private void FixedUpdate()
    {
        timerManager.UpdateTimers(Time.fixedDeltaTime);
    }

    // public static GM GetGM()
    // {
    //     return GameObject.FindGameObjectWithTag("GM").GetComponent<GM>();
    // }

    public static GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }

    public static Vector3 GetPlayerPosition()
    {
        return PlayerInstance.transform.position;
    }

    public static PFManager GetPFManager()
    {
        return Instance.pfManager;
    }

    public static AudioManager GetAudioManager()
    {
        return Instance.audioManager;
    }
}

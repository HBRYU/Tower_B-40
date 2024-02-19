using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// AudioManager is a singleton class responsible for managing audio playback in the game.
/// It uses a pooling system to efficiently handle multiple audio sources, allowing for
/// the prioritization and dynamic playback of audio clips. AudioSourceInstances are used
/// to manage individual audio sources, with functionality to set the audio clip, volume,
/// and other properties, as well as to handle positioning and conditional freeing of the sources.
/// 
/// Usage:
/// - Call the Request method to play an audio clip with specified properties such as volume,
///   reverb, looping, and priority. The AudioManager will either use a free audio source from the pool
///   or replace a currently playing source with lower priority.
/// - The AudioManager supports updating the position of audio sources and freeing them based on
///   custom conditions, making it suitable for a variety of audio playback scenarios in the game.
///
/// Note:
/// - Ensure the maxInstances value is set according to the needs of the game and available resources.
/// - The class uses a simple priority system where the instance with lowest Priority gets replaced if
///   instances are full.
/// </summary>


public class AudioManager : MonoBehaviour
{
    public int maxInstances = 50; // Maximum number of audio instances to be handled

    public class AudioSourceInstance
    {
        public AudioSource AudioSource { get; private set; }
        public bool free; // Flag indicating if the AudioSourceInstance is available
        public int Priority { get; private set; }
        public Func<Vector3> TargetPositionGetter {get; private set;}  // Function to get the position for the audio source
        public Func<bool> FreeCondition {get; private set;}           // Function to determine when to free the audio source

        public AudioSourceInstance(AudioSource audioSource, bool free, Func<Vector3> targetPositionGetter)
        {
            AudioSource = audioSource;
            this.free = free;
            TargetPositionGetter = targetPositionGetter;
        }

        public void UpdatePosition()
        {
            // Update audio source position if a position getter is provided
            if(TargetPositionGetter != null)
                AudioSource.transform.position = TargetPositionGetter();
        }

        // Sets the audio clip and related properties for playback
        public void SetAudioSource(AudioClip clip, Func<Vector3> targetPositionGetter, Func<bool> freeCondition, 
            float volume = 1f, float reverb = 0f, bool loop = false, float spatialBlend = 0f, int priority = 0)
        {
            free = false;
            AudioSource.clip = clip;
            AudioSource.gameObject.SetActive(true);

            AudioSource.volume = volume;
            AudioSource.reverbZoneMix = reverb;
            AudioSource.loop = loop;
            AudioSource.spatialBlend = spatialBlend;
            
            // No one fucking needs doppler effect
            AudioSource.dopplerLevel = 0f;

            TargetPositionGetter = targetPositionGetter;
            FreeCondition = freeCondition ?? (() => !AudioSource.isPlaying);  // Apply default if null
            
            Priority = priority;
        }

        // Frees the audio source for reuse
        public void Free()
        {
            free = true;
            AudioSource.Stop();
            AudioSource.gameObject.SetActive(false);
        }
    }

    private List<AudioSourceInstance> instances; // Pool of audio source instances

    private void Awake()
    {
        // Initialize the pool of audio source instances
        instances = new List<AudioSourceInstance>();
        for (int i = 0; i < maxInstances; i++)
        {
            GameObject thisObj = new GameObject("AudioSourceObject", typeof(AudioSource));
            thisObj.transform.SetParent(transform, false);
            AudioSource thisAudioSource = thisObj.GetComponent<AudioSource>();
            thisObj.SetActive(false);

            instances.Add(new AudioSourceInstance(thisAudioSource, true, () => Vector3.zero));
        }
    }

    void Update()
    {
        // Iterate through all instances to update positions and free if conditions are met
        foreach (var instance in instances)
        {
            if(instance.free)
                continue;
                
            instance.UpdatePosition();
            if (instance.FreeCondition != null && instance.FreeCondition())
            {
                instance.Free();
            }
        }
    }

    
    /// <summary>
    /// Requests an audio clip to be played, using an available or the least important audio source instance.
    /// </summary>
    /// <param name="clip">The audio clip to play.</param>
    /// <param name="targetPositionGetter">Function that returns the Vector3 position for the audio source.</param>
    /// <param name="freeCondition">Function that defines the condition under which the audio source will be freed. (null=Free on clip end)</param>
    /// <param name="volume">Volume of the audio clip (default 1f).</param>
    /// <param name="reverb">Reverb mix of the audio source (default 0f).</param>
    /// <param name="spatialBlend">Spatial blend for stereo audio (default 0f).</param> 
    /// <param name="loop">Whether the audio should loop (default false).</param>
    /// <param name="priority">Priority of the audio clip. Lower numbers are higher priority (default 0).</param>
    /// <returns>The AudioSourceInstance that is used to play the requested audio clip, or null if no instance is available.</returns>
    /// <remarks>
    /// The method will first try to find a free audio source instance. If none are available, 
    /// it will look for an instance playing a clip with a lower priority and replace it. 
    /// If all instances are playing higher priority clips, the request will be ignored, and null is returned.
    /// </remarks>
    public AudioSourceInstance Request(AudioClip clip, Func<Vector3> targetPositionGetter, Func<bool> freeCondition, float volume = 1f,
        float reverb = 0f, bool loop = false, float spatialBlend = 0f, int priority = 0)
    {
        int lowestPriority = int.MaxValue;
        AudioSourceInstance lowestPriorityInstance = null;
        foreach (var instance in instances)
        {
            if (instance.free)
            {
                instance.SetAudioSource(clip, targetPositionGetter, freeCondition, volume, reverb, loop, spatialBlend, priority);
                return instance;
            }

            if (lowestPriority > instance.Priority)
            {
                lowestPriority = instance.Priority;
                lowestPriorityInstance = instance;
            }
        }

        if (lowestPriority < priority)
        {
            lowestPriorityInstance.SetAudioSource(clip, targetPositionGetter, freeCondition, volume, reverb, loop, spatialBlend, priority);
            return lowestPriorityInstance;
        }
        
        // No free instances
        Debug.Log("Audio pool instances full. Request ignored.");
        return null;
    }
}

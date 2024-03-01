using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class DemoLM : MonoBehaviour
{
    [System.Serializable]
    public struct TriggerEvent
    {
        public string name;
        public TriggerObject triggerObject;
    }

    public List<TriggerEvent> triggerEvents;

    public GameObject[] flies;
    public GameObject mimic;
    
    void Start()
    {
        
    }

    void SpawnFlies()
    {
        foreach (var fly in flies)
        {
            fly.SetActive(true);
        }
    }

    void EnterBossRoom()
    {
        mimic.SetActive(true);
        CloseBossRoomDoor();
        //StartTimer();
    }

    void CloseBossRoomDoor()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Unity.VisualScripting.Dependencies.NCalc;
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
    public Animator bossRoomGateAnimator;
    
    void Start()
    {
        SetupTriggerActions();
    }

    void Update()
    {
        int n = 0;
        foreach (var fly in flies)
        {
            if (fly.activeSelf && fly.GetComponent<MobStatsInterface>().stats.Dead)
                n++;
        }

        if (n == 3)
        {
            bossRoomGateAnimator.SetTrigger("Open Gate");
        }
    }

    void SetupTriggerActions()
    {
        triggerEvents[0].triggerObject.triggerAction = SpawnFlies;
        triggerEvents[0].triggerObject.destroyOnTrigger = true;
        triggerEvents[1].triggerObject.triggerAction = EnterBossRoom;
        triggerEvents[1].triggerObject.destroyOnTrigger = true;
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
        bossRoomGateAnimator.SetTrigger("Close Gate");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TriggerObject : MonoBehaviour
{
    public Action triggerAction;
    public bool destroyOnTrigger;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == GM.PlayerInstance)
        {
            triggerAction?.Invoke();
            if(destroyOnTrigger)
                Destroy(gameObject);
        }
    }
}

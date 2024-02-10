using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MobStats
{
    public string id;
    public bool Dead { get; private set; }
    public float health, maxHealth;
    public Action deathAction, takeDamageAction;

    public MobStats(string id, float health)
    {
        this.id = id;
        this.health = health;
        Dead = false;
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            Die();
        }

        takeDamageAction();
    }

    public void Die()
    {
        deathAction();
        Dead = true;
    }
}

public class MobStatsInterface : MonoBehaviour
{
    public MobStats stats;
}

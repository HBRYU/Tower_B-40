using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingOil :  MonoBehaviour{
    private PlayerStats playerStats;

    public float healingAmount;

    void Start()
    {
        playerStats = GM.PlayerInstance.GetComponent<PlayerStats>();
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject == GM.PlayerInstance){
            heal();
            Destroy(gameObject);
        }
    }

    public void heal(){
        playerStats.Heal(healingAmount);
    }

}
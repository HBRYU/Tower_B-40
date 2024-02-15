using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingOil :  MonoBehaviour{
    private PlayerStats playerStats;

    public float healing_amount;

    void Start()
    {
        playerStats = GM.PlayerInstance.GetComponent<PlayerStats>();
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.name == "Player"){
            heal();
            Destroy(gameObject);
        }
    }

    public void heal(){
        playerStats.Heal(healing_amount);
    }

}
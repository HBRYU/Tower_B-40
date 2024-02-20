using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BreakObj :  MonoBehaviour{
    public GameObject itemPrefab;

    private MobStats stats;

    void Start(){
        stats = GetComponent<MobStatsInterface>().stats;
    }
    

    public void Update(){
        // for some reasome, stats.Dead doesn't work
        if (stats.health < 0f){
            // spawn item
            GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
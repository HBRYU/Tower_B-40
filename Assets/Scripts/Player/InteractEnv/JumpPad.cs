// JumpPad that makes the player jump without controlling

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class JumpPad : MonoBehaviour
{
    private PlayerMovement playerMovement;  // Changed gameObject to PlayerMovement to avoid frequent GetComponent()
    public float coeff; // how much the player will jump

    private void Start()
    {
        // Auto assign playerMovement instead of using public.
        // You can access GM to fetch useful GameObjects and Components! Check GM.cs
        playerMovement = GM.PlayerInstance.GetComponent<PlayerMovement>();
    }

    // on collision, player will jump on contact
    private void OnCollisionEnter2D(Collision2D other) {
        // Changed name comparison to CompareTag()
        if (other.gameObject.CompareTag("Player")){
            playerMovement.jumpSpeed *= coeff;
            playerMovement.Free_Jump();
            playerMovement.jumpSpeed /= coeff;
        }
    }

}

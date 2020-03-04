﻿// ================================================================================================================================
// File:        ElectrodeAI.cs
// Description:	Electrodes are stationary 
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ElectrodeAI : MonoBehaviour
{
    public Animator AnimationController;    //Used to trigger the decay animation when the electrode has been shot by the player
    private bool IsActive = true;   //Flagged once the decay process begins so we dont start it over again
    public float DecayLength = 0f;    //How long this type of electrode takes to play out its decay animation
    private float DecayRemaining = 0f;  //Time remaining until the decay animation is complete

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Ignore all collisions once the decay process has begun
        if (!IsActive)
            return;

        //Begin the decay process if the electrode is hit by one of the players projectiles
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            //Destroy the players projectile and start the decay process
            GameObject.Destroy(collision.gameObject);
            IsActive = false;
            AnimationController.SetTrigger("Decay");
            DecayRemaining = DecayLength;
        }
    }

    private void Update()
    {
        //Countdown the decay timer once its been started
        if(!IsActive)
        {
            DecayRemaining -= Time.deltaTime;

            //Destroy the Electrode once the decay animation has finished playing out
            if (DecayRemaining <= 0.0f)
                GameObject.Destroy(this.gameObject);
        }
    }
}
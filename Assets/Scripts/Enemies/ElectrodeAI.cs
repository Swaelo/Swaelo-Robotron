// ================================================================================================================================
// File:        ElectrodeAI.cs
// Description:	Electrodes are stationary 
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ElectrodeAI : HostileEntity
{
    public Animator AnimationController;    //Used to trigger the decay animation when the electrode has been shot by the player
    private bool IsActive = true;   //Flagged once the decay process begins so we dont start it over again
    public float DecayLength = 0f;    //How long this type of electrode takes to play out its decay animation
    private float DecayRemaining = 0f;  //Time remaining until the decay animation is complete

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Countdown the decay timer once its been started
        if (!IsActive)
        {
            DecayRemaining -= Time.deltaTime;

            //Destroy the Electrode once the decay animation has finished playing out
            if (DecayRemaining <= 0.0f)
            {
                WaveManager.Instance.EnemyDead(this);
                GameObject.Destroy(this.gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Player projectiles which hit the Electrode are destroyed, also killing the Electrode in the process
        if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            Die();
        }
        //Grunts which hit the Electrode are killed, also destroying the Electrode in the process
        else if (collision.transform.CompareTag("Grunt"))
            Die();
        //Kill both the Player character and the Electrode if they come into contact with one another
        else if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }

    //Triggers the electrodes death animation and tells the wave manager this entity has been destroyed
    private void Die()
    {
        //Play sound
        SoundEffectsPlayer.Instance.PlaySound("ElectrodeDie");
        //Start the death animation
        IsActive = false;
        AnimationController.SetBool("IsDead", true);
        DecayRemaining = DecayLength;
        //Destroy the collider component so no unwanted collisions occur during the death animation
        Destroy(GetComponent<BoxCollider2D>());
    }
}

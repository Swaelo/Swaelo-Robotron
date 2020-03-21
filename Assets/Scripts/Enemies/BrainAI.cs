// ================================================================================================================================
// File:        BrainAI.cs
// Description:	Brains move around quite slowly, they fire cruise missiles at the player and they seek out any human survivors they
//              can find so they can turn them into Progs
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class BrainAI : HostileEntity
{
    private float MoveSpeed = 0.65f;    //How fast the Brains can move around the level
    private bool IsAlive = true;    //Flagged once the Brain has been killed so it can remain until its death animation has finished playing
    private float DeathAnimationRemaining = 0.417f; //Time remaining until the Brains death animation is finished playing out
    public Animator[] AnimationControllers;    //Animation controllers which must be told to play the death animation when the Brain is killed
    private GameObject HumanTarget; //Current human the Brain is moving towards so it can be turned into a Prog

    private void Update()
    {
        //All Enemy AI is disabled during the round warmup period
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        if (!IsAlive)
            PlayDeath();
    }

    //Waits for the death animation to complete before the brain destroys itself
    private void PlayDeath()
    {
        //Wait for the animation timer to expire
        DeathAnimationRemaining -= Time.deltaTime;
        if (DeathAnimationRemaining <= 0.0f)
        {
            WaveManager.Instance.TargetEnemyDead(this);
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Destroy any player projectiles which hit the Brain, also killing the Brain in the process
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            Die();
        }
        //Kill the player character on contact
        else if(collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }

    //Triggers the death animation to start playing and removes any components which are no longer needed
    private void Die()
    {
        //Tell the wave manager this enemy is now dead, and award points to the player for destroying it
        GameState.Instance.IncreaseScore((int)PointValue.Brain);
        //Set the Brain as dead and trigger the death animation
        IsAlive = false;
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetTrigger("Death");
        //Destroy the rigidbody and boxcollider components so no unwanted collision event occur during the death animation
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
    }
}

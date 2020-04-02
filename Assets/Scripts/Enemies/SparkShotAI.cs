// ================================================================================================================================
// File:        SparkShotAI.cs
// Description:	Spark Shots are the projectiles which are fired by the Enforcer enemies.
//              Spark Shots will be shot toward the player with a small random amount of spread, a certain percentage of these shots
//              however will be aimed at the players predicated location, guaranteeing to hit them if they dont adjust their movement
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class SparkShotAI : MonoBehaviour
{
    private EnforcerAI ParentEnforcer = null;   //Keep reference to the Enforcer which fired this projectile so it can be updated when this projected is destroyed

    //Movement
    private bool MovementSet = false;   //The shot wont move until its been given a direction and speed by the Enforcer who fired it
    private float Speed = 0f;   //The projectiles speed of travel
    private Vector3 Direction;  //The projectiles direction of travel

    //Life/Death
    private bool IsAlive = true;    //Set to false once its been hit by once of the players projectiles
    private float DeathAnimationRemaining = 0.417f; //How long until the shots death animation is finished playing out
    public Animator AnimationController;    //Referenced to be told when to start playing the death animation
    private float ProjectileLifetime = 5.0f;    //Maximum lifespan of the projectile

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Perform normal behaviours while alive
        if (IsAlive)
        {
            //Move by values set from the Enforcer who shot the projectile, if they have indeed been set
            if (MovementSet)
                transform.position += Direction * Speed * Time.deltaTime;

            //Trigger an early death for the projectile if its reached its maximum lifespan without hitting anything to destroy it
            ProjectileLifetime -= Time.deltaTime;
            if (ProjectileLifetime <= 0.0f)
                TriggerDeath();
        }
        //Wait for death animation to complete while dead
        else
        {
            DeathAnimationRemaining -= Time.deltaTime;
            if (DeathAnimationRemaining <= 0.0f)
            {
                //Award points for destroying the projectile, then alert the parent Enforcer before destroying this object itself
                GameState.Instance.IncreaseScore((int)PointValue.SparkShot);
                if (ParentEnforcer != null)
                    ParentEnforcer.SparkShotDestroyed(gameObject);
                Destroy(this.gameObject);
            }
        }
    }

    //Enforcer calls this function to provide the projectile with its required values upon firing
    public void InitializeProjectile(float Speed, Vector3 Direction, EnforcerAI Parent)
    {
        //Store all the values that were provided
        MovementSet = true;
        this.Speed = Speed;
        this.Direction = Direction;
        ParentEnforcer = Parent;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Collisions with any of the players projectiles causes both projectiles to be destroyed
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            TriggerDeath();
        }
        //Colliding with any walls removes the projectiles velocity in that direction
        else if(collision.transform.CompareTag("Wall"))
        {
            //Hitting top or bottom wall removes any of the projectiles Y velocity
            if (collision.transform.name == "Top Wall" || collision.transform.name == "Bottom Wall")
                Direction.y = 0f;
            //Hitting left or right wall removes any of the projectiles X velocity
            if (collision.transform.name == "Left Wall" || collision.transform.name == "Right Wall")
                Direction.x = 0f;
        }
        //Colliding with the player kills them immediately
        else if(collision.transform.CompareTag("Player"))
        {
            GameState.Instance.KillPlayer();
            Destroy(this.gameObject);
        }
    }

    //Starts playing the death animation, then waits for it to complete before the projectile destroys itself
    private void TriggerDeath()
    {
        SoundEffectsPlayer.Instance.PlaySound("SparkShotExplode");
        //Trigger the animation controller to begin the death projectiles death animation
        AnimationController.SetTrigger("Death");
        //Flag the projectile as dead, and destroy its components that might cause trouble during the death animation
        IsAlive = false;
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
    }

    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}

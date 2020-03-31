// ================================================================================================================================
// File:        TankShellAI.cs
// Description:	These are the projectiles fired by the Tank enemies, they bounce off the walls increasing their speed each bounce
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class TankShellAI : MonoBehaviour
{
    private TankAI ParentTank = null;   //Keep reference to the Tank which fired this projectile so it can be told before its about to be destroyed

    //Movement
    private bool MovementSet = false;   //The shell wont move until its been told which direction to go by the Tank which fired it
    private Vector3 CurrentDirection;   //Current direction of movement as set by the Tank which fired the shell
    private float CurrentSpeed = 3f; //How fast the projectile moves 
    private float BounceSpeedIncrease = 0.25f;   //How much the speed increases each time the shell bounces off a wall
    private float MaxSpeed = 5f;  //Maximum speed the projectile can reach
    private float ProjectileLifetime = 8.0f;    //Maximum lifespan of the projectile

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Keep travelling forward is movement values have been set
        if (MovementSet)
            transform.position += CurrentDirection * CurrentSpeed * Time.deltaTime;

        //Destroy the projectile once the lifetime has expired
        ProjectileLifetime -= Time.deltaTime;
        if (ProjectileLifetime <= 0.0f)
            Destroy(gameObject);
    }

    //Called by the Tank which fired this projectile to set the initial movement direction
    public void InitializeProjectile(Vector3 Direction, TankAI Parent)
    {
        MovementSet = true;
        CurrentDirection = Direction;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Collision with any of the players projectiles causes both projectiles to be destroyed
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            if (ParentTank != null)
                ParentTank.TankShellDestroyed(gameObject);
            Destroy(gameObject);
        }
        //Colliding with any Walls reflects the shells direction off that wall, and increases its speed
        else if(collision.transform.CompareTag("Wall"))
        {
            //Reflect the balls movement direction off the surface normal of the wall that it collided with
            Vector3 SurfaceNormal = collision.contacts[0].normal;
            CurrentDirection = Vector3.Reflect(CurrentDirection, SurfaceNormal);
            //Increase the projectiles speed if it hasnt already reached maximum
            if (CurrentSpeed < MaxSpeed)
                CurrentSpeed += BounceSpeedIncrease;
        }
        //Colliding with the player kills them immediately
        else if(collision.transform.CompareTag("Player"))
        {
            GameState.Instance.KillPlayer();
            Destroy(this.gameObject);
        }
    }

    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}

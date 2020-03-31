// ================================================================================================================================
// File:        EnforcerAI.cs
// Description:	Enforcers are a flying enemy which hover around the level firing spark shots at the player.
//              They seek toward a random location offset from the player similar to the Hulks, however they're movement has a random
//              velocity added to it which is inversely proportional to their distance from the player. This velocity is frequently
//              updated, causing them to have a much more smooth and complex movement when compared to the Grunt or Hulk enemies.
//              They fire spark shots at the player, these shots have a random velocity which is directly proportional to the distance
//              from the player, meaning they have a greater speed when shot further away from the player.
//              The sparks are aimed at the player with a random amount of spread, so they arent fired directly toward the player.
//              A certain percentage of these shots however are fired directly at the players predicted location meaning they will be
//              a direct hit if the player continues moving in their current direction without making any changes before the shot hits.
//              When these spark shots hit one of the outer wall boundaries, they lose all velocity in that direction and begin sliding
//              along the wall until they come to rest in one of the corners where they remain until they eventually fade away.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public class EnforcerAI : HostileEntity
{
    //Targetting/Movement 
    private Vector3 Target; //Current location the Enforcer is seeking toward
    private Vector2 TargetOffsetRange = new Vector2(0.25f, 1.15f);    //Value range allowed when finding a new target location near the player to seek to
    private bool TargetAcquired = false;    //Tracks if the Enforcer has access to the player character
    private Vector2 PlayerDistanceRange = new Vector2(0.5f, 18f);   //Distance between the Enforcer and Player will always fall in this range somewhere
    private Vector2 SpeedRange = new Vector2(0.55f, 1.75f);  //Value range of speed values at which the enforcer can travel at

    //Shooting
    public GameObject SparkShotPrefab;  //Projectile which is fired at the player
    private float VolleyCooldown = 3.0f;    //How long the Enforcer must wait after firing a volley of shots before it can fire again
    private float VolleyCooldownLeft = 3.0f;   //Seconds remaining before the Enforcer can fire another volley of projectiles
    private float ShotCooldown = 0.25f; //Time between shots fired in a volley
    private float ShotCooldownLeft; //Seconds left before the next projectile in the current volley is fired
    private Vector2 VolleyShotRange = new Vector2(1, 5);    //Range of the amount of shots that can be fired in a volley
    private int VolleyShotsLeft;    //Number of shots left to be fired in the current volley
    private bool FiringVolley = false;  //Tracks if the Enforcer is currently in the middle of viring a volley of shots at the player
    private Vector2 AimOffsetRange = new Vector2(0.75f, 1.25f); //Value range allowed when finding a new target location near the player to shoot at
    private Vector2 ShotSpeedRange = new Vector2(2.5f, 5f);  //Range of speed values in which the projectiles may be fired at
    private List<GameObject> ActiveSparkShots = new List<GameObject>(); //Keep a list of active spark shots which have been fired by the Enforcer

    //Status/Animation
    private bool IsAlive = true;    //Set to false while waiting for death animation to play
    private float DeathAnimationRemaining = 0.47f;  //Time left until the death animation finishes playing
    public Animator[] AnimationControllers; //Animation controllers used to trigger the death animation when the Enforcer is killed

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Perform all normal behaviours while still alive
        if (IsAlive)
        {
            //Move towards our current target location
            SeekTarget();
            //Fire projectiles at the player
            FireProjectiles();
        }
        //Wait for death animation to complete while dead
        else
        {
            DeathAnimationRemaining -= Time.deltaTime;
            if (DeathAnimationRemaining <= 0.0f)
            {
                WaveManager.Instance.TargetEnemyDead(this);
                Destroy(this.gameObject);
            }
        }
    }

    //Returns a position offset from the players location by some random amount within the allowed value range
    private Vector3 GetPlayerPositionOffset(Vector2 OffsetRange)
    {
        //Get 2 random values to offset from on the X and Y axis
        float XOffset = Random.Range(OffsetRange.x, OffsetRange.y);
        float YOffset = Random.Range(OffsetRange.x, OffsetRange.y);

        //Decide in which direction to apply these offset values
        bool PositiveXOffset = Random.value >= 0.5f;
        bool PositiveYOffset = Random.value >= 0.5f;

        //Create a new vector by applying these offset values to the players current location
        Vector3 PositionOffset = GameState.Instance.Player.transform.position;
        PositionOffset.x += PositiveXOffset ? XOffset : -XOffset;
        PositionOffset.y += PositiveYOffset ? YOffset : -YOffset;
        return PositionOffset;
    }

    //Move toward the current target location
    private void SeekTarget()
    {
        //Grab a new target location to move to if we dont currently have one
        if (!TargetAcquired)
            Target = GetPlayerPositionOffset(TargetOffsetRange);

        //Movement speed is inversely proportional to the players distance
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        //Find the ratio of this value when mapped onto the PlayerDistanceRange values
        float DistanceRatio = (PlayerDistance - PlayerDistanceRange.x) / (PlayerDistanceRange.y - PlayerDistanceRange.x);
        //Map this onto the SpeedRange values to get a scaled speed value based on the players distance
        float ScaledSpeed = DistanceRatio * (SpeedRange.x - SpeedRange.y) + SpeedRange.y;

        //Move the Enforcer toward its target at this speed
        Vector3 Direction = Vector3.Normalize(Target - transform.position);
        transform.position += Direction * ScaledSpeed * Time.deltaTime;

        //If the Enforcer gets close enough to its target, a new target needs to be acquired
        if (Vector3.Distance(transform.position, Target) <= 2.5f)
            Target = GetPlayerPositionOffset(TargetOffsetRange);
    }

    //Fires a projectile toward the player
    private void FireProjectiles()
    {
        //Wait for the volley cooldown to expire before another volley of shots can be fired
        if(!FiringVolley)
        {
            VolleyCooldownLeft -= Time.deltaTime;
            if(VolleyCooldownLeft <= 0.0f)
            {
                //Start firing a new volley of projectiles
                FiringVolley = true;
                VolleyShotsLeft = (int)Random.Range(VolleyShotRange.x, VolleyShotRange.y);
                ShotCooldownLeft = 0.0f;
            }
        }
        //Keep firing projectiles until the current volley is complete
        else
        {
            //Wait until the current shot cooldown expires
            ShotCooldownLeft -= Time.deltaTime;
            if(ShotCooldownLeft <= 0.0f)
            {
                //Reset the timer
                ShotCooldownLeft = ShotCooldown;
                //Grab a location near the player where the projectile will be aimed, and get the direction to that location
                Vector3 ShotTarget = GetPlayerPositionOffset(AimOffsetRange);
                Vector3 ShotDirection = Vector3.Normalize(ShotTarget - transform.position);
                //15% of projectiles will be aimed at the players predicated location
                if (Random.Range(1, 100) <= 15)
                    ShotDirection += GameState.Instance.Player.GetComponent<PlayerMovement>().MovementVelocity;
                //Scale the projectiles speed directly propertional to the players distance
                float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
                float DistanceRatio = (PlayerDistance - PlayerDistanceRange.x) / (PlayerDistanceRange.y - PlayerDistanceRange.x);
                float ShotSpeed = DistanceRatio * (ShotSpeedRange.y - ShotSpeedRange.x) + ShotSpeedRange.x;
                //Spawn a new projectile and store it in the list with any others fired by this enemy
                GameObject SparkShot = Instantiate(SparkShotPrefab, transform.position, Quaternion.identity);
                ActiveSparkShots.Add(SparkShot);
                //Give the new projectile its travel direction and movement speed
                SparkShot.GetComponent<SparkShotAI>().InitializeProjectile(ShotSpeed, ShotDirection, this);
                //Reduce the number of projectiles left to be fired in this volley, and check if they have all run out yet
                VolleyShotsLeft--;
                if(VolleyShotsLeft <= 0)
                {
                    //End the volley and begin the volley cooldown once all the shots have been fired
                    FiringVolley = false;
                    VolleyCooldownLeft = VolleyCooldown;
                }
            }
        }
    }

    //Handle collisions with certain other objects and entities that we come into contact with
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Any player projectiles which hit the Enforcer are destroyed, killing the Enforcer
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            CleanProjectiles();
            TriggerDeath();
        }
        //Kill the player character on contact
        else if(collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }

    //Disables all normal behaviours, triggers the death animation and waits for that to complete
    private void TriggerDeath()
    {
        GameState.Instance.IncreaseScore((int)PointValue.Enforcer);
        //Trigger each of the Enforcers Animation Controllers to start playing the death animation
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetTrigger("Death");
        //Flag the Enforcer as being dead, and destroy and of its components which may cause issues while playing the death animation
        IsAlive = false;
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
    }

    //Spark Shot projectiles fired by this Enforcer will alert it when they are going to be destroyed so they can be removed from the tracking list
    public void SparkShotDestroyed(GameObject DestroyedSparkShot)
    {
        //Remove the shot from the tracking list if its listed there
        if(ActiveSparkShots.Contains(DestroyedSparkShot))
            ActiveSparkShots.Remove(DestroyedSparkShot);
    }

    //Cleans up any active spark shot projectiles which have been fired by this enforcer
    public void CleanProjectiles()
    {
        foreach (GameObject SparkShot in ActiveSparkShots)
            if (SparkShot != null)
                Destroy(SparkShot);
        ActiveSparkShots.Clear();
    }
}

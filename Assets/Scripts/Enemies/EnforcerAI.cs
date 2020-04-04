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
    public float MoveSpeed = 1.75f; //Enforcers current move speed
    private Vector2 SpeedRange = new Vector2(1.75f, 3.75f); //Enforcers speed must remain in these values
    private Vector3 TargetPos;  //Location offset from the player to seek to
    private float NextTargetUpdate = 0f;    //How long until a new target pos is acquired
    private Vector2 TargetUpdateInterval = new Vector2(0.5f, 2.5f); //New target is acquired randomly every X amount of seconds in this range
    private Vector2 TargetOffsetRange = new Vector2(0.35f, 2.65f);  //Target pos is offset from the player by random amount in this range
    private Vector2 PlayerDistanceRange = new Vector2(0.35f, 16.6f); //Players distance will always fall within this range
    private float NextSpeedUpdate = 0.15f;  //How long until a new movespeed value is acquired
    private Vector2 SpeedUpdateInterval = new Vector2(0.35f, 1.35f);  //How often a new movespeed value is selected

    //Shooting
    public GameObject SparkShotPrefab;  //Projectile fired by the Enforcer
    private float VolleyCooldown = 3.0f;    //How long the Enforcer must wait between firing another volley of projectiles
    private float NextVolley = 0f;    //Time left until another volley of projectiles can be fired
    private float ShotCooldown = 0.25f; //Time between shots during a volley
    private float NextShot; //Time until the next shot in the current volley is fired
    private int ShotsLeft;  //How many more shots can be fired in the current volley
    private Vector2 VolleyLength = new Vector2(1, 5);   //Number of shots fired per volley falls within this range
    private bool FiringVolley = false;  //Tracks when the Enforcer is actively in the process of firing a volley of shots
    private Vector2 AimOffsetRange = new Vector2(0.75f, 1.85f); //Position at which projectiles are fired is offset from the players location by random amount in this range
    private Vector2 ShotSpeedRange = new Vector2(0.5f, 10f); //Speed at which projectiles are fired is directly proportional to the players distance, falls somewhere within this 

    //Animation
    private bool IsAlive = true;    //Set to false once killed and waiting for death animation to complete before the object destroys itself
    private float DeathAnimationLeft = 0.47f;   //Time left until the death animation completes
    public Animator[] AnimationControllers; //Used to handle the sprites animations during gameplay

    private void Update()
    {
        //Only process AI logic whenever the game isnt paused or anything like that
        if(GameState.Instance.ShouldAdvanceGame())
        {
            //Perform normal behaviours while the Enforcer is still alive
            if(IsAlive)
            {
                SeekPlayer();
                FireProjectiles();
            }
            //Otherwise wait for the death animation to finish before the enforcer destroys itself
            else
            {
                DeathAnimationLeft -= Time.deltaTime;
                if (DeathAnimationLeft <= 0.0f)
                    Destroy(this.gameObject);
            }
        }
    }

    private void SeekPlayer()
    {
        //Get a new target whenever the timer expires, or the current target has been reached
        float TargetDistance = Vector3.Distance(transform.position, TargetPos);
        NextTargetUpdate -= Time.deltaTime;
        if(TargetDistance <= 0.15f || NextTargetUpdate <= 0.0f)
        {
            //Reset the timer and grab a new target pos
            NextTargetUpdate = Random.Range(TargetUpdateInterval.x, TargetUpdateInterval.y);
            UpdateTargetPos();
        }

        //Get a new movement speed whenever the timer expires
        NextSpeedUpdate -= Time.deltaTime;
        if (NextSpeedUpdate <= 0.0f)
        {
            NextSpeedUpdate = Random.Range(SpeedUpdateInterval.x, SpeedUpdateInterval.y);
            UpdateMoveSpeed();
        }

        //Move towards the current target
        Vector3 TargetDirection = Vector3.Normalize(TargetPos - transform.position);
        transform.position += TargetDirection * MoveSpeed * Time.deltaTime;
    }

    //Gets a new position which is randomly offset from the players current location for the Enforcer to travel towards
    private void UpdateTargetPos()
    {
        //Set a new target position and update the current movement speed
        TargetPos = GetPlayerPositionOffset(TargetOffsetRange);
        UpdateMoveSpeed();
    }

    //Calculates a new movement speed for the Enforcer, inversely proportional to its distance from the Player
    private void UpdateMoveSpeed()
    {
        //Get the current distance from the player
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);

        //Find the ratio of this value against the range of possible distance values
        float DistanceRatio = (PlayerDistance - PlayerDistanceRange.x) / (PlayerDistanceRange.y - PlayerDistanceRange.x);
        //Map this ratio onto the range of allowable move speed values
        float ScaledSpeed = DistanceRatio * (SpeedRange.y - SpeedRange.x) + SpeedRange.x;

        //Set this as the new movement speed
        MoveSpeed = ScaledSpeed;
    }

    //Periodically fires a volley of projectiles towards the player
    private void FireProjectiles()
    {
        //Wait until cooldown resets, then start a new volley
        if(!FiringVolley)
        {
            //Wait for the cooldown timer to expire
            NextVolley -= Time.deltaTime;
            if(NextVolley <= 0.0f)
            {
                //Start a new volley
                FiringVolley = true;
                ShotsLeft = (int)Random.Range(VolleyLength.x, VolleyLength.y);
                NextShot = 0.0f;
            }
        }
        //Keep firing projectiles until none remain in the current volley
        else
        {
            //Wait before the next shot can be fired
            NextShot -= Time.deltaTime;
            if(NextShot <= 0.0f)
            {
                //Reset the shot cooldown timer
                NextShot = ShotCooldown;
                //Grab a random location offset from the player at which the projectile will be aimed toward
                Vector3 ShotTarget = GetPlayerPositionOffset(AimOffsetRange);
                //Create a directional vector pointing from the Enforcer to this ShotTarget position
                Vector3 ShotDirection = Vector3.Normalize(ShotTarget - transform.position);
                //Get a speed value for the projectile directly propertional to the players distance from the enforcer
                float PlayerDistance = Vector3.Distance(GameState.Instance.Player.transform.position, transform.position);
                float DistanceRatio = (PlayerDistance - PlayerDistanceRange.x) / (PlayerDistanceRange.y - PlayerDistanceRange.x);
                float ShotSpeed = DistanceRatio * (ShotSpeedRange.y - ShotSpeedRange.x) + ShotSpeedRange.x;
                //Spawn a new projectile and tell it which direction it should travel, and at what speed
                GameObject SparkShot = Instantiate(SparkShotPrefab, transform.position, Quaternion.identity);
                SparkShot.GetComponent<SparkShotAI>().InitializeProjectile(ShotSpeed, ShotDirection);
                //Play the firing sound effect
                SoundEffectsPlayer.Instance.PlaySound("EnforcerShoot");
                //Reduce the number of shots left and check if they have all run out
                ShotsLeft--;
                if(ShotsLeft <= 0)
                {
                    //End the volley and start the cooldown before a new volley can be fired
                    FiringVolley = false;
                    NextVolley = VolleyCooldown;
                }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Player projectiles are destroyed on contact, killing the Enforcer
        if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            StartDying();
        }
        //Kill the player character on contact
        else if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Find a new target pos whenever a wall is run into
        else if (collision.transform.CompareTag("Wall"))
            UpdateTargetPos();
    }

    //Removes physics components from the Enforcer, disables its AI behaviours and starts the death animation
    private void StartDying()
    {
        //Flag the Enforcer as now dead, and remove its physics components
        IsAlive = false;
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
        //Tell the wave manager this enemy has been destroyed
        WaveManager.Instance.EnemyDead(this);
        //Play a sound effect for the enemy dying and award some points to the player for killing it
        SoundEffectsPlayer.Instance.PlaySound("EnforcerDie");
        GameState.Instance.IncreaseScore((int)PointValue.Enforcer);
        //Start playing the death animation
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetBool("IsDead", true);
    }
}

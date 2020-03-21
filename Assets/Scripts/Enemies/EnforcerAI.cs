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

public class EnforcerAI : HostileEntity
{
    //Targetting/Movement 
    private GameObject Player;  //The player character gameobject
    private Vector3 Target; //Current location the Enforcer is seeking toward
    private Vector2 TargetOffsetRange = new Vector2(0.25f, 1.15f);    //Value range allowed when finding a new target location near the player to seek to
    private Vector2 PlayerDistanceRange = new Vector2(0.5f, 18f);   //Distance between the Enforcer and Player will always fall in this range somewhere
    private Vector2 SpeedRange = new Vector2(0.55f, 1.75f);  //Value range of speed values at which the enforcer can travel at

    //Shooting
    public GameObject SparkShotPrefab;  //Projectile which is fired at the player
    private float ShotCooldownLength = 1.5f;  //How often projectiles can be fired
    private float ShotCooldown = 0.5f;  //Seconds remaining on the current shot cooldown
    private Vector2 AimOffsetRange = new Vector2(0.75f, 1.25f); //Value range allowed when finding a new target location near the player to shoot at
    private Vector2 ShotSpeedRange = new Vector2(2.5f, 5f);  //Range of speed values in which the projectiles may be fired at

    //Status/Animation
    private bool IsAlive = true;    //Set to false while waiting for death animation to play
    private float DeathAnimationRemaining = 0.47f;  //Time left until the death animation finishes playing
    public Animator[] AnimationControllers; //Animation controllers used to trigger the death animation when the Enforcer is killed

    private void Awake()
    {
        //Find the player
        Player = GameObject.FindGameObjectWithTag("Player");

        //Find a new position near the player to move toward
        Target = GetPlayerPositionOffset(TargetOffsetRange);
    }

    private void Update()
    {
        //All Enemy AI is disabled during the round warmup period
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        //Perform all normal behaviours while still alive
        if (IsAlive)
        {
            //Move towards our current target location
            SeekTarget();
            //Fire projectiles at the player
            FireProjectile();
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
        Vector3 PositionOffset = Player.transform.position;
        PositionOffset.x += PositiveXOffset ? XOffset : -XOffset;
        PositionOffset.y += PositiveYOffset ? YOffset : -YOffset;
        return PositionOffset;
    }

    //Move toward the current target location
    private void SeekTarget()
    {
        //Movement speed is inversely proportional to the players distance
        float PlayerDistance = Vector3.Distance(transform.position, Player.transform.position);
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
    private void FireProjectile()
    {
        //Decrement the current shot cooldown timer
        ShotCooldown -= Time.deltaTime;
        
        //Only allow a new projectile to be fired when off cooldown
        if(ShotCooldown <= 0.0f)
        {
            //Start the shooting cooldown timer
            ShotCooldown = ShotCooldownLength;

            //Grab a location nearby the player to shoot at and create a direction vector pointing from the Enforcer to this location
            Vector3 ShotTarget = GetPlayerPositionOffset(AimOffsetRange);
            Vector3 ShotDirection = Vector3.Normalize(ShotTarget - transform.position);

            //Projectiles speed is directly proportional to the players distance
            float PlayerDistance = Vector3.Distance(transform.position, Player.transform.position);
            //Find the ratio of this value when mapped onto the PlayerDistanceRange values
            float DistanceRatio = (PlayerDistance - PlayerDistanceRange.x) / (PlayerDistanceRange.y - PlayerDistanceRange.x);
            //Map this onto the ShotSpeedRange valuers to get a scavled speed value based on the players distance
            float ScaledSpeed = DistanceRatio * (ShotSpeedRange.y - ShotSpeedRange.x) + ShotSpeedRange.x;

            //Spawn a new spark shot and fire it in the target direction
            Vector3 ShotSpawn = transform.position + ShotDirection * 0.5f;
            GameObject SparkShot = Instantiate(SparkShotPrefab, ShotSpawn, Quaternion.identity);
            SparkShot.GetComponent<SparkShotAI>().InitializeProjectile(ScaledSpeed, ShotDirection);
        }
    }

    //Handle collisions with certain other objects and entities that we come into contact with
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Any player projectiles which hit the Enforcer are destroyed, killing the Enforcer
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
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
}

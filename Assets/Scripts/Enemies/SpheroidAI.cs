// ================================================================================================================================
// File:        SpheroidAI.cs
// Description:	Spheroid is an Enemy generator, they can spawn 1-6 Enforcers during their lifetime. They like to hang around the
//              corners of the arena, sometimes crossing over from one corner to another in order to avoid the player character.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

//The first Enforcer can be spawned 5-8 seconds after the Spheroid has spawned
//If an Enforcer is spawned while the Spheroid is moving, the cooldown is 3-6 seconds
//If an Enforcer is spawned while the Spheroid is idle in one of the corners, and the
//Spheroid has been in that corner for atleast 3.5 seconds, the cooldown is 1-1.5 seconds.
//If this spheroids maximum number of Enforcers have been spawned, it self destructs

using UnityEngine;

public class SpheroidAI : HostileEntity
{
    private float MoveSpeed = 4.5f; //How fast the spheroid moves around the level
    private Vector3 CurrentTarget;  //Current position the spheroid is seeking towards
    //Position of each corner of the arena where the spheroids like to rest at
    private Vector3[] CornerPositions =
    {
        new Vector3(7.009f, 4.006f, 0f),    //North-East Corner
        new Vector3(7.009f, -4.37f, 0f),   //South-East Corner
        new Vector3(-7.369f, -4.37f, 0f),  //South-West Corner
        new Vector3(-7.369f, 4.006f, 0f)    //North-West Corner
    };
    private int SpawnsLeft; //How many more Enforcers this Spheroid is able to spawn before it self-destructs
    private float SpawnCooldown;    //Cooldown remaining until another Enforcer can be spawned in
    private Vector2 InitialSpawnCooldown = new Vector2(5f, 8f); //Time before the first Enforcer can be spawned in
    private Vector2 MovingSpawnCooldown = new Vector2(3f, 6f);  //Time before the next Enforcer can be spawned while the Spheroid is moving
    private Vector2 IdleSpawnCooldown = new Vector2(1f, 1.5f);  //Time before the next Enforcer can be spawned while the Spheroid is safe in one of the corners
    private int MaxSpawnCount = 6;  //Maximum number of Enforcers that any one Spheroid is able to spawn
    private bool InCorner = false;  //Tracks if the Spheroid is currently idling in one of the corners
    private float TimeInCorner = 0.0f;  //Tracks how long the Spheroid has spent sitting in the corner
    private float CornerSafeTimer = 3.5f;   //How much time must be spent in 1 corner before the Spheroid considers itself to be in a safe position
    private Vector2 XSpawnRange = new Vector2(-7.173f, 7.172f); //Range along the X axis where enforcers can be spawned in
    private Vector2 YSpawnRange = new Vector2(-4.126f, 4.125f); //Range along the Y axis where enforcers can be spawned in
    private Vector2 SpawnRangeOffset = new Vector2(0.5f, 1.25f);    //How far in each direction an Enforcers spawn location will be offset from the Spheroids location
    private Vector2 HitPointRange = new Vector2(1, 3);  //Value range of hitpoints that may be assigned to the Spheroid when its spawned in
    private int HitPoints; //Hits left before the Spheroid dies

    private void Start()
    {
        //Assign a random number of health points to the spheroid
        HitPoints = (int)Random.Range(HitPointRange.x, HitPointRange.y);

        //Randomly set the number of Enforcers that this Spheroid will be allowed to spawn before it self-destructs
        SpawnsLeft = Random.Range(1, MaxSpawnCount);

        //Randomly select one of the 4 corner positions to start moving toward
        int CornerSelection = Random.Range(1, 4);
        CurrentTarget = CornerPositions[CornerSelection - 1];

        //Set the timer before the first Enforcer can be spawned in
        SpawnCooldown = Random.Range(InitialSpawnCooldown.x, InitialSpawnCooldown.y);
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Seek the current corner target until the Spheroid reaches that location
        if (!InCorner)
            SeekCorner();
        else
            IdleInCorner();

        SpawnEnforcers();
    }

    //Moves towards the CurrentTarget until the destination has been reached
    private void SeekCorner()
    {
        //Find the direction to the current corner location
        Vector3 CornerDirection = Vector3.Normalize(CurrentTarget - transform.position);
        //Move toward this corner
        transform.position += CornerDirection * MoveSpeed * Time.deltaTime;
        //Check if we have reached the target corner location yet
        float CornerDistance = Vector3.Distance(transform.position, CurrentTarget);
        if (CornerDistance <= 0.15f)
            InCorner = true;
    }

    //Idles in the corner until the player gets too close, then moves away to another corner
    private void IdleInCorner()
    {
        //Travel to a different corner if the player gets too close to this one
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        if (PlayerDistance <= 3.5f)
            TargetCornerAwayFromPlayer();
        //Track how long has been spend in this corner
        TimeInCorner += Time.deltaTime;
    }

    //Spawns in a new Enforcer whenever the cooldown timer expires
    private void SpawnEnforcers()
    {
        //Wait for the cooldown timer to expire
        SpawnCooldown -= Time.deltaTime;
        if(SpawnCooldown <= 0.0f)
        {
            //Reset the timer, length based on if the Spheroid is safe in a corner or not
            SpawnCooldown = TimeInCorner >= CornerSafeTimer ?
                Random.Range(IdleSpawnCooldown.x, IdleSpawnCooldown.y) :
                Random.Range(MovingSpawnCooldown.x, MovingSpawnCooldown.y);

            //Get a random location near the Spheroid and spawn an Enforcer in there
            Vector3 SpawnLocation = GetEnforcerSpawnLocation();
            GameObject NewEnforcer = Instantiate(PrefabSpawner.Instance.GetPrefab("Enforcer"), SpawnLocation, Quaternion.identity);

            //Add this enforcer to the wave managers list of target enemies
            WaveManager.Instance.AddTargetEnemy(NewEnforcer.GetComponent<HostileEntity>());

            //Play sound effect
            SoundEffectsPlayer.Instance.PlaySound("SpawnEnforcer");

            //Take 1 away from this Spheroid spawn counter, then check if its time for the Spheroid to self-destruct
            SpawnsLeft -= 1;
            if(SpawnsLeft <= 0)
            {
                //Tell the wave manager to remove this enemy from its lists, then destroy it
                WaveManager.Instance.TargetEnemyDead(this);
                Destroy(this.gameObject);
            }
        }
    }

    //Returns a random location near the Spheroid where an Enforcer may be spawned in at
    private Vector3 GetEnforcerSpawnLocation()
    {
        //Start with the Spheroids current location
        Vector3 SpawnLocation = transform.position;

        //Offset this location randomly in the X and Y axis
        SpawnLocation.x += Random.value >= 0.5f ?
            Random.Range(SpawnRangeOffset.x, SpawnRangeOffset.y) :
            Random.Range(-SpawnRangeOffset.x, -SpawnRangeOffset.y);
        SpawnLocation.y += Random.value >= 0.5f ?
            Random.Range(SpawnRangeOffset.x, SpawnRangeOffset.y) :
            Random.Range(-SpawnRangeOffset.x, -SpawnRangeOffset.y);

        //Make sure this location stays inside the level bounds
        SpawnLocation.x = Mathf.Clamp(SpawnLocation.x, XSpawnRange.x, XSpawnRange.y);
        SpawnLocation.y = Mathf.Clamp(SpawnLocation.y, YSpawnRange.x, YSpawnRange.y);

        //Return the new location
        return SpawnLocation;
    }

    //Acquires a new corner target position which is the furthest away from the player character
    private void TargetCornerAwayFromPlayer()
    {
        //Figure out which corner location is furthest away from the player character
        Vector3 PlayerPosition = GameState.Instance.Player.transform.position;
        Vector3 FurthestCorner = CornerPositions[0];
        float FurthestCornerDistance = Vector3.Distance(PlayerPosition, FurthestCorner);
        for(int i = 1; i < 4; i++)
        {
            float CornerDistance = Vector3.Distance(CornerPositions[i], PlayerPosition);
            if(CornerDistance > FurthestCornerDistance)
            {
                FurthestCorner = CornerPositions[i];
                FurthestCornerDistance = CornerDistance;
            }
        }

        //Disable the InCorner flag and timer, and start moving toward the new corner target
        InCorner = false;
        TimeInCorner = 0.0f;
        CurrentTarget = FurthestCorner;
    }

    //Removes one of the Spheroids remaining hit points, kills it once its hitpoints have run out
    private void TakeDamage()
    {
        //Take away 1 from the Spheroids hitpoints and check if its still alive
        HitPoints--;
        if(HitPoints <= 0)
        {
            //Kill the Spheroid once its hitpoints reach zero
            WaveManager.Instance.TargetEnemyDead(this);
            GameState.Instance.IncreaseScore((int)PointValue.Spheroid);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if we collide with them
        if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Destroy any player projectiles which hit the Spheroid, and deal 1 point of damage to the Spheroid
        else if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            TakeDamage();
        }
    }
}

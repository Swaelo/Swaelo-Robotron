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

using System.Collections.Generic;
using UnityEngine;

public class SpheroidAI : HostileEntity
{
    private List<Vector3> CornerPositions = new List<Vector3>(); //Corner position targets the spheroids will move between to avoid the player
    public Vector3 CurrentTarget;  //Current corner position the spheroid is seeking towards
    private float MoveSpeed = 4.5f; //How fast the spheroid moves around the level
    private Vector3 Steering = Vector3.zero; //Current direction the spheroid is steering in
    public bool InCorner = false;  //Tracks if we are in a safe spot or not

    private void Start()
    {
        //Grab the corner locations from the level borders manager
        CornerPositions = LevelBorders.Instance.GetCornerPositions();

        //Select the closest corner to start moving toward
        CurrentTarget = GetClosestCornerPos();
        OffsetCornerTargetPos(1.5f);
    }

    private void Update()
    {
        //AI needs to be put on hold at times defined by the gamestate manager
        if(!GameState.Instance.ShouldAdvanceGame())
            return;

        Movement();
    }

    private void Movement()
    {
        if(!InCorner)
            SeekCorner();
        else
            IdleInCorner();
    }

    private void SeekCorner()
    {
        //Get current steering based on other spheroids around me as they move in flocks
        List<SpheroidAI> OtherSpheroids = WaveManager.Instance.GetSpheroidList();
        Steering = GetSteering(this, OtherSpheroids, CurrentTarget, GameState.Instance.Player.transform.position);

        //Figure out now target location and move towards it
        Vector3 MovementVelocity = Steering * MoveSpeed;
        transform.position += MovementVelocity * Time.deltaTime;

        //Check if we have reached the corner location yet
        float CornerDistance = Vector3.Distance(transform.position, CurrentTarget);
        if (CornerDistance <= 2f)
            InCorner = true;
    }

    private void IdleInCorner()
    {
        //Travel to a different corner if the player gets too close to this one
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        if (PlayerDistance <= 5f)
        {
            //Target a random other corner which isnt our current target
            CurrentTarget = GetRandomOtherCornerPos();
            OffsetCornerTargetPos(1.5f);
            //Disable the InCorner flag and timer, and start moving toward the new corner target
            InCorner = false;
        }
    }

    private Vector3 GetClosestCornerPos()
    {
        Vector3 ClosestCornerPos = CornerPositions[0];
        float CornerPosDistance = Vector3.Distance(transform.position, ClosestCornerPos);

        for(int i = 1; i < 3; i++)
        {
            float CornerPosCompare = Vector3.Distance(transform.position, CornerPositions[i]);
            if (CornerPosCompare < CornerPosDistance)
            {
                ClosestCornerPos = CornerPositions[i];
                CornerPosDistance = CornerPosCompare;
            }
        }

        return ClosestCornerPos;
    }
    
    //Returns a random corner position which isnt the one we are closest to
    private Vector3 GetRandomOtherCornerPos()
    {
        //Find the index of the closest corner
        int ClosestCornerIndex = 0;
        float ClosestCornerDistance = Vector3.Distance(transform.position, CornerPositions[0]);

        for(int i = 1; i < CornerPositions.Count; i++)
        {
            float CornerDistance = Vector3.Distance(transform.position, CornerPositions[i]);
            if(CornerDistance < ClosestCornerDistance)
            {
                ClosestCornerDistance = CornerDistance;
                ClosestCornerIndex = i;
            }
        }

        //Build a list excluding that index
        List<Vector3> CornerOptions = new List<Vector3>();

        for(int i = 0; i < CornerPositions.Count; i++)
        {
            if(i != ClosestCornerIndex)
                CornerOptions.Add(CornerPositions[i]);
        }

        //Return a random one from the remaining corners
        return CornerOptions[Random.Range(0, CornerOptions.Count)];
    }

    //Returns a target location to move towards somewhere near the current corner target
    private void OffsetCornerTargetPos(float Radius)
    {
        Vector2 CornerOffset = Random.insideUnitCircle * Radius;
        CurrentTarget += new Vector3(CornerOffset.x, CornerOffset.y, 0f);
    }

    private Vector3 GetVelocity()
    {
        return Steering * MoveSpeed;
    }

    //Computes steering to apply flocking movement
    private Vector3 ComputeFlockingVector(SpheroidAI Self, List<SpheroidAI> All, float FlockingRadius, float SeperationRadius)
    {
        Vector3 Alignment = Vector3.zero;
        Vector3 Cohesion = Vector3.zero;
        Vector3 Seperation = Vector3.zero;

        int FlockCount = 0;
        int SeperationCount = 0;

        foreach(SpheroidAI Other in All)
        {
            if(Other == Self) continue;

            float Distance = Vector3.Distance(Self.transform.position, Other.transform.position);

            //Alignment and Cohesion
            if(Distance < FlockingRadius)
            {
                Alignment += Other.GetVelocity();
                Cohesion += Other.transform.position;
                FlockCount ++;
            }

            //Seperation
            if(Distance < SeperationRadius && Distance > 0f)
            {
                Vector3 Away = (Self.transform.position - Other.transform.position);
                Seperation += Away.normalized / Distance;
                SeperationCount++;
            }
        }

        //Average alignment and cohesion
        if(FlockCount > 0)
        {
            Alignment /= FlockCount;
            Alignment.Normalize();

            Cohesion /= FlockCount;
            Cohesion = (Cohesion - Self.transform.position);
        }

        if(SeperationCount > 0)
            Seperation /= SeperationCount;

        float AlignmentWeight = 1f;
        float CohesionWeight = 0.5f;
        float SeperationWeight = 2f;

        Vector3 FlockingVector = Alignment * AlignmentWeight +
                                Cohesion * CohesionWeight +
                                Seperation * SeperationWeight;

        return FlockingVector;
    }

    //Prevents them running into the walls and corners
    private Vector3 ComputeWallAvoidance(float AvoidanceDistance)
    {
        Vector3 Pos = transform.position;
        Vector3 AvoidanceForce = Vector3.zero;

        Vector3 XBounds = LevelBorders.Instance.XBounds;
        Vector3 YBounds = LevelBorders.Instance.YBounds;

        //Left Wall
        if(Pos.x - XBounds.x < AvoidanceDistance)
            AvoidanceForce += new Vector3(1f / Mathf.Max(Pos.x - XBounds.x, 0.01f), 0f);

        //Right Wall
        if(XBounds.y - Pos.x < AvoidanceDistance)
            AvoidanceForce += new Vector3(-1f / Mathf.Max(XBounds.y - Pos.x, 0.01f), 0f);

        //Bottom Wall
        if(Pos.y - YBounds.x < AvoidanceDistance)
            AvoidanceForce += new Vector3(0f, 1f / Mathf.Max(Pos.y - YBounds.x, 0.01f));

        //Top Wall
        if(YBounds.y - Pos.y < AvoidanceDistance)
            AvoidanceForce += new Vector3(0f, -1f / Mathf.Max(YBounds.y - Pos.y, 0.01f));
        
        return AvoidanceForce.normalized;
    }

    //Causes the spheroids to flee when the player gets close enough
    private Vector3 ComputePlayerAvoidance()
    {
        Vector3 AvoidPlayer = Vector3.zero;

        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        if(PlayerDistance < 3f)
            AvoidPlayer = (transform.position - GameState.Instance.Player.transform.position).normalized * 3f;
        
        return AvoidPlayer;
    }

    //Causes multiple spheroids to flock together
    Vector3 GetSteering(SpheroidAI Self, List<SpheroidAI> OtherSpheroids, Vector3 CornerTarget, Vector3 PlayerPos)
    {
        Vector3 Flocking = ComputeFlockingVector(Self, OtherSpheroids, 4f, 1f);
        Vector3 MoveToCorner = (CurrentTarget - transform.position).normalized * 2f;
        Vector3 AvoidPlayer = ComputePlayerAvoidance();
        Vector3 WallAvoidance = ComputeWallAvoidance(.25f) * 3f;

        Vector3 Steering = Flocking + MoveToCorner + AvoidPlayer + WallAvoidance;

        Steering = Vector3.ClampMagnitude(Steering, 1f);
        return Steering;
    }

    // //Position of each corner of the arena where the spheroids like to rest at
    // private int SpawnsLeft; //How many more Enforcers this Spheroid is able to spawn before it self-destructs
    // private float SpawnCooldown;    //Cooldown remaining until another Enforcer can be spawned in
    // private Vector2 InitialSpawnCooldown = new Vector2(5f, 8f); //Time before the first Enforcer can be spawned in
    // private Vector2 MovingSpawnCooldown = new Vector2(3f, 6f);  //Time before the next Enforcer can be spawned while the Spheroid is moving
    // private Vector2 IdleSpawnCooldown = new Vector2(1f, 1.5f);  //Time before the next Enforcer can be spawned while the Spheroid is safe in one of the corners
    // private int MaxSpawnCount = 6;  //Maximum number of Enforcers that any one Spheroid is able to spawn
    // private bool InCorner = false;  //Tracks if the Spheroid is currently idling in one of the corners
    // private float TimeInCorner = 0.0f;  //Tracks how long the Spheroid has spent sitting in the corner
    // private float CornerSafeTimer = 3.5f;   //How much time must be spent in 1 corner before the Spheroid considers itself to be in a safe position
    // private Vector2 SpawnRangeOffset = new Vector2(0.5f, 1.25f);    //How far in each direction an Enforcers spawn location will be offset from the Spheroids location

    // private Vector2 HitPointRange = new Vector2(1, 3);  //Value range of hitpoints that may be assigned to the Spheroid when its spawned in
    // private int HitPoints; //Hits left before the Spheroid dies

    // public float FlockRadius = 3f;
    // public float SeperationRadius = 1.5f;

    // public Vector2 Steering = Vector2.zero;

    // private void Start()
    // {
    //     

    //     //Assign a random number of health points to the spheroid
    //     HitPoints = (int)Random.Range(HitPointRange.x, HitPointRange.y);

    //     //Randomly set the number of Enforcers that this Spheroid will be allowed to spawn before it self-destructs
    //     SpawnsLeft = Random.Range(1, MaxSpawnCount);

    //     //Set the timer before the first Enforcer can be spawned in
    //     SpawnCooldown = Random.Range(InitialSpawnCooldown.x, InitialSpawnCooldown.y);
    // }

    // private void Update()
    // {
    //     //All game logic and AI should be paused at certain times
    //     if (!GameState.Instance.ShouldAdvanceGame())
    //         return;

    //     Movement();

    //     SpawnEnforcers();
    // }

    // //Spawns in a new Enforcer whenever the cooldown timer expires
    // private void SpawnEnforcers()
    // {
    //     //Wait for the cooldown timer to expire
    //     SpawnCooldown -= Time.deltaTime;
    //     if(SpawnCooldown <= 0.0f)
    //     {
    //         //Reset the timer, length based on if the Spheroid is safe in a corner or not
    //         SpawnCooldown = TimeInCorner >= CornerSafeTimer ?
    //             Random.Range(IdleSpawnCooldown.x, IdleSpawnCooldown.y) :
    //             Random.Range(MovingSpawnCooldown.x, MovingSpawnCooldown.y);

    //         //Get a random location near the Spheroid and spawn an Enforcer in there
    //         Vector3 SpawnLocation = GetEnforcerSpawnLocation();
    //         GameObject NewEnforcer = Instantiate(PrefabSpawner.Instance.GetPrefab("Enforcer"), SpawnLocation, Quaternion.identity);

    //         //Play sound effect
    //         SoundEffectsPlayer.Instance.PlaySound("SpawnEnforcer");

    //         //Have the WaveManager add them to the entity tracking lists
    //         WaveManager.Instance.AddNewEnemy(NewEnforcer.GetComponent<HostileEntity>());

    //         //Take 1 away from this Spheroid spawn counter, then check if its time for the Spheroid to self-destruct
    //         SpawnsLeft -= 1;
    //         if(SpawnsLeft <= 0)
    //         {
    //             //Tell the wave manager to remove this enemy from its lists, then destroy it
    //             WaveManager.Instance.EnemyDead(this);
    //             Destroy(this.gameObject);
    //         }
    //     }
    // }

    // //Returns a random location near the Spheroid where an Enforcer may be spawned in at
    // private Vector3 GetEnforcerSpawnLocation()
    // {
    //     //Start with the Spheroids current location
    //     Vector3 SpawnLocation = transform.position;

    //     //Offset this location randomly in the X and Y axis
    //     SpawnLocation.x += Random.value >= 0.5f ?
    //         Random.Range(SpawnRangeOffset.x, SpawnRangeOffset.y) :
    //         Random.Range(-SpawnRangeOffset.x, -SpawnRangeOffset.y);
    //     SpawnLocation.y += Random.value >= 0.5f ?
    //         Random.Range(SpawnRangeOffset.x, SpawnRangeOffset.y) :
    //         Random.Range(-SpawnRangeOffset.x, -SpawnRangeOffset.y);

    //     Vector2 XBounds = LevelBorders.Instance.XBounds;
    //     Vector2 YBounds = LevelBorders.Instance.YBounds;

    //     //Make sure this location stays inside the level bounds
    //     SpawnLocation.x = Mathf.Clamp(SpawnLocation.x, XBounds.x, XBounds.y);
    //     SpawnLocation.y = Mathf.Clamp(SpawnLocation.y, YBounds.x, YBounds.y);

    //     //Return the new location
    //     return SpawnLocation;
    // }

    // //Removes one of the Spheroids remaining hit points, kills it once its hitpoints have run out
    // private void TakeDamage()
    // {
    //     //Take away 1 from the Spheroids hitpoints and check if its still alive
    //     HitPoints--;
    //     if(HitPoints <= 0)
    //     {
    //         //Kill the Spheroid once its hitpoints reach zero
    //         SoundEffectsPlayer.Instance.PlaySound("SpheroidDie");
    //         WaveManager.Instance.EnemyDead(this);
    //         GameState.Instance.IncreaseScore((int)PointValue.Spheroid);
    //         Destroy(gameObject);
    //     }
    // }

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     //Kill the player if we collide with them
    //     if (collision.transform.CompareTag("Player"))
    //         GameState.Instance.KillPlayer();
    //     //Destroy any player projectiles which hit the Spheroid, and deal 1 point of damage to the Spheroid
    //     else if (collision.transform.CompareTag("PlayerProjectile"))
    //     {
    //         Destroy(collision.gameObject);
    //         TakeDamage();
    //     }
    // }
}

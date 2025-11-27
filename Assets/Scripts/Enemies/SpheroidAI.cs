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
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class SpheroidAI : HostileEntity
{
    private float MoveSpeed = 4.5f; //How fast the spheroid moves around the level
    public Vector2 CurrentTarget;  //Current position the spheroid is seeking towards
    //Position of each corner of the arena where the spheroids like to rest at
    private List<Vector2> CornerPositions;
    private int SpawnsLeft; //How many more Enforcers this Spheroid is able to spawn before it self-destructs
    private float SpawnCooldown;    //Cooldown remaining until another Enforcer can be spawned in
    private Vector2 InitialSpawnCooldown = new Vector2(5f, 8f); //Time before the first Enforcer can be spawned in
    private Vector2 MovingSpawnCooldown = new Vector2(3f, 6f);  //Time before the next Enforcer can be spawned while the Spheroid is moving
    private Vector2 IdleSpawnCooldown = new Vector2(1f, 1.5f);  //Time before the next Enforcer can be spawned while the Spheroid is safe in one of the corners
    private int MaxSpawnCount = 6;  //Maximum number of Enforcers that any one Spheroid is able to spawn
    private bool InCorner = false;  //Tracks if the Spheroid is currently idling in one of the corners
    private float TimeInCorner = 0.0f;  //Tracks how long the Spheroid has spent sitting in the corner
    private float CornerSafeTimer = 3.5f;   //How much time must be spent in 1 corner before the Spheroid considers itself to be in a safe position
    private Vector2 SpawnRangeOffset = new Vector2(0.5f, 1.25f);    //How far in each direction an Enforcers spawn location will be offset from the Spheroids location
    private Vector2 HitPointRange = new Vector2(1, 3);  //Value range of hitpoints that may be assigned to the Spheroid when its spawned in
    private int HitPoints; //Hits left before the Spheroid dies

    public float FlockRadius = 3f;
    public float SeperationRadius = 1.5f;

    public Vector2 Steering = Vector2.zero;

    private Vector2 GetVelocity()
    {
        return Steering * MoveSpeed;
    }

    private void GetSteering()
    {
        List<SpheroidAI> OtherSpheroids = WaveManager.Instance.GetSpheroidList();
        Steering = GetSteering(this, OtherSpheroids, CurrentTarget, (Vector2)GameState.Instance.Player.transform.position);
    }

    private void Start()
    {
        //Set the corner positions based on the constraints of the game level
        Vector2 XBounds = LevelBorders.Instance.GetXBounds();
        Vector2 YBounds = LevelBorders.Instance.GetYBounds();
        CornerPositions = new List<Vector2>();
        CornerPositions.Add(new Vector2(XBounds.y, YBounds.y)); //North-East
        CornerPositions.Add(new Vector2(XBounds.y, YBounds.x)); //South-East
        CornerPositions.Add(new Vector2(XBounds.x, YBounds.x)); //South_West
        CornerPositions.Add(new Vector2(XBounds.x, YBounds.y)); //North-West

        //Offset corner position targets so we are moving towards the corners instead of right to them
        for(int i = 0; i < CornerPositions.Count; i++)
        {
            Vector2 MiddleDirection = -CornerPositions[i];
            Vector2 OffsetAmount = MiddleDirection.normalized * LevelBorders.Instance.BorderThickness;
            CornerPositions[i] = CornerPositions[i] + OffsetAmount;
        }

        //Assign a random number of health points to the spheroid
        HitPoints = (int)Random.Range(HitPointRange.x, HitPointRange.y);

        //Randomly set the number of Enforcers that this Spheroid will be allowed to spawn before it self-destructs
        SpawnsLeft = Random.Range(1, MaxSpawnCount);

        //Select the closest corner to start moving toward
        SortCornerPositions();
        CurrentTarget = CornerPositions[0];

        //Set the timer before the first Enforcer can be spawned in
        SpawnCooldown = Random.Range(InitialSpawnCooldown.x, InitialSpawnCooldown.y);
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        Movement();

        SpawnEnforcers();
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
        //Get steering based on other spheroids positions around me
        GetSteering();

        //Figure out the new target location
        Vector2 MovementVelocity = Steering * MoveSpeed;

        //Apply the final movement velocity to move in a pack but not into walls
        transform.position += (Vector3)MovementVelocity * Time.deltaTime;

        //Check if we have reached the corner location yet
        float CornerDistance = Vector3.Distance(transform.position, CurrentTarget);
        if (CornerDistance <= 2f)
            InCorner = true;
    }

    //Idles in the corner until the player gets too close, then moves away to another corner
    private void IdleInCorner()
    {
        ///Travel to a different corner if the player gets too close to this one
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        if (PlayerDistance <= 5f)
        {
            //Target the next closest corner from the player
            SortCornerPositions();
            CurrentTarget = CornerPositions[1];
            //Disable the InCorner flag and timer, and start moving toward the new corner target
            InCorner = false;
            TimeInCorner = 0.0f;
        }

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

            //Play sound effect
            SoundEffectsPlayer.Instance.PlaySound("SpawnEnforcer");

            //Have the WaveManager add them to the entity tracking lists
            WaveManager.Instance.AddNewEnemy(NewEnforcer.GetComponent<HostileEntity>());

            //Take 1 away from this Spheroid spawn counter, then check if its time for the Spheroid to self-destruct
            SpawnsLeft -= 1;
            if(SpawnsLeft <= 0)
            {
                //Tell the wave manager to remove this enemy from its lists, then destroy it
                WaveManager.Instance.EnemyDead(this);
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

        Vector2 XBounds = LevelBorders.Instance.GetXBounds();
        Vector2 YBounds = LevelBorders.Instance.GetYBounds();

        //Make sure this location stays inside the level bounds
        SpawnLocation.x = Mathf.Clamp(SpawnLocation.x, XBounds.x, XBounds.y);
        SpawnLocation.y = Mathf.Clamp(SpawnLocation.y, YBounds.x, YBounds.y);

        //Return the new location
        return SpawnLocation;
    }

    //Sort corner positions by distance
    private void SortCornerPositions()
    {
        CornerPositions.Sort((a, b) =>
        {
            float distA = Vector2.Distance(transform.position, a);
            float distB = Vector2.Distance(transform.position, b);
            return distA.CompareTo(distB);
        });
    }

    //Removes one of the Spheroids remaining hit points, kills it once its hitpoints have run out
    private void TakeDamage()
    {
        //Take away 1 from the Spheroids hitpoints and check if its still alive
        HitPoints--;
        if(HitPoints <= 0)
        {
            //Kill the Spheroid once its hitpoints reach zero
            SoundEffectsPlayer.Instance.PlaySound("SpheroidDie");
            WaveManager.Instance.EnemyDead(this);
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

    //Computes force to apply to prevent spheroids from bunching up together
    Vector2 ComputeSeperation(SpheroidAI Self, List<SpheroidAI> AllSpheroids, float SeperationRadius)
    {
        //Compute a force to apply which will prevent the spheroids from colliding with each other
        Vector2 SeperationForce = Vector2.zero;
        int SpheroidCount = 0;

        //Grab the list of all other spheroid enemies and loop through to compare and apply forces
        List<SpheroidAI> Spheroids = WaveManager.Instance.GetSpheroidList();
        foreach(SpheroidAI Other in Spheroids)
        {
            //Ignore self
            if(Other == this) continue;

            //Apply seperation force if other spheroid is inside the seperation radius
            float OtherDistance = Vector2.Distance(transform.position, Other.transform.position);
            if(OtherDistance < SeperationRadius && OtherDistance > 0f)
            {
                SeperationForce += ((Vector2)transform.position - (Vector2)Other.transform.position).normalized / OtherDistance;
                SpheroidCount++;
            }
        }

        if(SpheroidCount > 0)
            SeperationForce /= SpheroidCount;

        return SeperationForce;
    }

    //Matches velocity/direction with nearby spheroids
    Vector2 ComputeAlignment(SpheroidAI Self, List<SpheroidAI> AllSpheroids, float AlignmentRadius)
    {
        //Find the average velocity of all nearby spheroids
        Vector2 Average = Vector2.zero;
        int Count = 0;

        //Compare against all others to find the average
        foreach(SpheroidAI Other in AllSpheroids)
        {
            float OtherDistance = Vector2.Distance(transform.position, Other.transform.position);
            if (Other != Self && OtherDistance < AlignmentRadius)
            {
                Average += Other.GetVelocity();
                Count++;
            }
        }

        if(Count > 0)
        {
            Average /= Count;
            return Average.normalized;
        }

        return Vector2.zero;
    }

    //Pulls the spheroid towards the center of the pack
    Vector2 ComputeCohesion(SpheroidAI Self, List<SpheroidAI> AllSpheroids, float CohesionRadius)
    {
        //Where to store the center of the pack
        Vector2 Center = Vector2.zero;
        int Count = 0;

        //Compare against other in the pack to find the center of the pack
        foreach (SpheroidAI Other in AllSpheroids)
        {
            float OtherDistance = Vector2.Distance(transform.position, Other.transform.position);
            if(Other != Self && OtherDistance < CohesionRadius)
            {
                Center += (Vector2)Other.transform.position;
                Count++;
            }
        }

        //Average the position to get the center location
        if(Count > 0)
        {
            Center /= Count;
            return (Center - (Vector2)transform.position).normalized;
        }

        return Vector2.zero;
    }

    //Prevents them running into the walls and corners
    private Vector2 ComputeWallAvoidance(float AvoidanceDistance)
    {
        Vector2 Pos = transform.position;
        Vector2 AvoidanceForce = Vector2.zero;

        Vector2 XBounds = LevelBorders.Instance.GetXBounds();
        Vector2 YBounds = LevelBorders.Instance.GetYBounds();

        //Left Wall
        if(Pos.x - XBounds.x < AvoidanceDistance)
            AvoidanceForce += new Vector2(1f / Mathf.Max(Pos.x - XBounds.x, 0.01f), 0f);

        //Right Wall
        if(XBounds.y - Pos.x < AvoidanceDistance)
            AvoidanceForce += new Vector2(-1f / Mathf.Max(XBounds.y - Pos.x, 0.01f), 0f);

        //Bottom Wall
        if(Pos.y - YBounds.x < AvoidanceDistance)
            AvoidanceForce += new Vector2(0f, 1f / Mathf.Max(Pos.y - YBounds.x, 0.01f));

        //Top Wall
        if(YBounds.y - Pos.y < AvoidanceDistance)
            AvoidanceForce += new Vector2(0f, -1f / Mathf.Max(YBounds.y - Pos.y, 0.01f));
        
        return AvoidanceForce.normalized;
    }

    //Causes multiple spheroids to flock together
    Vector2 GetSteering(SpheroidAI Self, List<SpheroidAI> OtherSpheroids, Vector2 CornerTarget, Vector2 PlayerPos)
    {
        Vector2 Seperation = ComputeSeperation(Self, OtherSpheroids, 3f) * 4f;
        Vector2 Alignment = ComputeAlignment(Self, OtherSpheroids, 4f);
        Vector2 Cohesion = ComputeCohesion(Self, OtherSpheroids, 4f);

        Vector2 AvoidPlayer = ((Vector2)Self.transform.position - PlayerPos).normalized * 3f;

        Vector2 MoveToCorner = (CurrentTarget - (Vector2)transform.position).normalized;

        Vector2 WallAvoidance = ComputeWallAvoidance(1f) * 5f;

        Vector2 Steering =
            Seperation +
            Alignment +
            Cohesion +
            AvoidPlayer +
            MoveToCorner +
            WallAvoidance;

        return Steering.normalized;
    }
}

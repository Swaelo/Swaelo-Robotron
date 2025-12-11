// ================================================================================================================================
// File:        SpheroidAI.cs
// Description:	Spheroid is an Enemy generator, they can spawn 1-6 Enforcers during their lifetime. They like to hang around the
//              corners of the arena, sometimes crossing over from one corner to another in order to avoid the player character.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class SpheroidAI : HostileEntity
{
    //Movement
    private List<Vector3> CornerPositions = new List<Vector3>(); //Corner position targets the spheroids will move between to avoid the player
    public Vector3 CurrentTarget;  //Current corner position the spheroid is seeking towards
    public float MoveSpeed = 2.5f; //How fast the spheroid moves around the level
    private Vector3 Steering = Vector3.zero; //Current direction the spheroid is steering in

    //Pathfinding
    private bool HasPath = false;   //Tracks if we currently have a pathway to our target location
    private List<MeshNode> PathToTarget = new List<MeshNode>(); //List of mesh nodes to navigate through to reach out target location

    //Enemy spawning
    public bool InCorner = false;  //Tracks if we are in a safe spot or not
    private float TimeInCorner = 0f;    //Track how long we have been sitting in the corner in a safe spot
    private float TimeToStartSpawning = 1f; //How long we need to wait in the corner before we start spawning a new enemy
    private float TimeToFinishSpawning = 3.5f; //How long we need to wait in the corner before we finish spawning a new enemy
    private bool StartedSpawning = false;
    private float SpawnAnimationTimer = 0f; //How long we have been in the process to spawn a new enemy in
    private float SpawnAnimationDuration = 2.5f;  //How long it takes to spawn an enemy in
    private Vector3 BaseScale;
    private AudioSource SoundEffectPlayer;

    //Hits until dead
    private int HitPoints = 3;


    private void Start()
    {
        //Grab the corner locations from the level borders manager
        CornerPositions = LevelBorders.Instance.GetCornerPositions();

        //Store transformation scale
        BaseScale = transform.localScale;

        SoundEffectPlayer = GetComponent<AudioSource>();

        //Select the closest corner to start moving toward
        CurrentTarget = GetClosestCornerPos();
        OffsetCornerTargetPos(1.5f);
    }

    private void Update()
    {
        //AI needs to be put on hold at times defined by the gamestate manager
        if(!GameState.Instance.ShouldAdvanceGame())
            return;

        //If we aren't in a corner, move towards our current corner target location
        if(!InCorner)
            SeekCorner();
        //If we are in a corner, we can try to summon enemies while we are hiding there
        else
            IdleInCorner();
    }

    private void SeekCorner()
    {
        //If we dont have a pathway to our target, we need to create it
        if(!HasPath)
        {
            PathToTarget = NavMeshManager.Instance.FindPathway(transform.position, CurrentTarget);
            HasPath = true;

            //Light up the pathway to see that it worked
            foreach(MeshNode Node in PathToTarget)
                Node.SetColor(Color.blue);
        }
        else
        {
            //If we get too close to the player we need to find a new pathway to our target
            float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
            if(PlayerDistance < 1f)
            {
                //Set the color of the current pathway nodes back to green as we arent using them now
                foreach(MeshNode Node in PathToTarget)
                    Node.SetColor(Color.green);

                //Find a new pathway to the target location and light those up instead
                PathToTarget = NavMeshManager.Instance.FindPathway(transform.position, CurrentTarget);
                foreach(MeshNode Node in PathToTarget)
                    Node.SetColor(Color.blue);
            }

            //Continue along our pathway of nodes while there are still any to follow
            if(PathToTarget.Count > 0)
            {
                //Get the position of our current node target
                Vector3 NodeTarget = PathToTarget[0].NodePos;
                    
                //Get current steering based on other spheroids around me as they move in flocks
                List<BaseEntity> OtherSpheroids = WaveManager.Instance.GetEntityList(EntityType.Spheroid);
                Steering = GetSteering(this, OtherSpheroids, NodeTarget, GameState.Instance.Player.transform.position);

                //Move towards it
                Vector3 MovementVelocity = Steering * MoveSpeed;
                transform.position += MovementVelocity * Time.deltaTime;

                //Check out how close we are now to our current node on the pathway
                float NodeDistance = Vector3.Distance(transform.position, NodeTarget);
                //Remove that node from the pathway once we reach it
                if(NodeDistance <= .1f)
                {
                    PathToTarget[0].SetColor(Color.green);
                    PathToTarget.RemoveAt(0);
                }
            }
            //Once the pathway has been completed, we continue moving onto the corner target location
            else
            {
                //Get current steering based on other spheroids around me as they move in flocks
                List<BaseEntity> OtherSpheroids = WaveManager.Instance.GetEntityList(EntityType.Spheroid);
                Steering = GetSteering(this, OtherSpheroids, CurrentTarget, GameState.Instance.Player.transform.position);

                //Move towards it
                Vector3 MovementVelocity = Steering * MoveSpeed;
                transform.position += MovementVelocity * Time.deltaTime;

                //Check current distance from our target location
                float TargetDistance = Vector3.Distance(transform.position, CurrentTarget);

                //Otherwise we check for having reached the target corner location
                if (TargetDistance <= 2f)
                {
                    InCorner = true;
                    HasPath = false;
                }
            }
        }
    }

    private void IdleInCorner()
    {
        //Time how long we have been hiding in the corner for
        TimeInCorner += Time.deltaTime;

        //Start spawning a new enemy once we have been hiding in the corner for long enough
        if(TimeInCorner >= TimeToStartSpawning && !StartedSpawning)
        {
            StartedSpawning = true;
            SoundEffectPlayer.Play();
        }

        if(StartedSpawning && TimeInCorner < TimeToFinishSpawning)
        {
            SpawnAnimationTimer += Time.deltaTime;
            //Get progress from 1 to 0 of the current spawn duration
            float SpawnProgress = Mathf.Clamp01(SpawnAnimationTimer / SpawnAnimationDuration);

            //Scale the growth, small at the start and largest right before completing the spawning
            float TransformScale = Mathf.Lerp(1f, 3f, SpawnProgress);
            transform.localScale = BaseScale * TransformScale;
            //Ramp up the spinning over time too
            float SpinSpeed = Mathf.Lerp(360f * 3f, 360f * 8f, SpawnProgress * SpawnProgress);
            transform.Rotate(0f, 0f, SpinSpeed * Time.deltaTime);

            //Rise in pitch the summoning sound effect
            SoundEffectPlayer.pitch = Mathf.Lerp(1f, 2f, SpawnProgress);
        }

        //Spawn in a new enemy once we have stayed spawning for long enough, then move to a new corner
        if(TimeInCorner >= TimeToFinishSpawning)
        {
            Vector3 SpawnLocation = GetEnforcerSpawnLocation();

            // //Spawn the new enemy in
            GameObject NewEnforcer = Instantiate(PrefabSpawner.Instance.GetPrefab("Enforcer"), SpawnLocation, Quaternion.identity);
            WaveManager.Instance.AddNewEnemy(NewEnforcer.GetComponent<HostileEntity>());

            //Play sound effect
            SoundEffectsPlayer.Instance.PlaySound("SpheroidSpawnComplete");
            

            MoveToAnotherCorner();
            return;
        }

        //Travel to a different corner if the player gets too close to this one
        float PlayerDistance = Vector3.Distance(transform.position, GameState.Instance.Player.transform.position);
        if (PlayerDistance <= 3f)
            MoveToAnotherCorner();
    }

    //Causes the spheroid to pick a new corner to start hiding in and start moving there straight away
    private void MoveToAnotherCorner()
    {
        //Target a random other corner which isnt our current target
        CurrentTarget = GetRandomOtherCornerPos();
        OffsetCornerTargetPos(1.5f);
        //Disable the InCorner flag and timer, and start moving toward the new corner target
        InCorner = false;
        TimeInCorner = 0f;
        StartedSpawning = false;
        SpawnAnimationTimer = 0f;
        transform.localScale = BaseScale;
        SoundEffectPlayer.Stop();
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

    //Returns a random location near the Spheroid where an Enforcer may be spawned in at
    private Vector3 GetEnforcerSpawnLocation()
    {
        //Start with the Spheroids current location
        Vector3 SpawnLocation = transform.position;

        //Pick a random direction on the XY plane
        Vector2 SpawnDirection = Random.insideUnitCircle.normalized;

        //Pick a random distance in a certain range
        float SpawnDistance = Random.Range(0.25f, 1.25f);

        //Apply the offset
        SpawnLocation += new Vector3(SpawnDirection.x, SpawnDirection.y, 0f) * SpawnDistance;

        //Clamp into the arena
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;
        SpawnLocation.x = Mathf.Clamp(SpawnLocation.x, XBounds.x, XBounds.y);
        SpawnLocation.y = Mathf.Clamp(SpawnLocation.y, YBounds.x, YBounds.y);

        //Return the new location
        return SpawnLocation;
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
    private Vector3 ComputeFlockingVector(SpheroidAI Self, List<BaseEntity> All, float FlockingRadius, float SeperationRadius)
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
        if(PlayerDistance < .5f)
            AvoidPlayer = (transform.position - GameState.Instance.Player.transform.position).normalized * 2f;
        
        return AvoidPlayer;
    }

    //Causes multiple spheroids to flock together
    Vector3 GetSteering(SpheroidAI Self, List<BaseEntity> OtherSpheroids, Vector3 CornerTarget, Vector3 PlayerPos)
    {
        Vector3 Flocking = ComputeFlockingVector(Self, OtherSpheroids, 2f, .5f);

        //Get steering towards current target location
        Vector3 MoveToCorner = (CornerTarget - transform.position).normalized * 2f;


        Vector3 AvoidPlayer = ComputePlayerAvoidance();
        Vector3 WallAvoidance = ComputeWallAvoidance(.25f) * 3f;

        Vector3 Steering = MoveToCorner + AvoidPlayer;

        Steering = Vector3.ClampMagnitude(Steering, 1f);
        return Steering;
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
}

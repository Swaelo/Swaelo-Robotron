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
    //Engine components
    private NavMeshManager NavMesh = null;
    private WaveManager Waves = null;
    private GameState Game = null;
    private PrefabSpawner Spawner = null;
    public SoundEffectsPlayer Sounds = null;

    //Movement
    public Vector3 CurrentTarget;  //Current corner position the spheroid is seeking towards
    public float MoveSpeed = 2.5f; //How fast the spheroid moves around the level
    private Vector3 Steering = Vector3.zero; //Current direction the spheroid is steering in

    //Pathfinding
    private bool HasPath = false;   //Tracks if we currently have a pathway to our target location
    private List<MeshNode> PathToTarget = new List<MeshNode>(); //List of mesh nodes to navigate through to reach out target location

    private Vector3 BaseScale;  //The original transform scale of the enemy
    private AudioSource SoundEffectPlayer;  //For playing sound effects

    //New enemy spawning
    public bool InHiding = false; //Tracks if we are in a safe spot or not
    private float TimeHiding = 0f;  //Track how long we have been hiding in a safe spot
    private bool SpawningEnemy = false; //Tracks when the spheroid has started spawning an enemy in its safe spot
    private float EnemySpawnStartLimit = 3f; //How long we need to wait in the safe spot before we can start spawning an enemy
    private float EnemySpawnDuration = 3.5f; //How long it takes to channel the spawning of a new enemy
    private float EnemySpawnTimer = 0f; //Tracks how long into the spawn process we are

    //Hits until dead
    private int HitPoints = 3;

    private void Start()
    {
        NavMesh = NavMeshManager.Instance;
        Waves = WaveManager.Instance;
        Game = GameState.Instance;
        Spawner = PrefabSpawner.Instance;
        Sounds = SoundEffectsPlayer.Instance;

        //Store transformation scale
        BaseScale = transform.localScale;

        SoundEffectPlayer = GetComponent<AudioSource>();

        //Select the closest corner to start moving toward
        CurrentTarget = FindHidingSpot();
    }

    private void Update()
    {
        //AI needs to be put on hold at times defined by the gamestate manager
        if(!Game.ShouldAdvanceGame())
        {
            SoundEffectPlayer.Stop();
            return;
        }

        // //If we aren't hiding in a safe spot, move towards it
        // if(!InHiding)
        //     SeekSafeSpot();
        // //Once we reach a safe spot, we can try to summon enemies while we are in hiding
        // else
        //     Hide();
    }

    //Moves towards our current safe spot target location
    private void SeekSafeSpot()
    {
        //If we dont have a pathway to our safe spot, we need to find one
        if(!HasPath)
        {
            CurrentTarget = FindHidingSpot();
            PathToTarget = NavMesh.FindPathway(transform.position, CurrentTarget);

            Debug.Log("Path to target is " + PathToTarget.Count + " nodes long");

            //If no path is found, just stay put and skip movement this frame
            if(PathToTarget.Count == 0)
            {
                Debug.Log("end of pathway has been reached");
                HasPath = false;
                return;
            }

            HasPath = true;

            //Light up the pathway for debugging
            foreach(MeshNode Node in PathToTarget)
                Node.SetColor(Color.blue);
        }
        //If we already have a pathway then we will follow that
        else
        {
            //If we get too near the player, find a new pathway which goes around them
            //AvoidPlayer();

            //Set the current target location we will be moving to, based on if we still have pathway nodes to follow or not
            bool PathFinished = PathToTarget.Count > 0;
            Vector3 TargetLocation = PathFinished ? PathToTarget[0].NodePos : CurrentTarget;

            //Get current steering for the flocking behaviour with other spheroids
            List<BaseEntity> OtherSpheroids = Waves.GetEntityList(EntityType.Spheroid);
            Steering = GetSteering(this, OtherSpheroids, TargetLocation, Game.Player.transform.position);

            //Move towards our current location with this steering also applied
            Vector3 MovementVelocity = Steering * MoveSpeed;
            Vector3 PreviousPosition = transform.position;
            transform.position += MovementVelocity * Time.deltaTime;

            Debug.Log("Moving from " + PreviousPosition + " to " + transform.position);

            //If were still following a pathway, knock the front node off the list once we get close enough to it
            if(!PathFinished && Vector3.Distance(transform.position, TargetLocation) <= .1f)
            {
                Debug.Log("path node reached");

                PathToTarget[0].SetColor(Color.green);
                PathToTarget.RemoveAt(0);
                if(PathToTarget.Count == 0)
                    HasPath = false;
            }

            //If the pathway has completed check if we have reached the final target location
            if(!HasPath && Vector3.Distance(transform.position, CurrentTarget) <= 2f)
            {
                InHiding = true;
                HasPath = false;
            }
        }
    }

    //Reconstructs a new pathway to our target location to avoid the player if we get too close to them
    private void AvoidPlayer(float AvoidanceDistance = 1f)
    {
        //Check how close we are to the player character
        float PlayerDistance = Vector3.Distance(transform.position, Game.Player.transform.position);
        if(PlayerDistance < AvoidanceDistance)
        {
            Debug.Log("too close to player, running away");

            //Once we get too close to the player, clear our current pathway
            foreach(MeshNode Node in PathToTarget)
                Node.SetColor(Color.green);

            //Find a new pathway to the hiding spot which avoids going near the player
            PathToTarget = NavMesh.FindPathwayAvoidingPlayer(transform.position, CurrentTarget);
        }
    }

    //Finds a reachable location away from the player character as the target for a hiding spot
    private Vector3 FindHidingSpot()
    {
        Vector3 HidingSpot = Vector3.zero;

        //Find variables we need access to for the search
        Vector3 EnemyPos = transform.position;
        Vector3 PlayerPos = Game.Player.transform.position;
        float MinPlayerDistance = 1.5f;
        float MaxSearchRadius = 10f;
        int MaxSearchAttempts = 50;
        
        //Find the direction we need to move to travel away from the player
        Vector3 AwayFromPlayerDirection = (EnemyPos - PlayerPos).normalized;

        //Iterate over and compare viable hiding spots
        float BestHidingScore = float.MinValue;
        Vector3 BestHidingCandidate = Vector3.zero;
        bool HidingSpotFound = false;

        

        return HidingSpot;
    }

    //Hides in the hiding spot and tries to summon enemies
    private void Hide()
    {
        //Track how long we have been in hiding for
        TimeHiding += Time.deltaTime;

        //We can start spawning a new enemy once we have been hiding in the corner for long enough
        if(TimeHiding >= EnemySpawnStartLimit && !SpawningEnemy)
        {
            //Begin the spawning process
            SpawningEnemy = true;
            SoundEffectPlayer.Play();
        }

        //Continue the spawning process until it completed
        if(SpawningEnemy && TimeHiding < EnemySpawnDuration)
        {
            //Progress the timer and get a current progression percentage
            EnemySpawnTimer += Time.deltaTime;
            float SpawnProgress = Mathf.Clamp01(EnemySpawnTimer / EnemySpawnDuration);

            //Scale the transform to apply growth over the course of the summoning duration
            float TransformScale = Mathf.Lerp(1f, 2f, SpawnProgress);
            transform.localScale = BaseScale * TransformScale;
            //Ramp up the rotation over time
            float SpinSpeed = Mathf.Lerp(360f * 3f, 360f * 8f, SpawnProgress * SpawnProgress);
            transform.Rotate(0f, 0f, SpinSpeed * Time.deltaTime);
            //Rise the pitch of the summoning sound
            SoundEffectPlayer.pitch = Mathf.Lerp(1f, 2f, SpawnProgress);
        }

        //Once we have stayed in hiding for long enough, spawn an enemy and head to a new hiding spot
        if(TimeHiding >= EnemySpawnDuration)
        {
            //Spawn the new enemy in
            Vector3 SpawnLocation = GetEnforcerSpawnLocation();
            GameObject Enforcer = Instantiate(Spawner.GetPrefab("Enforcer"), SpawnLocation, Quaternion.identity);
            Waves.AddNewEnemy(Enforcer.GetComponent<HostileEntity>());
            Sounds.PlaySound("SpheroidSpawningComplete");

            //Move to another location
            FindHidingSpot();
            return;
        }

        //Find a new hiding spot if the player comes to close to us while we are still spawning
        float PlayerDistance = Vector3.Distance(transform.position, Game.Player.transform.position);
        if(PlayerDistance <= 2.5f)
            FindHidingSpot();
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

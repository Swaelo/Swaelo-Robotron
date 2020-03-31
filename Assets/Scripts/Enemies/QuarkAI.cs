// ================================================================================================================================
// File:        QuarkAI.cs
// Description:	Quarks wander around the level sporadically while spawning Tanks into the level 
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class QuarkAI : HostileEntity
{
    private float MoveSpeed = 3.5f; //How fast the Quark moves around the level
    private Vector3 CurrentTarget;  //Position the Quark is currently moving towards
    private Vector2 TargetRangeOffset = new Vector2(3.5f, 7.5f);    //How far in each direction a Quarks new target location can be from its current position
    private Vector2 XWanderRange = new Vector2(-7f, 7f);    //Position range on the X axis where the Quark is able to wander to
    private Vector2 YWanderRange = new Vector2(-4f, 4f);    //Position range on the Y axis where the Quark is able to wander to
    private float NewTargetRange = 1.5f;    //How close the Quark must be from its current target before it picks up a new target location to seek to
    private int SpawnsLeft; //How many more Tanks this Quark is able to spawn before it self-destructs
    private float SpawnCooldown;    //Time left until another Tank can be spawned in
    private Vector2 SpawnCooldownRange = new Vector2(1.5f, 3.75f);    //Time before the first Tank can be spawned in
    private int MaxSpawnCount = 6;  //Maximum number of Tanks that any one Quark is able to spawn in
    private Vector2 XSpawnRange = new Vector2(-7.087f, 7.084f); //Range along the X axis where Tanks can be spawned in
    private Vector2 YSpawnRange = new Vector2(-4f, 4f); //Range along the Y axis where Tank can be spawned in
    private Vector2 SpawnRangeOffset = new Vector2(0.75f, 1.65f);   //How far in each direction a Tanks spawn location may be offset from the Quarks location
    private Vector2 HitPointRange = new Vector2(1, 3);  //Value range of hitpoints that may be assigned to the Quark when its spawned in
    private int HitPoints;  //Hits that can be taken before the Quark dies

    private void Start()
    {
        //Assign a random number of health points to the spheroid
        HitPoints = (int)Random.Range(HitPointRange.x, HitPointRange.y);

        //Randomly set the number of Tanks this Quark will be allowed to spawn before it self-destructs
        SpawnsLeft = Random.Range(1, MaxSpawnCount);

        //Get a random target location to seek towards
        GetNewTarget();

        //Set the timer before the first Tank can be spawned
        SpawnCooldown = Random.Range(SpawnCooldownRange.x, SpawnCooldownRange.y);
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        SeekTarget();
        SpawnTanks();
    }

    //Moves towards the current target location
    private void SeekTarget()
    {
        //Seek towards current target
        Vector3 TargetDirection = Vector3.Normalize(CurrentTarget - transform.position);
        transform.position += TargetDirection * MoveSpeed * Time.deltaTime;

        //Fetch a new target when we reach the current one
        float TargetDistance = Vector3.Distance(CurrentTarget, transform.position);
        if (TargetDistance <= NewTargetRange)
            GetNewTarget();
    }

    //Gets a new random target location away from the Quark to move towards
    private void GetNewTarget()
    {
        //Start with the current position
        Vector3 NewTarget = transform.position;

        //Offset from this position by a random amount on each axis
        NewTarget.x += Random.value >= 0.5f ?
            Random.Range(TargetRangeOffset.x, TargetRangeOffset.y) :
            Random.Range(-TargetRangeOffset.x, -TargetRangeOffset.y);
        NewTarget.y += Random.value >= 0.5f ?
            Random.Range(TargetRangeOffset.x, TargetRangeOffset.y) :
            Random.Range(-TargetRangeOffset.x, -TargetRangeOffset.y);

        //Make sure the new location stays inside the level boundaries
        NewTarget.x = Mathf.Clamp(NewTarget.x, XWanderRange.x, XWanderRange.y);
        NewTarget.y = Mathf.Clamp(NewTarget.y, YWanderRange.x, YWanderRange.y);

        //Set this location as the new target
        CurrentTarget = NewTarget;
    }

    //Spawns in a new Tank whenever the cooldown timer expires
    private void SpawnTanks()
    {
        //Wait for the cooldown timer to expire
        SpawnCooldown -= Time.deltaTime;
        if(SpawnCooldown <= 0.0f)
        {
            //Reset the spawn timer
            SpawnCooldown = Random.Range(SpawnCooldownRange.x, SpawnCooldownRange.y);

            //Get a random location near the Quark and spawn the Enforcer there
            Vector3 SpawnLocation = GetTankSpawnLocation();
            GameObject NewTank = Instantiate(PrefabSpawner.Instance.GetPrefab("Tank"), SpawnLocation, Quaternion.identity);

            //Let the WaveManager know this new enemy exists
            WaveManager.Instance.AddTargetEnemy(NewTank.GetComponent<HostileEntity>());

            //Take 1 away from this Quarks spawns counter, then check if its run out to trigger self-destruct
            SpawnsLeft -= 1;
            if(SpawnsLeft <= 0)
            {
                //Tell the wave manager this enemies being killed, then destroy it
                WaveManager.Instance.TargetEnemyDead(this);
                Destroy(this.gameObject);
            }
        }
    }

    //Returns a random location near the Quark where a Tank may be spawned in at
    private Vector3 GetTankSpawnLocation()
    {
        //Start with the Quarks current location
        Vector3 SpawnLocation = transform.position;

        //Offset this location randomly in both the X and Y axes
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

    //Removes one of the Quarks hit points, kills it when they run out
    private void TakeDamage()
    {
        //Take away 1 of the Quarks hitpoints and check if its still alive
        HitPoints--;
        if(HitPoints <= 0)
        {
            //Kill the Quark once its hitpoints reach zero
            WaveManager.Instance.TargetEnemyDead(this);
            GameState.Instance.IncreaseScore((int)PointValue.Quark);
            Destroy(gameObject);
        }
    }

    //Handle collisions with certain other objects and entities that we come into contact with
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Player projectiles kill the Quark
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            WaveManager.Instance.TargetEnemyDead(this);
            Destroy(gameObject);
        }
        //Player is killed on contact
        else if(collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }
}
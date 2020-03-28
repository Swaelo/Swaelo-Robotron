// ================================================================================================================================
// File:        WaveManager.cs
// Description:	Handles the spawning in and removal of enemies at the beginning and end of waves, tracks what is active in current wave
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum WavePreperationStage
{
    SpawningFriendlies = 0,
    SpawningHostiles = 1,
    WarmUp = 2
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;
    private void Awake() { Instance = this; }
    public List<BaseEntity> ActiveEntities = new List<BaseEntity>();    //Every entity currently active in the game
    public List<HostileEntity> TargetEntities = new List<HostileEntity>();  //Every enemy currently active that must be killed for the wave to complete
    private Vector2 XBounds = new Vector2(-7f, 7f); //Value range that entities can be spawned along the X axis while remaining inside the level boundaries
    private Vector2 YBounds = new Vector2(-4f, 4f); //Value range that entities can be spawned along the Y axis while remaining inside the level boundaries
    private float MinimumEntityDistacne = 0.65f;  //Minimum distance entities can be spawned from one another
    private float MinimumPlayerDistance = 1.5f; //Minimum distance entities can be spawned from the player
    private int MaxPositionTries = 15;  //Maxmimum number of tries allowed to get a new random position for spawning something in
    public bool RoundWarmingUp = false;    //Flagged when a new round has begun then disabled once the countdown has expired
    public Text UIRoundDisplay; //UI Text component used to display the current round number
    private WavePreperationStage WavePrepStage = WavePreperationStage.SpawningFriendlies;
    private float SpawnPeriodDuration = 0.75f;  //How much time is allowed at the start of a new round for each group (friendly and hostile) of entities to be spawned in
    private List<GameObject> FriendlySpawns = new List<GameObject>();
    private float FriendlySpawnInterval;    //How often to spawn in friendly entities during the friendly spawn period
    private float NextFriendlySpawn;    //How long until the next friendly entity should be spawned in during the friendly spawn period
    private int FriendlySpawnsRemaining;    //How many friendly spawns remaining for the current friendly spawn period
    private List<GameObject> HostileSpawns = new List<GameObject>();
    private float HostileSpawnInterval; //How often to spawn in hostile entities during the hostile spawn period
    private float NextHostileSpawn; //How long until the next hostile entity should be spawned in during the hostile spawn period
    private int HostileSpawnsRemaining; //How many hostile spawns remaining for the current hostile spawn period
    private float WarmUpPeriod = 0.5f;  //How long the game waits once everything has been spawned before the round begins
    private float WarmUpRemaining = 0.5f;   //Time left until the round begins

    //Spawns in all the enemies for the given wave number
    public void StartWave(int WaveNumber)
    {
        //Update the UI round display
        UIRoundDisplay.text = "Round    " + WaveNumber.ToString();

        //Reset the human rescue score multiplier
        GameState.Instance.RescueMultiplier = 1;

        //Grab the list of entities which belong to the given wave number
        WaveEntities Entities = WaveData.Instance.GetWaveData(WaveNumber);

        //Count the total number of hostile and friendly entities there are for this round
        FriendlySpawnsRemaining = Entities.Mommies + Entities.Daddies + Entities.Mikeys;
        HostileSpawnsRemaining = Entities.Grunts + Entities.Electrodes + Entities.Hulks + Entities.Brains + Entities.Spheroids + Entities.Quarks + Entities.Enforcer + Entities.Tank;

        //Set the timers for how often each friendly and hostile entity should be spawned in so they spread out across their respective spawning periods
        FriendlySpawnInterval = SpawnPeriodDuration / FriendlySpawnsRemaining;
        NextFriendlySpawn = FriendlySpawnInterval;
        HostileSpawnInterval = SpawnPeriodDuration / HostileSpawnsRemaining;
        NextHostileSpawn = HostileSpawnInterval;

        //Begin the warmup period, started by spawning in all the friendly entities first
        RoundWarmingUp = true;
        WavePrepStage = WavePreperationStage.SpawningFriendlies;

        //Create two lists, of all the friendly and hostile entities that need to be spawned into the level
        FriendlySpawns = new List<GameObject>();
        for (int i = 0; i < Entities.Mommies; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Mummy"));
        for (int i = 0; i < Entities.Daddies; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Daddy"));
        for (int i = 0; i < Entities.Mikeys; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Mikey"));

        HostileSpawns = new List<GameObject>();
        for (int i = 0; i < Entities.Grunts; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Grunt"));
        for (int i = 0; i < Entities.Electrodes; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        for (int i = 0; i < Entities.Hulks; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Hulk"));
        for (int i = 0; i < Entities.Brains; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Brain"));
        for (int i = 0; i < Entities.Spheroids; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Spheroid"));
        for (int i = 0; i < Entities.Quarks; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Quark"));
        for (int i = 0; i < Entities.Enforcer; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Enforcer"));
        for (int i = 0; i < Entities.Tank; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Tank"));
        
    }

    //Restarts the current wave with the same amount of enemies and everything thats left
    public void RestartWave()
    {
        //Make a new WaveData so we can count up all the entities that still remain in this wave before we clean them all up
        WaveEntities EntityCount = new WaveEntities();
        
        //Loop through all the entities in active list and add them to the counter
        foreach(BaseEntity Entity in ActiveEntities)
        {
            switch(Entity.Type)
            {
                case (EntityType.Brain):
                    EntityCount.Brains++;
                    break;
                case (EntityType.Daddy):
                    EntityCount.Daddies++;
                    break;
                case (EntityType.Electrode):
                    EntityCount.Electrodes++;
                    break;
                case (EntityType.Enforcer):
                    //Tell Enforcers to clean up any remaining projectiles they have fired before they get cleaned up
                    Entity.GetComponent<EnforcerAI>().CleanProjectiles();
                    EntityCount.Enforcer++;
                    break;
                case (EntityType.Grunt):
                    EntityCount.Grunts++;
                    break;
                case (EntityType.Hulk):
                    EntityCount.Hulks++;
                    break;
                case (EntityType.Mikey):
                    EntityCount.Mikeys++;
                    break;
                case (EntityType.Mummy):
                    EntityCount.Mommies++;
                    break;
                case (EntityType.Quark):
                    EntityCount.Quarks++;
                    break;
                case (EntityType.Spheroid):
                    EntityCount.Spheroids++;
                    break;
                case (EntityType.Tank):
                    //They need to be told to clean up any left over projectiles they have fired though
                    Entity.GetComponent<TankAI>().CleanProjectiles();
                    EntityCount.Tank++;
                    break;
                case (EntityType.DaddyProg):
                    EntityCount.DaddyProg++;
                    break;
                case (EntityType.MummyProg):
                    EntityCount.MummyProg++;
                    break;
                case (EntityType.MikeyProg):
                    EntityCount.MikeyProg++;
                    break;
            }
        }

        //Reset the human rescue score multiplier
        GameState.Instance.RescueMultiplier = 1;

        //Have any remaining player projectiles cleaned up, and return the player back to the level center
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();

        //Clean up all the enemies which are still in the level, and reset the two tracking lists
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        ActiveEntities.Clear();
        TargetEntities.Clear();

        //Count the total number of hostile and friendly entities that will need to be spawned back in to restart this round properly
        FriendlySpawnsRemaining = EntityCount.Mommies + EntityCount.Daddies + EntityCount.Mikeys;
        HostileSpawnsRemaining = EntityCount.Grunts + EntityCount.Electrodes + EntityCount.Hulks + EntityCount.Brains + EntityCount.Spheroids + EntityCount.Quarks + EntityCount.Enforcer + EntityCount.Tank + EntityCount.DaddyProg + EntityCount.MummyProg + EntityCount.MikeyProg;

        //Set the timers for how often each entity type should be spawned in during their periods
        FriendlySpawnInterval = SpawnPeriodDuration / FriendlySpawnsRemaining;
        NextFriendlySpawn = FriendlySpawnInterval;
        HostileSpawnInterval = SpawnPeriodDuration / HostileSpawnsRemaining;
        NextHostileSpawn = HostileSpawnInterval;

        //Begin the warmup period, spawning in friendlies first
        RoundWarmingUp = true;
        WavePrepStage = WavePreperationStage.SpawningFriendlies;

        //Create two lists, one for each of all the friendly and hostile entities that need to be spawned back in to restart this round
        FriendlySpawns = new List<GameObject>();
        for (int i = 0; i < EntityCount.Mommies; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Mummy"));
        for (int i = 0; i < EntityCount.Daddies; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Daddy"));
        for (int i = 0; i < EntityCount.Mikeys; i++)
            FriendlySpawns.Add(PrefabSpawner.Instance.GetPrefab("Mummy"));

        HostileSpawns = new List<GameObject>();
        for (int i = 0; i < EntityCount.Grunts; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Grunt"));
        for (int i = 0; i < EntityCount.Electrodes; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        for (int i = 0; i < EntityCount.Hulks; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Hulk"));
        for (int i = 0; i < EntityCount.Brains; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Brain"));
        for (int i = 0; i < EntityCount.Spheroids; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Spheroid"));
        for (int i = 0; i < EntityCount.Quarks; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Quark"));
        for (int i = 0; i < EntityCount.Enforcer; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Enforcer"));
        for (int i = 0; i < EntityCount.Tank; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("Tank"));
        for (int i = 0; i < EntityCount.DaddyProg; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("DaddyProg"));
        for (int i = 0; i < EntityCount.MummyProg; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("MummyProg"));
        for (int i = 0; i < EntityCount.MikeyProg; i++)
            HostileSpawns.Add(PrefabSpawner.Instance.GetPrefab("MikeyProg"));
    }

    private void Update()
    {
        //All enemy/human AI and player controls are disabled during the warmup period
        if(RoundWarmingUp)
        {
            //Go to the current round prep stage
            switch(WavePrepStage)
            {
                //In the first stage, all friendly entities are spawned in
                case (WavePreperationStage.SpawningFriendlies):
                    SpawnFriendlyEntities();
                    break;
                //In the second stage, all enemies are spawned in
                case (WavePreperationStage.SpawningHostiles):
                    SpawnHostileEntities();
                    break;
                //There is a final short waiting period after everything is spawned, before the round starts
                case (WavePreperationStage.WarmUp):
                    WaitForWarmUp();
                    break;
            }
        }
    }

    //Spawns in friendly entities over the course of the friendly entity spawning period
    private void SpawnFriendlyEntities()
    {
        //Wait until the timer to spawn in the next friendly entity expires
        NextFriendlySpawn -= Time.deltaTime;
        if(NextFriendlySpawn <= 0.0f)
        {
            //Reset the timer and decrement the number of friendly spawns remaining
            NextFriendlySpawn = FriendlySpawnInterval;
            FriendlySpawnsRemaining--;
            //Spawn in the next friendly entity
            Vector3 SpawnPos = GetNewSpawnPosition(0);
            GameObject FriendlySpawn = Instantiate(FriendlySpawns[0], SpawnPos, Quaternion.identity);
            //Remove it from the list to be spawned and add it to the list of active entities
            FriendlySpawns.RemoveAt(0);
            ActiveEntities.Add(FriendlySpawn.GetComponent<BaseEntity>());
            //Move onto the hostile spawning stage if all friendly entities have now been spawned in
            if (FriendlySpawnsRemaining <= 0)
                WavePrepStage = WavePreperationStage.SpawningHostiles;
        }
    }

    //Spawns in hostile entities over the course of the hostile entity spawning period
    private void SpawnHostileEntities()
    {
        //Wait until the timer to spawn in the next hostile entity expires
        NextHostileSpawn -= Time.deltaTime;
        if(NextHostileSpawn <= 0.0f)
        {
            //Reset the timer and decrement the number of hostile spawns remaining
            NextHostileSpawn = HostileSpawnInterval;
            HostileSpawnsRemaining--;
            //Spawn in the next hostile entity
            Vector3 SpawnPos = GetNewSpawnPosition(0);
            GameObject HostileSpawn = Instantiate(HostileSpawns[0], SpawnPos, Quaternion.identity);
            //Remove it from the list of hostiles to be spawned, add it to the list of active entities and the list of targets if required
            HostileSpawns.RemoveAt(0);
            ActiveEntities.Add(HostileSpawn.GetComponent<BaseEntity>());
            if (IsEnemyRequired(HostileSpawn))
                TargetEntities.Add(HostileSpawn.GetComponent<HostileEntity>());
            //Move onto the final preperation waiting period once all hostile entities have been spawned in
            if (HostileSpawnsRemaining <= 0)
            {
                WavePrepStage = WavePreperationStage.WarmUp;
                WarmUpRemaining = WarmUpPeriod;
            }
        }
    }

    //Waits until the warm up period is over before allowing the round to actually begin
    private void WaitForWarmUp()
    {
        //Start the round once the warm up period is over
        WarmUpRemaining -= Time.deltaTime;
        if (WarmUpRemaining <= 0.0f)
            RoundWarmingUp = false;
    }

    //Checks if the enemy that has been spawned in should be added to the list of target enemies that must be killed for the round to progress
    private bool IsEnemyRequired(GameObject Enemy)
    {
        //Get the enemies type
        EntityType EnemyType = Enemy.GetComponent<HostileEntity>().Type;

        //Grunts, Brains, Spheroids, Quarks, Enforcers and Tanks are required enemy types
        if (EnemyType == EntityType.Grunt || EnemyType == EntityType.Brain || EnemyType == EntityType.Spheroid || EnemyType == EntityType.Quark || EnemyType == EntityType.Enforcer || EnemyType == EntityType.Tank || EnemyType == EntityType.DaddyProg || EnemyType == EntityType.MummyProg || EnemyType == EntityType.MikeyProg )
            return true;

        //All the others (electrodes and hulks) are not required to be killed to complete the round
        return false;
    }

    //Cleans up anything left over from the current wave
    private void CleanWave()
    {
        //Destroy any leftover entities
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        //Clear the tracking lists
        ActiveEntities.Clear();
        TargetEntities.Clear();
        //Move the player character back to the center of the screen
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();
        //Have any remaining player projectiles cleaned up
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
    }
    
    //Returns a random spawn position that isnt too close to the player character or too close to any other already active entity
    private Vector3 GetNewSpawnPosition(int Iterations)
    {
        //Get a random position inside the level
        Vector3 NewPosition = new Vector3(Random.Range(XBounds.x, XBounds.y), Random.Range(YBounds.x, YBounds.y), 0f);

        //Print an error message and return the new position if the random position iteration count reached the maximum count
        if(Iterations > MaxPositionTries)
        {
            Debug.Log("Exceeded maximum tries for getting a random position for spawning entity, returning whatever position.");
            return NewPosition;
        }

        //Grab a new position if this is too close to any existing entity
        foreach(BaseEntity Entity in ActiveEntities)
        {
            //Compare distance between the new position and this entity
            float EntityDistance = Vector3.Distance(NewPosition, Entity.transform.position);

            //Return a new position if this entity is too close
            if (EntityDistance < MinimumEntityDistacne)
                return GetNewSpawnPosition(Iterations+1);
        }

        //Return a new position this new position is too close to the players position
        float PlayerDistance = Vector3.Distance(NewPosition, GameState.Instance.Player.transform.position);
        if (PlayerDistance < MinimumPlayerDistance)
            return GetNewSpawnPosition(Iterations+1);

        //Return this new position if it wasnt found to be too close to anything else
        return NewPosition;
    }

    //Any enemy/friendly entity that does alerts the WaveManager through this function so it can be removed from the tracking lists
    public void OptionalEnemyDead(HostileEntity OptionalEnemy)
    {
        ActiveEntities.Remove(OptionalEnemy);
    }
    public void TargetEnemyDead(HostileEntity TargetEnemy)
    {
        //Ignore this is wave progression has been disabled
        if (GameState.Instance.DisableWaveProgression)
            return;

        //Remove the entity from both of the tracking lists
        ActiveEntities.Remove(TargetEnemy);
        TargetEntities.Remove(TargetEnemy);

        //Whenever a target enemy has been destroyed, check if it was the last one
        if(TargetEntities.Count == 0)
        {
            //Clean up the current wave, then move onto and begin the next one
            CleanWave();
            GameState.Instance.CurrentWave++;
            StartWave(GameState.Instance.CurrentWave);
        }
    }

    //Adds a new enemy to the list of target enemies needing to be destroyed before the current round can be complete, called by Spheroids and Quarks when they spawn enemies into the game
    public void AddTargetEnemy(HostileEntity TargetEnemy)
    {
        ActiveEntities.Add(TargetEnemy);
        TargetEntities.Add(TargetEnemy);
    }

    //Removes a human survivor from the active entity list
    public void RemoveHumanSurvivor(BaseEntity Human, bool KilledByEnemy = false)
    {
        //If the entity is being removes as a result of it being killed by one of the enemies, spawn in the skull and crossbones near them
        if (KilledByEnemy)
            PrefabSpawner.Instance.SpawnPrefab("Skull", Human.transform.position, Quaternion.identity);

        //Remove the entity from the tracking list
        ActiveEntities.Remove(Human);
    }
}
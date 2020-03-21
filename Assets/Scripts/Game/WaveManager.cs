// ================================================================================================================================
// File:        WaveManager.cs
// Description:	Handles the spawning in and removal of enemies at the beginning and end of waves, tracks what is active in current wave
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

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
    private float PreGameWarmUp = 1.25f;    //How long the game is paused for at the start of a new round
    private float WarmUpRemaining = 1.25f;  //Time left in the pregame warmup until the round begins
    public bool RoundWarmingUp = false;    //Flagged when a new round has begun then disabled once the countdown has expired

    //Spawns in all the enemies for the given wave number
    public void StartWave(int WaveNumber)
    {
        //Start the pregame warmup period
        RoundWarmingUp = true;
        WarmUpRemaining = PreGameWarmUp;

        //Reset the human rescue score multiplier
        GameState.Instance.RescueMultiplier = 1;

        //Grab the list of entities which belong to the given wave number
        WaveEntities Entities = WaveData.Instance.GetWaveData(WaveNumber);

        //First spawn in the enemies that are required for this round
        SpawnEnemies(Entities.Grunts, PrefabSpawner.Instance.GetPrefab("Grunt"), true);
        SpawnEnemies(Entities.Electrodes, PrefabSpawner.Instance.GetElectrodePrefab(), false);
        SpawnEnemies(Entities.Hulks, PrefabSpawner.Instance.GetPrefab("Hulk"), false);
        SpawnEnemies(Entities.Brains, PrefabSpawner.Instance.GetPrefab("Brain"), true);
        SpawnEnemies(Entities.Spheroids, PrefabSpawner.Instance.GetPrefab("Spheroid"), true);
        SpawnEnemies(Entities.Quarks, PrefabSpawner.Instance.GetPrefab("Quark"), true);

        //Now spawn in any human survivors for this round
        SpawnHumans(Entities.Mommies, PrefabSpawner.Instance.GetPrefab("Mummy"));
        SpawnHumans(Entities.Daddies, PrefabSpawner.Instance.GetPrefab("Daddy"));
        SpawnHumans(Entities.Mikeys, PrefabSpawner.Instance.GetPrefab("Mikey"));
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
                //Enforcers are ignored, as the Spheroids are going to have their spawn counters reset when the round restarts
                case (EntityType.Enforcer):
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
                //Tanks are ignored, as the Quarks are going to have their spawn counters reset when the round restarts
                case (EntityType.Tank):
                    break;
            }
        }

        //Have any remaining player projectiles cleaned up, and return the player back to the level center
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();

        //Clean up all the enemies which are still in the level, and reset the two tracking lists
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        ActiveEntities.Clear();
        TargetEntities.Clear();

        //Start the warmup period
        RoundWarmingUp = true;
        WarmUpRemaining = PreGameWarmUp;

        //Reset the rescue multiplier
        GameState.Instance.RescueMultiplier = 1;

        //Respawn back in the enemies that were left in this round when the player died
        SpawnEnemies(EntityCount.Grunts, PrefabSpawner.Instance.GetPrefab("Grunt"), true);
        SpawnEnemies(EntityCount.Electrodes, PrefabSpawner.Instance.GetElectrodePrefab(), false);
        SpawnEnemies(EntityCount.Hulks, PrefabSpawner.Instance.GetPrefab("Hulk"), false);
        SpawnEnemies(EntityCount.Brains, PrefabSpawner.Instance.GetPrefab("Brain"), true);
        SpawnEnemies(EntityCount.Spheroids, PrefabSpawner.Instance.GetPrefab("Spheroid"), true);
        SpawnEnemies(EntityCount.Quarks, PrefabSpawner.Instance.GetPrefab("Quark"), true);

        ////Now spawn in any human survivors for this round
        SpawnHumans(EntityCount.Mommies, PrefabSpawner.Instance.GetPrefab("Mummy"));
        SpawnHumans(EntityCount.Daddies, PrefabSpawner.Instance.GetPrefab("Daddy"));
        SpawnHumans(EntityCount.Mikeys, PrefabSpawner.Instance.GetPrefab("Mikey"));
    }

    private void Update()
    {
        //All enemy/human AI and player controls are disabled during the warmup period
        if(RoundWarmingUp)
        {
            //Wait for the warmup period to complete
            WarmUpRemaining -= Time.deltaTime;
            if(WarmUpRemaining <= 0.0f)
            {
                //Disable the warmup period
                RoundWarmingUp = false;
            }
        }
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

    //Spawns in X amount of the given enemy prefab, adding it to the ActiveEntities list, and the TargetEntities list if its required to be killed
    private void SpawnEnemies(int Amount, GameObject Prefab, bool MustBeKilled)
    {
        //Loop to spawn the required amount of enemies
        for(int i = 0; i < Amount; i++)
        {
            //Get a random position to spawn the enemy
            Vector3 SpawnPosition = GetNewSpawnPosition(0);
            //Spawn the enemy in
            GameObject EnemySpawn = Instantiate(Prefab, SpawnPosition, Quaternion.identity);
            //Add it to the active entity list
            ActiveEntities.Add(EnemySpawn.GetComponent<BaseEntity>());
            //Add it to the target entities list if required
            if (MustBeKilled)
                TargetEntities.Add(EnemySpawn.GetComponent<HostileEntity>());
        }
    }

    //Spawns in X amount of the given human survivor prefab
    private void SpawnHumans(int Amount, GameObject Prefab)
    {
        //Loop to spawn the required amount
        for(int i = 0; i < Amount; i++)
        {
            //Get a random position to spawn the human
            Vector3 SpawnPosition = GetNewSpawnPosition(0);
            //Spawn the human in
            GameObject HumanSpawn = Instantiate(Prefab, SpawnPosition, Quaternion.identity);
            //Add it to the active entities list
            ActiveEntities.Add(HumanSpawn.GetComponent<BaseEntity>());
        }
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
    public void RemoveHumanSurvivor(BaseEntity Human)
    {
        ActiveEntities.Remove(Human);
    }
}
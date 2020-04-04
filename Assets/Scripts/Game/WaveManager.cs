// ================================================================================================================================
// File:        WaveManager.cs
// Description:	Handles the spawning in and removal of enemies at the beginning and end of waves, tracks what is active in current wave
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//Used to track progress through the different stages required to set up a new round and spawn in everything correctly
public enum WaveStartProgress
{
    SpawnHumans = 1,    //Spawning in all the human survivors which must be rescued
    SpawnEnemies = 2,   //Spawning in all the enemies which must be killed
    WarmingUp = 3   //Once everything is spawned, theres a short pause before the round begins to give the player a chance to plan their strategy
}

public class WaveManager : MonoBehaviour
{
    //Singleton Instance
    public static WaveManager Instance;
    private void Awake() { Instance = this; }

    //User Interface
    public Text WaveDisplay; //Used to display the current wave number to the user during gameplay

    //Wave Override
    public bool WaveStartOverride = false;  //If enabled through the instead the game will skip straight to the specified wave number
    public int CustomWaveStart = 1; //Which wave to skip to when the game begins

    //Entities
    private List<BaseEntity> ActiveEntities = new List<BaseEntity>();   //A list of every entity currently active in the game
    private List<HostileEntity> TargetEntities = new List<HostileEntity>(); //A list of every entity currently active which is a required target that must be killed to complete the round

    //Wave Setup
    public bool SpawnPeriodActive = false; //Set as true when preparations are done and its time to start spawning everything in
    private WaveStartProgress WavePrepStage = WaveStartProgress.SpawnHumans;    //Tracks progress through the stages of setting up the wave
    private float SpawnPeriodDuration = 0.75f;  //How long each group of entities is given to spawn into the game
    private float WarmUpPeriod = 0.5f;  //Time between when the final enemy is spawned and the round begins
    private float WarmUpLeft = 0.5f;    //How long until the warmup period is over and the round can begin

    //Spawn Restrictions
    private Vector2 XBounds = new Vector2(-7f, 7f); //Range of X position values that lie inside the level bounds
    private Vector2 YBounds = new Vector2(-4f, 4f); //Range of Y position values that lie inside the level bounds
    private float MinPlayerDistance = 1.5f;    //How close entities are allowed to spawn to the player character
    private float MinEntityDistance = 0.15f;    //How close entities are allowed to spawn to one another

    //Spawning
    private List<GameObject> HumansToSpawn = new List<GameObject>();    //List of humans which still need to be spawned into the game
    private float HumanSpawnInterval;   //How often humans will be spawned during the friendly spawning stage
    private float NextHumanSpawn;   //How long until the next human survivor is spawned in
    private List<GameObject> EnemiesToSpawn = new List<GameObject>();   //List of enemies which still need to be spawned into the game
    private float EnemySpawnInterval;   //How often enemies will be spawned during the hostile spawning stage
    private float NextEnemySpawn;   //How long until the next enemy is spawned in

    //Spawns all the enemies in which belong to the given wave number
    public void StartWave(int WaveNumber)
    {
        //Overwrite the WaveNumber argument with the CustomWaveStart if WaveStartOverride has been enabled through the inspector
        if (WaveStartOverride)
        {
            WaveNumber = CustomWaveStart;
            WaveStartOverride = false;
        }

        //Display the new wave number to the user
        WaveDisplay.text = "Round " + WaveNumber.ToString();

        //Reset the rescue score multiplier
        GameState.Instance.RescueMultiplier = 1;

        //Get the list of enemies and humans to spawn in on this wave
        WaveEntities WaveInfo = WaveData.Instance.GetWaveData(WaveNumber);

        //Fill up the two lists with all the humans and enemies which will need to be spawned in to start this wave
        HumansToSpawn = new List<GameObject>();
        AddHumansToSpawn("Mummy", WaveInfo.Mommies);
        AddHumansToSpawn("Daddy", WaveInfo.Daddies);
        AddHumansToSpawn("Mikey", WaveInfo.Mikeys);
        EnemiesToSpawn = new List<GameObject>();
        AddEnemiesToSpawn("Grunt", WaveInfo.Grunts);
        for (int i = 0; i < WaveInfo.Electrodes; i++)
            EnemiesToSpawn.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        AddEnemiesToSpawn("Hulk", WaveInfo.Hulks);
        AddEnemiesToSpawn("Brain", WaveInfo.Brains);
        AddEnemiesToSpawn("Spheroid", WaveInfo.Spheroids);
        AddEnemiesToSpawn("Quark", WaveInfo.Quarks);

        //Figure out how often to spawn each human and enemy to space them out evenly over the spawning period
        HumanSpawnInterval = SpawnPeriodDuration / HumansToSpawn.Count;
        NextHumanSpawn = HumanSpawnInterval;
        EnemySpawnInterval = SpawnPeriodDuration / EnemiesToSpawn.Count;
        NextEnemySpawn = EnemySpawnInterval;

        //Now the round is ready to begin, first the humans will be spawned in
        SpawnPeriodActive = true;
        WavePrepStage = WaveStartProgress.SpawnHumans;
    }

    //Restarts the current wave with mostly the same amount of enemies that were remaining when the player died
    public void RestartWave()
    {
        //Clean up the players projectiles and reset their position and score multiplier
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();
        GameState.Instance.RescueMultiplier = 1;

        //Get a tally of all the humans and enemies which need to be respawned to restart the wave
        WaveEntities RestartingEntities = GetRestartEntities();

        //Destroy all the remaining entities and reset the tracking lists
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        ActiveEntities.Clear();
        TargetEntities.Clear();

        //Find any destroy any left over enemy projectiles
        foreach (GameObject EnemyProjectile in GameObject.FindGameObjectsWithTag("EnemyProjectile"))
            Destroy(EnemyProjectile);

        //Reset and fill up the spawning lists with the humans and enemies which will persist ones the round begins over again
        HumansToSpawn = new List<GameObject>();
        AddHumansToSpawn("Mummy", RestartingEntities.Mommies);
        AddHumansToSpawn("Daddy", RestartingEntities.Daddies);
        AddHumansToSpawn("Mikey", RestartingEntities.Mikeys);
        EnemiesToSpawn = new List<GameObject>();
        AddEnemiesToSpawn("Grunt", RestartingEntities.Grunts);
        for (int i = 0; i < RestartingEntities.Electrodes; i++)
            EnemiesToSpawn.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        AddEnemiesToSpawn("Hulk", RestartingEntities.Hulks);
        AddEnemiesToSpawn("Brain", RestartingEntities.Brains);
        AddEnemiesToSpawn("Spheroid", RestartingEntities.Spheroids);
        AddEnemiesToSpawn("Quark", RestartingEntities.Quarks);
        AddEnemiesToSpawn("Tank", RestartingEntities.Tanks);

        //Reconfigure the timers for spawning in the entities during their spawn periods
        HumanSpawnInterval = SpawnPeriodDuration / HumansToSpawn.Count;
        NextHumanSpawn = HumanSpawnInterval;
        EnemySpawnInterval = SpawnPeriodDuration / EnemiesToSpawn.Count;
        NextEnemySpawn = EnemySpawnInterval;

        //Now the round is ready to begin over again
        SpawnPeriodActive = true;
        WavePrepStage = WaveStartProgress.SpawnHumans;
    }

    //Returns a new WaveEntities object which lists the number and type of all humans and enemies which should be respawned to restart the current wave
    private WaveEntities GetRestartEntities()
    {
        //Create a new object to store the numbers of everything
        WaveEntities Entities = new WaveEntities();

        //Loop through all entities that currently exist
        foreach(BaseEntity Entity in ActiveEntities)
        {
            //Search for the entity types which should be respawned on a wave restart and count them all in the WaveEntities object we created
            switch(Entity.Type)
            {
                //Human Types
                case (EntityType.Mummy):
                    Entities.Mommies++;
                    break;
                case (EntityType.Daddy):
                    Entities.Daddies++;
                    break;
                case (EntityType.Mikey):
                    Entities.Mikeys++;
                    break;
                //Enemy Types
                case (EntityType.Grunt):
                    Entities.Grunts++;
                    break;
                case (EntityType.Electrode):
                    Entities.Electrodes++;
                    break;
                case (EntityType.Hulk):
                    Entities.Hulks++;
                    break;
                case (EntityType.Brain):
                    Entities.Brains++;
                    break;
                case (EntityType.Spheroid):
                    Entities.Spheroids++;
                    break;
                case (EntityType.Quark):
                    Entities.Quarks++;
                    break;
                case (EntityType.Tank):
                    Entities.Tanks++;
                    break;
            }
        }

        return Entities;
    }

    //Adds a number of human prefabs to the list of humans to be spawned in
    private void AddHumansToSpawn(string PrefabName, int Amount)
    {
        for (int i = 0; i < Amount; i++)
            HumansToSpawn.Add(PrefabSpawner.Instance.GetPrefab(PrefabName));
    }
    //Adds a number of enemy prefabs to the list of enemies to be spawned in
    private void AddEnemiesToSpawn(string PrefabName, int Amount)
    {
        for (int i = 0; i < Amount; i++)
            EnemiesToSpawn.Add(PrefabSpawner.Instance.GetPrefab(PrefabName));
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (GameState.Instance.IsGamePaused())
            return;

        //During the spawning period, keep spawning in everything until the round is ready to begin
        if (SpawnPeriodActive)
            ContinueSpawningProcess();
    }

    //Progresses through the stages of getting everything spawned in to start the new round
    private void ContinueSpawningProcess()
    {
        //Break out into the current stage of preparation
        switch(WavePrepStage)
        {
            //Humans are spawned in during the first stage
            case (WaveStartProgress.SpawnHumans):
                SpawnHumans();
                break;
            //Enemies are spawned in during the second stage
            case (WaveStartProgress.SpawnEnemies):
                SpawnEnemies();
                break;
            //Small pause after everything has spawned before the round starts
            case (WaveStartProgress.WarmingUp):
                WaitForRoundStart();
                break;
        }
    }

    //Spawns in humans during their stage of wave preparation
    private void SpawnHumans()
    {
        //Wait for the spawn timer
        NextHumanSpawn -= Time.deltaTime;
        if(NextHumanSpawn <= 0.0f)
        {
            //Reset the timer
            NextHumanSpawn = HumanSpawnInterval;
            //Spawn a new human in a random location
            Vector3 SpawnPos = GetSpawnPos();
            GameObject NewSpawn = Instantiate(HumansToSpawn[0], SpawnPos, Quaternion.identity);
            //Remove them from the HumansToSpawn list and add them to the list of active entities
            HumansToSpawn.RemoveAt(0);
            ActiveEntities.Add(NewSpawn.GetComponent<BaseEntity>());
            //If all humans have now been spawned move on to the enemy spawning stage
            if (HumansToSpawn.Count <= 0)
                WavePrepStage = WaveStartProgress.SpawnEnemies;
        }
    }

    //Spawns in enemies during their stage of wave preparation
    private void SpawnEnemies()
    {
        //Wait for the spawn timer
        NextEnemySpawn -= Time.deltaTime;
        if(NextEnemySpawn <= 0.0f)
        {
            //Reset the timer
            NextEnemySpawn = EnemySpawnInterval;
            //Spawn the next enemy in a random location
            Vector3 SpawnPos = GetSpawnPos();
            GameObject NewSpawn = Instantiate(EnemiesToSpawn[0], SpawnPos, Quaternion.identity);
            //Remove them from the EnemiesToSpawn list and add them to both the list of active entities
            EnemiesToSpawn.RemoveAt(0);
            ActiveEntities.Add(NewSpawn.GetComponent<BaseEntity>());
            //Also have them added onto the list of targets if this is an enemy type that must be killed to complete the round
            if (IsEnemyRequired(NewSpawn))
                TargetEntities.Add(NewSpawn.GetComponent<HostileEntity>());
            //If all enemies have been spawned in, move onto the final waiting period before the round begins
            if(EnemiesToSpawn.Count <= 0)
            {
                WavePrepStage = WaveStartProgress.WarmingUp;
                WarmUpLeft = WarmUpPeriod;
            }
        }
    }

    //Starts the round once the warmup period expires
    private void WaitForRoundStart()
    {
        WarmUpLeft -= Time.deltaTime;
        if (WarmUpLeft <= 0.0f)
            SpawnPeriodActive = false;
    }

    //Checks if the given enemy is a target that must be killed for the round to be completed
    private bool IsEnemyRequired(GameObject Enemy)
    {
        //Get the enemies type
        EntityType EnemyType = Enemy.GetComponent<HostileEntity>().Type;

        //Brains, Progs, Enforcers, Grunts, Quarks, Spheroids and Tanks are all the required enemies that must be killed for the round to progress
        if (EnemyType == EntityType.Brain || EnemyType == EntityType.DaddyProg || EnemyType == EntityType.MummyProg || EnemyType == EntityType.MikeyProg || EnemyType == EntityType.Enforcer || EnemyType == EntityType.Grunt
             || EnemyType == EntityType.Quark || EnemyType == EntityType.Spheroid || EnemyType == EntityType.Tank)
            return true;

        //All others are optional
        return false;
    }

    //Cleans up anything remaining in the current wave, allowing everything to be replaced and start over again
    private void CleanWave()
    {
        //Destroy any remaining entities
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);

        //Empty the tracking lists
        ActiveEntities.Clear();
        TargetEntities.Clear();

        //Cleanup any old player projectiles
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();

        //Reposition the player character
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();
    }

    //Randomly selects a location in the level which will be used to spawn in humans and enemies during the round setup period
    private Vector3 GetSpawnPos()
    {
        //Get a random pos inside the level
        Vector3 SpawnPos = GetRandomPos();

        //Return this position straight away if its already clear of everything
        if (PosAwayFromPlayer(SpawnPos) && PosAwayFromEntities(SpawnPos))
            return SpawnPos;

        //Otherwise, make a maximum of 10 attempts trying to find a spawn pos free of anyway
        int FreePosAttempts = 1;
        while(FreePosAttempts < 10)
        {
            //Get a new position
            SpawnPos = GetRandomPos();
            //Return this if its a proper position
            if (PosAwayFromPlayer(SpawnPos) && PosAwayFromEntities(SpawnPos))
                return SpawnPos;
            //Increment attempt counter
            FreePosAttempts++;
        }

        //After 10 failed tries just find a position away from the player and that will have to do
        return GetPosAwayFromPlayer();
    }

    //Returns a random position inside the level bounds
    private Vector3 GetRandomPos()
    {
        return new Vector3(Random.Range(XBounds.x, XBounds.y), Random.Range(YBounds.x, YBounds.y), 0f);
    }

    //Returns a random position which isnt too close to the player character
    private Vector3 GetPosAwayFromPlayer()
    {
        Vector3 RandomPos = GetRandomPos();
        if (PosAwayFromPlayer(RandomPos))
            return RandomPos;
        return GetPosAwayFromPlayer();
    }

    //Checks if a spawn position is too close to the player charcter
    private bool PosAwayFromPlayer(Vector3 SpawnPos)
    {
        float PlayerDistance = Vector3.Distance(SpawnPos, GameState.Instance.Player.transform.position);
        return PlayerDistance > MinPlayerDistance;
    }

    //Checks if a spawn position is too close to any other entity
    private bool PosAwayFromEntities(Vector3 SpawnPos)
    {
        foreach(BaseEntity Entity in ActiveEntities)
        {
            float EntityDistance = Vector3.Distance(SpawnPos, Entity.transform.position);
            if (EntityDistance <= MinEntityDistance)
                return false;
        }

        return true;
    }

    //Whenever friendly or hostile entities are killed, they alert the WaveManager through this function
    public void EnemyDead(HostileEntity Enemy)
    {
        //Totally ignore this if wave progression has been disabled
        if (GameState.Instance.DisableWaveProgression)
            return;

        //Remove the entity from the ActiveEntities list
        ActiveEntities.Remove(Enemy);

        //Remove them from the TargetEntities list if they're a required target
        bool IsTarget = IsEnemyRequired(Enemy.gameObject);
        if (IsTarget)
            TargetEntities.Remove(Enemy);

        //Whenever a target enemy is destroyed, check to see if they were the last one left
        if (IsTarget && TargetEntities.Count == 0)
        {
            //Progress onto the next round now since all the target enemies have been destroyed
            CleanWave();
            GameState.Instance.CurrentWave++;
            StartWave(GameState.Instance.CurrentWave);
            SoundEffectsPlayer.Instance.PlaySound("RoundComplete");
            Instantiate(PrefabSpawner.Instance.GetPrefab("RoundCompleteAnimation"), Vector3.zero, Quaternion.identity);
        }
    }
    public void HumanDead(BaseEntity Human, bool KilledByEnemy = false)
    {
        //Remove them from the ActiveEntities list
        ActiveEntities.Remove(Human);

        //Spawn a skull underneath the human if they died as a result of conflict with an enemy
        if (KilledByEnemy)
            PrefabSpawner.Instance.SpawnPrefab("Skull", Human.transform.position, Quaternion.identity);
    }

    //For adding newly created Tanks and Enforcers to the tracking lists when they are spawned into the game by the Spheroids and Quarks
    public void AddNewEnemy(HostileEntity NewEnemy)
    {
        //Add them to both tracking lists
        ActiveEntities.Add(NewEnemy);
        TargetEntities.Add(NewEnemy);
    }
}
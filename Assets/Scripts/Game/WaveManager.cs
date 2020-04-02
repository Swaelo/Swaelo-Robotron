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
    private float MinPlayerDistance = 1.75f;    //How close entities are allowed to spawn to the player character
    private float MinEntityDistance = 0.65f;    //How close entities are allowed to spawn to one another

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
            WaveNumber = CustomWaveStart;

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

    //Restarts the current wave with mostly the same amount of enemies that were remaining when the player died
    public void RestartWave()
    {
        //Clean up the players projectiles and reset their position and score multiplier
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();
        GameState.Instance.RescueMultiplier = 1;

        //Tally up the number of remaining humans, and the number of each enemy type which is will remain when the round starts over again
        WaveEntities RestartingEntities = new WaveEntities();
        foreach (BaseEntity Entity in ActiveEntities)
        {
            //Tally up any remaining human survivors
            if (Entity.Type == EntityType.Mummy || Entity.Type == EntityType.Daddy || Entity.Type == EntityType.Mikey)
                TallyHumanSurvivor(RestartingEntities, Entity);
            //Tally up enemies only including Grunts, Electrodes, Hulks, Brains, Spheroids, Quarks and Tanks; Progs and Enforcers do not respawn when the round starts over again
            if (Entity.Type == EntityType.Grunt || Entity.Type == EntityType.Electrode || Entity.Type == EntityType.Hulk || Entity.Type == EntityType.Brain || Entity.Type == EntityType.Spheroid || Entity.Type == EntityType.Quark || Entity.Type == EntityType.Tank)
                TallyPersistantEnemy(RestartingEntities, Entity);

            //Instruct any Brains, Enforcers and Tanks to clean up any remaining projectiles that belong to them
            if (Entity.Type == EntityType.Brain)
                Entity.GetComponent<BrainAI>().CleanMissiles();
            else if (Entity.Type == EntityType.Enforcer)
                Entity.GetComponent<EnforcerAI>().CleanProjectiles();
            else if (Entity.Type == EntityType.Tank)
                Entity.GetComponent<TankAI>().CleanProjectiles();

            //Instruct any Progs to clean up their trail sprites that follow behind
            if (Entity.Type == EntityType.MummyProg || Entity.Type == EntityType.DaddyProg || Entity.Type == EntityType.MikeyProg)
                Entity.GetComponent<ProgAI>().DestroyTrailSprites();
        }

        //Clean up all the entities which were still active when the player died, and reset the tracking lists
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        ActiveEntities.Clear();
        TargetEntities.Clear();

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
        AddEnemiesToSpawn("Tank", RestartingEntities.Tank);

        //Reconfigure the timers for spawning in the entities during their spawn periods
        HumanSpawnInterval = SpawnPeriodDuration / HumansToSpawn.Count;
        NextHumanSpawn = HumanSpawnInterval;
        EnemySpawnInterval = SpawnPeriodDuration / EnemiesToSpawn.Count;
        NextEnemySpawn = EnemySpawnInterval;

        //Now the round is ready to begin over again
        SpawnPeriodActive = true;
        WavePrepStage = WaveStartProgress.SpawnHumans;
    }

    //Adds one of the human survivors to the tally
    private void TallyHumanSurvivor(WaveEntities TallyBoard, BaseEntity HumanEntity)
    {
        switch(HumanEntity.Type)
        {
            case (EntityType.Mummy):
                TallyBoard.Mommies++;
                break;
            case (EntityType.Daddy):
                TallyBoard.Daddies++;
                break;
            case (EntityType.Mikey):
                TallyBoard.Mikeys++;
                break;
        }
    }

    //Adds one of the enemies to the tally
    private void TallyPersistantEnemy(WaveEntities TallyBoard, BaseEntity HostileEntity)
    {
        switch(HostileEntity.Type)
        {
            case (EntityType.Grunt):
                TallyBoard.Grunts++;
                break;
            case (EntityType.Electrode):
                TallyBoard.Electrodes++;
                break;
            case (EntityType.Hulk):
                TallyBoard.Hulks++;
                break;
            case (EntityType.Brain):
                TallyBoard.Brains++;
                break;
            case (EntityType.Spheroid):
                TallyBoard.Spheroids++;
                break;
            case (EntityType.Quark):
                TallyBoard.Quarks++;
                break;
            case (EntityType.Tank):
                TallyBoard.Tank++;
                break;
        }
    }

    private void Update()
    {
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

        //Grunts, Brains, Spheroids, Quarks, Enforcers and Tanks are all required targets to finish the round
        if (EnemyType == EntityType.Grunt || EnemyType == EntityType.Brain || EnemyType == EntityType.Spheroid || EnemyType == EntityType.Quark || EnemyType == EntityType.Enforcer || EnemyType == EntityType.Tank)
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

        //Return this position if its clear of anything else
        if (SpawnPosFree(SpawnPos))
            return SpawnPos;

        //If the first random pos wasnt free, try up to 10 times to find one
        int FreePosAttempts = 1;
        while(FreePosAttempts < 10)
        {
            SpawnPos = GetRandomPos();
            if (SpawnPosFree(SpawnPos))
                return SpawnPos;
            FreePosAttempts++;
        }

        //If we tried 10 times and failed, just return whatever was the last position that we generated and it will have to do
        return SpawnPos;
    }

    //Returns a random position inside the level bounds
    private Vector3 GetRandomPos()
    {
        return new Vector3(Random.Range(XBounds.x, XBounds.y), Random.Range(YBounds.x, YBounds.y), 0f);
    }

    //Checks if a position is too close to the player character or any of the entities which are already active
    private bool SpawnPosFree(Vector3 SpawnPos)
    {
        //Check distance from the player character
        float PlayerDistance = Vector3.Distance(SpawnPos, GameState.Instance.Player.transform.position);
        if (PlayerDistance <= MinPlayerDistance)
            return false;

        //Check distance from any active entities
        foreach(BaseEntity Entity in ActiveEntities)
        {
            float EntityDistance = Vector3.Distance(SpawnPos, Entity.transform.position);
            if (EntityDistance <= MinEntityDistance)
                return false;
        }

        //All checks have passed, this position is free
        return true;
    }

    //Whenever friendly or hostile entities are killed, they alert the WaveManager through this function
    public void EnemyDead(HostileEntity Enemy)
    {
        //Remove the entity from the ActiveEntities list
        ActiveEntities.Remove(Enemy);

        //Remove them from the TargetEntities list if they're a required target
        bool IsTarget = IsEnemyRequired(Enemy.gameObject);
        if (IsTarget)
            TargetEntities.Remove(Enemy);

        //Whenever a target enemy is destroyed, check to see if they were the last one left
        if(IsTarget && TargetEntities.Count == 0)
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

    //Adds a newly created enemy to the list of required targets, called by Quarks when they spawn tanks into the game
    public void AddNewTank(HostileEntity Tank)
    {
        //Add them to both entity lists
        ActiveEntities.Add(Tank);
        TargetEntities.Add(Tank);
    }
}
// ================================================================================================================================
// File:        WaveManager.cs
// Description:	Handles the spawning in and removal of enemies at the beginning and end of waves, tracks what is active in current wave
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    //Singleton Instance
    public static WaveManager Instance;
    private void Awake() { Instance = this; }

    //User Interface
    public Text WaveDisplay; //Used to display the current wave number to the user during gameplay
    private int CustomNumberOffset = 0;

    //Wave Override
    public bool WaveStartOverride = false;  //If enabled through inspector the game will instead skip straight to the specified wave number
    public int CustomWaveStart = 1; //Which wave to skip to when the game begins
    public bool PlayCustomWave = false; //If set in the inspector, a wave will begin with the enemies set in the CustomWaveData script and loop that, for testing

    //Entities
    private List<BaseEntity> ActiveEntities = new List<BaseEntity>();   //A list of every entity currently active in the game
    private List<HostileEntity> TargetEntities = new List<HostileEntity>(); //A list of every entity currently active which is a required target that must be killed to complete the round

    //Wave Setup
    public bool SpawnPeriodActive = false; //Set as true when preparations are done and its time to start spawning everything in
    private float SpawnPeriodDuration = 1.5f;  //How long each group of entities is given to spawn into the game
    public bool WarmingUp = false; //Starts once all entities for the new wave have been spawned in
    private float WarmUpPeriod = 0.5f;  //Time between when the final enemy is spawned and the round begins
    private float WarmUpLeft = 0.5f;    //How long until the warmup period is over and the round can begin

    //Spawning
    private List<GameObject> EntitiesToSpawn = new List<GameObject>();  //List of entites which need to be spawned into the game
    private float EntitySpawnInterval;  //How often to spawn each entity
    private int EntitySpawnCount = 0; //Number of entities spawned so far this wave
    private float NextEntitySpawn;  //How long until the next entity should be spawned
    private Vector2 SpawnRange = new Vector2(2f, 8.5f); //Distance range that enemies can be spawned from the middle x is min y is max

    public float MinPlayerSpawnDistance = 3.5f; //How close to the player new enemies can be spawned into the level
    public float MinEnemySpawnDistance = 1.5f; //How close together enemies can be spawned from one another
    private List<Vector2> EnemySpawnLocations = new List<Vector2>(); //List of enemy spawn locations to be used when starting a new wave

    //Grabs the current border level size from the level border manager

    //Spawns all the enemies in which belong to the given wave number
    public void StartWave(int WaveNumber)
    {
        EntitySpawnCount = 0;

        //Overwrite the WaveNumber argument with the CustomWaveStart if WaveStartOverride has been enabled through the inspector
        if (WaveStartOverride)
        {
            WaveNumber = CustomWaveStart;
            WaveStartOverride = false;
        }

        //After the player reaches wave 40 it loops back to wave 21 enemies
        if(WaveNumber == 40)
        {
            CustomNumberOffset += 20;
            WaveNumber = 21;
        }

        //Display the new wave number to the user
        WaveDisplay.text = "Round " + (WaveNumber + CustomNumberOffset).ToString();

        //Reset the rescue score multiplier
        GameState.Instance.RescueMultiplier = 1;

        if(PlayCustomWave)
        {
            StartCustomWave();
            return;
        }

        //Get the list of enemies and humans to spawn in on this wave
        WaveEntities WaveInfo = WaveData.Instance.GetWaveData(WaveNumber);

        //Fill up the list with all the entities which need to be spawned in to start this wave
        EntitiesToSpawn = new List<GameObject>();
        AddEntitiesToSpawnList("Mummy", WaveInfo.Mommies);
        AddEntitiesToSpawnList("Daddy", WaveInfo.Daddies);
        AddEntitiesToSpawnList("Mikey", WaveInfo.Mikeys);
        AddEntitiesToSpawnList("Grunt", WaveInfo.Grunts);
        for (int i = 0; i < WaveInfo.Electrodes; i++)
            EntitiesToSpawn.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        AddEntitiesToSpawnList("Hulk", WaveInfo.Hulks);
        AddEntitiesToSpawnList("Brain", WaveInfo.Brains);
        AddEntitiesToSpawnList("Spheroid", WaveInfo.Spheroids);
        AddEntitiesToSpawnList("Quark", WaveInfo.Quarks);

        //Now all entities are in the list it needs to be shuffled
        EntitiesToSpawn = ShuffleList(EntitiesToSpawn);

        //Grab a location of where to spawn each new enemy at
        EnemySpawnLocations = GetSpawnLocations(EntitiesToSpawn.Count);

        //Figure out how often to spawn each entity
        EntitySpawnInterval = SpawnPeriodDuration / EntitiesToSpawn.Count;

        //Now the round is ready to begin, first the humans will be spawned in
        SpawnPeriodActive = true;
    }

    private void StartCustomWave()
    {
        //Start a new list of enemies to spawn in
        CustomWaveData WaveData = GetComponent<CustomWaveData>();
        EntitiesToSpawn = new List<GameObject>();

        //Get the count of each type from the custom wave data
        AddEntitiesToSpawnList("Grunt", WaveData.Grunts);
        AddEntitiesToSpawnList("Mummy", WaveData.Mommies);
        AddEntitiesToSpawnList("Daddy", WaveData.Daddies);
        AddEntitiesToSpawnList("Mikey", WaveData.Mikeys);
        AddEntitiesToSpawnList("Hulk", WaveData.Hulks);
        AddEntitiesToSpawnList("Brain", WaveData.Brains);
        AddEntitiesToSpawnList("Spheroid", WaveData.Spheroids);
        AddEntitiesToSpawnList("Quark", WaveData.Quarks);
        AddEntitiesToSpawnList("Enforcer", WaveData.Enforcers);
        AddEntitiesToSpawnList("Tank", WaveData.Tanks);
        AddEntitiesToSpawnList("DaddyProg", WaveData.DaddyProgs);
        AddEntitiesToSpawnList("MummyProg", WaveData.MummyProgs);
        AddEntitiesToSpawnList("MikeyProg", WaveData.MikeyProgs);
        for (int i = 0; i < WaveData.Electrodes; i++)
            EntitiesToSpawn.Add(PrefabSpawner.Instance.GetElectrodePrefab());

        //Shuffle the list, get spawn locations and prepare them for spawning
        EntitiesToSpawn = ShuffleList(EntitiesToSpawn);
        EnemySpawnLocations = GetSpawnLocations(EntitiesToSpawn.Count);
        EntitySpawnInterval = SpawnPeriodDuration / EntitiesToSpawn.Count;
        SpawnPeriodActive = true;
    }

    //Fisher Yates CardDeck Shuffle from pKallv answer on this thread https://answers.unity.com/questions/486626/how-can-i-shuffle-alist.html
    public static List<GameObject> ShuffleList(List<GameObject> List)
    {
        System.Random Random = new System.Random();
        GameObject ShufflingEntity;
        int Count = List.Count;
        for(int i = 0; i < Count; i++)
        {
            int Selection = i + (int)(Random.NextDouble() * (Count - i));
            ShufflingEntity = List[Selection];
            List[Selection] = List[i];
            List[i] = ShufflingEntity;
        }
        return List;
    }

    //Cleans up anything remaining in the current wave, allowing everything to be replaced and start over again
    private void CleanWave()
    {
        //Clean up players projectiles, reset their position and score multiplier
        GameState.Instance.Player.GetComponent<PlayerShooting>().CleanProjectiles();
        GameState.Instance.Player.GetComponent<PlayerMovement>().ResetPosition();
        GameState.Instance.RescueMultiplier = 1;

        //Destroy any remaining entities and reset the tracking lists
        foreach (BaseEntity Entity in ActiveEntities)
            Destroy(Entity.gameObject);
        ActiveEntities.Clear();
        TargetEntities.Clear();

        //Clean up any remaining enemy projectiles
        foreach (GameObject EnemyProjectile in GameObject.FindGameObjectsWithTag("EnemyProjectile"))
            Destroy(EnemyProjectile);
    }

    //Restarts the current wave with mostly the same amount of enemies that were remaining when the player died
    public void RestartWave()
    {
        EntitySpawnCount = 0;

        //Get a tally of all the entities which need to be respawned to restart the wave properly
        WaveEntities RestartingEntities = GetRestartEntities();

        //Clean up everything left over from the previous wave
        CleanWave();

        //Reset and fill up the spawning lists with the humans and enemies which will persist ones the round begins over again
        EntitiesToSpawn = new List<GameObject>();
        AddEntitiesToSpawnList("Mummy", RestartingEntities.Mommies);
        AddEntitiesToSpawnList("Daddy", RestartingEntities.Daddies);
        AddEntitiesToSpawnList("Mikey", RestartingEntities.Mikeys);
        AddEntitiesToSpawnList("Grunt", RestartingEntities.Grunts);
        for (int i = 0; i < RestartingEntities.Electrodes; i++)
            EntitiesToSpawn.Add(PrefabSpawner.Instance.GetElectrodePrefab());
        AddEntitiesToSpawnList("Hulk", RestartingEntities.Hulks);
        AddEntitiesToSpawnList("Brain", RestartingEntities.Brains);
        AddEntitiesToSpawnList("Spheroid", RestartingEntities.Spheroids);
        AddEntitiesToSpawnList("Quark", RestartingEntities.Quarks);
        AddEntitiesToSpawnList("Tank", RestartingEntities.Tanks);

        //Now shuffle the list of entities needing to be spawned in
        EntitiesToSpawn = ShuffleList(EntitiesToSpawn);

        //Grab a location of where to spawn each new enemy at
        EnemySpawnLocations = GetSpawnLocations(EntitiesToSpawn.Count);

        //Figure out how often to spawn each enemy, and how far to rotate the spawn direction after each one
        EntitySpawnInterval = SpawnPeriodDuration / EntitiesToSpawn.Count;

        //Now the round is ready to begin
        SpawnPeriodActive = true;
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

    //Adds a number of prefabs to the list of entities to be spawned in
    private void AddEntitiesToSpawnList(string PrefabName, int Amount)
    {
        for (int i = 0; i < Amount; i++)
            EntitiesToSpawn.Add(PrefabSpawner.Instance.GetPrefab(PrefabName));
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (GameState.Instance.IsGamePaused())
            return;

        //During the spawning period, keep spawning in everything until the round is ready to begin
        if (SpawnPeriodActive)
            ContinueSpawningProcess();
        else if (WarmingUp)
            WaitForRoundStart();
    }

    //Progresses through the stages of getting everything spawned in to start the new round
    private void ContinueSpawningProcess()
    {
        //Wait for the spawn timer
        NextEntitySpawn -= Time.deltaTime;
        if(NextEntitySpawn <= 0.0f)
        {
            //Reset the spawn timer
            NextEntitySpawn = EntitySpawnInterval;
            //Spawn the next entity in at a random location
            Vector3 SpawnPos = GetSpawnPos(EntitySpawnCount);
            EntitySpawnCount++;
            GameObject NewSpawn = Instantiate(EntitiesToSpawn[0], SpawnPos, Quaternion.identity);
            //Remove them from the list of what needs to be spawned, and add them to the tracking lists
            EntitiesToSpawn.RemoveAt(0);
            ActiveEntities.Add(NewSpawn.GetComponent<BaseEntity>());
            if (NewSpawn.GetComponent<HostileEntity>() != null && IsEnemyRequired(NewSpawn))
                TargetEntities.Add(NewSpawn.GetComponent<HostileEntity>());
            //If there are no more entities which need to be spawned in then we can move on to the warmup period
            if(EntitiesToSpawn.Count <= 0)
            {
                SpawnPeriodActive = false;
                WarmingUp = true;
                WarmUpLeft = WarmUpPeriod;
            }
        }
    }

    //Rotates the spawn direction vector, then gets the next spawn location from that
    private Vector3 GetSpawnPos(int SpawnIndex)
    {
        return EnemySpawnLocations[SpawnIndex];
    }

    //Returns a list of valid spawn locations to use for all the enemies to be placed into the level
    private List<Vector2> GetSpawnLocations(int LocationCount)
    {
        //Get the current level bounds we will constrain the enemies within
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;

        //Start the list and set a hard limit on attempts to prevent soft locking
        List<Vector2> SpawnPositions = new List<Vector2>();
        int SpawnAttempts = 0;
        int MaxSpawnAttempts = 5000;

        //Generate new spawning positions until enough positions have been found or attempts have run out
        while (SpawnPositions.Count < LocationCount && SpawnAttempts < MaxSpawnAttempts)
        {
            SpawnAttempts++;

            //Get a new random spawn position
            Vector2 SpawnPosCandidate = new Vector2(
                Random.Range(XBounds.x, XBounds.y),
                Random.Range(YBounds.x, YBounds.y));
            
            //Reject the new position if its too close to the center
            if(SpawnPosCandidate.magnitude < MinPlayerSpawnDistance)
                continue;
            
            //Check if this new position is too close to any other positions already selected
            bool TooClose = false;
            foreach (Vector2 SpawnPosition in SpawnPositions)
            {
                if(Vector2.Distance(SpawnPosCandidate, SpawnPosition) < MinEnemySpawnDistance)
                {
                    TooClose = true;
                    break;
                }
            }

            //Add the new position to the list if its found to be valid
            if(!TooClose)
            SpawnPositions.Add(SpawnPosCandidate);
        }

        //Now that we have the list of locations, we want to sort them by angle from the middle so everythings spawns in a ring / clock like fashion
        SpawnPositions.Sort((a, b) =>
        {
            float AngleA = Mathf.Atan2(a.y, a.x);
            float AngleB = Mathf.Atan2(b.y, b.x);
            return AngleB.CompareTo(AngleA);
        });

        return SpawnPositions;
    }

    //Starts the round once the warmup period expires
    private void WaitForRoundStart()
    {
        //Round cannot start if the navmesh isnt ready yet
        if(!NavMeshManager.Instance.NavMeshReady)
            return;

        WarmUpLeft -= Time.deltaTime;
        if (WarmUpLeft <= 0.0f)
            WarmingUp = false;
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

    //Returns a list of all the spheroid enemies, they need access to each other for their flocking behaviour
    public List<SpheroidAI> GetSpheroidList()
    {
        //Start a list to store all the spheroids
        List<SpheroidAI> SpheroidList = new List<SpheroidAI>();

        //Go through all the currently active hostile entities
        foreach(HostileEntity Hostile in TargetEntities)
        {
            if(Hostile.gameObject.tag == "Spheroid")
                SpheroidList.Add(Hostile.GetComponent<SpheroidAI>());
        }

        //Return the list of all other spheroids
        return SpheroidList;
    }
}
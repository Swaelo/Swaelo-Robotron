// ================================================================================================================================
// File:        WaveManager.cs
// Description:	Handles the spawning in and removal of enemies at the beginning and end of waves, tracks what is active in current wave
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class WaveManager : MonoBehaviour
{
    //Singleton Instance
    public static WaveManager Instance;
    private void Awake() { Instance = this; }

    //Entity tracking
    private List<BaseEntity> ActiveEntities = new List<BaseEntity>();   //A list of every entity currently active in the game
    private List<HostileEntity> TargetEntities = new List<HostileEntity>(); //A list of every entity currently active which is a required target that must be killed to complete the round

    //Gives the player a few seconds to breathe at the end of a wave
    private float WaveEndRestInterval = 1.5f; //How long the player gets to rest before the next wave begins
    private float WaveEndRestRemaining; //How long left until the wave end rest is over
    private bool WaveEndResting = false;    //Tracks when we are resting before starting the next wave

    //Custom wave start override
    public int CustomWave = -1;
    public bool PlayCustomWave = false;

    private void Start()
    {
        LevelBorders.Instance.InitLevelBorders();
    }

    private void Update()
    {
        if(WaveEndResting)
            WaveEndRest();
    }

    //Spawns all the enemies in for the new wave
    public void StartWave(int WaveNumber)
    {
        //Override wave number if custom flag is set
        if(PlayCustomWave)
            WaveNumber = CustomWave;

        //Pause the game for a short time
        GameState.Instance.PauseGame(1.5f);

        //Get the list of entities which need to be spawned in this wave
        WaveEntities WaveInfo = WaveData.Instance.GetWaveData(WaveNumber);

        //Create a list of the entities that need to be spawned in
        List<GameObject> WaveSpawns = new List<GameObject>();

        //Iterate all fields in the wave info to get everything that needs to be spawned in
        foreach(FieldInfo Field in typeof(WaveEntities).GetFields())
        {
            int Count = (int)Field.GetValue(WaveInfo);
            if (Count <= 0) continue;

            //Use the field name to find that prefab
            string PrefabName = Field.Name;

            //Iterate for the count of each type, adding in the required amount
            for(int i = 0; i < Count; i++)
            {
                //Electrodes are different as they have 4 random types that can be created
                if(PrefabName == "Electrode")
                    WaveSpawns.Add(PrefabSpawner.Instance.GetElectrodePrefab());
                //Otherwise we add whatever the entity is
                else
                    WaveSpawns.Add(PrefabSpawner.Instance.GetPrefab(PrefabName));
            }
        }

        //Shuffle the list of entities which are going to be spawned in
        WaveSpawns = ShuffleList(WaveSpawns);

        //Grab a random location to spawn each one
        List<Vector2> SpawnLocations = GetSpawnLocations(WaveSpawns.Count);

        //Get all the new entities added in
        for(int i = 0; i < WaveSpawns.Count; i++)
        {
            //Spawn the new entity in at its new location
            GameObject NewSpawn = Instantiate(WaveSpawns[i], SpawnLocations[i], Quaternion.identity);

            //Add it to the list of active entities, and the list of enemies if it is one
            ActiveEntities.Add(NewSpawn.GetComponent<BaseEntity>());
            if(NewSpawn.GetComponent<HostileEntity>() != null && IsEnemyRequired(NewSpawn))
                TargetEntities.Add(NewSpawn.GetComponent<HostileEntity>());
        }

        //Have any electrodes that were spawned in, mark their mesh nodes as unwalkable
        NavMeshManager.Instance.MarkElectrodeNodesUnwalkable();

        //Mark the level boundaries as unwalkable
        LevelBorders.Instance.MarkWallsUnwalkable();
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

        //Reset the mesh nodes to all be walkable
        NavMeshManager.Instance.SetAllNodesWalkable(true);
    }

    //Restarts the current wave with mostly the same amount of enemies that were remaining when the player died
    public void RestartWave()
    {
        //Pause the game for a moment
        GameState.Instance.PauseGame(1.5f);

        //Get a tally of all remaining entities and then clean them up
        WaveEntities WaveInfo = GetRestartEntities();
        CleanWave();

        //Create a list of entities to spawn back in
        List<GameObject> WaveSpawns = new List<GameObject>();

        //Iterate through all the field structs to add the number of each type into the list
        foreach(FieldInfo Field in typeof(WaveEntities).GetFields())
        {
            int Count = (int)Field.GetValue(WaveInfo);
            if (Count <= 0) continue;

            //Use the field name to find that prefab
            string PrefabName = Field.Name;

            //Iterate for the count of each type, adding in the required amount
            for(int i = 0; i < Count; i++)
            {
                //Electrodes are different as they have 4 random types that can be created
                if(PrefabName == "Electrode")
                    WaveSpawns.Add(PrefabSpawner.Instance.GetElectrodePrefab());
                //Otherwise we add whatever the entity is
                else
                    WaveSpawns.Add(PrefabSpawner.Instance.GetPrefab(PrefabName));
            }
        }

        //Shuffle the list of entities which are going to be spawned in
        WaveSpawns = ShuffleList(WaveSpawns);

        //Get their spawn locations
        List<Vector2> SpawnLocations = GetSpawnLocations(WaveSpawns.Count);

        //Spawn them all back in
        for(int i = 0; i < WaveSpawns.Count; i++)
        {
            //Spawn the new entity in at its new location
            GameObject NewSpawn = Instantiate(WaveSpawns[i], SpawnLocations[i], Quaternion.identity);

            //Add it to the list of active entities, and the list of enemies if it is one
            ActiveEntities.Add(NewSpawn.GetComponent<BaseEntity>());
            if(NewSpawn.GetComponent<HostileEntity>() != null && IsEnemyRequired(NewSpawn))
                TargetEntities.Add(NewSpawn.GetComponent<HostileEntity>());
        }

        //Mark mesh nodes under electrodes unwalkable
        NavMeshManager.Instance.MarkElectrodeNodesUnwalkable();

        //Mark the level boundaries as unwalkable
        LevelBorders.Instance.MarkWallsUnwalkable();
    }

    private WaveEntities GetRestartEntities()
    {
        //Create struct to count number of each entity being spawned back in
        WaveEntities RestartEntities = new WaveEntities();

        //Work on a boxed copy so FieldInfo.SetValue actually mutates it
        object Boxed = RestartEntities;
        System.Type Type = typeof(WaveEntities);

        //Cache all fields
        FieldInfo[] Fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        //Loop through each active entity left in the game field
        foreach(BaseEntity Entity in ActiveEntities)
        {
            //Get the name of each entity type
            string EntityName = Entity.Type.ToString();

            //Compare it against all entity types
            foreach(FieldInfo Field in Fields)
            {
                //Find the matching type
                if(Field.Name == EntityName)
                {
                    //Add 1 more of the matching entity type into the counting structure
                    int Current = (int)Field.GetValue(Boxed);
                    Field.SetValue(Boxed, Current + 1);
                    break;
                }
            }
        }

        //Unbox back into the structure
        RestartEntities = (WaveEntities)Boxed;

        //Send the final structure back
        return RestartEntities;
    }

    //Returns a list of random spawn locations to place down the entities onto
    private List<Vector2> GetSpawnLocations(int LocationCount)
    {
        //Need to constrain all locations inside the level borders 
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;

        //Get a list of spawn locations that can be used
        List<Vector2> CandidateLocations = Utils.GeneratePoints(0.5f, XBounds, YBounds);

        //Shuffle the list
        CandidateLocations = ShuffleList(CandidateLocations);

        //Generate a final list of locations we will use
        List<Vector2> FinalSpawnLocations = new List<Vector2>();
        float MinDistanceFromCenter = 1.5f; //How far valid positions must be from the middle
        float MinDistanceSqr = MinDistanceFromCenter * MinDistanceFromCenter;

        foreach(Vector2 Candidate in CandidateLocations)
        {
            //Skip points too close to the center
            if(Candidate.sqrMagnitude < MinDistanceSqr)
                continue;

            //Otherwise add it to the list
            FinalSpawnLocations.Add(Candidate);

            //Break out once we have enough
            if(FinalSpawnLocations.Count >= LocationCount)
                break;
        }

        //Return the final list of spawn locations
        return FinalSpawnLocations;
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
        //Remove the entity from the ActiveEntities list
        ActiveEntities.Remove(Enemy);

        //Remove them from the TargetEntities list if they're a required target
        bool IsTarget = IsEnemyRequired(Enemy.gameObject);
        if (IsTarget)
            TargetEntities.Remove(Enemy);

        //Whenever a target enemy is destroyed, check to see if they were the last one left
        if (IsTarget && TargetEntities.Count == 0)
        {
            //Allow the player time to rest before we start up the next round
            WaveEndResting = true;
            WaveEndRestRemaining = WaveEndRestInterval;
        }
    }

    //Allows the player time to breathe at the end of wave before the next wave begins
    private void WaveEndRest()
    {
        WaveEndRestRemaining -= Time.deltaTime;
        if(WaveEndRestRemaining <= 0f)
        {
            WaveEndResting = false;
            //Progress onto the next round now since all the target enemies have been destroyed
            CleanWave();
            GameState.Instance.CurrentWave++;
            //After passing round 40, loop back to round 21
            if(GameState.Instance.CurrentWave > 40)
                GameState.Instance.CurrentWave = 21;
            //Spawn in entities for the new wave
            StartWave(GameState.Instance.CurrentWave);
            //Play startup sound based on current wave type
            if(GameState.Instance.CurrentWave % 10 == 5)    //Brains are every 5th wave
                SoundEffectsPlayer.Instance.PlaySound("BrainWaveStart");
            else
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

    //Returns a list of all entities of a certain type
    public List<BaseEntity> GetEntityList(EntityType Type)
    {
        //Start a list to store them all
        List<BaseEntity> EntityList = new List<BaseEntity>();

        //Loop through all the currently active entities
        foreach(BaseEntity Entity in ActiveEntities)
        {
            //Add them to the list if they are the type we are looking for
            if(Entity.Type == Type)
                EntityList.Add(Entity);
        }

        //Return the final list
        return EntityList;
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

    public static List<Vector2> ShuffleList(List<Vector2> List)
    {
        System.Random Random = new System.Random();
        Vector2 ShufflingEntity;
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
}
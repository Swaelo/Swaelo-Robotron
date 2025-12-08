// ================================================================================================================================
// File:        WaveData.cs
// Description:	Details the count of enemies to spawn in each wave as the game progresses
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

[System.Serializable]
public struct WaveEntities
{
    //Counts how many of each type of enemy and human will spawn at the start of some round
    public int Grunt;
    public int Electrode;
    public int Mummy;
    public int Daddy;
    public int Mikey;
    public int Hulk;
    public int Brain;
    public int Spheroid;
    public int Quark;
    public int Enforcer;
    public int Tank;
    public int DaddyProg;
    public int MummyProg;
    public int MikeyProg;
}

public class WaveData : MonoBehaviour
{
    //Singleton Instance
    public static WaveData Instance;
    private void Awake() { Instance = this; }

    //List of the enemies that are spawned in each wave in the game
    public WaveEntities[] WaveEnemies;

    //Returns the EnemyCount data object for the given wave number
    public WaveEntities GetWaveData(int WaveNumber)
    {
        return WaveEnemies[WaveNumber - 1];
    }
}

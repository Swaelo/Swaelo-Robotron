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
    public int Grunts;
    public int Electrodes;
    public int Mommies;
    public int Daddies;
    public int Mikeys;
    public int Hulks;
    public int Brains;
    public int Spheroids;
    public int Quarks;
    public int Enforcer;
    public int Tank;
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

// ================================================================================================================================
// File:        GameState.cs
// Description:	Tracks all things like the players score, remaining lives, starting/changing waves, spawning enemies etc.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    //Singleton Instance
    public static GameState Instance;
    private void Awake() { Instance = this; }

    public GameObject Player;   //Player character object
    private int Score;   //The players current score value
    private int Lives;   //How many extra lives the player has remaining
    public int CurrentWave;    //What wave the player is currently on
    public bool DisableWaveProgression = false; //Waves will not begin or progress while this is enabled (for debugging purposes)
    public int RescueMultiplier = 1;    //Increased by 1 every time a human is rescued, reset back to 1 at the start of every round

    private void Start()
    {
        //Begin the player on the first wave
        Score = 0;
        Lives = 2;
        CurrentWave = 1;
        Player = PrefabSpawner.Instance.SpawnPrefab("Player", Vector3.zero, Quaternion.identity);

        //Spawn in everything for this wave, if wave progression is enabled
        if(!DisableWaveProgression)
            WaveManager.Instance.StartWave(CurrentWave);
    }

    //Adds points to the total score counter
    public void IncreaseScore(int Amount)
    {
        Score += Amount;
    }

    //Adds points to the score counter for rescuing a human survivor
    public void ScoreRescueSurvivor()
    {
        Score += 1000 * RescueMultiplier;
        RescueMultiplier++;
    }

    //Restarts the round if the player has lived remaining, otherwise proceeds to the gameover screen
    public void KillPlayer()
    {
        Lives--;

        //Go to gameover if the player has no lives left
        if (Lives == 0)
        {
            SceneManager.LoadScene(2);
            return;
        }

        //Otherwise we restart the current wave
        WaveManager.Instance.RestartWave();
    }
}

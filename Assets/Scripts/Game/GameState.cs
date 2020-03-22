// ================================================================================================================================
// File:        GameState.cs
// Description:	Tracks all things like the players score, remaining lives, starting/changing waves, spawning enemies etc.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    //Singleton Instance
    public static GameState Instance;
    private void Awake() { Instance = this; }

    public GameObject Player;   //Player character object
    private int Score;   //The players current score value
    private int ExtraLives;   //How many extra lives the player has remaining
    public int CurrentWave;    //What wave the player is currently on
    public bool DisableWaveProgression = false; //Waves will not begin or progress while this is enabled (for debugging purposes)
    public int RescueMultiplier = 1;    //Increased by 1 every time a human is rescued, reset back to 1 at the start of every round
    public Text UIScoreDisplay; //UI Text component used to display the players current score counter

    private void Start()
    {
        //Begin the player on the first wave
        Score = 0;
        ExtraLives = 2;
        LivesDisplay.Instance.SetExtraLivesDisplay(ExtraLives);
        CurrentWave = 1;
        Player = PrefabSpawner.Instance.SpawnPrefab("Player", Vector3.zero, Quaternion.identity);

        //Spawn in everything for this wave, if wave progression is enabled
        if(!DisableWaveProgression)
            WaveManager.Instance.StartWave(CurrentWave);
    }

    //Adds points to the total score counter
    public void IncreaseScore(int Amount)
    {
        //Increase the score count
        Score += Amount;
        //Update the UI score display
        UIScoreDisplay.text = Score.ToString();
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
        //Go to gameover if the player has no lives left
        if (ExtraLives == 0)
        {
            SceneManager.LoadScene(2);
            return;
        }
        
        //Remove one of the players extra lives in order to allow them to restart the current round
        ExtraLives--;
        LivesDisplay.Instance.SetExtraLivesDisplay(ExtraLives);

        //Restart the current wave
        WaveManager.Instance.RestartWave();
    }
}

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
    private int MaxExtraLives = 5;  //Maximum amount of extra lives the player is able to hold at once
    private int NextFreeLife = 25000;   //Current score milestone the player needs to reach to recieve their next free life
    private int FreeLifeInterval = 25000;   //Added onto the NextFreeLife milestone once it has been reached to set the next free life milestone
    public int CurrentWave;    //What wave the player is currently on
    public bool DisableWaveProgression = false; //Waves will not begin or progress while this is enabled (for debugging purposes)
    public int RescueMultiplier = 1;    //Increased by 1 every time a human is rescued, reset back to 1 at the start of every round
    private int MaxRescueMultiplier = 5;    //Maximum rescue multiplier the player is able to reach
    public Text UIScoreDisplay; //UI Text component used to display the players current score counter
    private bool GamePaused = false; //Everything is paused whenever this is true
    private float PlayerDeathTimeout = 2.5f;    //How long the game is frozen for when the player dies before the round is restarted
    private float DeathTimeoutLeft = 2.5f;  //Time remaining before the current death timeout expires
    public bool InDeathTimeout = false;

    private void Start()
    {
        //Begin the player on the first wave
        Score = 0;
        ExtraLives = 2;
        LivesDisplay.Instance.SetExtraLivesDisplay(ExtraLives);
        CurrentWave = 1;

        //Play a sound for the game beginning
        SoundEffectsPlayer.Instance.PlaySound("GameStart");

        //Spawn in everything for this wave, if wave progression is enabled
        if(!DisableWaveProgression)
            WaveManager.Instance.StartWave(CurrentWave);
    }

    private void Update()
    {
        //Toggle the games paused state
        if (Input.GetKeyDown(KeyCode.P))
            GamePaused = !GamePaused;

        //All game logic and AI should be paused at certain times
        if (GamePaused || WaveManager.Instance.SpawnPeriodActive)
            return;

        //Restart the current wave when the death timeout timer expires
        if (InDeathTimeout)
        {
            DeathTimeoutLeft -= Time.deltaTime;
            if (DeathTimeoutLeft <= 0.0f)
            {
                WaveManager.Instance.RestartWave();
                InDeathTimeout = false;
            }
        }
    }

    //Checks if the game is paused
    public bool IsGamePaused()
    {
        return GamePaused;
    }

    //Used from AI scripts and other stuff to check if the game should be running right now
    public bool ShouldAdvanceGame()
    {
        if (GamePaused || WaveManager.Instance.SpawnPeriodActive || WaveManager.Instance.WarmingUp || InDeathTimeout)
            return false;
        return true;
    }

    //Adds points to the total score counter
    public void IncreaseScore(int Amount)
    {
        //Increase the score count
        Score += Amount;
        //Update the UI score display
        UIScoreDisplay.text = Score.ToString();

        //Check if the player has reached the current free life milestone
        if(Score >= NextFreeLife)
        {
            //Award an extra life to the player if they arent already at the maximum amount
            if(ExtraLives < MaxExtraLives)
            {
                SoundEffectsPlayer.Instance.PlaySound("ExtraLife");
                ExtraLives++;
                LivesDisplay.Instance.SetExtraLivesDisplay(ExtraLives);
            }

            //Update the milestone for the next free life the player will be able to recieve
            NextFreeLife += FreeLifeInterval;
        }
    }

    //Adds points to the score counter for rescuing a human survivor
    public void ScoreRescueSurvivor()
    {
        //Award points for the rescue
        Score += 1000 * RescueMultiplier;

        //Spawn in the score display object for the amount of points that were awarded to the player
        string PrefabName = RescueMultiplier.ToString() + "000Points";
        PrefabSpawner.Instance.SpawnPrefab(PrefabName, Player.transform.position, Quaternion.identity);

        //Increase the multiplier until it reaches the maximum allowed value
        if(RescueMultiplier < MaxRescueMultiplier)
            RescueMultiplier++;
    }

    //Restarts the round if the player has lived remaining, otherwise proceeds to the gameover screen
    public void KillPlayer()
    {
        //Go to gameover if the player has no lives left
        if (ExtraLives == 0)
        {
            //Spawn in the ScoreCarry object, store the players score and final round number in it, and make sure it stays while we change over to the game over scene
            GameObject ScoreCarrier = PrefabSpawner.Instance.SpawnPrefab("ScoreCarry", Vector3.zero, Quaternion.identity);
            ScoreCarrier.GetComponent<ScoreCarry>().FinalScore = Score;
            ScoreCarrier.GetComponent<ScoreCarry>().FinalRound = CurrentWave;
            DontDestroyOnLoad(ScoreCarrier);

            //Now load into the game over scene
            SceneManager.LoadScene(2);
            return;
        }

        //Play sound effect
        SoundEffectsPlayer.Instance.PlaySound("PlayerDie");
        
        //Remove one of the players extra lives in order to allow them to restart the current round
        ExtraLives--;
        LivesDisplay.Instance.SetExtraLivesDisplay(ExtraLives);

        //Start the death timeout process
        DeathTimeoutLeft = PlayerDeathTimeout;
        InDeathTimeout = true;
    }
}

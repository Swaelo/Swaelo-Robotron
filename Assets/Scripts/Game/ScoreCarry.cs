// ================================================================================================================================
// File:        ScoreCarry.cs
// Description:	Used to carry the players score over from the game scene to the game over scene when they have run out of lives
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ScoreCarry : MonoBehaviour
{
    public static ScoreCarry Instance;
    private void Awake() { Instance = this; }

    public int FinalScore = 0;
    public int FinalRound = 1;
}

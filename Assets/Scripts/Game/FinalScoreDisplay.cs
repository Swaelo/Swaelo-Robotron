// ================================================================================================================================
// File:        FinalScoreDisplay.cs
// Description:	Displays the players final score and what round they survived to in the game over screen once they have run out of lives
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.UI;

public class FinalScoreDisplay : MonoBehaviour
{
    public Text ScoreDisplayText;   //UI text component used to display the players final score to them
    public Text RoundDisplayText;   //UI text component usued to display the players final round number to them

    private void Awake()
    {
        //Fetch the values from the ScoreCarrier object and display them to the UI
        GameObject ScoreCarrier = ScoreCarry.Instance.gameObject;
        if(ScoreCarrier != null)
        {
            ScoreDisplayText.text = "Final Score: " + ScoreCarry.Instance.FinalScore;
            RoundDisplayText.text = "You made it to round " + ScoreCarry.Instance.FinalRound;
        }
    }
}

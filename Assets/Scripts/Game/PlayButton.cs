// ================================================================================================================================
// File:        PlayButton.cs
// Description:	Proceeds from the main menu or game over scenes to the main game scene
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public void ClickPlayButton()
    {
        //Incase we are coming from the gameover scene, we need to check for the ScoreCarrier and destroy it before we return back to the main game scene
        if (ScoreCarry.Instance != null && ScoreCarry.Instance.gameObject != null)
            Destroy(ScoreCarry.Instance.gameObject);

        SceneManager.LoadScene(1);
    }
}

// ================================================================================================================================
// File:        MenuButtonFunctions.cs
// Description:	Gives functionality to all the buttons in each menu scene
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonFunctions : MonoBehaviour
{
    //Proceeds from the main menu or the game over scene back to the main gameplay scene
    public void PlayButtonFunction()
    {
        //Incase we are coming from the gameover scene, we need to check for the ScoreCarrier and destroy it before we return back to the main game scene
        if (ScoreCarry.Instance != null && ScoreCarry.Instance.gameObject != null)
            Destroy(ScoreCarry.Instance.gameObject);

        //Load the game scene
        SceneManager.LoadScene(1);
    }

    //Goes from the game over screen back to the main menu
    public void GameOverExitFunction()
    {
        SceneManager.LoadScene(0);
    }

    //Closes the application from the main menu
    public void GameQuitFunction()
    {
        Application.Quit();
    }
}

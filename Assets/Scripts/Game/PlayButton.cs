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
        SceneManager.LoadScene(1);
    }
}

// ================================================================================================================================
// File:        LivesDisplay.cs
// Description:	Controls the sprites on the UI to display to the user how many extra lives they have remaining
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class LivesDisplay : MonoBehaviour
{
    public static LivesDisplay Instance;
    private void Awake() { Instance = this; }

    public GameObject[] LifeDisplaySprites; //List of objects used to display to the user how many lives they have remaining

    //Displays only the requested number of extra life symbols to the UI
    public void SetExtraLivesDisplay(int ExtraLifeCount)
    {
        for (int i = 0; i < 5; i++)
            LifeDisplaySprites[i].SetActive(ExtraLifeCount > i);
    }
}

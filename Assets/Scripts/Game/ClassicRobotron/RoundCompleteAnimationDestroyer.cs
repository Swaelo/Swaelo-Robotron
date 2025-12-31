// ================================================================================================================================
// File:        RoundCompleteAnimationDestroyer.cs
// Description:	Automatically destroys the round completion animation object once its finished playing through the animation
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class RoundCompleteAnimationDestroyer : MonoBehaviour
{
    private float AnimationTimeLeft = 1.875f;

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Destroy the object once the animation has finished
        AnimationTimeLeft -= Time.deltaTime;
        if (AnimationTimeLeft <= 0.0f)
            Destroy(gameObject);
    }
}

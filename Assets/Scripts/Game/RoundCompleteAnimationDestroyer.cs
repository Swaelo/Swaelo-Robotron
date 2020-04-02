// ================================================================================================================================
// File:        RoundCompleteAnimationDestroyer.cs
// Description:	Automatically destroys the round completion animation object once its finished playing through the animation
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class RoundCompleteAnimationDestroyer : MonoBehaviour
{
    private float AnimationTimeLeft = 2.983f;

    private void Update()
    {
        AnimationTimeLeft -= Time.deltaTime;
        if (AnimationTimeLeft <= 0.0f)
            Destroy(gameObject);
    }
}

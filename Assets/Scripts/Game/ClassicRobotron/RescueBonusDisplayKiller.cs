// ================================================================================================================================
// File:        RescueBonusDisplayKiller.cs
// Description:	Automatically destroys any rescue bonus score displayers which are spawned into the level during gameplay
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class RescueBonusDisplayKiller : MonoBehaviour
{
    private float LifetimeRemaining = 1f;   //Time left until the object needs to be destroyed

    private void Update()
    {
        //Wait for the lifetime to expire
        LifetimeRemaining -= Time.deltaTime;
        if (LifetimeRemaining <= 0.0f)
            Destroy(gameObject);
    }
}

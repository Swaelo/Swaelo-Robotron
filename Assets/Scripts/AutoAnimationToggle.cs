// ================================================================================================================================
// File:        AutoAnimationToggle.cs
// Description:	Automatically enables and disables an objects animations when the game is paused, unpaused etc.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class AutoAnimationToggle : MonoBehaviour
{
    public Animator[] Animators;    //Animators to be toggles
    private void Update()
    {
        //Check if sprites should be animated right now
        bool ShouldAnimate = GameState.Instance.ShouldAdvanceGame();
        //Tell the Animators what they should be doing
        foreach (Animator Animator in Animators)
            if(Animator != null)
                Animator.SetBool("ShouldAnimate", ShouldAnimate);
    }
}

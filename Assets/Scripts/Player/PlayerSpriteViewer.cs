// ================================================================================================================================
// File:        PlayerSpriteViewer.cs
// Description:	Handles which of the player sprites are shown depending on which direction the player has been moving
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public enum EyeColors
{
    Red = 0,
    Green = 1,
    Blue = 2
}

public class PlayerSpriteViewer : MonoBehaviour
{
    //Eye Color
    private EyeColors EyeColor = EyeColors.Blue;    //The players current eye color
    private float NextEyeColor = 0.5f;  //How long until the eye color changes
    private float ColorChangeRate = 0.5f;   //How often the players eye color changes

    public SpriteRenderer[] FrontSprites;   //Sprites for rendering the front view of the player
    public SpriteRenderer[] SideSprites; //Sprites for rendering the side view of the player

    public Animator[] BodyAnimators;    //Body part animators used to control walking animation
    public Animator[] EyeAnimators; //Used to change the players eye color

    //Players change in position over time is used to determine which sprites need to be displayed and when to play the animations
    private Vector3 PreviousPos;

    private void Awake()
    {
        //Store initial position and enable only the front sprites
        PreviousPos = transform.position;
        SetFrontSpriteVisibility(true);
        SetSideSpriteVisibility(false);
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Change eye color over time
        CycleEyeColors();

        //Tell the body animators if the player is moving
        bool IsMoving = transform.position != PreviousPos;
        foreach (Animator BodyAnimator in BodyAnimators)
            BodyAnimator.SetBool("IsMoving", IsMoving);

        //Update what sprites are viewed when moving around
        if(IsMoving)
        {
            //Find the distance travelled in each direction since the last frame
            float VerticalMovement = Mathf.Abs(transform.position.y - PreviousPos.y);
            float HorizontalMovement = Mathf.Abs(transform.position.x - PreviousPos.x);

            //View front when moving vertical, otherwise view side sprites
            bool MovingVertically = VerticalMovement > HorizontalMovement;
            SetFrontSpriteVisibility(MovingVertically);
            SetSideSpriteVisibility(!MovingVertically);

            //If moving horizontal to the right, flip the side sprites
            bool MovingRight = transform.position.x > PreviousPos.x;
            foreach (SpriteRenderer Renderer in SideSprites)
                Renderer.flipX = MovingRight;
        }

        //Store current position for next update
        PreviousPos = transform.position;
    }

    //Changes the players eye color periodically
    private void CycleEyeColors()
    {
        //Wait for the timer to expire
        NextEyeColor -= Time.deltaTime;
        if(NextEyeColor <= 0.0f)
        {
            //Reset the timer
            NextEyeColor = ColorChangeRate;
            //Update the color
            switch(EyeColor)
            {
                case (EyeColors.Red):
                    SetEyeColor(EyeColors.Green);
                    break;
                case (EyeColors.Green):
                    SetEyeColor(EyeColors.Blue);
                    break;
                case (EyeColors.Blue):
                    SetEyeColor(EyeColors.Red);
                    break;
            }
        }
    }

    //Updates the eye animators to display the new eye color
    private void SetEyeColor(EyeColors NewColor)
    {
        EyeColor = NewColor;
        foreach(Animator EyeAnimator in EyeAnimators)
        {
            EyeAnimator.SetBool("Blue", NewColor == EyeColors.Blue);
            EyeAnimator.SetBool("Red", NewColor == EyeColors.Red);
            EyeAnimator.SetBool("Green", NewColor == EyeColors.Green);
        }
    }

    //Toggles the visibility of the front view sprites
    private void SetFrontSpriteVisibility(bool ShouldDisplay)
    {
        foreach (SpriteRenderer Renderer in FrontSprites)
            Renderer.forceRenderingOff = !ShouldDisplay;
    }
    private void SetSideSpriteVisibility(bool ShouldDisplay)
    {
        foreach (SpriteRenderer Renderer in SideSprites)
            Renderer.forceRenderingOff = !ShouldDisplay;
    }
}

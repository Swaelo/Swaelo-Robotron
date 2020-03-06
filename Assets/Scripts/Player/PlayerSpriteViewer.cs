// ================================================================================================================================
// File:        PlayerSpriteViewer.cs
// Description:	Handles which of the player sprites are shown depending on which direction the player has been moving
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public enum PlayerEyeColor
{
    Red = 0,
    Green = 1,
    Blue = 2
}

public class PlayerSpriteViewer : MonoBehaviour
{
    //Current eye color and timer for cycling through
    private PlayerEyeColor CurrentEyeColor = PlayerEyeColor.Red;
    private float EyeColorChangeRate = 0.5f;
    private float NextEyeColorChange = 0.5f;

    //Sprites for the front and side views of the player
    public GameObject FrontSprites;
    public GameObject SideSprites;

    //Animators controlling the walking animations
    public Animator FrontViewAnimator;
    public Animator SideViewAnimator;

    //Animators controlling the eye colors
    public Animator FrontEyesAnimator;
    public Animator SideEyesAnimator;

    //Renderers for Side Body components, so they can be flipped when moving left/right
    public SpriteRenderer SideBodyRenderer;
    public SpriteRenderer SideEyesRenderer;

    //Players change in position over time is used to determine which sprites need to be displayed and when to play the animations
    private Vector3 PreviousPosition;

    private void Awake()
    {
        PreviousPosition = transform.position;
        SideSprites.SetActive(false);
    }

    private void Update()
    {
        CycleEyeColor();

        //Find distance travelled in each direction this frame
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //Tell the animators if we are currently moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontViewAnimator.SetBool("IsMoving", IsMoving);
        SideViewAnimator.SetBool("IsMoving", IsMoving);

        //Transition between different spriters being displayed when the Player is moving around
        if(IsMoving)
        {
            //Update what sprites are being displayed if movement has occured
            bool MovingVertical = VerticalMovement < HorizontalMovement;
            FrontSprites.SetActive(MovingVertical);
            SideSprites.SetActive(!MovingVertical);

            //Flip the side view sprites based on left/right movement
            bool MovingLeft = transform.position.x < PreviousPosition.x;
            SideBodyRenderer.flipX = !MovingLeft;
            SideEyesRenderer.flipX = !MovingLeft;
        }

        //Store current position for next frames comparison
        PreviousPosition = transform.position;
    }

    private void CycleEyeColor()
    {
        //Update the eye color timer and move to the next eye color when it runs out
        NextEyeColorChange -= Time.deltaTime;

        if(NextEyeColorChange <= 0.0f)
        {
            //Reset the eye color
            NextEyeColorChange = EyeColorChangeRate;

            //Move to the next eye color
            switch(CurrentEyeColor)
            {
                case (PlayerEyeColor.Red):
                    CurrentEyeColor = PlayerEyeColor.Green;
                    FrontEyesAnimator.SetTrigger("Green");
                    SideEyesAnimator.SetTrigger("Green");
                    break;
                case (PlayerEyeColor.Green):
                    CurrentEyeColor = PlayerEyeColor.Blue;
                    FrontEyesAnimator.SetTrigger("Blue");
                    SideEyesAnimator.SetTrigger("Blue");
                    break;
                case (PlayerEyeColor.Blue):
                    CurrentEyeColor = PlayerEyeColor.Red;
                    FrontEyesAnimator.SetTrigger("Red");
                    SideEyesAnimator.SetTrigger("Red");
                    break;
            }
        }
    }
}

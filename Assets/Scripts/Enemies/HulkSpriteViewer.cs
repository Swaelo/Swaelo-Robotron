// ================================================================================================================================
// File:        HulkSpriteViewer.cs
// Description:	Handles which of the hulk sprites are shown depending on which direction it has been moving in
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class HulkSpriteViewer : MonoBehaviour
{
    //Sprites for the front/side view of the Hulk enemy
    public GameObject FrontSprites;
    public GameObject SideSprites;

    //Animators controlling the walking animations
    public Animator FrontLegAnimator;
    public Animator SideLegAnimator;
    public Animator SideArmAnimator;

    //Renderers for side body components to be flipped when moving left/right horizontally
    public SpriteRenderer SideBodyRenderer;
    public SpriteRenderer SideArmRenderer;

    //Hulks change in position over time is used to determine which sprites need to be displayed and when to play the animations
    private Vector3 PreviousPosition;

    //While the Hulk stops to change direction, quickly flash between the two sprites to help visualize this
    private bool IsTurning = false;      //Flashes between current and next sprite while turning
    private CardinalDirection CurrentDirection;  //The current facing direction
    private CardinalDirection NextDirection; //The facing direction we are changing to
    private float TurnAnimationInterval = 0.25f;    //How often to flash between sprites to visualize the Hulk turning
    private float NextTurnSwap = 0.25f; //How long until the next flash between sprites
    private bool ShowingCurrentSprite = true;   //Tracks which sprite is currently being shown when flashing between them

    private void Start()
    {
        PreviousPosition = transform.position;
        SideSprites.SetActive(false);
    }

    private void Update()
    {
        if (IsTurning)
            DisplayTurningAnimation();
        else
            DisplayMovementAnimations();
    }

    //Displays animations while the Hulk is moving around the level
    private void DisplayMovementAnimations()
    {
        //Find distance travelled in each direction this frame
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //Tell the animators if the Hulk is currently moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontLegAnimator.SetBool("IsMoving", IsMoving);
        SideLegAnimator.SetBool("IsMoving", IsMoving);
        SideArmAnimator.SetBool("IsMoving", IsMoving);

        //Transition between displaying the different sprites based on which direction the Hulk is moving
        if (IsMoving)
        {
            //Update what sprites are being displayed
            bool MovingVertical = VerticalMovement < HorizontalMovement;
            FrontSprites.SetActive(MovingVertical);
            SideSprites.SetActive(!MovingVertical);

            //Flip the side sprites based on left/right movement
            bool MovingLeft = transform.position.x < PreviousPosition.x;
            SideBodyRenderer.flipX = !MovingLeft;
            SideArmRenderer.flipX = !MovingLeft;
        }

        //Store position for the next update
        PreviousPosition = transform.position;
    }

    //Displays animations while the Hulk is turning to change direction
    private void DisplayTurningAnimation()
    {
        //Decrement the sprite swap timer
        NextTurnSwap -= Time.deltaTime;
        //Swap sprites when the timer reaches zero
        if(NextTurnSwap <= 0.0f)
        {
            //Reset the timer value
            NextTurnSwap = TurnAnimationInterval;
            //Swap to displaying the other sprite
            ShowingCurrentSprite = !ShowingCurrentSprite;
            FaceDirection(ShowingCurrentSprite ? CurrentDirection : NextDirection);
        }
    }

    //Allows the HulkAI script to force change the current sprite immediately when finished changing to face a new direction
    public void FaceDirection(CardinalDirection Direction)
    {
        switch(Direction)
        {
            case (CardinalDirection.North):
                FrontSprites.SetActive(true);
                SideSprites.SetActive(false);
                break;
            case (CardinalDirection.East):
                FrontSprites.SetActive(false);
                SideSprites.SetActive(true);
                SideBodyRenderer.flipX = true;
                SideArmRenderer.flipX = true;
                break;
            case (CardinalDirection.South):
                FrontSprites.SetActive(true);
                SideSprites.SetActive(false);
                break;
            case (CardinalDirection.West):
                FrontSprites.SetActive(false);
                SideSprites.SetActive(true);
                SideBodyRenderer.flipX = false;
                SideArmRenderer.flipX = false;
                break;
        }
    }

    //Begins the animation to visualize the Hulk turning to change its direction
    public void StartTurning(CardinalDirection CurrentDirection, CardinalDirection NextDirection)
    {
        //Store which directions we are changing between
        this.CurrentDirection = CurrentDirection;
        this.NextDirection = NextDirection;
        //Start the flash animation timer
        IsTurning = true;
        NextTurnSwap = TurnAnimationInterval;
        ShowingCurrentSprite = true;
        //Make sure we begin with the current direction being shown properly
        FaceDirection(CurrentDirection);
    }

    //Ends the turning animation, setting the next direction to be current
    public void StopTurning()
    {
        IsTurning = false;
        CurrentDirection = NextDirection;
        FaceDirection(CurrentDirection);
    }
}

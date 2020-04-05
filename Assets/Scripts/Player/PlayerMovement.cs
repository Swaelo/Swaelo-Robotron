// ================================================================================================================================
// File:        PlayerMovement.cs
// Description:	Allows the player to move around the screen
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

//Colors available for the players eyes
public enum EyeColors
{
    Red = 1,
    Green = 2,
    Blue = 3
};

public class PlayerMovement : MonoBehaviour
{
    //Movement
    public float MoveSpeed = 1.5f;  //How fast the player can move
    private Vector3 PreviousPos = Vector3.zero;   //Players position last frame
    public Vector3 MovementVelocity = Vector3.zero;    //Velocity at which the player is currently travelling

    //Eyes
    private EyeColors EyeColor = EyeColors.Blue;    //Current eye color
    private float NextEyeColor = 0.5f;  //Time until next eye color change
    private float ColorChangeRate = 0.5f;   //How often the eye color changes

    //Rendering
    public SpriteRenderer[] FrontSprites;   //Sprites for rendering the front view of the player
    public SpriteRenderer[] SideSprites;    //Sprites for rendering the side view of the player
    public Animator[] BodyAnimators;    //Body part animators used to control walking animation
    public Animator[] EyeAnimators; //Used to change the players eye color

    //Physics
    public BoxCollider2D FrontCollider; //Used for collision detection during front view mode
    public BoxCollider2D SideCollider;  //Used for collision detection during side view mode

    private void Awake()
    {
        //Set initial previous position and set only the front sprites to be viewed right now
        PreviousPos = transform.position;
        MovementVelocity = Vector3.zero;
        ToggleViewMode(true);
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Cycle through eye colors periodically
        CycleEyeColor();

        //Create a new movement vector
        float HorizontalInput = Mathf.Clamp(Input.GetAxis("KeyboardHorizontalMovement") + Input.GetAxis("ControllerHorizontalMovement"), -1f, 1f);
        float VerticalInput = Mathf.Clamp(Input.GetAxis("KeyboardVerticalMovement") + Input.GetAxis("ControllerVerticalMovement"), -1f, 1f);
        Vector3 MovementVector = new Vector3(HorizontalInput, VerticalInput, 0f);

        //Apply this to update the players position
        transform.position += MovementVector * MoveSpeed * Time.deltaTime;

        //Update the body animators if the player is moving or not
        bool IsMoving = transform.position != PreviousPos;
        foreach (Animator BodyAnimator in BodyAnimators)
            BodyAnimator.SetBool("IsMoving", IsMoving);

        //Change between front and side modes when moving around
        if(IsMoving)
        {
            //Measure distance travelled in each direction since last frame
            float VertMovement = Mathf.Abs(transform.position.y - PreviousPos.y);
            float HoriMovement = Mathf.Abs(transform.position.x - PreviousPos.x);

            //View front when moving vertically, otherwise use side view
            bool MovingVertically = VertMovement > HoriMovement;
            ToggleViewMode(MovingVertically);

            //If moving horizontally towards the right hand side, flip all the side sprites on the X axis
            bool MovingRight = transform.position.x > PreviousPos.x;
            foreach (SpriteRenderer Renderer in SideSprites)
                Renderer.flipX = MovingRight;
        }

        //Calculate movement velocity between frames and store current position for next frames update
        MovementVelocity = transform.position - PreviousPos;
        PreviousPos = transform.position;
    }

    //Changes the players eye color periodically
    private void CycleEyeColor()
    {
        //Wait for the timer to expire
        NextEyeColor -= Time.deltaTime;
        if (NextEyeColor <= 0.0f)
        {
            //Reset the timer
            NextEyeColor = ColorChangeRate;
            //Update the color
            switch (EyeColor)
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
        foreach (Animator EyeAnimator in EyeAnimators)
        {
            EyeAnimator.SetBool("Blue", NewColor == EyeColors.Blue);
            EyeAnimator.SetBool("Red", NewColor == EyeColors.Red);
            EyeAnimator.SetBool("Green", NewColor == EyeColors.Green);
        }
    }

    //Resets the player back to the middle of the level
    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    //Toggles between front and side view modes
    private void ToggleViewMode(bool UseFrontView)
    {
        //Toggle visibilty of all the sprites
        foreach (SpriteRenderer Renderer in FrontSprites)
            Renderer.forceRenderingOff = !UseFrontView;
        foreach (SpriteRenderer Renderer in SideSprites)
            Renderer.forceRenderingOff = UseFrontView;
        //Toggle colliders
        FrontCollider.enabled = UseFrontView;
        SideCollider.enabled = !UseFrontView;
    }
}

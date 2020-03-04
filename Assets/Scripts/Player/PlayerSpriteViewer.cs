// ================================================================================================================================
// File:        PlayerSpriteViewer.cs
// Description:	Handles which of the player sprites are shown depending on which direction the player has been moving
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class PlayerSpriteViewer : MonoBehaviour
{
    //Sprites for the front and side views of the player
    public GameObject FrontSprites;
    public GameObject SideSprites;

    //Animators controlling the walking animations
    public Animator FrontViewAnimator;
    public Animator SideViewAnimator;

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
        //Find distance travelled in each direction this frame
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //Tell the animators if we are currently moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontViewAnimator.SetBool("IsMoving", IsMoving);
        SideViewAnimator.SetBool("IsMoving", IsMoving);

        //Transition between different spriters being ddispalyed when the Player is moving around
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
}

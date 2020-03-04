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

        //Tell the animators if the Hulk is currently moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontLegAnimator.SetBool("IsMoving", IsMoving);
        SideLegAnimator.SetBool("IsMoving", IsMoving);
        SideArmAnimator.SetBool("IsMoving", IsMoving);

        //Transition between displaying the different sprites based on which direction the Hulk is moving
        if(IsMoving)
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
}

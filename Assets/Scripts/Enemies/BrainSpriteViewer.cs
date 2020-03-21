// ================================================================================================================================
// File:        BrainSpriteViewer.cs
// Description:	Handles which of the brain sprites are shown depending on which direction it has been moving in
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class BrainSpriteViewer : MonoBehaviour
{
    //Sprites for the front/back and side views of the Brain enemy
    public GameObject FrontSprites;
    public GameObject BackSprites;
    public GameObject SideSprites;

    //Animation controllers for all main body parts of the Brain
    public Animator FrontBodyAnimator;
    public Animator BackBodyAnimator;
    public Animator SideBodyAnimator;

    //Animation controllers for all subcomponents parts of the Brain
    public Animator FrontBrainAnimator;
    public Animator FrontEyesAnimator;
    public Animator BackBrainAnimator;
    public Animator SideBrainAnimator;
    public Animator SideEyesAnimator;

    //Renderers for the side body and its subcompnents
    public SpriteRenderer SideBodyRenderer;
    public SpriteRenderer SideBrainRenderer;
    public SpriteRenderer SideEyesRenderer;

    //Change in position over time is measured to determine what sprites should be rendered
    private Vector3 PreviousPosition;

    private void Awake()
    {
        //Store initial position and enable only the front sprites
        PreviousPosition = transform.position;
        FrontSprites.SetActive(true);
        BackSprites.SetActive(false);
        SideSprites.SetActive(false);
    }

    private void Update()
    {
        //Inform the main animation controllers if the brain is currently moving or not
        bool IsMoving = transform.position != PreviousPosition;
        FrontBodyAnimator.SetBool("IsMoving", IsMoving);
        BackBodyAnimator.SetBool("IsMoving", IsMoving);
        SideBodyAnimator.SetBool("IsMoving", IsMoving);

        //Transition between different sprites while the Brain is moving around
        if(IsMoving)
        {
            //Measure distance travelled in each direction this frame
            float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
            float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

            //First check for movement on the Y axis
            if(VerticalMovement < HorizontalMovement)
            {
                //Toggle view of sprites based on which direction on the Y axis the Brain is moving
                bool MovingUp = transform.position.y > PreviousPosition.y;
                FrontSprites.SetActive(!MovingUp);
                BackSprites.SetActive(MovingUp);
                SideSprites.SetActive(false);
            }
            //Otherwise we know the Brain is moving on the X axis
            else
            {
                //Enable only the side sprites
                FrontSprites.SetActive(false);
                BackSprites.SetActive(false);
                SideSprites.SetActive(true);
                //Flip the sprites based on which direction on the X axis the Brain is moving
                bool MovingRight = transform.position.x < PreviousPosition.x;
                SideBodyRenderer.flipX = MovingRight;
                SideBrainRenderer.flipX = MovingRight;
                SideEyesRenderer.flipX = MovingRight;
            }
        }

        //Store position for next frames update
        PreviousPosition = transform.position;
    }
}

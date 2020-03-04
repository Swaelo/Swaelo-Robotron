// ================================================================================================================================
// File:        BrainSpriteViewer.cs
// Description:	Handles which of the brain sprites are shown depending on which direction it has been moving in
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class BrainSpriteViewer : MonoBehaviour
{
    //Sprites for the front/back and side view of the Brain enemy
    public GameObject FrontSprites;
    public GameObject BackSprites;
    public GameObject SideSprites;

    //Animators controlling the walking animations
    public Animator FrontAnimator;
    public Animator BackAnimator;
    public Animator SideAnimator;

    //Renderers for side components so they can be flipped when moving in the opposite direction
    public SpriteRenderer SideBodyRenderer;
    public SpriteRenderer SideBrainRenderer;
    public SpriteRenderer SideEyeRenderer;

    //Change in position over time is measured to determine what sprites should be rendered
    private Vector3 PreviousPosition;

    private void Awake()
    {
        PreviousPosition = transform.position;
        BackSprites.SetActive(false);
        SideSprites.SetActive(false);
    }

    private void Update()
    {
        //Measure distance travelled in each direction
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //Tell the animation controllers if the brain is moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontAnimator.SetBool("IsMoving", IsMoving);
        BackAnimator.SetBool("IsMoving", IsMoving);
        SideAnimator.SetBool("IsMoving", IsMoving);

        //Transition between different sprites being displayed when the Brain is moving around
        if(IsMoving)
        {
            //Hide/Show different sprites if movement has occured
            bool MovingVertical = VerticalMovement < HorizontalMovement;
            if(MovingVertical)
            {
                bool MovingUp = transform.position.y > PreviousPosition.y;
                SideSprites.SetActive(false);
                FrontSprites.SetActive(!MovingUp);
                BackSprites.SetActive(MovingUp);
            }
            else
            {
                //Flip the side sprites based on left/right movement
                bool MovingLeft = transform.position.x > PreviousPosition.x;
                FrontSprites.SetActive(false);
                BackSprites.SetActive(false);
                SideSprites.SetActive(true);
                SideBodyRenderer.flipX = !MovingLeft;
                SideBrainRenderer.flipX = !MovingLeft;
                SideEyeRenderer.flipX = !MovingLeft;
            }
        }

        //Store position for next frames update
        PreviousPosition = transform.position;
    }
}

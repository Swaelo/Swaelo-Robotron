// ================================================================================================================================
// File:        HumanSpriteViewer.cs
// Description:	Handles which of the Humans sprites are shown depending on which direction hes moving
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class HumanSpriteViewer : MonoBehaviour
{
    //Should side sprites be flipped or not (some sprite image files are facing opposite directions)
    public bool FlipSideSprites = false;

    //Objects for viewing front/back and side views of Human
    public GameObject FrontSprites;
    public GameObject BackSprites;
    public GameObject SideSprites;

    //Animation controllers for handling transition between idle/walking animations
    public Animator FrontAnimationController;
    public Animator BackAnimationController;
    public Animator SideAnimationController;

    public SpriteRenderer SideBodyRenderer; //Renderer for the side body so it can be flipped when moving left/right

    private Vector3 PreviousPosition;   //Used to calculate when Human is moving or idle

    private void Awake()
    {
        //Store initial position and disable back/side sprites
        PreviousPosition = transform.position;
        BackSprites.SetActive(false);
        SideSprites.SetActive(false);
    }

    private void Update()
    {
        //Inform the animation controllers if Human is currently moving
        bool IsMoving = transform.position != PreviousPosition;
        FrontAnimationController.SetBool("IsMoving", IsMoving);
        BackAnimationController.SetBool("IsMoving", IsMoving);
        SideAnimationController.SetBool("IsMoving", IsMoving);

        //Keep sprites updated while the Human is moving
        if (IsMoving)
            UpdateSpritesView();

        //Store current position for next frames update
        PreviousPosition = transform.position;
    }

    //Activates/Deactivates different sprites for Human depending on which way he's moving
    private void UpdateSpritesView()
    {
        //Calculate distance travelled in each direction since the last update
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //First check if the Human is moving vertically
        if (VerticalMovement < HorizontalMovement)
        {
            //Check for vertical movement in the positive direction
            if(transform.position.y > PreviousPosition.y)
            {
                //Set only the back sprites as active
                BackSprites.SetActive(true);
                FrontSprites.SetActive(false);
                SideSprites.SetActive(false);
            }
            //Otherwise we know the Human is moving vertically in the negative direction
            else
            {
                //Set only the front sprites as active
                FrontSprites.SetActive(true);
                BackSprites.SetActive(false);
                SideSprites.SetActive(false);
            }
        }
        //Otherwise we know the Human is moving horizontally
        else
        {
            //Set only the side sprites as active
            SideSprites.SetActive(true);
            FrontSprites.SetActive(false);
            BackSprites.SetActive(false);

            //Flip the side sprites while moving horizontally in the positive direction
            SideBodyRenderer.flipX = FlipSideSprites ? (!(transform.position.x > PreviousPosition.x)) : (transform.position.x > PreviousPosition.x);
        }
    }
}

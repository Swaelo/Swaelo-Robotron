// ================================================================================================================================
// File:        ProgSpriteViewer.cs
// Description:	Handles which of the Progs sprites are shown depending on which direction its moving
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ProgSpriteViewer : MonoBehaviour
{
    //Sets if the side sprites should be flipper or not
    public bool FlipSideSprites = false;

    //All the sprites used to render the Prog
    public SpriteRenderer FrontBodySprite;
    public SpriteRenderer SideBodySprite;
    public SpriteRenderer[] FrontTrailSprites;
    public SpriteRenderer[] SideTrailSprites;

    //Used to measure distance travelled over time
    private Vector3 PreviousPosition;

    private void Awake()
    {
        //Store initial position
        PreviousPosition = transform.position;
        //Unparent all the trail sprites from the Prog
        foreach (SpriteRenderer TrailRenderer in FrontTrailSprites)
            TrailRenderer.transform.parent = null;
        foreach (SpriteRenderer TrailRenderer in SideTrailSprites)
            TrailRenderer.transform.parent = null;
        //Set only the front sprites to be visible right now
        SetVerticalSpriteVisibility(true);
        SetHorizontalSpriteVisibility(false);
    }

    private void Update()
    {
        //Calculate distance travelled in each direction since last frame
        float VerticalMovement = Mathf.Abs(transform.position.x - PreviousPosition.x);
        float HorizontalMovement = Mathf.Abs(transform.position.y - PreviousPosition.y);

        //Check for vertical movement
        if(VerticalMovement < HorizontalMovement)
        {
            //Set only the vertical sprites to be active
            SetVerticalSpriteVisibility(true);
            SetHorizontalSpriteVisibility(false);
        }
        //Otherwise handle horizontal rendering
        else
        {
            //Set only horizontal sprites as active
            SetHorizontalSpriteVisibility(true);
            SetVerticalSpriteVisibility(false);

            //Flip all the horizontal sprites based on left/right movement
            bool FlipSprites = FlipSideSprites ? (!(transform.position.x > PreviousPosition.x)) : (transform.position.x > PreviousPosition.x);
            SetHorizontalSpriteFlipping(FlipSprites);
        }

        //Store position for next frame
        PreviousPosition = transform.position;
    }

    //Toggles x flipping of all horizontal sprites
    private void SetHorizontalSpriteFlipping(bool ShouldFlip)
    {
        SideBodySprite.flipX = ShouldFlip;
        foreach (SpriteRenderer TrailRenderer in SideTrailSprites)
            TrailRenderer.flipX = ShouldFlip;
    }

    //Toggles visbility of all horizontal sprites
    private void SetHorizontalSpriteVisibility(bool IsVisible)
    {
        SideBodySprite.forceRenderingOff = !IsVisible;
        foreach (SpriteRenderer TrailRenderer in SideTrailSprites)
            TrailRenderer.forceRenderingOff = !IsVisible;
    }

    //Toggles visibility of all vertical sprites
    private void SetVerticalSpriteVisibility(bool IsVisible)
    {
        FrontBodySprite.forceRenderingOff = !IsVisible;
        foreach (SpriteRenderer TrailRenderer in FrontTrailSprites)
            TrailRenderer.forceRenderingOff = !IsVisible;
    }
}

// ================================================================================================================================
// File:        FriendlyEntity.cs
// Description:	Controls movement and collisions of all human survivors
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class FriendlyEntity : BaseEntity
{
    //Movement/Wandering
    private float MoveSpeed = 0.65f;    //How fast the human survivors wander around the level
    private Vector3 CurrentDirection;   //Current direction the entity is wandering in
    private Vector2 WanderRange = new Vector2(0.15f, 3f);   //Range of seconds that a survive may wander in its current direction before deciding on a new direction
    private float WanderRemaining;  //Time left to wander in the current direction before finding a new direction to move in

    //Rendering/Animation
    public bool FlipSideSprites = false;    //Some sprites may need to be flipped on the X axis as the image files are drawn facing a the opposite direction
    public SpriteRenderer FrontBodyRenderer;    //Renderer for the front view sprite
    public SpriteRenderer SideBodyRenderer; //Renderer for the side view sprite
    public SpriteRenderer BackBodyRenderer; //Renderer for the back view sprite
    public Animator[] AnimationControllers; //Animators for each sprite of the entity

    public bool TargettedByBrain = false;   //Flagged true when a Brain decides this will be their target, to make sure no other Brain tries to capture the same one
    public bool BeingReprogrammed = false; //Flagged to true once the human has been captured by a Brain

    private Vector3 PreviousPos;    //For measuring when and in which direction the entity is moving
    
    private void Start()
    {
        //Store initial position into previous
        PreviousPos = transform.position;
        //Set only the front view sprite to be viewed
        FrontBodyRenderer.forceRenderingOff = false;
        SideBodyRenderer.forceRenderingOff = true;
        BackBodyRenderer.forceRenderingOff = true;
        //Get an initial direction for the entity to wander in
        NewWanderDirection();
    }

    //Selects a new random direction for the entity to walk in
    private void NewWanderDirection()
    {
        //First randomly decide how long we will wander in the new direction for
        WanderRemaining = Random.Range(WanderRange.x, WanderRange.y);

        //Get a new random direction to wander set it
        Vector3 NewDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
        CurrentDirection = NewDirection.normalized;
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Keep wandering around
        WanderAround();

        //Make sure the correct sprites are being rendered, and their animations are playing correctly
        ManageSpriteRendering();
    }

    //Wanders around randomly
    private void WanderAround()
    {
        //Get a new wander direction when we've wandered in the current direction for long enough
        WanderRemaining -= Time.deltaTime;
        if (WanderRemaining <= 0.0f)
            NewWanderDirection();

        //Wander in the current direction
        transform.position += CurrentDirection * MoveSpeed * Time.deltaTime;
    }

    //Toggles which sprites are being viewed, and manages their animation playback
    private void ManageSpriteRendering()
    {
        //Keep all the animation controllers aware of when the entity is moving so they can transition between the Idle/Walking animations
        bool IsMoving = transform.position != PreviousPos;
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetBool("IsMoving", IsMoving);

        //Measure distance travelled in each direction to figure out which way the entity is moving
        float VerticalMovement = Mathf.Abs(transform.position.y - PreviousPos.y);
        float HorizontalMovement = Mathf.Abs(transform.position.x - PreviousPos.x);
        bool MovingVertical = VerticalMovement > HorizontalMovement;

        //Manage rendering of Front/Back sprites during vertical movement
        if(MovingVertical)
        {
            //Side sprites should always be hidden during vertical movement
            SideBodyRenderer.forceRenderingOff = true;
            //View either the Front/Back sprite based on the direction of vertical movement
            bool MovingUp = transform.position.y > PreviousPos.y;
            FrontBodyRenderer.forceRenderingOff = MovingUp;
            BackBodyRenderer.forceRenderingOff = !MovingUp;
        }
        //Manage renderering of the Side sprite during horizontal movement
        else
        {
            //Set only the side sprite to be rendered during horizontal movement
            FrontBodyRenderer.forceRenderingOff = true;
            BackBodyRenderer.forceRenderingOff = true;
            SideBodyRenderer.forceRenderingOff = false;
            //Set the side sprite to get flipped on the X axis based on the direction of horizontal movement
            bool MovingRight = transform.position.x > PreviousPos.x;
            bool ShouldFlip = FlipSideSprites ? !MovingRight : MovingRight;
            SideBodyRenderer.flipX = ShouldFlip;
        }

        //Store current position for next frames movement calculations
        PreviousPos = transform.position;
    }

    //Called by a Brain enemy when it has captured the Human
    public void CapturedForReprogramming()
    {
        //Start playing the reprogramming animation and make sure the entities AI is disabled and it doesnt trigger any more collisions
        BeingReprogrammed = true;
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetTrigger("Reprogram");
        Destroy(GetComponent<BoxCollider2D>());
        Destroy(GetComponent<Rigidbody2D>());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //The entity is rescued if it comes into contact with the player character
        if(collision.transform.CompareTag("Player"))
        {
            WaveManager.Instance.RemoveHumanSurvivor(this);
            GameState.Instance.ScoreRescueSurvivor();
            Destroy(this.gameObject);
            //Play sound effect
            SoundEffectsPlayer.Instance.PlaySound("RescueHuman");
        }
        //Reflect the entitys current movement direction if they walk into one of the walls
        else if(collision.transform.CompareTag("Wall"))
        {
            Vector3 SurfaceNormal = collision.contacts[0].normal;
            CurrentDirection = Vector3.Reflect(CurrentDirection, SurfaceNormal);
        }
        //Turn and go back in the opposite direction if they walk into an electrode or a Grunt enemy
        else if(collision.transform.CompareTag("Electrode") || collision.transform.CompareTag("Electrode"))
        {
            CurrentDirection = -CurrentDirection;
        }
    }
}

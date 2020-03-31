// ================================================================================================================================
// File:        HulkAI.cs
// Description:	Controls the Hulk enemy AI, they cant be killed, player projectiles push them back, they seek random offset from 
//              the player (highly randomized), they block enemy projectiles, they kill the player and humans on contact
//              They can only walk in straight lines, it takes them a short moment to turn 90 degrees to change their direction
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

//We need to track which direction the Hulks are facing as they need to manually turn around
public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
}

public class HulkAI : HostileEntity
{
    //Movement/Targetting
    private Direction FacingDirection;  //Current direction the Hulk is facing, they can only move forward in this direction
    private float MoveSpeed = 1.25f;    //How fast the Hulk is able to move
    private Vector3 TargetPos;  //Hulks seek to a position randomly offset from where the player is located, updated periodically
    private Vector2 PosOffsetRange = new Vector2(0.25f, 1f);    //How far on each axis a new target pos may be offset from the players pos
    private float TargetUpdateInterval = 2.0f;  //How often the Hulk updates its movement target position
    private float NextTargetUpdate = 3.5f;  //How long until the Hulks next target position update
    private float TargetUpdateMinimumDistance = 3.5f;   //Distance between current target pos and player pos required to force trigger a new target location to be acquired
    private bool SeekingX = true;   //Which axis the Hulk is currently focused on travelling to move toward its target location
    private float ProjectilePushback = 0.75f;   //How far the Hulks are pushed back when hit by the players projectiles
    private Vector2 ValidXPosRange = new Vector2(-7.43f, 7.43f);    //Range of XPos values the Hulk may travel between while remaining inside the level bounds
    private Vector2 ValidYPosRange = new Vector2(-4.4f, 4.4f);  //Range of YPos values the Hulk my travel between while remaining inside the level bounds
    private Vector3 PreviousPos;    //Used to measure the Hulks distance travelled between frames

    //Turning
    private bool TurningClockwise = true;   //Tracks which direction the Hulk is turning in
    private float TurningInterval = 2.5f;   //How long it takes for the Hulk to change directions
    private float TurnTimeLeft = 2.5f;  //How long until the Hulk completes its current turning process
    private bool IsTurning = false; //True while the Hulk is performing its direction changing process
    private Direction TurningFrom = Direction.North;  //The direction the Hulk is currently turning away from
    private Direction TurningTo = Direction.North;    //The new direction the Hulk is currently turning to
    private float TurningAnimationInterval = 0.25f; //How often to flash between the current and next direction during the turning animation
    private float NextTurnSwap = 0.25f; //How long until the next flash between current and next directions
    private bool ShowingCurrentDirection = true;    //Tracks which direction if currently being viewed during the turning animation

    //Rendering/Animation
    public SpriteRenderer[] FrontRenderers;   //Renderers for viewing the Hulks front view sprites
    public SpriteRenderer[] SideRenderers;    //Renderers for viewing the Hulks side view sprites
    public Animator[] Animators;    //Animation Controllers for all the Hulks main body sprites

    private void Start()
    {
        //Acquire a new target location to seek towards
        NewTarget();
        //Set only the front view sprites to be shown
        SetFrontSpriteVisibility(true);
        SetSideSpriteVisibility(true);
    }

    private void Update()
    {
        //All logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Continue active turning process until its complete, movement disallowed until its done
        if(IsTurning)
        {
            TurnTimeLeft -= Time.deltaTime;
            if (TurnTimeLeft <= 0.0f)
                FinishTurning();
        }
        //Otherwise, allow normal movement
        else
        {
            UpdateTarget();
            SeekTarget();
        }

        //Manage which sprites are viewed, and their animations being played
        AnimateAndRender();
    }

    //Gets a new location offset from the player to seek towards
    private void NewTarget()
    {
        //Start with players pos
        TargetPos = GameState.Instance.Player.transform.position;

        //Offset the X and Y values by a random amount
        TargetPos.x = OffsetPosValue(TargetPos.x);
        TargetPos.y = OffsetPosValue(TargetPos.y);

        //Randomly decide which axis to start travelling on first
        SeekingX = Random.value >= 0.5f;
    }

    //Offsets a position value by a random amount
    private float OffsetPosValue(float PosValue)
    {
        //Decide whether to offset by a positive or negative amount (50-50% change either way)
        bool PositiveOffset = Random.value >= 0.5f;

        //Calculate a random amount to offset the original value by
        float OffsetAmount = Random.Range(PosOffsetRange.x, PosOffsetRange.y);

        //Return the new value offset by a random amount
        return PosValue += PositiveOffset ? OffsetAmount : -OffsetAmount;
    }

    //Starts the direction changing process
    private void StartTurning()
    {
        //Store which directions are being changed between
        TurningFrom = FacingDirection;
        TurningTo = GetNewDirection();
        //Start the turning process
        TurnTimeLeft = TurningInterval;
        IsTurning = true;
        NextTurnSwap = TurningAnimationInterval;
        ShowingCurrentDirection = true;
        FaceDirection(FacingDirection);
    }

    //Completes the direction changing process
    private void FinishTurning()
    {
        FacingDirection = TurningTo;
        IsTurning = false;
        FaceDirection(TurningTo);
    }

    //Returns the new direction the Hulk should be facing after completing its turn
    private Direction GetNewDirection()
    {
        switch (FacingDirection)
        {
            case (Direction.North):
                return TurningClockwise ? Direction.East : Direction.West;
            case (Direction.East):
                return TurningClockwise ? Direction.South : Direction.North;
            case (Direction.South):
                return TurningClockwise ? Direction.West : Direction.East;
            case (Direction.West):
                return TurningClockwise ? Direction.North : Direction.South;
            default:
                return Direction.North;
        }
    }

    //Periodically updates the Hulks target position
    private void UpdateTarget()
    {
        //Update the target whenever the timer expires
        NextTargetUpdate -= Time.deltaTime;
        if(NextTargetUpdate <= 0.0f)
        {
            //Reset the timer and check the distance between the current target and the player
            NextTargetUpdate = TargetUpdateInterval;
            float PlayerTargetDistance = Vector3.Distance(GameState.Instance.Player.transform.position, TargetPos);
            //Update the current target pos if the player has gove too far away from the current target
            if (PlayerTargetDistance >= TargetUpdateMinimumDistance)
                NewTarget();
        }
    }

    //Moves toward the current target pos
    private void SeekTarget()
    {
        //Get the distance from the Hulk and its current target pos on each axis
        float XDistance = Mathf.Abs(transform.position.x - TargetPos.x);
        float YDistance = Mathf.Abs(transform.position.y - TargetPos.y);

        //Get a new target whenever the Hulk gets close enough to its current one
        if (XDistance <= 0.25f && YDistance <= 0.25f)
        {
            NewTarget();
            //Recalculate the X and Y distance values from the new target pos
            XDistance = Mathf.Abs(transform.position.x - TargetPos.x);
            YDistance = Mathf.Abs(transform.position.y - TargetPos.y);
        }

        //If already travelling on X axis, and X distance is small enough, start travelling on the Y axis instead
        if (XDistance <= 0.25f && SeekingX)
            SeekingX = false;
        //Perform the same check on the Y axis
        else if (YDistance <= 0.25f && !SeekingX)
            SeekingX = true;

        //Travel toward the current target along whatever axis the Hulk is currently travelling
        if (SeekingX)
            SeekX();
        else
            SeekY();
    }

    //Seeks towards the current target along the X axis
    private void SeekX()
    {
        //Figure out which direction we should be moving on this axis
        bool ShouldMoveLeft = transform.position.x < TargetPos.x;

        //Check if we are facing the right direction to move this way
        bool CorrectFacing = (ShouldMoveLeft && FacingDirection == Direction.East) ||
            (!ShouldMoveLeft && FacingDirection == Direction.West);

        //Start the turning process if we are facing the wrong direction
        if (!CorrectFacing)
        {
            //Figure out which direction we need to turn in, then start the process
            if (ShouldMoveLeft)
                TurningClockwise = FacingDirection != Direction.South;
            else
                TurningClockwise = FacingDirection != Direction.North;
            StartTurning();
        }
        //Move forward if we are facing the right direction
        else
        {
            Vector3 MovementVector = new Vector3(ShouldMoveLeft ? MoveSpeed : -MoveSpeed, 0f, 0f);
            transform.position += MovementVector * Time.deltaTime;
        }
    }

    //Seeks towards the current target along the Y axis
    private void SeekY()
    {
        //Figure out which direction we should be moving on this axis
        bool ShouldMoveUp = transform.position.y < TargetPos.y;

        //Check if we are facing the right direction to move this way
        bool CorrectFacing = (ShouldMoveUp && FacingDirection == Direction.North) || (!ShouldMoveUp && FacingDirection == Direction.South);

        //Start the turning process if we are facing the wrong direction
        if(!CorrectFacing)
        {
            //Figure out which direction we need to turn in, then start the process
            if (ShouldMoveUp)
                TurningClockwise = FacingDirection != Direction.East;
            else
                TurningClockwise = FacingDirection != Direction.West;
            StartTurning();
        }
        //Move foward if we are facing the right direction
        else
        {
            Vector3 MovementVector = new Vector3(0f, ShouldMoveUp ? MoveSpeed : -MoveSpeed, 0f);
            transform.position += MovementVector * Time.deltaTime;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player character on contact
        if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Kill any humans on contact
        else if (collision.transform.CompareTag("Human"))
        {
            WaveManager.Instance.RemoveHumanSurvivor(collision.transform.GetComponent<BaseEntity>(), true);
            Destroy(collision.gameObject);
        }
        //Push the Hulk back when hit by players projectiles
        else if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Vector3 ShotDirection = collision.transform.GetComponent<ProjectileMovement>().GetDirectionVector();
            PushBack(ShotDirection);
            Destroy(collision.transform.gameObject);
        }
        //Destroy enemy projectiles which hit the Hulk
        else if (collision.transform.CompareTag("EnemyProjectile"))
            Destroy(collision.transform.gameObject);
        //Start travelling in another direction when colliding with another Hulk to avoid them getting stuck together
        else if (collision.transform.CompareTag("Hulk"))
            AvoidOtherHulk();
    }

    //Pushes the Hulk back a small amount when hit by one of the players projectiles
    private void PushBack(Vector3 ShotDirection)
    {
        //Get a new location to move the Hulk to, based on which direction the projectile was travelling that hit the Hulk
        Vector3 NewPos = transform.position + ShotDirection * ProjectilePushback * Time.deltaTime;
        //Make sure the new positions isnt outside the level boundaries
        NewPos.x = Mathf.Clamp(NewPos.x, ValidXPosRange.x, ValidXPosRange.y);
        NewPos.y = Mathf.Clamp(NewPos.y, ValidYPosRange.x, ValidYPosRange.y);
        //Move the Hulk to the new position
        transform.position = NewPos;
    }

    //Forces the Hulk to change which axis its currently travelling toward its target whenever it runs into another Hulk to stop them getting stuck together
    private void AvoidOtherHulk()
    {
        //Change which axis the Hulk is currently travelling
        SeekingX = !SeekingX;

        //Figure out which way the Hulk needs to rotate so it faces the direction needed to travel in the new direction
        if(SeekingX)
        {
            //First figure out which direction we want to be travelling on this axis to reach our target
            bool ShouldMoveLeft = transform.position.x <= TargetPos.x;
            //Now figure out which way the Hulk needs to rotate to travel in that direction
            if (ShouldMoveLeft)
                TurningClockwise = FacingDirection != Direction.South;
            else
                TurningClockwise = FacingDirection != Direction.North;
            StartTurning();
        }
        else
        {
            bool ShouldMoveUp = transform.position.y <= TargetPos.y;
            if (ShouldMoveUp)
                TurningClockwise = FacingDirection != Direction.East;
            else
                TurningClockwise = FacingDirection != Direction.West;
            StartTurning();
        }
    }

    //Toggles visibility of the front sprites
    private void SetFrontSpriteVisibility(bool ShouldRender)
    {
        foreach (SpriteRenderer Renderer in FrontRenderers)
            Renderer.forceRenderingOff = !ShouldRender;
    }

    //Toggles visibility of the side sprites
    private void SetSideSpriteVisibility(bool ShouldRender)
    {
        foreach (SpriteRenderer Renderer in SideRenderers)
            Renderer.forceRenderingOff = !ShouldRender;
    }

    //Toggles X Axis flipping of the side view sprites
    private void SetSideSpriteFlipping(bool ShouldFlip)
    {
        foreach (SpriteRenderer Renderer in SideRenderers)
            Renderer.flipX = ShouldFlip;
    }

    //Displays whatever animations are active and sets visibility for what sprites should be shown
    private void AnimateAndRender()
    {
        //Display turning animation during the turning process
        if (IsTurning)
            DisplayTurningAnimation();
        else
            DisplayMovementAnimations();
    }

    //Fips between the current facing direction and next facing direction until the turning process is completed
    private void DisplayTurningAnimation()
    {
        //Swap sprites every time the timer expires
        NextTurnSwap -= Time.deltaTime;
        if(NextTurnSwap <= 0.0f)
        {
            NextTurnSwap = TurningAnimationInterval;
            ShowingCurrentDirection = !ShowingCurrentDirection;
            FaceDirection(ShowingCurrentDirection ? FacingDirection : TurningTo);
        }
    }

    //Immediately starts viewing the Hulk as facing in the given direction
    private void FaceDirection(Direction NewDirection)
    {
        SetFrontSpriteVisibility(NewDirection == Direction.North || NewDirection == Direction.South);
        SetSideSpriteVisibility(NewDirection == Direction.East || NewDirection == Direction.West);
        SetSideSpriteFlipping(NewDirection == Direction.East);
    }

    //Manages display of movement animations while the Hulk is moving around the level
    private void DisplayMovementAnimations()
    {
        //Keep all animation controllers aware of when the Hulk is and isnt moving
        bool IsMoving = transform.position != PreviousPos;
        foreach (Animator AnimationController in Animators)
            AnimationController.SetBool("IsMoving", IsMoving);

        //Handle which sprites are displaying during movement
        if (IsMoving)
        {
            //Measure distance travelled on each axis since last update
            float XMovement = Mathf.Abs(transform.position.x - PreviousPos.x);
            float YMovement = Mathf.Abs(transform.position.y - PreviousPos.y);

            //Figure out which direction the Hulk is currently moving
            bool MovingVertical = YMovement > XMovement;
            bool MovingRight = transform.position.x > PreviousPos.x;

            //Set only the current sprites to be shown, and flip them if nessacery
            SetFrontSpriteVisibility(MovingVertical);
            SetSideSpriteVisibility(!MovingVertical);
            SetSideSpriteFlipping(MovingRight);
        }

        //Save position for next frames update
        PreviousPos = transform.position;
    }
}
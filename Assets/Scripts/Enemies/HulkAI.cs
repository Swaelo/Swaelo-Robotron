// ================================================================================================================================
// File:        HulkAI.cs
// Description:	Controls the Hulk enemy AI, they cant be killed, player projectiles push them back, they seek random offset from 
//              the player (highly randomized), they block enemy projectiles, they kill the player and humans on contact
//              They can only walk in straight lines, it takes them a short moment to turn 90 degrees to change their direction
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

//We need to track which direction the Hulks are facing as they need to manually turn around
public enum CardinalDirection
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
}

public class HulkAI : HostileEntity
{
    private float MoveSpeed = 1.25f;     //How fast the Hulk is able to move
    private GameObject PlayerTarget;    //The Hulks target
    private Vector3 CurrentTarget;      //Hulks seek random offset positions from the player, not the player directly
    private CardinalDirection CurrentDirection = CardinalDirection.North;   //Which direction the Hulk is currently facing / moving in
    private float TargetUpdateInterval = 2.0f;  //How often to check if the player has moved too far away from the current target location, requiring a new target location to be found
    private float NextTargetUpdate = 2.0f;  //How long until the Hulk checks if its target location needs to be updated
    private float TargetUpdateRequirement = 3.5f;   //Distance between player and target location required to trigger a new target to be made before reaching the current target
    private bool SeekingX = true;       //On which axis the Hulk is currently seeking toward its target location
    private float MinXPos = -7.429f;    //Left most position the hulk can move to
    private float MaxXPos = 7.422f;     //Right most position the hulk can move to
    private float MinYPos = -4.45f;     //Bottom most position the hulk can move to
    private float MaxYPos = 4.396f;     //Top most position the hulk can move to
    private HulkSpriteViewer SpriteViewer;  //Used to immediately force changing which sprite is being rendered for the Hulk once it finishes changing its facing direction
    private bool ChangingDirection = false; //The Hulk cannot move while its changing directions
    private bool TurningClockwise = true;   //Which direction the Hulk is currently turning
    private float DirectionChangeInterval = 2.5f;   //How long it takes to change directions
    private float DirectionChangeRemaining = 2.5f;  //How long until the current direction change completes
    private float ProjectilePushback = 0.75f;   //How far Hulks are pushed back when hit by the players projectiles

    private void Awake()
    {
        //Find and store reference to the Player character, and any components of the Hulk which we will need to access later
        PlayerTarget = GameObject.FindGameObjectWithTag("Player").gameObject;
        SpriteViewer = GetComponent<HulkSpriteViewer>();

        //Get an initial target location for the hulk to move toward
        NewTarget();
    }

    private void Update()
    {
        //All Enemy AI is disabled during the round warmup period
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        //Hulk cannot move or do anything while they are turning around
        if (ChangingDirection)
        {
            //Complete the turning process when the timer expires
            DirectionChangeRemaining -= Time.deltaTime;
            if (DirectionChangeRemaining <= 0.0f)
                FinishTurning();
        }
        //Otherwise allow the Hulk to continue as normal
        else
        {
            UpdateTarget();
            SeekTarget();
        }
    }

    //Creates a new target location for the Hulk to move toward which is a random offset from the players position
    private void NewTarget()
    {
        //Start off with the players current location
        Vector3 NewTargetLocation = PlayerTarget.transform.position;

        //Offset the X and Y locations by a random amount
        float XOffset = Random.Range(0.25f, 1f);
        bool XPositiveOffset = Random.value > 0.5f;
        NewTargetLocation.x += XPositiveOffset ? XOffset : -XOffset;
        float YOffset = Random.Range(0.25f, 1f);
        bool YPositiveOffset = Random.value > 0.5f;
        NewTargetLocation.y += YPositiveOffset ? YOffset : -YOffset;

        //Ensure the new location is kept within the borders of the screen
        NewTargetLocation.x = Mathf.Clamp(NewTargetLocation.x, MinXPos, MaxXPos);
        NewTargetLocation.y = Mathf.Clamp(NewTargetLocation.y, MinYPos, MaxYPos);

        //Set this as the Hulks new target location
        CurrentTarget = NewTargetLocation;

        //Decide which axis will be seeked toward first
        SeekingX = Random.value > 0.5f;
    }

    //Periodically checks if the player has wandered too far from the current target, forcing a new target to be acquired before reaching the current target
    private void UpdateTarget()
    {
        //Decrement the timer until the next check needs to be made
        NextTargetUpdate -= Time.deltaTime;

        if(NextTargetUpdate <= 0.0f)
        {
            //Reset the timer
            NextTargetUpdate = TargetUpdateInterval;

            //Check the players distance from the current target location
            float PlayerTargetDistance = Vector3.Distance(PlayerTarget.transform.position, CurrentTarget);

            //Force a new target position to be acquired if the player has wandered too far away
            if (PlayerTargetDistance >= TargetUpdateRequirement)
                NewTarget();
        }
    }

    //Moves towards the current target position
    private void SeekTarget()
    {
        //Get the current difference between the Hulks X and Y position, compared to its target X and Y position
        float XDiff = Mathf.Abs(transform.position.x - CurrentTarget.x);
        float YDiff = Mathf.Abs(transform.position.y - CurrentTarget.y);

        //If both close to the target position in both X and Y axis then we need to find a new target to seek toward
        if(XDiff <= 0.25f && YDiff <= 0.25f)
        {
            NewTarget();
            return;
        }

        //If we are currently SeekingX, but already close enough on the X axis then we should change to seeking Y
        if (XDiff <= 0.25f && SeekingX)
            SeekingX = false;
        //Do the same thing for the Y axis
        else if (YDiff <= 0.25f && !SeekingX)
            SeekingX = true;

        if (SeekingX)
            SeekX();
        else
            SeekY();
    }

    //Seeks towards the current target X location
    private void SeekX()
    {
        //Figure out if we should be moving left or right
        bool MovingLeft = transform.position.x < CurrentTarget.x;

        //Figure out if we are currently facing the correct way to move in this direction
        bool CorrectFacing = (MovingLeft && CurrentDirection == CardinalDirection.East) ||
            (!MovingLeft && CurrentDirection == CardinalDirection.West);

        //If we are facing the wrong direction then we need to start turning
        if(!CorrectFacing)
        {
            //Figure out which direction we need to turn, then begin the process
            if (MovingLeft)
                TurningClockwise = CurrentDirection != CardinalDirection.South;
            else
                TurningClockwise = CurrentDirection != CardinalDirection.North;
            StartTurning();
        }

        //Move forward if we are facing the correct direction
        Vector3 MovementVector = new Vector3(MovingLeft ? MoveSpeed : -MoveSpeed, 0f, 0f);
        transform.position += MovementVector * Time.deltaTime;
    }

    //Seeks towards the current target Y location
    private void SeekY()
    {
        //Figure out if we should be moving up or down
        bool MovingUp = transform.position.y < CurrentTarget.y;

        //Figure out if we are currently facing the correct way to move in this direction
        bool CorrectFacing = (MovingUp && CurrentDirection == CardinalDirection.North) ||
            (!MovingUp && CurrentDirection == CardinalDirection.South);

        //IF we are facing the wrong direction then we need to start turning
        if(!CorrectFacing)
        {
            //Figure out which direction we need to turn, then begin the process
            if (MovingUp)
                TurningClockwise = CurrentDirection != CardinalDirection.East;
            else
                TurningClockwise = CurrentDirection != CardinalDirection.West;
            StartTurning();
        }

        //Move forward if we are facing the correct direction
        Vector3 MovementVector = new Vector3(0f, MovingUp ? MoveSpeed : -MoveSpeed, 0f);
        transform.position += MovementVector * Time.deltaTime;
    }

    //Changes the Hulks direction to avoid running into other Hulks
    private void AvoidAlly()
    {
        //Change to seeking on the other axis than what is current
        SeekingX = !SeekingX;

        //Figure out which way we need to turn to begin moving along this new axis
        if(SeekingX)
        {
            //Figure out if we should be moving left or right to reach our target on the X axis
            bool MovingLeft = transform.position.x <= CurrentTarget.x;
            //Find which way we need to turn to face toward our target on this axis
            if (MovingLeft)
                TurningClockwise = CurrentDirection != CardinalDirection.South;
            else
                TurningClockwise = CurrentDirection != CardinalDirection.North;
            //Begin the turning process
            StartTurning();
        }
        else
        {
            //Figure out if we should be moving up or down to reach our target on the Y axis
            bool MovingUp = transform.position.y <= CurrentTarget.y;
            //Find which way we need to turn to face toward our target on this axis
            if (MovingUp)
                TurningClockwise = CurrentDirection != CardinalDirection.East;
            else
                TurningClockwise = CurrentDirection != CardinalDirection.West;
            //Begin the turning process
            StartTurning();
        }
    }

    //Begins the process of turning the Hulk 90 degrees either clockwise or counterclockwise to change what direction they are facing
    private void StartTurning()
    {
        //Start the turning process
        ChangingDirection = true;
        DirectionChangeRemaining = DirectionChangeInterval;
        //Instruct the sprite view to begin the turning animation
        CardinalDirection NextDirection = GetNewDirection(TurningClockwise);
        SpriteViewer.StartTurning(CurrentDirection, NextDirection);
    }

    //Completes the process of turning the Hulk to change what direction its facing
    private void FinishTurning()
    {
        //Terminate the turning process and update the facing direction
        ChangingDirection = false;
        CurrentDirection = GetNewDirection(TurningClockwise);
        //Instruct the sprite viewer to stop the turning animation and start viewing the new facing direction
        SpriteViewer.StopTurning();
    }

    //Returns the new cardinal direction the Hulk will be facing after completing its turn
    private CardinalDirection GetNewDirection(bool TurningClockwise)
    {
        switch(CurrentDirection)
        {
            case (CardinalDirection.North):
                return TurningClockwise ? CardinalDirection.East : CardinalDirection.West;
            case (CardinalDirection.East):
                return TurningClockwise ? CardinalDirection.South : CardinalDirection.North;
            case (CardinalDirection.South):
                return TurningClockwise ? CardinalDirection.West : CardinalDirection.East;
            case (CardinalDirection.West):
                return TurningClockwise ? CardinalDirection.North : CardinalDirection.South;
            default:
                return CardinalDirection.North;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player character on contact
        if(collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Kill any human survivors on contact
        else if(collision.transform.CompareTag("Human"))
        {
            WaveManager.Instance.RemoveHumanSurvivor(collision.transform.GetComponent<BaseEntity>());
            Destroy(collision.gameObject);
        }
        //Have the Hulk get pushed back if its hit by any of the players projectiles
        else if(collision.transform.CompareTag("PlayerProjectile"))
        {
            //Fetch the direction the projectile was moving when it hit us, the Hulk will be pushed in that direction
            Vector3 ShotDirection = collision.transform.GetComponent<ProjectileMovement>().GetDirectionVector();
            //Get a new position value to move the Hulk to after its pushed by this projectile
            Vector3 NewPosition = transform.position + ShotDirection * ProjectilePushback * Time.deltaTime;
            NewPosition.x = Mathf.Clamp(NewPosition.x, MinXPos, MaxXPos);
            NewPosition.y = Mathf.Clamp(NewPosition.y, MinYPos, MaxYPos);
            //Apply the new position to the Hulk
            transform.position = NewPosition;
            //Destroy the projectile
            Destroy(collision.transform.gameObject);
        }
        //Simply destroy any enemy projectiles which hit the Hulk
        else if(collision.transform.CompareTag("EnemyProjectile"))
        {
            Destroy(collision.transform.gameObject);
        }
        //Cause Hulks to start moving on the opposite axis if they run into each other to avoid getting stuck together
        else if(collision.transform.CompareTag("Hulk"))
        {
            AvoidAlly();
        }
    }
}
﻿// ================================================================================================================================
// File:        GruntAI.cs
// Description:	Grunts slowly move toward the player like a magnet
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class GruntAI : HostileEntity
{
    //Grunts are only able to move in the 8 diagonal directions defined here
    private Vector3[] MovementDirections =
    {
        new Vector3(0, 1, 0),   //North
        new Vector3(1, 1, 0),   //North-East
        new Vector3(1, 0, 0),   //East
        new Vector3(1, -1, 0), //South-East
        new Vector3(0, -1, 0), //South
        new Vector3(-1, -1, 0),    //South-West
        new Vector3(-1, 0, 0),  //West
        new Vector3(-1, 1, 0)  //North-West
    };
    public float InitialMoveSpeed = 0.65f; //Grunts move speed at the start of a new round
    public float CurrentMoveSpeed = 0.65f; //Grunts current move speed, increases over the course of the current round
    public float MaxMoveSpeed = 2.5f;   //Maximum speed a Grunt can reach if left alive long enough
    private float MoveSpeedIncreaseRate = 0.123333f;   //How much the Grunts movement speed is increased per second, 15 seconds after the round begins they will reach maximum speed
    private Animator AnimationController;   //Used to control what animations are being played for the enemy
    private GameObject PlayerTarget;    //The Grunts target
    private bool IsAlive = true;        //Set to false once killed by the player while the Grunts death animation is played out
    private float DeathAnimationRemaining = .5f;   //Time left until the death animation is finished playing out
    private Vector3 PreviousPosition;   //Grunts position over time is measured and sent to the animator to transition between idle/walk animation
    private float StepSoundDistance = 1.5f; //How much distance must be travelled by the Grunt between it again playing its footstep sound effect
    private float NextStepSound = 1.5f; //Distance left to travel until the foot step sound is played again

    private void Start()
    {
        //Find and store reference to the player character
        PlayerTarget = GameObject.FindGameObjectWithTag("Player").gameObject;
        //Find and store reference to any of the Grunts components that will be used later
        AnimationController = GetComponent<Animator>();
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Seek player while alive
        if (IsAlive)
            SeekPlayer();
        //Play out death animation then destroy self while dead
        else
            PlayDeath();

        //Tell the animator if the Grunt is currently moving
        bool IsMoving = PreviousPosition != transform.position;
        AnimationController.SetBool("IsMoving", IsMoving);

        //Continuously increase the Grunts movement speed until it reaches the max
        if (CurrentMoveSpeed < MaxMoveSpeed)
            CurrentMoveSpeed += MoveSpeedIncreaseRate * Time.deltaTime;

        //Remove distance travelled from next step sound, then play the sound when its been reached
        float DistanceTravelled = Vector3.Distance(transform.position, PreviousPosition);
        NextStepSound -= DistanceTravelled;
        if(NextStepSound <= 0.0f)
        {
            NextStepSound = StepSoundDistance;
            SoundEffectsPlayer.Instance.PlaySound("GruntStep");
        }

        //Store position for next frame
        PreviousPosition = transform.position;
    }

    private void SeekPlayer()
    {
        //Get the direction from this Grunt to the player character
        Vector3 PlayerDirection = Vector3.Normalize(PlayerTarget.transform.position - transform.position);

        //Find the valid movement direction which is most similar to the direction from the Grunt to the Player
        Vector3 MovementDirection = GetMovementDirection(PlayerDirection);

        //Move in this direction to get closer to the player character
        transform.position += MovementDirection * CurrentMoveSpeed * Time.deltaTime;
    }

    //Returns the valid movement direction that is closest to the direction the Grunt needs to travel to reach the player
    private Vector3 GetMovementDirection(Vector3 PlayerDirection)
    {
        //Start by finding the angle between the player direction and north
        Vector3 ClosestDirection = MovementDirections[0];
        float ClosestAngle = Vector3.Angle(PlayerDirection, ClosestDirection);

        //Now check this against all the other movement direction to find which out of all of them is the closest to the player direction
        for(int i = 1; i < 8; i++)
        {
            //Find the angle between the players direction and each of the valid movement directions
            Vector3 CompareDirection = MovementDirections[i];
            float CompareAngle = Vector3.Angle(PlayerDirection, CompareDirection);

            //Update the values if this is closer to the target direction than what we have currently
            if(CompareAngle < ClosestAngle)
            {
                ClosestDirection = CompareDirection;
                ClosestAngle = CompareAngle;
            }
        }

        //Return whatever was found to be the closest movement direction when compared to the players direction from the Grunt
        return ClosestDirection;
    }

    private void PlayDeath()
    {
        //Decrement the death animation timer
        DeathAnimationRemaining -= Time.deltaTime;

        //Destroy self once the death animation has played out
        if (DeathAnimationRemaining <= 0.0f)
            Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if we collide with them
        if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Destroy any player projectiles which hit the grunt, killing the Grunt
        else if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            Die();
        }
        //Kill the Grunt if it comes into contact with any Electrodes
        else if (collision.transform.CompareTag("Electrode"))
            Die();
    }

    //Triggers the death animation to be played before destroying the Grunt
    private void Die()
    {
        //Tell the wave manager this enemy is now dead, and award points for killing it
        GameState.Instance.IncreaseScore((int)PointValue.Grunt);
        WaveManager.Instance.EnemyDead(this);
        //Flag the enemy as dead and start playing its death animation
        IsAlive = false;
        AnimationController.SetBool("IsDead", true);
        //Destroy the rigidbody and the box collider component as they are no longer needed
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
        //Play sound effect
        SoundEffectsPlayer.Instance.PlaySound("GruntDie", 0.45f);
    }
}

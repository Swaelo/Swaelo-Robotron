// ================================================================================================================================
// File:        GruntAI.cs
// Description:	Grunts slowly move toward the player like a magnet
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class GruntAI : MonoBehaviour
{
    public float InitialMoveSpeed = 1f; //Grunts move speed at the start of a new round
    public float CurrentMoveSpeed = 1f; //Grunts current move speed, increases over the course of the current round
    public float MaxMoveSpeed = 3.5f;   //Maximum speed a Grunt can reach if left alive long enough
    private Rigidbody2D Body;           //Rigidbody component used to move the Grunt around
    private Animator AnimationController;   //Used to control what animations are being played for the enemy
    private GameObject PlayerTarget;    //The Grunts target
    private bool IsAlive = true;        //Set to false once killed by the player while the Grunts death animation is played out
    private float DeathAnimationDuration = 1f; //Total time it takes for the grunts death animation to play
    private float DeathAnimationRemaining = 1f;   //Time left until the death animation is finished playing out
    private Vector3 PreviousPosition;   //Grunts position over time is measured and sent to the animator to transition between idle/walk animation

    private void Awake()
    {
        //Find and store reference to the player character
        PlayerTarget = GameObject.FindGameObjectWithTag("Player").gameObject;
        //Find and store reference to any of the Grunts components that will be used later
        Body = GetComponent<Rigidbody2D>();
        AnimationController = GetComponent<Animator>();
    }

    private void Update()
    {
        //Seek player while alive
        if (IsAlive)
            SeekPlayer();
        //Play out death animation then destroy self while dead
        else
            PlayDeath();

        //Tell the animator if the Grunt is currently moving, then store position for next frame
        bool IsMoving = PreviousPosition != transform.position;
        AnimationController.SetBool("IsMoving", IsMoving);
        PreviousPosition = transform.position;
    }

    private void SeekPlayer()
    {
        //Get the direction from this Grunt to the player character
        Vector3 PlayerDirection = Vector3.Normalize(PlayerTarget.transform.position - transform.position);

        //Move towards the player
        transform.position += PlayerDirection * CurrentMoveSpeed * Time.deltaTime;
    }

    private void PlayDeath()
    {
        //Decrement the death animation timer
        DeathAnimationRemaining -= Time.deltaTime;

        //Destroy self once the death animation has played out
        if (DeathAnimationRemaining <= 0.0f)
            GameObject.Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if we collide with them
        if (collision.transform.CompareTag("Player"))
            Debug.Log("Player killed by Grunt!");
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
        //Flag the enemy as dead and start playing its death animation
        IsAlive = false;
        DeathAnimationRemaining = DeathAnimationDuration;
        AnimationController.SetTrigger("Death");
        //Destroy the rigidbody and the box collider component as they are no longer needed
        Destroy(Body);
        Destroy(GetComponent<BoxCollider2D>());
    }
}

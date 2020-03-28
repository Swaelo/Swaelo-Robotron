// ================================================================================================================================
// File:        ProgAI.cs
// Description:	Prog AI is pretty much exactly the same as Grunt AI
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ProgAI : HostileEntity
{
    //Progs are only able to move in a straight like, similar to the Hulks, but they dont need to take time to turn around
    private Vector3[] MovementDirections =
    {
        new Vector3(0,1,0), //North
        new Vector3(1,0,0), //East
        new Vector3(0,-1,0), //South
        new Vector3(-1,0,0) //West
    };
    private float MoveSpeed = 2.35f;    //How fast Progs move toward the player character
    private GameObject PlayerTarget;    //The Progs target

    //Living/Dead status and death animation playback
    private bool IsAlive = true;    //Set to false once the prog is killed, but waiting for the death animation to finish playing before it destroys itself
    private float DeathAnimationLeft = 0.417f;  //Seconds remaining until the death animation finishes playing
    public Animator[] AnimationControllers;    //All the animation controllers that need to have their death animations called when the Prog dies

    private void Start()
    {
        //Store reference to the player character
        PlayerTarget = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        //All Enemy AI is disabled during the round warmup
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        //Seek player while alive
        if (IsAlive)
            SeekPlayer();
        //Play death animation otherwise
        else
            PlayDeath();
    }

    //Plays out the death animation then has the Prog destroy itself once its completed
    private void PlayDeath()
    {
        //Wait for the death animation to finish playing
        DeathAnimationLeft -= Time.deltaTime;
        if(DeathAnimationLeft <= 0.0f)
        {
            //Tell the wave manager this target enemy is now dead
            WaveManager.Instance.TargetEnemyDead(this);
            //Destroy all the trail sprites
            for (int i = 1; i < 6; i++)
                Destroy(AnimationControllers[i].gameObject);
            for (int i = 7; i < 12; i++)
                Destroy(AnimationControllers[i].gameObject);
            //Finally, destroy the Prog object itself
            Destroy(gameObject);
        }
    }

    //Seeks towards the player characters location
    private void SeekPlayer()
    {
        //Grab the player if we dont have a reference to them for some reason
        if (PlayerTarget == null)
            PlayerTarget = GameObject.FindGameObjectWithTag("Player");

        //Get the direction to the player, then use whatever valid movement direction is closest to that and move in that direction
        Vector3 PlayerDirection = Vector3.Normalize(PlayerTarget.transform.position - transform.position);
        Vector3 MovementDirection = GetMovementDirection(PlayerDirection);
        transform.position += MovementDirection * MoveSpeed * Time.deltaTime;
    }
     
    //Returns whatever valid movement direction is most similar to the given direction vector
    private Vector3 GetMovementDirection(Vector3 PlayerDirection)
    {
        //Start by finding the angle between the player direction and the north movement direction
        Vector3 ClosestDirection = MovementDirections[0];
        float ClosestAngle = Vector3.Angle(PlayerDirection, ClosestDirection);

        //Check this against the other valid directions, finding which out of them all is the closests
        for(int i = 1; i < 4; i++)
        {
            //Find the angle between the players direction and this movement direction
            Vector3 CompareDirection = MovementDirections[i];
            float CompareAngle = Vector3.Angle(PlayerDirection, CompareDirection);

            //Update this as the closest if its more similar
            if(CompareAngle < ClosestAngle)
            {
                ClosestDirection = CompareDirection;
                ClosestAngle = CompareAngle;
            }
        }

        //Return whatever was found to be the best movement direction
        return ClosestDirection;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if we collide with them
        if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Destroy and player projectiles which hit the Prog, also killing the Prog
        else if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            Die();
        }
    }

    //Starts playing the death animation and disables all physics and AI during its playback
    private void Die()
    {
        GameState.Instance.IncreaseScore((int)PointValue.Prog);
        IsAlive = false;
        foreach (Animator AnimationController in AnimationControllers)
            AnimationController.SetTrigger("Death");
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<BoxCollider2D>());
    }
}
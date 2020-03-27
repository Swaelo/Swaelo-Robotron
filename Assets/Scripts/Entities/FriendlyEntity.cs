// ================================================================================================================================
// File:        FriendlyEntity.cs
// Description:	Controls movement and collisions of all human survivors
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class FriendlyEntity : BaseEntity
{
    private float MoveSpeed = 0.65f;    //How fast the human survivors wander around the level
    private Vector3 CurrentDirection;   //Current direction the entity is wandering in
    private Vector2 WanderRange = new Vector2(0.15f, 3f);   //Range of seconds that a survive may wander in its current direction before deciding on a new direction
    private float WanderRemaining;  //Time left to wander in the current direction before finding a new direction to move in
    
    private void Awake()
    {
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
        //All AI disabled during round warmup period
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        //Keep wandering around
        WanderAround();
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //The entity is rescued if it comes into contact with the player character
        if(collision.transform.CompareTag("Player"))
        {
            WaveManager.Instance.RemoveHumanSurvivor(this);
            GameState.Instance.ScoreRescueSurvivor();
            Destroy(this.gameObject);
        }
        //Reflect the entitys current movement direction if they walk into one of the walls
        else if(collision.transform.CompareTag("Wall"))
        {
            Vector3 SurfaceNormal = collision.contacts[0].normal;
            CurrentDirection = Vector3.Reflect(CurrentDirection, SurfaceNormal);
        }
        //Turn and go back in the opposite direction if they walk into an electrode
        else if(collision.transform.CompareTag("Electrode"))
        {
            CurrentDirection = -CurrentDirection;
        }
    }
}

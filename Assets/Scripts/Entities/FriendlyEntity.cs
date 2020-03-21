// ================================================================================================================================
// File:        FriendlyEntity.cs
// Description:	Controls movement and collisions of all human survivors
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class FriendlyEntity : BaseEntity
{
    private float MoveSpeed = 0.65f;    //How fast the friendly entities wander around the level
    private Vector3 CurrentTarget;  //Current target for the entity to wander towards
    private float WanderDistance = 3.5f;    //How far in each direction a new target may be selected from for the entity to wander to
    private Vector2 XBounds = new Vector2(-7f, 7f); //Value range that friendlies can wander to along the X axis
    private Vector2 YBounds = new Vector2(-4f, 4f); //Value range that friendlies can wander to along the Y axis
    

    private void Awake()
    {
        //Get an initial target location for the entity to wander towards
        CurrentTarget = NewTarget();
    }

    //Selects a new target position for the entity to move towards
    private Vector3 NewTarget()
    {
        //Start with the entities current position
        Vector3 NewTargetPos = transform.position;

        //Offset this position on each axis
        NewTargetPos.x += Random.Range(-WanderDistance, WanderDistance);
        NewTargetPos.y += Random.Range(-WanderDistance, WanderDistance);

        //Make sure the new location remains inside the level boundaries
        NewTargetPos.x = Mathf.Clamp(NewTargetPos.x, XBounds.x, XBounds.y);
        NewTargetPos.y = Mathf.Clamp(NewTargetPos.y, YBounds.x, YBounds.y);

        //Return the new target location
        return NewTargetPos;
    }

    private void Update()
    {
        //All Human AI is disabled during the round warmup period
        if (WaveManager.Instance.RoundWarmingUp)
            return;

        //Check the distance from the current target position
        float TargetDistance = Vector3.Distance(transform.position, CurrentTarget);

        //Grab a new wander target once we are close enough to the current target
        if (TargetDistance <= 1f)
            CurrentTarget = NewTarget();

        //Seek towards the current target location
        SeekTarget();
    }

    //Seeks towards the current wander target location
    private void SeekTarget()
    {
        //Get the direction to the current target location
        Vector3 TargetDirection = Vector3.Normalize(CurrentTarget - transform.position);

        //Move in that direction
        transform.position += TargetDirection * MoveSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Friendly entities are rescued if they come into contact with the player character
        if(collision.transform.CompareTag("Player"))
        {
            WaveManager.Instance.RemoveHumanSurvivor(this);
            GameState.Instance.ScoreRescueSurvivor();
            Destroy(this.gameObject);
        }
    }
}

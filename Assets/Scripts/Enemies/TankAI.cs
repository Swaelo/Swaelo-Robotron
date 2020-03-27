// ================================================================================================================================
// File:        TankAI.cs
// Description:	Tanks wander around the level sporadically while firing tank shells at the player
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public class TankAI : HostileEntity
{
    //Movement/Targetting
    private float MoveSpeed = 2.5f; //How fast the Quark moves around the level
    private Vector3 CurrentTarget;  //Position the Quark is currently moving towards
    private Vector2 TargetRangeOffset = new Vector2(3.5f, 7.5f);    //How far in each direction a Quarks new target location can be from its current position
    private Vector2 XWanderRange = new Vector2(-7f, 7f);    //Position range on the X axis where the Quark is able to wander to
    private Vector2 YWanderRange = new Vector2(-4f, 4f);    //Position range on the Y axis where the Quark is able to wander to
    private float NewTargetRange = 1.5f;    //How close the Quark must be from its current target before it picks up a new target location to seek to

    //Firing
    public GameObject TankShellPrefab;  //Projectile which is fired at the player
    private Vector2 ShotCooldownRange = new Vector2(0.5f, 1.75f); //How often projectiles can be fired
    private float ShotCooldownRemaining = 0.5f; //Time left until another projectile can be fired
    private Vector2 AimOffsetRange = new Vector2(0.75f, 1.25f); //How far from the Player to offset where the projectiles are aimed
    private List<GameObject> ActiveShellProjectiles = new List<GameObject>();   //Keep a list of active shell projectiles which have been fired by this Tank

    private void Awake()
    {
        //Get a random target location to seek towards
        GetNewTarget();
    }

    private void Update()
    {
        SeekTarget();
        FireProjectiles();
    }

    //Moves towards the current target location
    private void SeekTarget()
    {
        //Seek towards current target
        Vector3 TargetDirection = Vector3.Normalize(CurrentTarget - transform.position);
        transform.position += TargetDirection * MoveSpeed * Time.deltaTime;

        //Fetch a new target when we reach the current one
        float TargetDistance = Vector3.Distance(CurrentTarget, transform.position);
        if (TargetDistance <= NewTargetRange)
            GetNewTarget();
    }

    //Gets a new random target location away from the Quark to move towards
    private void GetNewTarget()
    {
        //Start with the current position
        Vector3 NewTarget = transform.position;

        //Offset from this position by a random amount on each axis
        NewTarget.x += Random.value >= 0.5f ?
            Random.Range(TargetRangeOffset.x, TargetRangeOffset.y) :
            Random.Range(-TargetRangeOffset.x, -TargetRangeOffset.y);
        NewTarget.y += Random.value >= 0.5f ?
            Random.Range(TargetRangeOffset.x, TargetRangeOffset.y) :
            Random.Range(-TargetRangeOffset.x, -TargetRangeOffset.y);

        //Make sure the new location stays inside the level boundaries
        NewTarget.x = Mathf.Clamp(NewTarget.x, XWanderRange.x, XWanderRange.y);
        NewTarget.y = Mathf.Clamp(NewTarget.y, YWanderRange.x, YWanderRange.y);

        //Set this location as the new target
        CurrentTarget = NewTarget;
    }

    //Fires projectiles towards the player
    private void FireProjectiles()
    {
        //Wait for the firing cooldown to reset
        ShotCooldownRemaining -= Time.deltaTime;
        if(ShotCooldownRemaining <= 0.0f)
        {
            //Reset the cooldown timer
            ShotCooldownRemaining = Random.Range(ShotCooldownRange.x, ShotCooldownRange.y);

            //50% of shots are fired toward the player, the other 50% aim to hit the player after rebounding off a wall
            bool DirectShot = Random.Range(1, 100) >= 50;
            if (DirectShot)
            {
                //Grab a location slightly offset from the player where we will aim the projectile, and find the direction to that location
                Vector3 ShotTarget = GetPlayerPositionOffset(AimOffsetRange);
                Vector3 ShotDirection = Vector3.Normalize(ShotTarget - transform.position);

                //Spawn a new tank shell and fire it in that direction
                Vector3 ShellSpawnPos = transform.position + ShotDirection * 0.5f;
                GameObject TankShell = Instantiate(TankShellPrefab, ShellSpawnPos, Quaternion.identity);

                //Store the shell projectile in the list with all the others
                ActiveShellProjectiles.Add(TankShell);

                //Tell the shot its initial movement direction
                TankShell.GetComponent<TankShellAI>().InitializeProjectile(ShotDirection, this);
            }
        }
    }

    //Returns a position offset from the players location by some random amount within the allowed value range
    private Vector3 GetPlayerPositionOffset(Vector2 OffsetRange)
    {
        //Get 2 random values to offset from on the X and Y axis
        float XOffset = Random.Range(OffsetRange.x, OffsetRange.y);
        float YOffset = Random.Range(OffsetRange.x, OffsetRange.y);

        //Decide in which direction to apply these offset values
        bool PositiveXOffset = Random.value >= 0.5f;
        bool PositiveYOffset = Random.value >= 0.5f;

        //Create a new vector by applying these offset values to the players current location
        Vector3 PositionOffset = GameState.Instance.Player.transform.position;
        PositionOffset.x += PositiveXOffset ? XOffset : -XOffset;
        PositionOffset.y += PositiveYOffset ? YOffset : -YOffset;
        return PositionOffset;
    }

    //Handle collisions with certain other objects and entities that we come into contact with
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Player projectiles kill the Tank and are destroyed on impact
        if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            CleanProjectiles();
            GameState.Instance.IncreaseScore((int)PointValue.Tank);
            WaveManager.Instance.TargetEnemyDead(this);
            Destroy(gameObject);
        }
        //Player character is killed on contact
        else if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }

    //Shell projectiles fired by this Tank will alert it when they are going to be destroyed so they can be removed from the tracking list
    public void TankShellDestroyed(GameObject DestroyedTankShell)
    {
        //Remove the shell from the tracking list if its listed there
        if (ActiveShellProjectiles.Contains(DestroyedTankShell))
            ActiveShellProjectiles.Remove(DestroyedTankShell);
    }

    //Cleans up any active tank shell projectiles which have been fired by this Tank
    public void CleanProjectiles()
    {
        foreach (GameObject TankShell in ActiveShellProjectiles)
            if (TankShell != null)
                Destroy(TankShell);
        ActiveShellProjectiles.Clear();
    }
}

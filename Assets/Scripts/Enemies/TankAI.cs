// ================================================================================================================================
// File:        TankAI.cs
// Description:	Tanks wander around the level sporadically while firing tank shells at the player
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

//Tanks will target one of the walls and attempt to bounce their shot off the wall to hit the player
public enum WallTarget
{
    TopWall = 0,
    BottomWall = 1,
    LeftWall = 2,
    RightWall = 3
}

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
    private int MaxActiveShots = 3; //Maximum number of tank shots each Tank may have active at any 1 time

    private void Awake()
    {
        //Get a random target location to seek towards
        GetNewTarget();
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

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

            //Count how many active shots the Tank currents has
            int ActiveShots = 0;
            foreach (GameObject ActiveShell in ActiveShellProjectiles)
                if (ActiveShell != null)
                    ActiveShots++;

            //Exit out if this tank already has the maximum amount of shots active
            if (ActiveShots >= MaxActiveShots)
                return;

            //Play sound
            SoundEffectsPlayer.Instance.PlaySound("FireTankShell");
    
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
            //The non direct shots will aim to hit the player after bouncing off one of the walls
            else
            {
                //Get the wall which is closest to both the Player and the Tank which we are going to use to bounce our shot off of
                WallTarget ClosestWall = GetClosestWall();
                //Find the location along that wall which is in between the Tank and Players location, then the direction to that location on the wall
                Vector3 WallTarget = GetWallTarget(ClosestWall);
                Vector3 ShotDirection = Vector3.Normalize(WallTarget - transform.position);
                //Spawn the new projectile, store it in the list with the others, then launch it toward that spot on the wall
                GameObject TankShell = Instantiate(TankShellPrefab, transform.position, Quaternion.identity);
                ActiveShellProjectiles.Add(TankShell);
                TankShell.GetComponent<TankShellAI>().InitializeProjectile(ShotDirection, this);
            }
        }
    }

    //Returns the position on the wall where the shot should be aimed so it will bounce of and hit the player
    private Vector3 GetWallTarget(WallTarget Wall)
    {
        //First get the players current position to quick access
        Vector3 PlayerPos = GameState.Instance.Player.transform.position;

        //Measure the half distance between the Player and the Tank on both the X and Y axis
        float HalfXDistance = Mathf.Abs(PlayerPos.x - transform.position.x) * 0.5f;
        float HalfYDistance = Mathf.Abs(PlayerPos.y - transform.position.y) * 0.5f;

        //Find which side of the Tank the Player is
        bool PlayerRightOfTank = PlayerPos.x > transform.position.x;
        bool PlayerAboveTank = PlayerPos.y > transform.position.y;

        //Now find the position along the target wall directly between the Tank and Player
        Vector3 WallPos = transform.position;
        switch(Wall)
        {
            case (WallTarget.TopWall):
                WallPos.x += PlayerRightOfTank ? HalfXDistance : -HalfXDistance;
                WallPos.y = 4.5f;
                break;
            case (WallTarget.BottomWall):
                WallPos.x += PlayerRightOfTank ? HalfXDistance : -HalfXDistance;
                WallPos.y = -4.5f;
                break;
            case (WallTarget.LeftWall):
                WallPos.x = -7.5f;
                WallPos.y += PlayerAboveTank ? HalfYDistance : -HalfYDistance;
                break;
            case (WallTarget.RightWall):
                WallPos.x = 7.5f;
                WallPos.y += PlayerAboveTank ? HalfYDistance : -HalfYDistance;
                break;
        }

        //Return the final target location where the projectile should be aimed to hit the player on a rebound
        return WallPos;
    }

    //Figures out which of the 4 walls is closests in relation to both the Tank and the Player
    private WallTarget GetClosestWall()
    {
        Vector3 PlayerPos = GameState.Instance.Player.transform.position;

        //First check the top walls relative distance
        float TankTopDistance = Mathf.Abs(transform.position.y - 4.5f);
        float PlayerTopDistance = Mathf.Abs(PlayerPos.y - 4.5f);
        float RelativeTopDistance = (TankTopDistance + PlayerTopDistance) / 2f;

        //Start by assuming this is the wall that is closest
        WallTarget ClosestWall = WallTarget.TopWall;
        float ClosestWallDistance = RelativeTopDistance;

        //Now check the bottom walls relative distance and compare that against the top
        float TankBottomDistance = Mathf.Abs(transform.position.y - -4.5f);
        float PlayerBottomDistance = Mathf.Abs(PlayerPos.y - -4.5f);
        float RelativeBottomDistance = (TankBottomDistance + PlayerBottomDistance) / 2f;
        if (RelativeBottomDistance < ClosestWallDistance)
        {
            ClosestWall = WallTarget.BottomWall;
            ClosestWallDistance = RelativeBottomDistance;
        }

        //Now do the same thing with both the Left wall
        float TankLeftDistance = Mathf.Abs(transform.position.x - -7.5f);
        float PlayerLeftDistance = Mathf.Abs(PlayerPos.x - -7.5f);
        float RelativeLeftDistance = (TankLeftDistance + PlayerLeftDistance) / 2f;
        if (RelativeLeftDistance < ClosestWallDistance)
        {
            ClosestWall = WallTarget.LeftWall;
            ClosestWallDistance = RelativeLeftDistance;
        }

        //and the Right wall
        float TankRightDistance = Mathf.Abs(transform.position.x - 7.5f);
        float PlayerRightDistance = Mathf.Abs(PlayerPos.x - 7.5f);
        float RelativeRightDistance = (TankRightDistance + PlayerRightDistance) / 2f;
        if (RelativeRightDistance < ClosestWallDistance)
        {
            ClosestWall = WallTarget.RightWall;
            ClosestWallDistance = RelativeRightDistance;
        }

        //Finally, return the wall which is going to be used for a rebound shot
        return ClosestWall;
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
            SoundEffectsPlayer.Instance.PlaySound("TankDie");
            Destroy(collision.gameObject);
            CleanProjectiles();
            GameState.Instance.IncreaseScore((int)PointValue.Tank);
            WaveManager.Instance.EnemyDead(this);
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

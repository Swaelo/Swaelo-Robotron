// ================================================================================================================================
// File:        PlayerShooting.cs
// Description:	Allows the player to fire projectiles by clicking on the game window
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public enum ProjectileColor
{
    Blue = 0,
    Green = 1,
    Red = 2,
    White = 3,
    Yellow = 4
}

public class PlayerShooting : MonoBehaviour
{
    public GameObject PlayerProjectilePrefab;   //Bullet prefab that the player shoots
    public LayerMask BackdropLayerMask;     //Layermask for raycasting on the backdrop object for getting mouse location
    private ProjectileColor CurrentColor = ProjectileColor.Blue;    //Current color of bullets being fired
    private float ColorChangeRate = 1f;  //How often the projectile color changes
    private float NextColorChange = 1f;  //How long until the projectile color changes again
    private float ShotCooldownDuration = 0.1f;  //How much time passes between firing projectiles
    private float ShotCooldownRemaining = 0.1f; //How long until the player can shoot another projectile
    public List<GameObject> ActiveProjectiles = new List<GameObject>();    //Keep a list of the players active projectiles so they can be cleaned up on demand
    
    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Decrement the shot cooldown
        ShotCooldownRemaining -= Time.deltaTime;

        //Allow the firing of projectiles whenever its not on cooldown
        if(ShotCooldownRemaining <= 0.0f)
        {
            //Aim projectiles toward the mouse cursor whenever the right mouse button is being held down
            if (Input.GetMouseButton(0))
                FireProjectileMouseCursor();
            //Aim projectiles with the right joystick whenever any input from it is being detected
            else if (Input.GetAxis("ControllerHorizontalFiring") != 0f || Input.GetAxis("ControllerVerticalFiring") != 0f)
                FireProjectileRightJoystick();
        }

        //Cycle through firing of different colored projectiles
        CycleProjectileColors();
    }

    //Fires a projectile aimed with the mouse cursor
    private void FireProjectileMouseCursor()
    {
        //Reset the shot cooldown timer
        ShotCooldownRemaining = ShotCooldownDuration;

        //Shoot a ray through the camera to see where the players mouse cursor is positioned
        Ray CursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit RayHit;
        if (Physics.Raycast(CursorRay, out RayHit, BackdropLayerMask))
        {
            //Find the direction from the player to where the mouse cursor is
            Vector3 MouseDirection = Vector3.Normalize(RayHit.point - transform.position);
            MouseDirection.Normalize();

            //Fire a projectile in this direction
            FireProjectile(MouseDirection);
        }
    }

    //Cleans up any active projectiles then resets the list which stores them all
    public void CleanProjectiles()
    {
        foreach (GameObject Projectile in ActiveProjectiles)
            if(Projectile != null)
                Destroy(Projectile);
        ActiveProjectiles.Clear();
    }

    //Fires a projectile aimed with the right joystick
    private void FireProjectileRightJoystick()
    {
        //Reset the shot cooldown timer
        ShotCooldownRemaining = ShotCooldownDuration;

        //Create a direction vector based on the right joystick input
        Vector3 JoystickDirection = new Vector3(Input.GetAxis("ControllerHorizontalFiring"), Input.GetAxis("ControllerVerticalFiring"));
        JoystickDirection.Normalize();

        //Fire a projectile in this direction
        FireProjectile(JoystickDirection);
    }

    //Fires a projectile in the given direction
    private void FireProjectile(Vector3 Direction)
    {
        //Create a quaternion from the angle of this direction vector, and find a suitable location to spawn it
        float Angle = Vector3.Angle(Direction, transform.right);
        Quaternion Rotation = Quaternion.Euler(0f, 0f, Direction.y > 0f ? Angle : -Angle);
        Vector3 Position = transform.position + (Direction * .25f);
        //Spawn a projectile with these values and set its movement direction and render color
        GameObject Projectile = Instantiate(PlayerProjectilePrefab, Position, Rotation);
        Projectile.GetComponent<ProjectileMovement>().InitializeProjectile(Direction, CurrentColor);
        //Store the projectile in the list with the other
        ActiveProjectiles.Add(Projectile);
        //Play the shooting sound effect
        SoundEffectsPlayer.Instance.PlaySound("PlayerShoot");
    }

    private void CycleProjectileColors()
    {
        //Count down the timer until the next color change
        NextColorChange -= Time.deltaTime;

        //Reset the timer and cycle to the next color whenever it reaches 0
        if(NextColorChange <= 0.0f)
        {
            //Reset the timer
            NextColorChange = ColorChangeRate;
            //Cycle to the next color
            switch(CurrentColor)
            {
                case (ProjectileColor.Blue):
                    CurrentColor = ProjectileColor.Green;
                    break;
                case (ProjectileColor.Green):
                    CurrentColor = ProjectileColor.Red;
                    break;
                case (ProjectileColor.Red):
                    CurrentColor = ProjectileColor.White;
                    break;
                case (ProjectileColor.White):
                    CurrentColor = ProjectileColor.Yellow;
                    break;
                case (ProjectileColor.Yellow):
                    CurrentColor = ProjectileColor.Blue;
                    break;
            }
        }
    }
}

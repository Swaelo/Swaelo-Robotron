// ================================================================================================================================
// File:        PlayerShooting.cs
// Description:	Allows the player to fire projectiles by clicking on the game window
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

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
    
    private void Update()
    {
        //Shoot a projectile whenever the player clicks the left mouse button
        if (Input.GetMouseButtonDown(0))
            FireProjectile();

        //Cycle through firing of different colored projectiles
        CycleProjectileColors();
    }

    private void FireProjectile()
    {
        //Shoot a ray through the camera to see where the players mouse cursor is positioned
        Ray CursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit RayHit;
        if (Physics.Raycast(CursorRay, out RayHit, BackdropLayerMask))
        {
            //Find the direction from the player to where the mouse cursor is
            Vector3 MouseDirection = Vector3.Normalize(RayHit.point - transform.position);

            //Get the angle of this vector so that same angle can be applied to the projectile when we spawn it in
            float ShotAngle = Vector3.Angle(MouseDirection, transform.right);

            //Create a quaternion to apply to the projectile
            bool ShotAbove = RayHit.point.y >= transform.position.y;
            Quaternion ShotRotation = Quaternion.Euler(0f, 0f, ShotAbove ? ShotAngle : -ShotAngle);

            //Spawn the projectile and give it some velocity
            GameObject Projectile = Instantiate(PlayerProjectilePrefab, transform.position + (MouseDirection * .25f), ShotRotation);
            Projectile.GetComponent<ProjectileMovement>().SetMovement(MouseDirection);

            //Tell the projectile its color
            Projectile.GetComponent<ProjectileMovement>().SetColor(CurrentColor);
        }
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

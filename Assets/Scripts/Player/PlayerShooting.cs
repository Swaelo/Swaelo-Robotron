// ================================================================================================================================
// File:        PlayerShooting.cs
// Description:	Allows the player to fire projectiles by clicking on the game window
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject PlayerProjectilePrefab;

    private void Update()
    {
        //Shoot a projectile whenever the player clicks on the game window
        if(Input.GetMouseButtonDown(0))
        {
            //Shoot a ray through the camera to see where the players mouse cursor is positioned
            Ray CursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit RayHit;
            if(Physics.Raycast(CursorRay, out RayHit))
            {
                //Find the direction from the player to where the mouse cursor is
                Vector3 MouseDirection = Vector3.Normalize(RayHit.point - transform.position);

                //Get the angle of this vector so that same angle can be applied to the projectile when we spawn it in
                float ShotAngle = Vector3.Angle(MouseDirection, transform.right);

                //Create a quaternion to apply to the projectile
                bool ShotAbove = RayHit.point.y >= transform.position.y;
                Quaternion ShotRotation = Quaternion.Euler(0f, 0f, ShotAbove ? ShotAngle : -ShotAngle);

                //Spawn the projectile and give it some velocity
                GameObject Projectile = Instantiate(PlayerProjectilePrefab, transform.position + (MouseDirection*.25f), ShotRotation);
                Projectile.GetComponent<ProjectileMovement>().SetMovement(MouseDirection);
            }
        }
    }
}

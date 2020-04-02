// ================================================================================================================================
// File:        CruiseMissileAI.cs
// Description:	Cruise Missiles zigzag around trying to kill the player
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class CruiseMissileAI : MonoBehaviour
{
    public GameObject[] TailSections;   //Set of tail sections which trail behind this main body section
    private float MoveSpeed = 3f;   //How fast the cruise missiles travel
    private Vector3 CurrentDirection;   //Current direction the cruise missile is travelling
    private Vector2 ZigZagInterval = new Vector2(0.25f, 1.25f); //How often the missile zigzags falls within these value ranges
    private float NextZigZag = 0.75f;   //Seconds left until the cruise missile zigzags again
    private bool MovingDirect = true;   //The zigzagging changes between moving directly toward the player, and random directions offset from that
    private Vector2 ZigZagRange = new Vector2(15f, 75f);    //Degrees of rotation that may be applied to the current movement direction when zigzagging

    private void Start()
    {
        //Unparent all the tail sections from the main body section
        foreach (GameObject TailSection in TailSections)
            TailSection.transform.parent = null;
        //Start by moving directly toward the player character
        DirectTowardPlayer();
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Move in a zigzag pattern
        ZigZagDirection();

        //Travel forward in the current direction
        transform.position += CurrentDirection * MoveSpeed * Time.deltaTime;
    }

    //Sets the current movement direction to go straight toward the players current location
    private void DirectTowardPlayer()
    {
        Vector3 PlayerPos = GameState.Instance.Player.transform.position;
        CurrentDirection = Vector3.Normalize(PlayerPos - transform.position);
    }

    //Periodically changes the movement direction to create a zigzag effect
    private void ZigZagDirection()
    {
        //ZigZag changing directions periodically
        NextZigZag -= Time.deltaTime;
        if (NextZigZag <= 0.0f)
        {
            //Reset the timer and change between direct and random rotation movement direction
            NextZigZag = Random.Range(ZigZagInterval.x, ZigZagInterval.y);
            MovingDirect = !MovingDirect;
            //If the missile is now set to move direct, get a new direction to travel which goes straight towards the players current location
            if (MovingDirect)
                DirectTowardPlayer();
            //Otherwise, we apply some random rotation onto the current direction to zigzag away from the player for a moment
            else
            {
                bool RotateClockwise = Random.value >= 0.5f;
                float RotationForce = Random.Range(ZigZagRange.x, ZigZagRange.y);
                CurrentDirection = Quaternion.Euler(0f, 0f, RotationForce) * CurrentDirection;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Collision with player projectile destroys both projectiles
        if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            DestroyProjectile();
        }
        //Kill the player if the cruise missile hits them
        else if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
        //Bounce the missiles off the walls
        else if (collision.transform.CompareTag("Wall"))
        {
            Vector3 SurfaceNormal = collision.contacts[0].normal;
            CurrentDirection = Vector3.Reflect(CurrentDirection, SurfaceNormal);
        }
    }

    //Destroys the cruise missle
    public void DestroyProjectile()
    {
        //Play sound
        SoundEffectsPlayer.Instance.PlaySound("CruiseMissileExplode");
        //First destroy all the tail section parts
        foreach (GameObject TailSection in TailSections)
            Destroy(TailSection);
        //Then destroy the main body part
        Destroy(gameObject);
    }
}

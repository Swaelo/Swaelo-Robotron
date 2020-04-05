// ================================================================================================================================
// File:        CruiseMissileAI.cs
// Description:	Cruise Missiles zigzag around trying to kill the player
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class CruiseMissileAI : MonoBehaviour
{
    //Targetting/Movement
    private float MoveSpeed = 3f;   //How fast the cruise missiles travel
    private Vector3 CurrentDirection;   //Current direction the cruise missile is travelling
    private bool MovingDirect = true;   //The zigzagging changes between moving directly toward the player, and random directions offset from that
    private Vector2 DirectSeekDurationRange = new Vector2(0.5f, 3.5f);  //How long to remain seeking toward the current target before zigzagging off
    private float DirectSeekLeft;   //Time left for direct seeking before zigzagging
    private Vector2 ZigZagSeekDurationRange = new Vector2(0.35f, 1.65f);    //How long to remain seeking in zigzag offset direction before going straight back toward the target
    private float ZigZagSeekLeft;   //Time left for zigzag seeking before going back to direct
    private Vector2 ZigZagPowerRange = new Vector2(15f, 75f);    //Degrees of rotation that may be applied to the currect direction to zigzag away

    //Rendering
    public GameObject[] TailSections;   //Set of tail sections which trail behind this main body section

    //Audio
    private static bool TravelSoundInUse = false;   //Tracks if any active cruise missile is currently playing the travel sound effect
    private bool PlayingTravelSound = false;    //Tracks if this cruise missile is playing the sound effect
    private float TravelSoundLength = 2.38f;    //How long the cruise missle travel sound lasts for
    private float TravelSoundLeft = 0f;  //Time left until the travel sound finishes playing and needs to be restarted

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

        //Start playing the travel sound if its not being played already
        if(!TravelSoundInUse && !PlayingTravelSound)
        {
            //Start playing the sound
            TravelSoundInUse = true;
            PlayingTravelSound = true;
            SoundEffectsPlayer.Instance.PlaySound("CruiseMissileTravel");
            TravelSoundLeft = TravelSoundLength;
        }
        //If we are playing it, wait for it to finish before starting it again
        else if(PlayingTravelSound)
        {
            //Play it again once it finishes
            TravelSoundLeft -= Time.deltaTime;
            if(TravelSoundLeft <= 0.0f)
            {
                TravelSoundLeft = TravelSoundLength;
                SoundEffectsPlayer.Instance.PlaySound("CruiseMissileTravel");
            }
        }
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
        //Keep moving straight if thats the current method of travel
        if(MovingDirect)
        {
            //Wait for the timer to expire
            DirectSeekLeft -= Time.deltaTime;
            if(DirectSeekLeft <= 0.0f)
            {
                //ZigZag to move in some totally different direction
                bool ZigZagLeft = Random.value >= 0.5f;
                float ZigZagStrength = Random.Range(ZigZagPowerRange.x, ZigZagPowerRange.y);
                CurrentDirection = Quaternion.Euler(0f, 0f, ZigZagLeft ? ZigZagStrength : -ZigZagStrength) * CurrentDirection;
                //Change to zigzag movement and decide how long thats going to remain for
                MovingDirect = false;
                ZigZagSeekLeft = Random.Range(ZigZagSeekDurationRange.x, ZigZagSeekDurationRange.y);
            }
        }
        //Otherwise keep zigzagging
        else
        {
            //Wait for the timer
            ZigZagSeekLeft -= Time.deltaTime;
            if(ZigZagSeekLeft <= 0.0f)
            {
                //Move back to direct movement now
                DirectTowardPlayer();
                MovingDirect = true;
                DirectSeekLeft = Random.Range(DirectSeekDurationRange.x, DirectSeekDurationRange.y);
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
        //If this missile was playing the travel sound effect, free it up for some other missile to use now instead
        if (TravelSoundInUse && PlayingTravelSound)
            TravelSoundInUse = false;
        //Play sound
        SoundEffectsPlayer.Instance.PlaySound("CruiseMissileExplode");
        //First destroy all the tail section parts
        foreach (GameObject TailSection in TailSections)
            Destroy(TailSection);
        //Then destroy the main body part
        Destroy(gameObject);
    }
}

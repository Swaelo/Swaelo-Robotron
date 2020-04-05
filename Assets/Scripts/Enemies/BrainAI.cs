// ================================================================================================================================
// File:        BrainAI.cs
// Description:	Brains move around quite slowly, they fire cruise missiles at the player and they seek out any human survivors they
//              can find so they can turn them into Progs
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public class BrainAI : HostileEntity
{
    //Movement/Targetting
    private float MoveSpeed = 1.15f;    //How fast the Brains can move around the level
    private GameObject HumanTarget; //Current human the Brain is moving towards so it can be turned into a Prog
    private bool TargetAvailable = false;   //Set to false when no humans remain, the Brain just focuses on fighting the player character
    private float ReprogramVicinity = 0.5f;    //How close the Brain must be from a Human to start reprogramming it
    private float ReprogramInterval = 2.5f;    //How long it takes a Brain to turn a Human into a Prog
    private float ReprogramLeft = 2.5f;    //Seconds left until the Brain finishes reprogramming its current target
    private bool Reprogramming = false; //Flagged to true while the Brain is busy reprogramming a Human into a Prog
    private Vector2 WanderDuration = new Vector2(0.15f, 3f);    //How long a Brain may wander in 1 direction when it has no Human to seek before it changes directions
    private float WanderTimeLeft;   //Time left to wander in the current direction before the Brain finds a new direction to move in
    private Vector3 WanderDirection;    //Current direction the Brain is wandering when it has no humans left to hunt down
    private float ReprogramSoundDuration = 2.757f;  //How long it takes to play the reprogramming sound effect
    private float ReprogramDurationLeft = 2.757f;    //Time left until the reprogram sound finishes playing
    private bool PlayingReprogramSound = false; //Tracks if this brain is currently using the reprogramming sound effect
    private static bool ReprogramSoundInUse = false; //Tracks if the reprogam sound is being played by any Brain that exists currently

    //Firing
    public GameObject CruiseMissilePrefab;  //Projectiles fired by the Brain which zigzag towards the player
    private Vector2 MissileCooldownInterval = new Vector2(6.5f, 10f);   //How often the Brain can fire cruise missiles at the player
    private float MissileCooldownLeft;  //Seconds left until the Brain can fire another cruise missile

    //Rendering/Animations
    public SpriteRenderer FrontBodyRenderer;    //Renders the front view body sprite
    public SpriteRenderer BackBodyRenderer; //Renders the back view body sprite
    public SpriteRenderer SideBodyRenderer; //Renders the side view body sprite
    public SpriteRenderer SideBrainRenderer;    //Renders the side view brain sprite
    public SpriteRenderer SideEyesRenderer; //Renders the side view eyes sprite
    public Animator[] BodyAnimators;    //Animators for each of the body sprites
    private Vector3 PreviousPos;    //Used to measure when the Brain is moving, and in what direction
    private bool IsAlive = true;    //Set to false once the Brain has been killed and its busy playing the death animations
    private float DeathAnimationRemaining = 0.417f; //Time remaining until the Brains death animation is finished playing out
    public Animator[] DeathAnimators;   //Animators for each component of the Brain which have death animations which can be played

    //Physics
    public BoxCollider2D FrontCollider;
    public BoxCollider2D SideCollider;

    private void Start()
    {
        //Store initial position and enable only the front sprites
        PreviousPos = transform.position;
        FrontBodyRenderer.forceRenderingOff = false;
        SideBodyRenderer.forceRenderingOff = true;
        BackBodyRenderer.forceRenderingOff = true;
        //Acquire a human target to reprogram
        TargetHuman();
        //Set an initial cooldown period before the Brain can start firing cruise missiles
        MissileCooldownLeft = Random.Range(MissileCooldownInterval.x * 0.5f, MissileCooldownInterval.y * 0.5f);
    }

    private void Update()
    {
        //All Enemy AI is disabled during the round warmup period
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        if (!IsAlive)
            PlayDeath();
        else
        {
            //Keep sprites rendering correctly
            RenderBodySprites();

            //Seek the current human target until we get close enough to start reprogramming it
            if (!Reprogramming)
            {
                //Seek towards the target human if the Brain has one
                if (TargetAvailable)
                    SeekHuman();
                //Otherwise just wander around
                else
                    WanderAround();

                //Fire projectiles whenever the Brain isnt busy reprogramming
                FireMissiles();
            }
            //Continue reprogramming the current target once we have captured it
            else
                ReprogramHuman();
        }

        //If this Brain is using the reprogram sound effect, free it up for use once its finished playing
        if(PlayingReprogramSound)
        {
            ReprogramDurationLeft -= Time.deltaTime;
            if(ReprogramDurationLeft <= 0.0f)
            {
                PlayingReprogramSound = false;
                ReprogramSoundInUse = false;
            }
        }
    }

    //Wanders around randomly
    private void WanderAround()
    {
        //Change directions and reset the timer whenever it expires
        WanderTimeLeft -= Time.deltaTime;
        if (WanderTimeLeft <= 0.0f)
            NewWanderDirection();
        transform.position += WanderDirection * MoveSpeed * Time.deltaTime;
    }

    //Controls rendering and animations of the main body parts of the Brain
    private void RenderBodySprites()
    {
        //Inform the main animation controllers if the brain is currently moving or not
        bool IsMoving = transform.position != PreviousPos;
        foreach (Animator AnimationController in BodyAnimators)
            AnimationController.SetBool("IsMoving", IsMoving);

        //Transition between different sprites while the Brain is moving around
        if (IsMoving)
        {
            //Measure distance travelled in each direction this frame
            float VerticalMovement = Mathf.Abs(transform.position.y - PreviousPos.y);
            float HorizontalMovement = Mathf.Abs(transform.position.x - PreviousPos.x);
            bool MovingVertical = VerticalMovement > HorizontalMovement;

            //Toggle box colliders
            if(FrontCollider != null)
                FrontCollider.enabled = MovingVertical;
            if(SideCollider != null)
                SideCollider.enabled = !MovingVertical;

            //Manage renderering of Front/Back sprites during vertical movement
            if (MovingVertical)
            {
                //Side sprites should be hidden during vertical movement
                SideBodyRenderer.forceRenderingOff = true;
                //View either the Front/Back sprite based on the direction of vertical movement
                bool MovingUp = transform.position.y > PreviousPos.y;
                FrontBodyRenderer.forceRenderingOff = MovingUp;
                SideBodyRenderer.forceRenderingOff = !MovingUp;
            }
            //Manage renderering of the Side sprite during horizontal movement
            else
            {
                //Only the side sprite should be displayed during horizontal movement
                FrontBodyRenderer.forceRenderingOff = true;
                BackBodyRenderer.forceRenderingOff = true;
                SideBodyRenderer.forceRenderingOff = false;
                //Set the side sprites to be flipped on the X axis based on the direction of horizontal movement
                bool MovingRight = transform.position.x > PreviousPos.x;
                SideBodyRenderer.flipX = !MovingRight;
                SideBrainRenderer.flipX = !MovingRight;
                SideEyesRenderer.flipX = !MovingRight;
            }
        }

        //Store position for next frames update
        PreviousPos = transform.position;
    }

    //Seeks the nearest human survivor, turning it into a Prog enemy if it can be caught before the Player rescues them
    private void SeekHuman()
    {
        //Check to make sure our target hasnt disappeared (could have been rescued, killed or another Brain captured it before us)
        if(HumanTarget == null)
        {
            //Look for a new target if the current one has been lost
            TargetHuman();
            return;
        }

        //Seek towards the current human target
        Vector3 TargetDirection = Vector3.Normalize(HumanTarget.transform.position - transform.position);
        transform.position += TargetDirection * MoveSpeed * Time.deltaTime;

        //Start reprogramming the human when we get close enough to it
        float TargetDistance = Vector3.Distance(transform.position, HumanTarget.transform.position);
        if(TargetDistance <= ReprogramVicinity)
        {
            //Make sure another Brain isnt already reprogramming this Human
            if(HumanTarget.GetComponent<FriendlyEntity>().BeingReprogrammed)
            {
                //If another Brain beat us to it, look for another one to go after instead
                TargetHuman();
                return;
            }

            //Start the reprogramming process
            Reprogramming = true;
            ReprogramLeft = ReprogramInterval;

            //Try playing the reprogram sound effect, making sure to not allow it to play at multiple times at once as it sounds very spammy
            if(!ReprogramSoundInUse)
            {
                //Start playing the sound
                ReprogramSoundInUse = true;
                PlayingReprogramSound = true;
                ReprogramDurationLeft = ReprogramSoundDuration;
                SoundEffectsPlayer.Instance.PlaySound("BrainReprogram", 0.5f);
            }

            //Alert the entity its been captured so it disables its AI and starts playing the reprogramming animation
            HumanTarget.GetComponent<FriendlyEntity>().CapturedForReprogramming();
                    //Start playing the Brains reprogramming animations
                    foreach (Animator AnimationController in BodyAnimators)
                        AnimationController.SetBool("Reprogramming", true);
        }
    }

    //Continues reprogramming the captured human until its been transformed into a Prog
    private void ReprogramHuman()
    {
        //Wait until the reprogramming process has been completed
        ReprogramLeft -= Time.deltaTime;
        if(ReprogramLeft <= 0.0f)
        {
            //Get the name of the prog which is going to replace the human that we captured
            EntityType HumanType = HumanTarget.GetComponent<FriendlyEntity>().Type;
            string PrefabName = HumanType.ToString() + "Prog";
            //Spawn in a new prog enemy in the place of the captured human
            GameObject Prog = Instantiate(PrefabSpawner.Instance.GetPrefab(PrefabName), HumanTarget.transform.position, Quaternion.identity);
            //Have the WaveManager add the new enemy to its tracking lists
            WaveManager.Instance.AddNewEnemy(Prog.GetComponent<HostileEntity>());
            //Destroy the human that it was created from
            WaveManager.Instance.HumanDead(HumanTarget.GetComponent<BaseEntity>());
            Destroy(HumanTarget.gameObject);
            //Finish the reprogramming process
            Reprogramming = false;
            foreach (Animator AnimationController in BodyAnimators)
                AnimationController.SetBool("Reprogramming", false);
            //Find a new target to capture
            TargetHuman();
        }
    }

    //Targets the nearest human survivor
    private void TargetHuman()
    {
        //Grab a list of all the survivors currently in the game
        GameObject[] Humans = GameObject.FindGameObjectsWithTag("Human");

        //Filter out any which have already been targetted by another Brain
        List<GameObject> AvailableHumans = new List<GameObject>();
        foreach(GameObject Human in Humans)
        {
            if (Human.GetComponent<FriendlyEntity>().TargettedByBrain)
                continue;
            AvailableHumans.Add(Human);
        }

        //If there arent any humans left to go after, then we just wander around aimlessly
        if(AvailableHumans.Count <= 0)
        {
            TargetAvailable = false;
            NewWanderDirection();
            return;
        }

        //Figure out which of the available humans is closest to the Brain
        GameObject ClosestHuman = AvailableHumans[0];
        float ClosestHumanDistance = Vector3.Distance(transform.position, AvailableHumans[0].transform.position);
        for(int i = 1; i < AvailableHumans.Count; i++)
        {
            //Compare each of them against one another
            GameObject CompareHuman = AvailableHumans[i];
            float CompareDistance = Vector3.Distance(transform.position, CompareHuman.transform.position);
            //Note if this is closer than any other so far
            if (CompareDistance < ClosestHumanDistance)
            {
                ClosestHuman = CompareHuman;
                ClosestHumanDistance = CompareDistance;
            }
        }

        //Now we have the closest human, set that as our target and flag it to make sure no other Brain tries to go after it
        TargetAvailable = true;
        HumanTarget = ClosestHuman;
        HumanTarget.GetComponent<FriendlyEntity>().TargettedByBrain = true;
    }

    //Gets a new random direction for the Brain to wander
    private void NewWanderDirection()
    {
        WanderTimeLeft = Random.Range(WanderDuration.x, WanderDuration.y);
        WanderDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
        WanderDirection.Normalize();
    }

    //Periodically fires cruise missiles at the player character
    private void FireMissiles()
    {
        //Fire a cruise missile at the player whenever the timer expires
        MissileCooldownLeft -= Time.deltaTime;
        if (MissileCooldownLeft <= 0.0f)
        {
            //Play sound
            SoundEffectsPlayer.Instance.PlaySound("FireCruiseMissile");
            MissileCooldownLeft = Random.Range(MissileCooldownInterval.x, MissileCooldownInterval.y);
            Vector3 ShotTarget = GameState.Instance.Player.transform.position;
            Vector3 ShotDirection = Vector3.Normalize(ShotTarget - transform.position);
            GameObject CruiseMissile = Instantiate(CruiseMissilePrefab, transform.position, Quaternion.identity);
        }
    }

    //Waits for the death animation to complete before the brain destroys itself
    private void PlayDeath()
    {
        //Wait for the animation timer to expire
        DeathAnimationRemaining -= Time.deltaTime;
        if (DeathAnimationRemaining <= 0.0f)
            Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Destroy any player projectiles which hit the Brain, also killing the Brain in the process
        if(collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            Die();
        }
        //Kill the player character on contact
        else if(collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }

    //Triggers the death animation to start playing and removes any components which are no longer needed
    private void Die()
    {
        //If the Brain is killed while it was reprogramming a human, that human is destroyed
        if(Reprogramming && HumanTarget != null)
        {
            WaveManager.Instance.HumanDead(HumanTarget.GetComponent<BaseEntity>());
            Destroy(HumanTarget);
        }
        //Tell the wave manager this enemy is now dead, and award points to the player for destroying it
        WaveManager.Instance.EnemyDead(this);
        GameState.Instance.IncreaseScore((int)PointValue.Brain);
        //Set the Brain as dead and trigger the death animation
        IsAlive = false;
        foreach (Animator AnimationController in DeathAnimators)
            AnimationController.SetBool("IsDead", true);
        //Destroy the rigidbody and boxcollider components so no unwanted collision event occur during the death animation
        Destroy(GetComponent<Rigidbody2D>());
        Destroy(FrontCollider);
        Destroy(SideCollider);
        //Play sound
        SoundEffectsPlayer.Instance.PlaySound("BrainDie");
    }

    private void OnDestroy()
    {
        //If this Brain is killed while its using the Reprogram sound effect, it needs to be freed up for use by other Brains
        if (PlayingReprogramSound)
            ReprogramSoundInUse = false;
    }
}

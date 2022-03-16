using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Sirenix.OdinInspector;

using GameManagement;

[Serializable]
public class AttackWindow
{
    [SerializeField]
    public float BaseDamageTime = 0;

    [SerializeField]
    public float BaseDelay = 0;

    [SerializeField]
    public float DamageTime = 0;

    [SerializeField]
    public float DamageDelay = 0;

    [SerializeField]
    public Collider2D HitBox;
}

public class Player : MonoBehaviour
{

    public static Player instance;

    #region Physics
    Rigidbody2D rb;

    [SerializeField, FoldoutGroup("Physics")]
    float WallSlideDownMod = 0.5f;

    //Used to Detect What Wall you are on
    [SerializeField, ReadOnly]
    bool OnWall = false;
    [SerializeField, ReadOnly]
    bool OnGround = false;
    [SerializeField, ReadOnly]
    bool WallLeft = false;
    [SerializeField, ReadOnly]
    bool WallRight = false;

    [SerializeField]
    Transform footPos;

    [SerializeField]
    GroundCheck groundCheck;

    [SerializeField]
    WallCheck wallCheck;


    //Force for Jumping off the Wall
    [SerializeField, FoldoutGroup("Physics")]
    float WallJumpPower = 1;

    [SerializeField, FoldoutGroup("Physics")]
    Vector2 Velocity = Vector2.zero;
    [SerializeField, FoldoutGroup("Physics")]
    Vector2 Decceleration = Vector2.zero;
    [SerializeField, FoldoutGroup("Physics")]
    Vector2 GroundDecceleration = Vector2.zero;
    [SerializeField, FoldoutGroup("Physics")]
    Vector2 AirDecceleration = Vector2.zero;

    [SerializeField, FoldoutGroup("Physics")]
    float MaxVelocity = 10;

    [SerializeField, FoldoutGroup("Physics")]
    float velocityCounterSpeed = 1;
    [SerializeField, FoldoutGroup("Physics")]
    float velocityCounterMod = 1;

    #endregion

    #region Animation

    [SerializeField]
    Animator animator;
    [SerializeField, ReadOnly]
    float aniSpeed = 0;

    #endregion

    #region Stats
    [SerializeField, FoldoutGroup("stats")]
    float Health = 100.0f;

    [SerializeField, FoldoutGroup("stats")]
    float speed = 0.1f;

    [SerializeField, FoldoutGroup("stats")]
    float AnimationSpeed = 1f;

    [SerializeField, FoldoutGroup("stats")]
    float jump = 0.1f;

    [SerializeField, FoldoutGroup("stats")]
    float AttackBuffer = 0.1f;

    [SerializeField, FoldoutGroup("stats")]
    float ImmuneBuffer = 0.1f;

    [SerializeField, TabGroup("Melee")]
    float damage = 50.0f;


    float CurrentAttackBuffer = 0;
    float CurrentImmuneBuffer = 0;

    [SerializeField, TabGroup("Melee")]
    List<AttackWindow> LightAttackWindows = new List<AttackWindow>();
    [SerializeField, TabGroup("Melee")]
    float LightAttackSpeedMod = 1;

    [SerializeField, TabGroup("Ranged")]
    Arrow Projectile;
    [SerializeField, TabGroup("Ranged")]
    float ArrowSpeed = 100;
    [SerializeField, TabGroup("Ranged")]
    float ArrowDrop = 100;
    [SerializeField, TabGroup("Ranged")]
    float SinkValue = 100;
    [SerializeField, TabGroup("Ranged")]
    float BowAttackSpeedMod = 1;
    [SerializeField, TabGroup("Ranged")]
    GameObject ProjectilePosition;
    [SerializeField, TabGroup("Ranged")]
    ArrowPool arrowPool;
    [SerializeField, TabGroup("Ranged")]
    float arrowDamage = 50.0f;

    [SerializeField, FoldoutGroup("Blockers")]
    List<string> AttackBlockers = new List<string>();

    [SerializeField, FoldoutGroup("Blockers")]
    List<string> MoveBlockers = new List<string>();

    #endregion

    #region Audio

    [SerializeField]
    PlayerSoundManager playerSoundManager;

    [SerializeField]
    GameObject listener;

    #endregion

    #region ReadOnly

    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    int LightCombo = 0;
    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    Collider2D attackCollider;
    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    float DrawSpeed = 0.75f;
    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    bool inAir = false;
    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    bool Moving = false;

    [SerializeField, FoldoutGroup("READONLY"), DisableInPlayMode]
    bool BowDrawn = false;
    #endregion

    [SerializeField]
    TileMapManager TileManager;

    [SerializeField]
    public SurfaceManager.Surfaces CurrentSurface = SurfaceManager.Surfaces.None;

    private void Awake()
    {
        instance = this;

        PlayerInfo.player = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVelocity();
        UpdatePostion();
        UpdateTimers();

        //used to check if player was on the ground last frame
        bool preGround = OnGround;

        OnGround = groundCheck.IsGrounded;
        OnWall = wallCheck.IsWall;

        WallLeft = wallCheck.IsLeft;
        WallRight = !wallCheck.IsLeft;


        if (!preGround && OnGround)
        {
            playerSoundManager.PlayLand();
            animator.SetTrigger("Land");
            //animator.SetBool("Jump", false);
            animator.SetBool("InAir", false);
        }

        if (OnGround)
        {
            OnWall = false;
        }

        if (!OnWall)
        {
            WallLeft = false;
            WallRight = false;

            try
            {
                SurfaceManager.Surfaces surface = TileManager.GetTileSoundInfo(footPos.position);
                CurrentSurface = surface;
            }
            catch (Exception e)
            {

                Debug.Log(e.Message);
                CurrentSurface = SurfaceManager.Surfaces.Rock;
            }
        }

        if (!OnGround && !OnWall && !animator.GetBool("Jump"))
        {
            inAir = true;
            playerSoundManager.PlayFall();
            animator.SetBool("InAir", true);
        }
        else
        {
            inAir = false;
            playerSoundManager.StopFall();
            animator.SetBool("InAir", false);
        }


        //Attack Loop
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftControl))
        {
            if (!AttackBlockers.Contains(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name))
            {
                if (attackCollider != null && attackCollider.enabled == true)
                {
                    attackCollider.enabled = false;
                }
                playerSoundManager.PlayAttack(LightCombo);
                playerSoundManager.PlayAttackVoice(LightCombo);
                animator.SetTrigger("LightAttack");
                animator.SetInteger("LightAttackCombo", LightCombo);
                LightCombo++;
                if (LightCombo >= 3)
                {
                    LightCombo = 0;
                }
                CurrentAttackBuffer = AttackBuffer;
            }
        }
        else if (Input.GetMouseButton(1))
        {
            if (!AttackBlockers.Contains(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name))
            {
                animator.SetTrigger("Bow");
            }
        }
        else
        {
            animator.SetBool("Bow", false);
        }

        if (BowDrawn)
        {
            if (Input.GetMouseButton(1))
            {
                animator.SetFloat("BowSpeed", 0);
            }
            else
            {
                animator.SetFloat("BowSpeed", 1);
                BowDrawn = false;
            }
        }

        //Debug
        if (Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(10);
        }

        animator.SetBool("Wall", OnWall);
    }

    
    [Button]
    void UpdateAttackSpeed()
    {
        animator.SetFloat("BowSpeed", 1 * BowAttackSpeedMod);
        animator.SetFloat("LightAttackSpeed", 1 * LightAttackSpeedMod);
    }

    [Button]
    public void TakeDamage(float Damage)
    {

        if (CurrentImmuneBuffer > 0)
            return;

        playerSoundManager.PlayHurt();
        animator.SetTrigger("Hurt");
        CurrentImmuneBuffer = ImmuneBuffer;

        Health -= Damage;

        if (Health <= 0)
        {
            SceneManager.LoadScene("YouDied");
        }

    }

    void UpdateTimers()
    {
        //Timers
        if (LightCombo != 0 && CurrentAttackBuffer < 0)
        {
            LightCombo = 0;
        }
        CurrentAttackBuffer -= Time.deltaTime;
        CurrentImmuneBuffer -= Time.deltaTime;

        if (velocityCounterMod < 1)
            velocityCounterMod += velocityCounterSpeed * Time.deltaTime;
        if (velocityCounterMod > 1)
            velocityCounterMod = 1;
    }

    #region Physics Functions

    void UpdateVelocity()
    {
        if (inAir || Moving)
        {
            Decceleration = AirDecceleration;
        }
        else
        {
            Decceleration = GroundDecceleration;
        }

        Moving = false;




        if (Mathf.Abs(Velocity.x) > 0)
        {

            if (Velocity.x > 0)
            {
                if (Velocity.x - Decceleration.x * Time.deltaTime < 0)
                {
                    Velocity = new Vector2(0, Velocity.y);
                }
                else
                    Velocity -= new Vector2(Decceleration.x * Time.deltaTime, 0);
            }
            else
            {

                if (Velocity.x + Decceleration.x * Time.deltaTime > 0)
                {
                    Velocity = new Vector2(0, Velocity.y);
                }
                else
                    Velocity += new Vector2(Decceleration.x * Time.deltaTime, 0);
            }
        }


    }

    void UpdatePostion()
    {
        Vector2 CappedVelocity = Velocity;

        //if (Mathf.Abs(Velocity.x) > MaxVelocity)
        //{
        //    if (Velocity.x > 0)
        //        CappedVelocity = new Vector2(MaxVelocity, Velocity.y);
        //    else
        //        CappedVelocity = new Vector2(-MaxVelocity, Velocity.y);
        //}

        if ((CappedVelocity.x < 0 && !WallRight) || (CappedVelocity.x > 0 && !WallLeft))
            rb.position += CappedVelocity * Time.deltaTime;
    }

    #endregion

    #region Movement

    public void MoveLeft()
    {
        if (!WallRight)
        {
            if (!MoveBlockers.Contains(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name))
            {
                if (Input.GetKey(KeyCode.A))
                {
                    Moving = true;

                    if (Velocity.x < -MaxVelocity)
                        return;

                    //rb.position -= new Vector2(speed * Time.deltaTime, 0);

                    if (Velocity.x - speed < -MaxVelocity)
                    {
                        Velocity = new Vector2(-MaxVelocity, Velocity.y);
                    }
                    else
                        Velocity -= new Vector2(speed * velocityCounterMod, 0);

                    //need to use velocty for this, but capped
                    aniSpeed = Mathf.Abs(MaxVelocity) * 0.5f;

                    transform.eulerAngles = new Vector3(0, 0, 0);
                    listener.transform.localEulerAngles = new Vector3(0, 0, 0);
                    if (OnGround)
                        playerSoundManager.PlayFootStep(aniSpeed);

                    animator.SetFloat("SpeedMod", aniSpeed * AnimationSpeed);
                }
            }
        }
    }

    public void MoveRight()
    {
        if (!WallLeft)
        {
            if (!MoveBlockers.Contains(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name))
            {
                if (Input.GetKey(KeyCode.D))
                {
                    Moving = true;

                    if (Velocity.x > MaxVelocity)
                        return;

                    //rb.position += new Vector2(speed * Time.deltaTime, 0);
                    if (Velocity.x + speed > MaxVelocity)
                    {
                        Velocity = new Vector2(MaxVelocity, Velocity.y);
                    }
                    else
                        Velocity += new Vector2(speed * velocityCounterMod, 0);
                    aniSpeed = Mathf.Abs(MaxVelocity) * 0.5f;

                    transform.eulerAngles = new Vector3(0, 180, 0);
                    listener.transform.localEulerAngles = new Vector3(0, 180, 0);

                    if (OnGround)
                        playerSoundManager.PlayFootStep(aniSpeed);

                    animator.SetFloat("SpeedMod", aniSpeed * AnimationSpeed);
                }
            }
        }
    }

    public void Idle()
    {
        animator.SetFloat("SpeedMod", 0);
    }

    public void Jump()
    {
        if (!MoveBlockers.Contains(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name))
        {
            if (!inAir)
            {
                if (!OnWall || groundCheck.IsGrounded)
                {
                    playerSoundManager.PlayJump();
                    rb.velocity += (new Vector2(0, 1) * jump);

                    //rb.AddForce(new Vector2(0f, jump));
                    animator.SetTrigger("Jump");
                    animator.SetBool("Land", false);
                    animator.SetBool("InAir", true);
                    inAir = true;

                }
                else if (true/*WallJumpCount < WallJumpLimit*/)
                {

                    playerSoundManager.PlayJump();
                    rb.velocity += (new Vector2(0, 1) * jump);

                    if (WallLeft)
                        Velocity -= new Vector2(WallJumpPower, 0);
                    else if (WallRight)
                        Velocity += new Vector2(WallJumpPower, 0);
                    //rb.AddForce(new Vector2(0f, jump));
                    animator.SetBool("Land", false);
                    animator.SetBool("InAir", true);
                    inAir = true;

                    velocityCounterMod = 0;
                }
            }
        }
    }

    #endregion

    #region Attacks

    public void FireArrow()
    {
        var arrow = arrowPool.SpawnArrow(Projectile.gameObject, ProjectilePosition);
        arrow.DropSpeed = ArrowDrop;
        arrow.Speed = ArrowSpeed;
        arrow.SinkValue = SinkValue;
        arrow.Damage = arrowDamage;
        playerSoundManager.PlayBow();
        Physics2D.IgnoreCollision(arrow.GetComponent<Collider2D>(), GetComponent<Collider2D>());
    }

    public void DrawBow()
    {
        //BowDrawn = true;
    }
    public void EnableAttackBox(int combo)
    {
        LightAttackWindows[combo].HitBox.enabled = true;
    }
    public void DisableAttackBox()
    {
        foreach (var item in LightAttackWindows)
        {
            item.HitBox.enabled = false;
        }
    }


    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Platform" && collision.otherCollider.gameObject.tag == "Foot")
        {

            try
            {
                SurfaceManager.Surfaces surface = TileManager.GetTileSoundInfo(footPos.position);
                CurrentSurface = surface;
            }
            catch (Exception e)
            {

                Debug.Log(e.Message);
                CurrentSurface = SurfaceManager.Surfaces.Rock;
            }

        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Platform" && collision.otherCollider.gameObject.tag == "Foot")
        {
            try
            {
                SurfaceManager.Surfaces surface = TileManager.GetTileSoundInfo(footPos.position);
                CurrentSurface = surface;
            }
            catch (Exception)
            {
                CurrentSurface = SurfaceManager.Surfaces.Rock;
            }
        }
        if (collision.gameObject.tag == "Platform" && collision.otherCollider.gameObject.tag == "WallChecker")
        {
            inAir = false;

            Vector2 AvgContact = new Vector2();

            List<ContactPoint2D> contacts = new List<ContactPoint2D>();

            AvgContact = collision.GetContact(0).point;

            if (transform.position.x < AvgContact.x)
            {
                WallLeft = true;
            }
            else
            {
                WallRight = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Platform" && collision.otherCollider.gameObject.tag == "WallChecker")
        {
            inAir = true;
            OnWall = false;
            WallRight = false;
            WallLeft = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Enemy") //enemy is hit
        {

            int mult = LightCombo;

            if (mult == 0)
                mult = 3;

            collision.gameObject.GetComponentInParent<Enemy>().OnDamaged(damage * (mult));
        }
        else if (collision.tag == "Father")
        {
            SceneManager.LoadScene("Winner");

        }
    }

}

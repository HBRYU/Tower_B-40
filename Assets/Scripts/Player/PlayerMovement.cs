/*
Legacy Code. Use only for reference.
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed;
    public float moveAccel, stopAccel, v0;
    private float walkAccelPow;
    public float jumpSpeed;
    public LayerMask groundLayers;
    [SerializeField] private Transform feetPosition;

    [HideInInspector] public bool facingRight;

    public SpriteRenderer sprite;
    private Rigidbody2D rb;

    private PlayerAnimation playerAnimation;
    private PlayerAudio playerAudio;
    private delegate void DELVoid();
    private DELVoid flipSprite, move, jump;
    public bool OnGround { get; private set; }
    private bool prevOnGround;
    private float onGroundTimer;

    [Header("Dash settings")]
    [SerializeField] private float dashDistance, dashCooldown;
    private float dashCooldownTimer;
    [SerializeField] private float dashYSnap, upDashSpeed;
    private bool useInputVelocity;
    void Start()
    {
        //Scripts
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerAudio = GetComponent<PlayerAudio>();

        //Delegates
        flipSprite = FlipSpritesByMovement;
        move = Move;
        jump = Jump;

        //Initial variable values
        walkAccelPow = stopAccel;
    }

    void FixedUpdate()
    {
        move();
        flipSprite();
    }

    void Update()
    {
        HandleUseInputX();
        
        jump();
        Dash();
    }

    void HandleUseInputX()
    {
        // Used to determine whether to use x velocity from Input.GetAxis or use its current rb velocity (from dash)
        
        OnGround = !(Physics2D.OverlapCircle(feetPosition.position, 0.5f, groundLayers) == null);
        if (OnGround && !prevOnGround || onGroundTimer >= 0.5f)
            useInputVelocity = true;
        prevOnGround = OnGround;
        if (OnGround)
            onGroundTimer += Time.deltaTime;
    }

    // Local Variables for Move()
    float prevInput;
    float direction = 1f;
    bool usingMoveAccel;
    bool playingWalkSFX = false;
    void Move()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        
        if (inputX != 0f) useInputVelocity = true;
        
        if (inputX != 0f && prevInput == 0f)
        {
            walkAccelPow = moveAccel;
            usingMoveAccel = true;
        }

        if (inputX == 0f && prevInput != 0f)
        {
            walkAccelPow = stopAccel;
            usingMoveAccel = false;
        }

        float vx = 0f;
        
        if (inputX != 0f)
            direction = inputX;
        
        if (usingMoveAccel && useInputVelocity)
        {
            vx = Mathf.Clamp(direction * walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) + direction * v0, -walkSpeed, walkSpeed);
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
        else if (useInputVelocity)
        {
            vx = walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) * direction;
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
        RequestAnimation("Walk", inputX != 0);
        RequestAnimation("Walk_play_Decel", Mathf.Abs(vx) >= walkSpeed * 0.8f);
        
        if (inputX * inputX > 0.01f && OnGround)
        {
            if (!playingWalkSFX)
            {
                playerAudio.PlayWalkSFX();
                playingWalkSFX = true;
            }
        }
        else
        {
            playingWalkSFX = false;
        }
        
        prevInput = inputX;
    }

    private void Jump()
    {
        RequestAnimation("On_Ground", OnGround);

        if (Input.GetKeyDown(KeyCode.W) && OnGround)
        {
            //print("Jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
        }

    }

    // jump without using w key
    public void Free_Jump(){
        if (OnGround){
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
        }
    }

    void FlipSpritesByMovement()
    {
        if(Input.GetAxisRaw("Horizontal") != 0)
            sprite.flipX = Input.GetAxisRaw("Horizontal") < 0;
        facingRight = !sprite.flipX;
    }

    void Dash()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var position = transform.position;
        Vector3 targetDashPos;
        
        if (mousePos.y > position.y - dashYSnap && mousePos.y < position.y + dashYSnap)
        {
            float dashDirectionX = mousePos.x > transform.position.x ? 1f : -1f;
            float d = dashDistance;
            targetDashPos = position + new Vector3(dashDirectionX, 0) * d;
        }
        else
        {
            float d = Vector3.Distance(position, mousePos) >= dashDistance ? dashDistance : Vector3.Distance(position, mousePos);
            
            targetDashPos = position + Vector3.Normalize(mousePos - position) * d;
        }
        if (Input.GetKeyDown(KeyCode.F) && dashCooldownTimer <= 0f)
        {
            rb.MovePosition(targetDashPos);
            dashCooldownTimer = dashCooldown;
            playerAnimation.RequestAnimation("Dash", "Trigger");
            
            sprite.flipX = position.x > mousePos.x;
            facingRight = !sprite.flipX;
            rb.velocity = (mousePos - position).normalized * upDashSpeed;
            useInputVelocity = false;
            onGroundTimer = 0f;
        }
        else
            dashCooldownTimer -= Time.deltaTime;
    }

    private void RequestAnimation<T>(string param, T value)
    {
        if (playerAnimation == null)
            return;
        playerAnimation.RequestAnimation(param, value);
    }
}*/

// Entire refactoring done by GPT4.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Public fields to set movement parameters from the Unity Inspector
    public float walkSpeed;
    public float moveAccel, stopAccel, v0;
    private float walkAccelPow;
    public float jumpSpeed;
    public LayerMask groundLayers;
    [SerializeField] private Transform feetPosition;

    [HideInInspector] public bool facingRight;

    // References to local components
    public SpriteRenderer sprite;
    private Rigidbody2D rb;
    private PlayerAnimation playerAnimation;
    private PlayerAudio playerAudio;

    // Delegates to modularize movement functions. Not sure why I did this but ok
    private delegate void DELVoid();
    private DELVoid flipSprite, move, jump;

    // Ground checking properties
    public bool OnGround { get; private set; }
    private bool prevOnGround;
    private float onGroundTimer;

    // Dash settings
    private float dashCooldownTimer;
    [SerializeField] private float dashDistance, dashCooldown;
    [SerializeField] private float dashYSnap, upDashSpeed;

    // Internal state variables for movement logic
    private bool useInputVelocity;
    private float prevInput;
    private float direction = 1f;
    private bool usingMoveAccel;
    private bool playingWalkSFX = false;

    [HideInInspector] public bool overrideMovement;

    void Start()
    {
        InitializeComponents();
        InitializeVariables();
    }

    void FixedUpdate()
    {
        if (overrideMovement)
            return;
        Move();
        FlipSpritesByMovement();
    }

    void Update()
    {
        if (overrideMovement)
            return;
        HandleUseInputX();
        Jump();
        HandleDash();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerAudio = GetComponent<PlayerAudio>();
    }

    private void InitializeVariables()
    {
        walkAccelPow = stopAccel;
    }

    private void HandleUseInputX()
    {
        // Used to determine whether to use x velocity from Input.GetAxis or use its current rb velocity (from dash)
        OnGround = Physics2D.OverlapCircle(feetPosition.position, 0.3f, groundLayers) != null;
        if (OnGround && !prevOnGround || onGroundTimer >= 0.5f || Input.GetAxisRaw("Horizontal") != 0f)
        {
            if(!useInputVelocity && onGroundTimer >= 0.5f) RequestAnimation("PlayDecel", "Trigger");
            useInputVelocity = true;
        }

        if (OnGround && !prevOnGround)
        {
            playerAudio.PlayLandSFX();
        }
        
        prevOnGround = OnGround;
        if (OnGround)
            onGroundTimer += Time.deltaTime;
    }

    private void Move()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        UpdateAcceleration(inputX);
        UpdateVelocity();
        HandleAnimations(inputX);
        HandleWalkSFX(inputX);
        prevInput = inputX;
    }

    private void UpdateAcceleration(float inputX)
    {
        if (inputX != 0f && prevInput == 0f)
        {
            walkAccelPow = moveAccel;
            usingMoveAccel = true;
        }
        else if (inputX == 0f && prevInput != 0f)
        {
            walkAccelPow = stopAccel;
            usingMoveAccel = false;
        }

        if (inputX != 0f)
            direction = inputX;
    }

    private void UpdateVelocity()
    {
        float vx = 0f;
        if (usingMoveAccel && useInputVelocity)
        {
            vx = Mathf.Clamp(direction * walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) + direction * v0, -walkSpeed, walkSpeed);
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
        else if (useInputVelocity)
        {
            vx = walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) * direction;
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
    }

    private void HandleAnimations(float inputX)
    {
        RequestAnimation("Walk", inputX != 0);
        RequestAnimation("Walk_play_Decel", Mathf.Abs(rb.velocity.x) >= walkSpeed * 0.8f);
    }

    private void HandleWalkSFX(float inputX)
    {
        if (inputX * inputX > 0.01f && OnGround)
        {
            if (!playingWalkSFX)
            {
                playerAudio.PlayWalkSFX();
                playingWalkSFX = true;
            }
        }
        else
        {
            playingWalkSFX = false;
        }
    }

    private void Jump()
    {
        RequestAnimation("On_Ground", OnGround);
        if (Input.GetKeyDown(KeyCode.W) && OnGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
            playerAudio.PlayJumpSFX();
        }
    }

    public void Free_Jump()
    {
        if (OnGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
        }
    }

    private void FlipSpritesByMovement()
    {
        if (Input.GetAxisRaw("Horizontal") != 0)
            sprite.flipX = Input.GetAxisRaw("Horizontal") < 0;
        facingRight = !sprite.flipX;
    }

    private bool dashAvailableFlag = false;
    private void HandleDash()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var position = transform.position;
        Vector3 targetDashPos;
        if (mousePos.y > position.y - dashYSnap && mousePos.y < position.y + dashYSnap)
        {
            float dashDirectionX = mousePos.x > transform.position.x ? 1f : -1f;
            float d = dashDistance;
            targetDashPos = position + new Vector3(dashDirectionX, 0) * d;
        }
        else
        {
            float d = Vector3.Distance(position, mousePos) >= dashDistance ? dashDistance : Vector3.Distance(position, mousePos);
    
            targetDashPos = position + Vector3.Normalize(mousePos - position) * d;
        }
        if (Input.GetKeyDown(KeyCode.F) && dashCooldownTimer <= 0f)
        {
            Dash();
        }
        else
            dashCooldownTimer -= Time.deltaTime;

        
        //UI
        float fill = dashCooldownTimer > 0f ? (dashCooldown - dashCooldownTimer) / dashCooldown : 1f;
        GM.InGameUIInstance.SetDashFill(fill);
        if (!dashAvailableFlag && dashCooldownTimer < 0f)
        {
            dashAvailableFlag = true;
            GM.InGameUIInstance.SetDashColor(Color.cyan);
        }
        
        
        void Dash()
        {
            rb.MovePosition(targetDashPos);
            dashCooldownTimer = dashCooldown;
            playerAnimation.RequestAnimation("Dash", "Trigger");
    
            sprite.flipX = position.x > mousePos.x;
            facingRight = !sprite.flipX;
            rb.velocity = (mousePos - position).normalized * upDashSpeed;
            useInputVelocity = false;
            onGroundTimer = 0f;
            
            playerAudio.PlayDashSFX();

            dashAvailableFlag = false;
        }
    }

    private void RequestAnimation<T>(string param, T value)
    {
        if (playerAnimation == null)
            return;
        playerAnimation.RequestAnimation(param, value);
    }

    public void FaceDirection(bool right)
    {
        facingRight = right;
        sprite.flipX = !right;
    }
}

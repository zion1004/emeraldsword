using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance;
    public Animator animator;

    private Rigidbody2D rigidBody;


    // x, y 입력값
    private float xInput;
    private float yInput;

    // 플레이어 방향
    private bool facingRight = true;
    private int facingDirection = 1;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 9;
    [SerializeField] private float jumpForce = 12;
    [SerializeField] private float doubleJumpForce = 12;
    private bool canDoubleJump;

    [Header("Buffer & Coyote jump")]
    [SerializeField] private float bufferJumpWindow = .1f;
    private float bufferJumpActivated = -1;
    [SerializeField] private float coyoteJumpWindow = .1f;
    private float coyoteJumpActivated = -1;

    [Header("Wall Interactions")]
    //[SerializeField] private float wallJumpDuration = 1f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(12, 12);
    private bool isWallJumping;
    private Coroutine wallJumpCoroutine;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 1f;
    [SerializeField] private Vector2 knockbackPower = new Vector2(0.1f, 0.1f);
    private bool isKnocked;
    private Coroutine knockbackCoroutine;

    [Header("Collision")]
    [SerializeField] private float groundCheckDistance = 1.34f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;
    private bool isAirborne;
    private bool isWallDetected;

    [Header("Attack")]
    public bool isAttacking = false;


    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        instance = this;
    }

    private void OnDrawGizmos()
    {
        // groundCheckDistance를 유니티에 표시하기
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDirection), transform.position.y));
    }

    private void Start()
    {

    }

    private void Update()
    {
        // for testing/debbuging
        if(Input.GetKeyDown(KeyCode.K))
        {
            Knockback();
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            rigidBody.transform.position = new Vector3(-27.38f, 7.7f, 0f);
        }

        Application.targetFrameRate = 120;

        // 플레이어 상태 업데이트
        UpdateAirborneStatus();

        if(isKnocked)
        {
            return;
        }

        // 헨들러들
        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
        HandleAnimation();
    }

    public void Knockback()
    {
        if(isKnocked)
        {
            if (knockbackCoroutine != null){ 
                StopCoroutine(knockbackCoroutine);
            }
        }

        knockbackCoroutine = StartCoroutine(KnockbackRoutine());
        animator.SetTrigger("knockback");
        rigidBody.linearVelocity = new Vector2(knockbackPower.x * -facingDirection, knockbackPower.y);
    }

    private IEnumerator KnockbackRoutine()
    {
        isKnocked = true;

        float elapsedTime = 0f;
        while(elapsedTime < knockbackDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isKnocked = false;
    }

    private void UpdateAirborneStatus()
    {
        if(isGrounded && isAirborne)
        {
            HandleLanding();
        }
        if(!isGrounded && !isAirborne)
        {
            BecomeAirborne();
        }
    }

    private void BecomeAirborne()
    {
        isAirborne = true;
        coyoteJumpActivated = Time.time;

    }

    private void HandleLanding()
    {
        isAirborne = false;
        canDoubleJump = true;

        AttemptBufferJump();
    }

    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, wallCheckDistance, whatIsGround);
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(!isAirborne || canDoubleJump)
            { 
                HandleJump();
            }
            else { 
                RequestBufferJump();
            }
        }
        if(Input.GetKeyDown(KeyCode.Z)) 
        {
            if(!isAttacking){ 
                Attack();
            }
        }
    }

    private void RequestBufferJump()
    {
        if(isAirborne)
        {
            bufferJumpActivated = Time.time;
        }
    }

    private void AttemptBufferJump()
    {
        if(Time.time < bufferJumpActivated + bufferJumpWindow)
        {
            bufferJumpActivated = Time.time - 1;
            Jump();
        }
    }


    private void HandleJump()
    {
        bool coyoteJumpAvailable = Time.time < coyoteJumpActivated + coyoteJumpWindow;

        if(isGrounded || coyoteJumpAvailable)
        {
            Jump();
        }
        else if(isWallDetected && !isGrounded)
        {
            WallJump();
        }
        else if(canDoubleJump)
        {
            DoubleJump();
        }

        coyoteJumpActivated = Time.time - 1;
    }

    private void Jump() => rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocityX, jumpForce);

    private void DoubleJump()
    {
        StopWallJumpCoroutine();
        isWallJumping = false;
        canDoubleJump = false;
        rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocityX, doubleJumpForce);
    }

    private void WallJump()
    {
        canDoubleJump = true;
        rigidBody.linearVelocity = new Vector2(wallJumpForce.x * -facingDirection, wallJumpForce.y);

        Flip();
        StopWallJumpCoroutine();
        wallJumpCoroutine = StartCoroutine(WallJumpCoroutine());
    }

    private void StopWallJumpCoroutine()
    {
        if(wallJumpCoroutine != null)
        {
            StopCoroutine(wallJumpCoroutine);
        }
    }

    private IEnumerator WallJumpCoroutine()
    {
        isWallJumping = true;
        //float elapsedTime = 0f;

        while(!isGrounded || isWallDetected)
        {
            //elapsedTime += Time.deltaTime;
        yield return null;
        }

        //yield return new WaitForSeconds(wallJumpDuration);
        isWallJumping = false;
    }

    private void HandleWallSlide()
    {
        bool canWallSlide = isWallDetected && rigidBody.linearVelocityY < 0;
        float yModifier = yInput < 0 ? 1 : 0.05f;

        if(canWallSlide == false)
        {
            animator.transform.localPosition = new Vector2(0.5f, 0); // magic numeber because the free sprite is shit
            return;
        }
        animator.transform.localPosition = new Vector2(0.74f, 0); // magic numeber because the free sprite is shit
        rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocityX, rigidBody.linearVelocityY * yModifier);
    }

    private void Attack()
    {
        isAttacking = true;
    }

    private void HandleMovement()
    {
        if(isWallDetected)
        {
            return;
        }
        
        if(isWallJumping)
        {
            if(isGrounded)
            {
                StopWallJumpCoroutine();
                wallJumpCoroutine = null;
                isWallJumping = false;
            }
            return;
        }
    
        rigidBody.linearVelocity = new Vector2(xInput * moveSpeed, rigidBody.linearVelocityY);
    }

    private void HandleFlip()
    {
        bool hasChangedDiraction = xInput < 0 && facingRight || xInput > 0 && !facingRight;
        if(hasChangedDiraction)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingDirection = facingDirection * -1;
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
    }

    private void HandleAnimation()
    {
        animator.SetFloat("xVelocity", rigidBody.linearVelocityX);
        animator.SetFloat("yVelocity", rigidBody.linearVelocityY);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWallDetected", isWallDetected);
    }
}
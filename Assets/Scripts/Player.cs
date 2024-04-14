using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    [SerializeField] InputActionAsset input;
    SpriteRenderer sprite;
    Rigidbody2D rb;
    Animator animator;

    GameObject slashObject;
    Animator slashAnimator;
    SpriteRenderer slashSprite;

    [Header("Settings")]
    [SerializeField] float walkSpeed;
    [SerializeField]float maxWalkSpeed;
    [SerializeField] float crouchWalkSpeed;
    [SerializeField] float rollForce;
    [SerializeField] float jumpHeight;
    [SerializeField] float climbSpeed;
    [SerializeField] float attackSpeed;

    [Header("Values")]
    [SerializeField] float move;
    [SerializeField] float updown;
    [SerializeField] float jump;
    [SerializeField] float climb;
    [SerializeField] float run;
    [SerializeField] float crouch;
    [SerializeField] float attack;
    [SerializeField] sbyte direction;

    [Header("Checks")]
    public bool isGrounded = false;
    public bool isWalledLeft = false;
    public bool isWalledRight = false;
    public bool isClimbing = false;
    public bool hasRolled = false;
    public bool attackEnded = false;

    [Header("Player Statistics")]
    [SerializeField] float playerSpeedX;
    [SerializeField] float playerSpeedY;


    enum State {Idle ,Walk ,Jump ,Air ,Fall ,Climb ,Hanging ,Slide_Down ,Crouch ,Crouch_Walk ,Roll, Attack, Attack_Up, Attack_Down }
    State state = State.Idle;

    //-------------------------------------------------
    //Estados e animação

    void Update()
    {
        SwitchState();

        playerSpeedX = rb.velocity.x;
        playerSpeedY = rb.velocity.y;
    }

    void IdleState()
    {
        //actions
        animator.Play("Idle");
        animator.speed = 1;

        //transitions
        if (move != 0 && isGrounded)
        {
            state = State.Walk;
        }
        else if (jump == 1 && isGrounded)
        {
            state = State.Jump;
        }
        else if (crouch == 1 && isGrounded && move == 0)
        {
            state = State.Crouch;
        }
        else if ((isWalledLeft || isWalledRight) && climb == 1)
        {
            state = State.Hanging;
        }
        else if (attack == 1)
        {
            state = State.Attack;
        }
    }

    void WalkState()
    {
        //actions
        if (rb.velocity.normalized.x != 0) { animator.Play("Walk"); } // Fixes changing animation when walking in walls
        animator.speed = Mathf.Abs(rb.velocity.x) * 0.05f;
        SpriteFlip();
        
        //transitions
        if (isGrounded && rb.velocity.normalized.x == 0 && move == 0)
        {
            state = State.Idle;
        }
        else if (jump == 1 && isGrounded)
        {
            state = State.Jump;
        }
        else if (crouch == 1 && isGrounded && move != 0)
        {
            state = State.Crouch_Walk;
        }
        else if (attack == 1)
        {
            state = State.Attack;
        }
    }

    void JumpState()
    {
        //actions
        animator.Play("Jump");
        animator.speed = Mathf.Abs(rb.velocity.normalized.y) * 0.5f;
        SpriteFlip();

        //transitions
        if (rb.velocity.y < 1 && rb.velocity.y > -1 && !isGrounded)
        {
            state = State.Air;
        }
        else if ((isWalledLeft || isWalledRight) && climb == 1)
        {
            state = State.Hanging;
        }
    }

    void AirState()
    {
        // actions
        animator.Play("Air");
        animator.speed = 1;
        SpriteFlip();

        //transitions
        if (rb.velocity.y < -1 && !isGrounded)
        {
            state = State.Fall;
        }
        else if ((isWalledLeft || isWalledRight) && climb == 1)
        {
            state = State.Hanging;
        }
    }

    void FallState()
    {
        //actions
        animator.Play("Fall");
        animator.speed = Mathf.Abs(rb.velocity.normalized.y) * 0.2f;
        SpriteFlip();

        //transition
        if (isGrounded)
        {
            state = State.Idle;
        }
        else if ((isWalledLeft || isWalledRight) && climb == 1)
        {
            state = State.Hanging;
        }
    }

    void CrouchState()
    {
        //actions
        animator.Play("Crouch");
        animator.speed = 1;
        hasRolled = false;

        //transition
        if (crouch == 0 && isGrounded && move == 0)
        {
            state = State.Idle;
        }
        else if (crouch == 1 && isGrounded && move != 0)
        {
            state = State.Crouch_Walk;
        }
        else if (crouch == 1 && jump == 1 && isGrounded)
        {
            state = State.Roll;
        }
    }

    void Crouch_WalkState()
    {
        //actions
        animator.Play("Crouch_Walk");
        animator.speed = crouchWalkSpeed * 0.25f;
        SpriteFlip();

        //transition
        if (crouch == 1 && isGrounded && move == 0)
        {
            state = State.Crouch;
        }
        else if (crouch == 0 && isGrounded && move != 0)
        {
            state = State.Walk;
        }
        else if (crouch == 1 && jump == 1 && isGrounded)
        {
            state = State.Roll;
        }
    }

    void RollState()
    {
        //actions
        animator.Play("Roll");
        animator.speed = Mathf.Abs(rb.velocity.x) * 0.05f;
        hasRolled = true;

        //transition
        if (rb.velocity.normalized.x == 0)
        {
            state = State.Crouch;
        }
    }

    void ClimbState()
    {
        //actions
        switch (updown)
        {
            case 1:
                animator.Play("Climb");
                break;
            case -1:
                animator.Play("Climb_Down");
                break;
        }
        animator.speed = Mathf.Abs(rb.velocity.y) * 0.05f;

        //transition
        if (updown == 0)
        {
            state = State.Hanging;
        }
        else if (jump == 1)
        {
            state = State.Jump;
            isClimbing = false;
        }
        else if (run == 1 && updown == -1)
        {
            state = State.Slide_Down;
        }

    }

    void HangingState()
    {
        //action
        animator.Play("Hanging");
        animator.speed = 1;
        isClimbing = true;

        if (isWalledLeft)
        {
            sprite.flipX = false;
            direction = -1;
        }
        else if (isWalledLeft)
        {
            sprite.flipX = true;
            direction = 1;
        }

        //transition
        if (jump == 1)
        {
            state = State.Jump;
            isClimbing = false;
        }
        else if (updown != 0 && run == 0)
        {
            state = State.Climb;
        }
        else if (run == 1 && updown == -1)
        {
            state = State.Slide_Down;
        }
    }

    void Slide_DownState()
    {
        //action
        animator.Play("Slide_Down");
        animator.speed = Mathf.Abs(rb.velocity.y) * 0.1f;


        //transition
        if (run == 0 && updown == 0)
        {
            state = State.Hanging;
        }
        else if (run == 0 && updown != 0)
        {
            state = State.Climb;
        }
        else if (jump == 1)
        {
            state = State.Jump;
            isClimbing = false;
        }

    }

    void AttackState()
    {
        //action
        animator.Play("Attack");
        animator.speed = attackSpeed * 0.1f;

        slashObject.transform.position = this.transform.position + new Vector3(0.6f * direction, 0.3f, 0);
        SlashSpriteFlip();
        slashSprite.enabled = true;

        //transition
        if (attackEnded)
        {
            attackEnded = false;
            slashSprite.enabled = false;

            if (isGrounded && move == 0)
            {
                state = State.Idle;
            }
            else if (isGrounded && move != 0)
            {
                state = State.Walk;
            }
            else if (!isGrounded)
            {
                state = State.Jump;
            }
        }
    }

    void SwitchState()
    {
        switch (state)
        {
            case State.Idle:        IdleState();        break;
            case State.Walk:        WalkState();        break;
            case State.Jump:        JumpState();        break;
            case State.Air :        AirState();         break;
            case State.Fall:        FallState();        break;
            case State.Crouch:      CrouchState();      break;
            case State.Crouch_Walk: Crouch_WalkState(); break;
            case State.Roll:        RollState();        break;
            case State.Climb:       ClimbState();       break;
            case State.Hanging:     HangingState();     break;
            case State.Slide_Down:  Slide_DownState();  break;
            case State.Attack:      AttackState();      break;
        }
    }

    void SpriteFlip()
    {
        //Virar sprite para direita
        if ((move > 0 || rb.velocity.normalized.x > 0) && sprite.flipX == false)
        {
            sprite.flipX = true;
            direction = 1;
        }
        //Virar sprite para esquerda
        else if ((move < 0 || rb.velocity.normalized.x < 0) && sprite.flipX == true)
        {
            sprite.flipX = false;
            direction = -1;
        }
    }

    void SlashSpriteFlip()
    {
        switch (direction)
        {
            case 1:
                slashSprite.flipX = true;
                break;
            case -1:
                slashSprite.flipX = false;
                break;
        }
    }

    void AttackEnd(int end)
    {
        if (end == 1)
        {
            attackEnded = true;
        }
        else
        {
            attackEnded = false;
        }
    }

    //---------------------------------------------------------
    //Física e input

    void FixedUpdate()
    {
        InputValues();
        StatePhysics();
    }

    private void StatePhysics()
    {
        switch (state)
        {
            case State.Jump:
            case State.Air:
            case State.Fall:
                rb.AddRelativeForce(new Vector2(move,0), ForceMode2D.Impulse);

                if (isGrounded)
                {
                rb.AddRelativeForce(new Vector2(0, jumpHeight), ForceMode2D.Impulse);
                }
                break;

            case State.Walk:
                rb.AddRelativeForce(new Vector2(move * walkSpeed, 0), ForceMode2D.Impulse);
                rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxWalkSpeed);
                break;

            case State.Crouch_Walk:
                rb.AddRelativeForce(new Vector2(move * crouchWalkSpeed, 0), ForceMode2D.Impulse);
                break;

            case State.Roll:
                if (!hasRolled)
                {
                    rb.AddRelativeForce(new Vector2(rollForce * direction, 0), ForceMode2D.Impulse);
                }
                break;

            case State.Climb:
                rb.velocity = new Vector2(0, updown * climbSpeed);
                break;

            case State.Slide_Down:
                rb.velocity = new Vector2(0, climbSpeed * -2);
                break;

            case State.Hanging:
                rb.velocity = new Vector2(0,0);
                break;
        }

        //Movimento no ar
        if ((state == State.Hanging || state == State.Climb || state == State.Slide_Down) && jump == 1)
        {
            rb.AddRelativeForce(new Vector2((jumpHeight) * (direction * -1),jumpHeight), ForceMode2D.Impulse);
        }

        //Tirar gravidade enquanto estiver escalando
        if (state == State.Hanging || state == State.Climb || state == State.Slide_Down)
        {
            rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = 1;
        }
    }

    void InputValues()
    {
        move    = input.FindActionMap("Player").FindAction("move").ReadValue<float>();
        jump    = Mathf.Ceil(input.FindActionMap("Player").FindAction("jump").ReadValue<float>());
        climb   = Mathf.Ceil(input.FindActionMap("Player").FindAction("climb").ReadValue<float>());
        run     = Mathf.Ceil(input.FindActionMap("Player").FindAction("run").ReadValue<float>());
        crouch  = Mathf.Ceil(input.FindActionMap("Player").FindAction("crouch").ReadValue<float>());
        attack  = Mathf.Ceil(input.FindActionMap("Player").FindAction("attack").ReadValue<float>());
        updown  = input.FindActionMap("Player").FindAction("updown").ReadValue<float>();
    }

    // Filtros para achar o contato, dependendo do angulo de contato do outro objeto
    ContactFilter2D contactFilterGround;
    ContactFilter2D contactFilterLeft;
    ContactFilter2D contactFilterRight;

    void OnCollisionEnter2D()
    {
        if (rb.IsTouching(contactFilterGround))
        {
            isGrounded = true;
        }

        if (rb.IsTouching(contactFilterLeft))
        {
            isWalledLeft = true;
        }
        else if (rb.IsTouching(contactFilterRight))
        {
            isWalledRight = true;
        }
    }

    void OnCollisionExit2D()
    {
        if (!rb.IsTouching(contactFilterGround))
        {
            isGrounded = false;
        }

        if (!rb.IsTouching(contactFilterLeft))
        {
            isWalledLeft = false;
        }

        if (!rb.IsTouching(contactFilterRight))
        {
            isWalledRight = false;
        }
    }

    //---------------------------------------------------

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        contactFilterGround.useNormalAngle = true;
        contactFilterGround.minNormalAngle = 90f;
        contactFilterGround.maxNormalAngle = 90f;

        contactFilterLeft.useNormalAngle = true;
        contactFilterLeft.minNormalAngle = 0f;
        contactFilterLeft.maxNormalAngle = 0f;

        contactFilterRight.useNormalAngle = true;
        contactFilterRight.minNormalAngle = 179f;
        contactFilterRight.maxNormalAngle = 180f;

        slashObject = this.transform.GetChild(0).gameObject;
        slashAnimator = slashObject.GetComponent<Animator>();
        slashSprite = slashObject.GetComponent<SpriteRenderer>();
        slashSprite.enabled = false;
    }
}

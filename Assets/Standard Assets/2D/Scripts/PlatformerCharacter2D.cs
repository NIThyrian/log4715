using System;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {
        [SerializeField] private float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        [SerializeField] private float m_JumpForce = 400f;                  // Amount of force added when the player jumps.
        [SerializeField] private float m_MaxChargedJumpForce = 400f;           // If the m_MaxChargedJumpForce is set to 0, then max jump force will be m_JumpForce
        [SerializeField] private uint m_MaxJumps = 3;                       // Max jumps               
        [Range(0, 1)][SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
        [SerializeField] private bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
        [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character

        private uint m_JumpCount = 0; // The current number of in air jumps
        private float m_AccumulatedChargedJumpForce = 0f; // The added force added to a charged jump
        private float m_ChargedJumpForceIncrement; // The increment of the accumulated jump force per FixedUpdate
        private const float k_DelayBeforeMaxAccumulatedChargedJumpForce = 3f; // The amount of time before m_AccumulatedChargedJumpForce is maxed

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded

        public bool Grounded { get; private set; }

        private Transform m_WallCheck;    // A position marking where to check if the player is against a wall.
        const float k_WallCheckRadius = .05f; // Radius of the overlap circle to determine if against a wall.
        private bool m_AgainstWall;            // Whether or not the player is against wall.
        private float m_WallTimer = 0f;    // A timer to recheck if player hits wall

        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up

        private Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.

        private int Direction => m_FacingRight ? 1 : -1;

        private void Awake()
        {
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_WallCheck = transform.Find("WallCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();

            // Cross product to calculate the increment amount to accumulate a max amount of charge with every Time.fixedDeltaTime seconds
            m_ChargedJumpForceIncrement = Time.fixedDeltaTime * m_MaxChargedJumpForce / k_DelayBeforeMaxAccumulatedChargedJumpForce;
        }


        private void FixedUpdate()
        {
            Grounded = false;
            m_AgainstWall = false;

            // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
            // This can be done using layers instead but Sample Assets will not overwrite your project settings.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject)
                    Grounded = true;
            }
            m_Anim.SetBool("Ground", Grounded);

            // Set the vertical animation
            m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);

            Collider2D[] wallCollider = Physics2D.OverlapCircleAll(m_WallCheck.position, k_WallCheckRadius, m_WhatIsGround);
            for (int i = 0; i < wallCollider.Length; i++)
            {
                if (wallCollider[i].gameObject != gameObject)
                    m_AgainstWall = true;
            }
        }


        public void Move(float move, bool crouch, bool jump, bool charging)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (Grounded || m_AirControl)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move * m_CrouchSpeed : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));

                // Move the character
                if (m_WallTimer > 0)
                {
                    m_WallTimer -= Time.fixedDeltaTime;
                }
                else
                {
                    m_Rigidbody2D.velocity = new Vector2(move * m_MaxSpeed, m_Rigidbody2D.velocity.y);

                    // If the input is moving the player right and the player is facing left...
                    if (move > 0 && !m_FacingRight)
                    {
                        // ... flip the player.
                        Flip();
                    }
                    // Otherwise if the input is moving the player left and the player is facing right...
                    else if (move < 0 && m_FacingRight)
                    {
                        // ... flip the player.
                        Flip();
                    }
                }

            }

            if (charging)
            {
                if (Grounded)
                {
                    // Increment m_AccumulatedChargedJumpForce until m_MaxChargedJumpForce
                    m_AccumulatedChargedJumpForce = Math.Min(m_MaxChargedJumpForce, m_AccumulatedChargedJumpForce + m_ChargedJumpForceIncrement);
                }
            }
            // If the player is not charging anymore and has accumulated some jump charge
            else if (Grounded && m_AccumulatedChargedJumpForce > 0)
            {
                Jump(m_JumpForce + m_AccumulatedChargedJumpForce);
                m_AccumulatedChargedJumpForce = 0;
            }
            else if (jump)
            {

                if (!Grounded && m_AgainstWall)
                {
                    m_WallTimer = 0.7f;
                    Flip();
                    m_Rigidbody2D.AddForce(new Vector2(m_JumpForce * Direction, m_JumpForce));
                }
                else
                {
                    if (Grounded)
                    {
                        m_JumpCount = 0;
                        Grounded = false;
                        m_Anim.SetBool("Ground", false);
                    }

                    // Handles infinity
                    if (m_MaxJumps == uint.MaxValue || m_JumpCount++ <= m_MaxJumps)
                    {
                        // Add a vertical force to the player.
                        Jump(m_JumpForce);
                    }
                }

            }
        }

        private void Jump( float force)
        {
            m_Rigidbody2D.AddForce(new Vector2(0f, force));
        }


        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }
}

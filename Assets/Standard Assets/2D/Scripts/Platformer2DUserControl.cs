using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets._2D
{
    [RequireComponent(typeof (PlatformerCharacter2D))]
    public class Platformer2DUserControl : MonoBehaviour
    {
        private PlatformerCharacter2D m_Character;
        private bool m_Jump;

        private bool IsCrouching => Input.GetKey(KeyCode.LeftControl);
        private bool IsJumping => Input.GetKey(KeyCode.Space);
        private bool IsCharging => IsCrouching && IsJumping;

        private void Awake()
        {
            m_Character = GetComponent<PlatformerCharacter2D>();
        }


        private void Update()
        {
            if (!m_Jump)
            {   
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }
            
        }


        private void FixedUpdate()
        {
            
            // Read the inputs.
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            // Pass all parameters to the character control script.
            m_Character.Move(h, IsCrouching, m_Jump, IsCharging);
            m_Jump = false;
        }
    }
}

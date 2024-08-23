using System;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Classes.Player.Input
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Movement Settings")] 
        public bool analogMovement;

        [Header("Mouse Cursor Settings")] 
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;
        
        [Header("Movement Input")]
        public float horizontal;
        public float vertical;
        public float moveAmount;
        
        [Header("Camera Inputs")] 
        public float camHorizontal;
        public float camVertical;
        public float camMoveAmount;
        
        [Space]
        public bool sprintInput;
        public bool jumpInput;
        
        [Header("Shooting Inputs")]
        public bool aimInput;
        public bool shootInput;
        
        [Header("UI Inputs")]
        public bool emoteWheelInput;
        public bool weaponWheelInput;
        public bool escapeMenuInput;
        public bool inventoryInput;
        
     
        
        // public bool findCover;  this is pseudo

        //private vector 2s
        public Vector2 moveInput;
        public Vector2 camMoveInput;
        
        private Player_Controls _playerControls;

        #region  Player flags

        public bool isAiming;
        public bool isShooting;
        public bool isInteracting;
        public bool isInMenu;

        #endregion


        public void OnEnable()
        {
            if (_playerControls == null)
            {
                _playerControls = new Player_Controls();
                PlayerMovementInputs();
                PlayerActionInputs();
                PlayerUIActionInputs();
                PlayerCameraMovementInputs();
            }
            
            _playerControls.Enable();
        }

        #region On Enable Custom Methods
       
        private void PlayerMovementInputs()
        {
            // movement input, this will need modification to allow for a virtual joystick adaptation
            _playerControls.Player_Movement.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            _playerControls.Player_Movement.Move.canceled += ctx => moveInput = ctx.ReadValue<Vector2>();
                
            _playerControls.Player_Movement.Sprint.performed += ctx => sprintInput = true;
            _playerControls.Player_Movement.Sprint.canceled += ctx => sprintInput = false;
                
            _playerControls.Player_Movement.Jump.performed += ctx => jumpInput = true;
            _playerControls.Player_Movement.Jump.canceled += ctx => jumpInput = false;
        }
        private void PlayerActionInputs()
        {
            // shoot / aim inputs, again we will need to figure out how we allow for shooing on mobile
            _playerControls.Player_Actions.Aim.performed += ctx => aimInput = true;
            _playerControls.Player_Actions.Aim.canceled += ctx => aimInput = false;
            
            _playerControls.Player_Actions.Shoot.performed += ctx => shootInput = true;
            _playerControls.Player_Actions.Shoot.canceled += ctx => shootInput = false;
        }

        private void PlayerUIActionInputs()
        {
            // all inputs for UI controls, some will require flags such as inventory menu, will need to use a flag to toggle on and off
            _playerControls.Player_UI_Actions.EmoteWheel.performed += ctx => emoteWheelInput = true;
            _playerControls.Player_UI_Actions.EmoteWheel.canceled += ctx => emoteWheelInput = false;
            
            _playerControls.Player_UI_Actions.WeaponWheel.performed += ctx => weaponWheelInput = true;
            _playerControls.Player_UI_Actions.WeaponWheel.canceled += ctx => weaponWheelInput = false;

            _playerControls.Player_UI_Actions.Inventory.performed += ctx => inventoryInput = !inventoryInput; // this should toggle the inventory bool
            _playerControls.Player_UI_Actions.Menu.performed += ctx => escapeMenuInput = !escapeMenuInput;


        }

        private void PlayerCameraMovementInputs()
        {
            _playerControls.Player_Camera_Movement.Look.performed += ctx => camMoveInput = ctx.ReadValue<Vector2>();
            _playerControls.Player_Camera_Movement.Look.canceled += ctx => camMoveInput = ctx.ReadValue<Vector2>();
        }
        
        #endregion

        #region Tick Setup

        public void TickInput(float delta)
        {
            // all handling methods will go in here
            
            HandleMoveInput();
            HandleCameraMoveInput();
        }

        #endregion


        #region Handlers

        private void HandleMoveInput()
        {
            horizontal = moveInput.x;
            vertical = moveInput.y;

            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
        }


        private void HandleCameraMoveInput()
        {
            camHorizontal = camMoveInput.x;
            camVertical = camMoveInput.y;

            camMoveAmount = Mathf.Clamp01(Mathf.Abs(camHorizontal) + camVertical);
        }
        #endregion
    }
}

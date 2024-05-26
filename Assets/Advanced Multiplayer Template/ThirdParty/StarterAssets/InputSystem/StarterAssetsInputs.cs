using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
        public bool aim;
        public bool shoot;
        public bool toggleCamera;
        public bool enter;
        public bool weaponWheel;
        public bool emoteWheel;

        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		/*public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}*/

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

        public void OnAim(InputValue value)
        {
            AimInput(value.isPressed);
        }

        public void OnWeaponWheel(InputValue value)
        {
            WeaponWheelInput(value.isPressed);
        }

        public void OnEmoteWheel(InputValue value)
        {
            EmoteWheelInput(value.isPressed);
        }

        public void OnShoot(InputValue value)
        {
            ShootInput(value.isPressed);
        }

        public void OnToggleCamera(InputValue value)
        {
            ToggleCameraInput(value.isPressed);
        }


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

        public void AimInput(bool newAimState)
        {
            aim = newAimState;
        }

        public void WeaponWheelInput(bool newWeaponWheelState)
        {
            weaponWheel = newWeaponWheelState;
        }

        public void EmoteWheelInput(bool newEmoteWheelState)
        {
            emoteWheel = newEmoteWheelState;
        }

        public void ShootInput(bool newShootState)
        {
            shoot = newShootState;
        }

        public void ToggleCameraInput(bool newToggleCameraState)
        {
            toggleCamera = newToggleCameraState;
        }

        public void EnterInput(bool newEnterState)
        {
            enter = newEnterState;
        }

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}
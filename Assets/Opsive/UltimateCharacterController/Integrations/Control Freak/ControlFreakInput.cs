/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using ControlFreak2;
#if UNITY_EDITOR
using UnityEditor;
using ControlFreak2Editor;
#endif

namespace Opsive.Shared.Input.ControlFreak
{
    /// <summary>
    /// Acts as a bridge for Control Freak input.
    /// </summary>
    public class ControlFreakInput : PlayerInput
    {
        [Tooltip("Hide all controls when non in gameplay mode?")]
        public bool m_HideAllTouchControlsWhenNotInGameplay = true;
        [Tooltip("Rig Switch used to disable gameplay-only controls.")]
        public string m_NonGameplaySwitchName = "Non-Gameplay";

        /// <summary>
        /// Internal method which returns true if the button is being pressed.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True of the button is being pressed.</returns>
        protected override bool GetButtonInternal(string name)
        {
            return CF2Input.GetButton(name);
        }

        /// <summary>
        /// Internal method which returns true if the button was pressed this frame.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True if the button is pressed this frame.</returns>
        protected override bool GetButtonDownInternal(string name)
        {
            return CF2Input.GetButtonDown(name);
        }

        /// <summary>
        /// Internal method which returnstrue if the button is up.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>True if the button is up.</returns>
        protected override bool GetButtonUpInternal(string name)
        {
            return CF2Input.GetButtonUp(name);
        }

        /// <summary>
        /// Internal method which returns the value of the axis with the specified name.
        /// </summary>
        /// <param name="name">The name of the axis.</param>
        /// <returns>The value of the axis.</returns>
        protected override float GetAxisInternal(string name)
        {
            return CF2Input.GetAxisRaw(name);
        }

        /// <summary>
        /// Internal method which returns the value of the raw axis with the specified name.
        /// </summary>
        /// <param name="name">The name of the axis.</param>
        /// <returns>The value of the raw axis.</returns>
        protected override float GetAxisRawInternal(string name)
        {
            return CF2Input.GetAxisRaw(name);
        }

        /// <summary>
        /// Returns the position of the mouse.
        /// </summary>
        /// <returns>The mouse position.</returns>
        public override Vector2 GetMousePosition()
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Returns true if the pointer is over a UI element.
        /// </summary>
        /// <returns>True if the pointer is over a UI element.</returns>
        public override bool IsPointerOverUI()
        {
            return false;
        }

        /// <summary>
        /// Enables or disables gameplay input. An example of when it will not be enabled is when there is a fullscreen UI over the main camera.
        /// </summary>
        /// <param name="enable">True if the input is enabled.</param>
        protected override void EnableGameplayInput(bool enable)
        {
            base.EnableGameplayInput(enable);

            if (CF2Input.activeRig != null) {
                Debug.Log(enable);
                if (m_HideAllTouchControlsWhenNotInGameplay) {
                    CF2Input.activeRig.ShowOrHideTouchControls(enable, false);
                }

                if (!string.IsNullOrEmpty(m_NonGameplaySwitchName)) {
                    CF2Input.activeRig.SetSwitchState(m_NonGameplaySwitchName, !enable);
                }
            }
        }

#if UNITY_EDITOR
        private const string m_DialogueTitle = "Add ControlFreak2Input Component";

        [MenuItem(CFEditorUtils.INTEGRATIONS_MENU_PATH + "Opsive UCC/Add CF2 Input to selected object.", false, CFEditorUtils.INTEGRATIONS_MENU_PRIO)]
        static private void AddToSelected()
        {
            if (Selection.activeTransform == null) {
                EditorUtility.DisplayDialog(m_DialogueTitle, "Nothing is selected!", "OK");
                return;
            }

            AddToObject(Selection.activeTransform);
        }
        
        [MenuItem(CFEditorUtils.INTEGRATIONS_MENU_PATH + "Opsive UCC/Find existing PlayeInput and add CF2 Input", false, CFEditorUtils.INTEGRATIONS_MENU_PRIO)]
        static private void AddToExistingPlayerInput()
        {
            var obj = GameObject.FindObjectOfType<PlayerInput>();

            if (obj == null) {
                EditorUtility.DisplayDialog(m_DialogueTitle, "There's no PlayerInput in the scene!", "OK");
                return;
            }

            AddToObject(obj.transform);
        }

        static private void AddToObject(Transform obj)
        {
            if (obj == null)
                return;

            // Look for other PlayerInput components.
            var inputList = obj.GetComponents<PlayerInput>();
            ControlFreakInput existingComponent = null;
            if (inputList != null) {
                for (int i = 0; i < inputList.Length; ++i) {
                    if (inputList[i] is ControlFreakInput) {
								if (!existingComponent || !existingComponent.enabled)
									existingComponent = inputList[i] as ControlFreakInput;
                    }

                }
            }

				if ((existingComponent && existingComponent.enabled) && (inputList.Length == 1))
					{
					EditorUtility.DisplayDialog(m_DialogueTitle, obj.name + " : CF2 Input is already added!", "OK");
               return;
					}


            var undoLabel = ((existingComponent == null) ? "Add ControlFreak2Input" : "Re-enable ControlFreak2Input");

            // Remove other PlayeInputs.

				int removedCount = 0;

				for (int i = 0; i < inputList.Length; ++i) {
					if (inputList[i] == existingComponent)
						continue;

               Undo.RecordObject(inputList[i], undoLabel);
               Undo.DestroyObjectImmediate(inputList[i]);

					++removedCount;
               }
            

            // Add Control Freak 2 Input.
            if (existingComponent == null) {
                ControlFreakInput c = obj.gameObject.AddComponent<ControlFreakInput>();
                Undo.RegisterCreatedObjectUndo(c, undoLabel);
            } else { // Re-enable.
                CFGUI.CreateUndo(undoLabel, existingComponent);
                existingComponent.enabled = true;
                EditorUtility.SetDirty(existingComponent);
            }

			Undo.FlushUndoRecordObjects();

            EditorUtility.DisplayDialog(m_DialogueTitle, ("[" + obj.name + "] : " +
                ((existingComponent == null) ? "Added" : "Re-enabled") + " ControlFreak2Input component" +
                ((removedCount > 0) ? " (and removed other Input components)" : "") + "."), "OK");
        }
#endif
    }
}
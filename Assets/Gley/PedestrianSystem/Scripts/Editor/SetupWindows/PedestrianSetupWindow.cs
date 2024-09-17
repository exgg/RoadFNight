using Gley.UrbanSystem.Editor;
using UnityEditor;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianSetupWindow : SetupWindowBase
    {
        protected PedestrianSettingsWindowData _editorSave;

        internal override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _editorSave = new SettingsLoader(Internal.PedestrianSystemConstants.WindowSettingsPath).LoadSettingsAsset<PedestrianSettingsWindowData>();
            return this;
        }


        internal override void DestroyWindow()
        {
            EditorUtility.SetDirty(_editorSave);
            base.DestroyWindow();
        }
    }
}
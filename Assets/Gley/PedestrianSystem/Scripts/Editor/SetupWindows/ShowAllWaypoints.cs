using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    internal class ShowAllWaypoints : ShowWaypointsWindow
    {
        internal override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            return this;
        }


        internal override void DrawInScene()
        {
            _pedestrianWaypointDrawer.ShowAllWaypoints(_editorSave.EditorColors.WaypointColor, _editorSave.ShowConnections, _editorSave.ShowVehicles, _editorSave.EditorColors.AgentColor, _editorSave.ShowPriority, _editorSave.EditorColors.PriorityColor);
            base.DrawInScene();
        }
    }
}
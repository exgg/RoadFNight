using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    internal class GridSetupWindow : GridSetupWindowBase
    {
        internal override void DrawInScene()
        {
            if (_viewGrid)
            {
                _gridDrawer.DrawGrid(false);
            }
            base.DrawInScene();
        }
    }
}

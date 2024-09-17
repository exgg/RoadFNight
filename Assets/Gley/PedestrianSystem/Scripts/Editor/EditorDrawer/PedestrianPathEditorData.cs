using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianPathEditorData : RoadEditorData<PedestrianPath>
    {
        internal override PedestrianPath[] GetAllRoads()
        {
            return _allRoads;
        }
    }
}

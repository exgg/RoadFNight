using System.Collections.Generic;
using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    /// <summary>
    /// Add additional specific pedestrian settings for the settings window data. 
    /// </summary>
    internal class PedestrianSettingsWindowData : SettingsWindowData
    {
        public List<PedestrianTypes> GlobalPedestrianList = new List<PedestrianTypes>();

        internal override SettingsWindowData Initialize()
        {
            if (LaneWidth == default)
            {
                LaneWidth = 4;
            }
            if (WaypointDistance == default)
            {
                WaypointDistance = 4;
            }
            return this;
        }
    }
}
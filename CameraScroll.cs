using System;
using Partiality.Modloader;

namespace CameraScroll
{
    public class CameraScroll : PartialityMod
    {
        public override void Init()
        {
            ModID = "Camera Scroll";
        }
        
        public override void OnLoad()
        {
            RoomCameraHK.Hook();
            SmallHK.Hook();
        }
    }
}
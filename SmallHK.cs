using System;
using UnityEngine;

namespace CameraScroll
{
    public static class SmallHK
    {
        public static void Hook()
        {
            On.GhostWorldPresence.GhostMode += GWPGhostModeHook;
            On.AboveCloudsView.CloseCloud.DrawSprites += ACVCCDrawSpritesHook;
            On.AboveCloudsView.DistantCloud.DrawSprites += ACVDCDrawSpritesHook;
            On.AboveCloudsView.FlyingCloud.DrawSprites += ACVFCDrawSpritesHook;
            On.SuperStructureProjector.SingleGlyph.DrawSprites += SSPSGDrawSpritesHook;
            On.SuperStructureProjector.GlyphMatrix.DrawSprites += SSPGMDrawSpritesHook;
            On.Room.ViewedByAnyCamera += ViewedByAnyCameraHook;
        }
        
        public static float GWPGhostModeHook(On.GhostWorldPresence.orig_GhostMode orig, GhostWorldPresence presence, Room room, int camPos)
        {
            if (!RoomCameraHK.ShouldScroll(room))
                return orig(presence, room, camPos);
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            float res = orig(presence, room, camPos);
            room.cameraPositions = cameraPositions;
            return res;
        }
        
        public static void ACVCCDrawSpritesHook(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, AboveCloudsView.CloseCloud cloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Room room = rCam.room;
            RoomCameraHK.EnsureRoomInit(room);
            if (!RoomCameraHK.ShouldScroll(room))
            {
                orig(cloud, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            orig(cloud, sLeaser, rCam, timeStacker, camPos);
            room.cameraPositions = cameraPositions;
        }
        
        public static void ACVDCDrawSpritesHook(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, AboveCloudsView.DistantCloud cloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Room room = rCam.room;
            RoomCameraHK.EnsureRoomInit(room);
            if (!RoomCameraHK.ShouldScroll(room))
            {
                orig(cloud, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            orig(cloud, sLeaser, rCam, timeStacker, camPos);
            room.cameraPositions = cameraPositions;
        }
        
        public static void ACVFCDrawSpritesHook(On.AboveCloudsView.FlyingCloud.orig_DrawSprites orig, AboveCloudsView.FlyingCloud cloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Room room = rCam.room;
            RoomCameraHK.EnsureRoomInit(room);
            if (!RoomCameraHK.ShouldScroll(room))
            {
                orig(cloud, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            orig(cloud, sLeaser, rCam, timeStacker, camPos);
            room.cameraPositions = cameraPositions;
        }
        
        public static void SSPSGDrawSpritesHook(On.SuperStructureProjector.SingleGlyph.orig_DrawSprites orig, SuperStructureProjector.SingleGlyph glyph, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Room room = rCam.room;
            RoomCameraHK.EnsureRoomInit(room);
            if (!RoomCameraHK.ShouldScroll(room))
            {
                orig(glyph, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            orig(glyph, sLeaser, rCam, timeStacker, camPos);
            room.cameraPositions = cameraPositions;
        }
        
        public static void SSPGMDrawSpritesHook(On.SuperStructureProjector.GlyphMatrix.orig_DrawSprites orig, SuperStructureProjector.GlyphMatrix matrix, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Room room = rCam.room;
            RoomCameraHK.EnsureRoomInit(room);
            if (!RoomCameraHK.ShouldScroll(room))
            {
                orig(matrix, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            Vector2[] cameraPositions = room.cameraPositions;
            room.cameraPositions = RoomCameraHK.origCameraPositions[room.abstractRoom.name];
            orig(matrix, sLeaser, rCam, timeStacker, camPos);
            room.cameraPositions = cameraPositions;
        }
        
        public static bool ViewedByAnyCameraHook(On.Room.orig_ViewedByAnyCamera orig, Room self, Vector2 pos, float margin)
        {
            RoomCameraHK.EnsureRoomInit(self);
            if (!RoomCameraHK.ShouldScroll(self))
                return orig(self, pos, margin);
            Vector2[] cameraPositions = self.cameraPositions;
            self.cameraPositions = RoomCameraHK.origCameraPositions[self.abstractRoom.name];
            bool res = orig(self, pos, margin);
            self.cameraPositions = cameraPositions;
            return res;
        }
    }
}
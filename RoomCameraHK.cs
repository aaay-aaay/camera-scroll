using System;
using RWCustom;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace CameraScroll
{
    public static class RoomCameraHK
    {
        public static void Hook()
        {
            On.Room.LoadFromDataString += LoadFromDataStringHook;
            On.RoomCamera.ChangeRoom += ChangeRoomHook;
            On.RoomCamera.Update += UpdateHook;
            On.RoomCamera.MoveCamera2 += MoveCamera2Hook;
            On.RoomCamera.ApplyPositionChange += ApplyPositionChangeHook;
            On.RoomCamera.DrawUpdate += DrawUpdateHook;
            On.RoomCamera.CamPos += CamPosHook;
            On.RoomCamera.PreLoadTexture += PreLoadTextureHook;
            On.RoomCamera.ApplyDepth += ApplyDepthHook;
        }
        
        public static void LoadFromDataStringHook(On.Room.orig_LoadFromDataString orig, Room room, string[] lines)
        {
            origCameraPositions.Clear();
            orig(room, lines);
        }
        
        public static bool ShouldScroll(RoomCamera rCam)
        {
            return ShouldScroll(rCam.room);
        }
        
        public static bool ShouldScroll(Room room)
        {
            if (room == null) return true;
            bool should;
            string name = room.abstractRoom.name;
            if (shouldScrollRooms.TryGetValue(name, out should))
            {
                return should;
            }
            // If the code reaches this point, no camera scroll code has touched the room.
            if (room.cameraPositions.Length == 1)
            {
                should = false;
            }
            else
            {
                should = true;
            }
            shouldScrollRooms[name] = should;
            return should;
        }
        
        public static bool ShouldScroll(RoomCamera rCam, string room)
        {
            AbstractRoom aroom = rCam.game.world.GetAbstractRoom(room);
            if (aroom == null || aroom.realizedRoom == null) return room.Split('_')[1][0] != 'A';
            return ShouldScroll(aroom.realizedRoom);
        }
        
        public static bool IsGate(string url)
        {
            string[] path = url.Split('_')[0].Split('\\');
            return path[path.Length - 1] == "GATE";
        }
        
        public static string GetRoomName(string url)
        {
            string[] parts = url.Split('_');
            string[] firstPart = parts[0].Split('\\');
            firstPart = new string[]{firstPart[firstPart.Length - 1]};
            return firstPart[0] + "_" + parts[1] + (firstPart[0] == "GATE" ? ("_" + parts[2]) : "");
        }
        
        public static string GetPng(string url)
        {
            string[] parts = url.Split('_');
            return parts[0] + "_" + parts[1] + (IsGate(url) ? "_" + parts[2] : "") + ".png";
        }
        
        public static void EnsureRoomInit(Room room)
        {
            if (!ShouldScroll(room)) return;
            if (origCameraPositions.ContainsKey(room.abstractRoom.name))
            {
                Debug.Log("ROOM " + room.abstractRoom.name + " ALREADY AT " + room.cameraPositions[0].x + "," + room.cameraPositions[0].y);
                return;
            }
            origCameraPositions[room.abstractRoom.name] = room.cameraPositions;
            float x = float.MaxValue;
            float y = float.MaxValue;
            for (int i = 0; i < room.cameraPositions.Length; i++)
            {
                Vector2 pos = room.cameraPositions[i];
                Debug.Log("CHECKING " + pos.x + "," + pos.y);
                if (pos.x < x) x = pos.x;
                if (pos.y < y) y = pos.y;
            }
            room.cameraPositions = new Vector2[1];
            room.cameraPositions[0] = new Vector2(x, y);
            Debug.Log("ROOM " + room.abstractRoom.name + " AT " + x + "," + y);
        }
        
        public static void ChangeRoomHook(On.RoomCamera.orig_ChangeRoom orig, RoomCamera rCam, Room newRoom, int cameraPosition)
        {
            if (!ShouldScroll(newRoom))
            {
                orig(rCam, newRoom, cameraPosition);
                return;
            }
            EnsureRoomInit(newRoom);
            orig(rCam, newRoom, 0);
        }
        
        public static void UpdateHook(On.RoomCamera.orig_Update orig, RoomCamera rCam)
        {
            orig(rCam);
            if (!ShouldScroll(rCam)) return;
            if (rCam.followAbstractCreature != null && rCam.followAbstractCreature.realizedCreature != null && rCam.followAbstractCreature.realizedCreature.mainBodyChunk != null
             && rCam.game != null && rCam.game.rainWorld != null && rCam.game.rainWorld.options != null)
            {
                Creature creature = rCam.followAbstractCreature.realizedCreature;
                Vector2 pos = creature.mainBodyChunk.pos;
                if (rCam.game.shortcuts != null)
                {
                    foreach (ShortcutHandler.ShortCutVessel vessel in rCam.game.shortcuts.transportVessels)
                    {
                        if (vessel.creature == creature)
                        {
                            pos = new Vector2(10 + vessel.pos.x * 20, 10 + vessel.pos.y * 20);
                            break;
                        }
                    }
                }
                rCam.pos = pos - (rCam.game.rainWorld.options.ScreenSize / 2);
            }
        }
        
        public static void MoveCamera2Hook(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera rCam, string requestedTexture)
        {
            if (requestedTexture.Split('_').Length > 2)
            {
                if (ShouldScroll(rCam, GetRoomName(requestedTexture))) requestedTexture = GetPng(requestedTexture);
            }
            orig(rCam, requestedTexture);
        }
        
        public static void ApplyPositionChangeHook(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera rCam)
        {
            WWW www = (WWW)typeof(RoomCamera).GetField("www", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rCam);
            Texture2D texture = rCam.game.rainWorld.persistentData.cameraTextures[rCam.cameraNumber, 0];
            if (ShouldScroll(rCam, GetRoomName(www.url)))
            {
                texture.Resize(www.texture.width, www.texture.height, TextureFormat.ARGB32, false);
            }
            else
            {
                texture.Resize(1400, 800, TextureFormat.ARGB32, false); // default
            }
            texture.Apply();
            
            FAtlasManager manager = Futile.atlasManager;
            List<FAtlas> atlases = (List<FAtlas>)typeof(FAtlasManager).GetField("_atlases", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            Dictionary<string, FAtlasElement> allElementsByName = (Dictionary<string, FAtlasElement>)typeof(FAtlasManager).GetField("_allElementsByName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            FAtlas atlas = manager.GetAtlasWithName("LevelTexture");
            atlases.Remove(atlas);
            allElementsByName.Remove("LevelTexture");
            atlas = null;
            manager.LoadAtlasFromTexture("LevelTexture", texture);
            
            rCam.ReturnFContainer("Foreground").RemoveChild(rCam.levelGraphic);
            rCam.levelGraphic = new FSprite("LevelTexture", true);
            rCam.levelGraphic.anchorX = 0;
            rCam.levelGraphic.anchorY = 0;
            rCam.levelGraphic.isVisible = true;
            rCam.levelGraphic.shader = rCam.game.rainWorld.Shaders["LevelColor"];
            rCam.ReturnFContainer("Foreground").AddChild(rCam.levelGraphic);
            
            rCam.currentCameraPosition = 0;
            orig(rCam);
        }
        
        public static void DrawUpdateHook(On.RoomCamera.orig_DrawUpdate orig, RoomCamera rCam, float timeStacker, float timeSpeed)
        {
            if (!ShouldScroll(rCam))
            {
                orig(rCam, timeStacker, timeSpeed);
                return;
            }
            if (rCam.hud != null)
            {
                rCam.hud.Draw(timeStacker);
            }
            if (rCam.room != null)
            {
                Vector2 vector = Vector2.Lerp(rCam.lastPos, rCam.pos, timeStacker);
                rCam.virtualMicrophone.DrawUpdate(timeStacker, timeSpeed);
                if (rCam.microShake > 0f)
                {
                    vector += Custom.RNV() * 8f * rCam.microShake * UnityEngine.Random.value;
                }
                if (!rCam.voidSeaMode)
                {
                    /*
                    vector.x = Mathf.Clamp(vector.x, rCam.CamPos(rCam.currentCameraPosition).x + rCam.hDisplace + 8f - 20f, rCam.CamPos(rCam.currentCameraPosition).x + rCam.hDisplace + 8f + 20f);
                    vector.y = Mathf.Clamp(vector.y, rCam.CamPos(rCam.currentCameraPosition).y + 8f - 7f - ((!rCam.splitScreenMode) ? 0f : 192f), rCam.CamPos(rCam.currentCameraPosition).y + 33f + ((!rCam.splitScreenMode) ? 0f : 192f));
                    */
                    vector.x = Mathf.Clamp(vector.x, rCam.CamPos(rCam.currentCameraPosition).x + rCam.hDisplace + 8f - 20f, rCam.CamPos(rCam.currentCameraPosition).x + rCam.hDisplace + 8f - 20f + rCam.levelTexture.width - (rCam.game.rainWorld.options.ScreenSize).x);
                    vector.y = Mathf.Clamp(vector.y, rCam.CamPos(rCam.currentCameraPosition).y + 8f - 7f - ((!rCam.splitScreenMode) ? 0f : 192f), rCam.CamPos(rCam.currentCameraPosition).y /*+ 33f*/ + ((!rCam.splitScreenMode) ? 0f : 192f) + rCam.levelTexture.height - (rCam.game.rainWorld.options.ScreenSize).y);
                    rCam.levelGraphic.isVisible = true;
                }
                else
                {
                    rCam.levelGraphic.isVisible = false;
                    vector.y = Mathf.Min(vector.y, -528f);
                }
                vector = new Vector2(Mathf.Floor(vector.x), Mathf.Floor(vector.y));
                vector.x -= 0.02f;
                vector.y -= 0.02f;
                vector += rCam.offset;
                if (rCam.waterLight != null)
                {
                    if (rCam.room.gameOverRoom)
                    {
                        rCam.waterLight.CleanOut();
                    }
                    else
                    {
                        rCam.waterLight.DrawUpdate(vector);
                    }
                }
                for (int i = rCam.spriteLeasers.Count - 1; i >= 0; i--)
                {
                    rCam.spriteLeasers[i].Update(timeStacker, rCam, vector);
                    if (rCam.spriteLeasers[i].deleteMeNextFrame)
                    {
                        rCam.spriteLeasers.RemoveAt(i);
                    }
                }
                for (int j = 0; j < rCam.singleCameraDrawables.Count; j++)
                {
                    rCam.singleCameraDrawables[j].Draw(rCam, timeStacker, vector);
                }
                if (rCam.room.game.DEBUGMODE)
                {
                    rCam.levelGraphic.x = 5000f;
                }
                else
                {
                    rCam.levelGraphic.x = rCam.CamPos(rCam.currentCameraPosition).x - vector.x;
                    rCam.levelGraphic.y = rCam.CamPos(rCam.currentCameraPosition).y - vector.y;
                    rCam.backgroundGraphic.x = rCam.CamPos(rCam.currentCameraPosition).x - vector.x;
                    rCam.backgroundGraphic.y = rCam.CamPos(rCam.currentCameraPosition).y - vector.y;
                }
                rCam.shortcutGraphics.Draw(0f, vector);
                Shader.SetGlobalVector("_spriteRect", new Vector4((-vector.x - 0.5f + rCam.CamPos(rCam.currentCameraPosition).x) / rCam.sSize.x, (-vector.y + 0.5f + rCam.CamPos(rCam.currentCameraPosition).y) / rCam.sSize.y, (-vector.x - 0.5f + rCam.levelGraphic.width + rCam.CamPos(rCam.currentCameraPosition).x) / rCam.sSize.x, (-vector.y + 0.5f + rCam.levelGraphic.height + rCam.CamPos(rCam.currentCameraPosition).y) / rCam.sSize.y));
                Shader.SetGlobalVector("_camInRoomRect", new Vector4(vector.x / rCam.room.PixelWidth, vector.y / rCam.room.PixelHeight, rCam.sSize.x / rCam.room.PixelWidth, rCam.sSize.y / rCam.room.PixelHeight));
                Shader.SetGlobalVector("_screenSize", rCam.sSize);
                if (!rCam.room.abstractRoom.gate && !rCam.room.abstractRoom.shelter)
                {
                    float num = 0f;
                    if (rCam.room.waterObject != null)
                    {
                        num = rCam.room.waterObject.fWaterLevel + 100f;
                    }
                    else if (rCam.room.deathFallGraphic != null)
                    {
                        num = rCam.room.deathFallGraphic.height + 180f;
                    }
                    Shader.SetGlobalFloat("_waterLevel", Mathf.InverseLerp(rCam.sSize.y, 0f, num - vector.y));
                }
                else
                {
                    Shader.SetGlobalFloat("_waterLevel", 0f);
                }
                float num2 = 1f;
                if (rCam.room.roomSettings.DangerType != RoomRain.DangerType.None)
                {
                    num2 = rCam.room.world.rainCycle.ShaderLight;
                }
                if (rCam.room.lightning != null)
                {
                    if (!rCam.room.lightning.bkgOnly)
                    {
                        num2 = rCam.room.lightning.CurrentLightLevel(timeStacker);
                    }
                    rCam.paletteTexture.SetPixel(0, 7, rCam.room.lightning.CurrentBackgroundColor(timeStacker, rCam.currentPalette));
                    rCam.paletteTexture.SetPixel(1, 7, rCam.room.lightning.CurrentFogColor(timeStacker, rCam.currentPalette));
                    rCam.paletteTexture.Apply();
                }
                if (rCam.room.roomSettings.Clouds == 0f)
                {
                    Shader.SetGlobalFloat("_light", 1f);
                }
                else
                {
                    Shader.SetGlobalFloat("_light", Mathf.Lerp(Mathf.Lerp(num2, -1f, rCam.room.roomSettings.Clouds), -0.4f, rCam.ghostMode));
                }
                Shader.SetGlobalFloat("_cloudsSpeed", 1f + 3f * rCam.ghostMode);
                if (rCam.lightBloomAlphaEffect != RoomSettings.RoomEffect.Type.None)
                {
                    rCam.lightBloomAlpha = rCam.room.roomSettings.GetEffectAmount(rCam.lightBloomAlphaEffect);
                }
                if (rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.VoidMelt && rCam.fullScreenEffect != null)
                {
                    if (rCam.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidSea) > 0f)
                    {
                        rCam.lightBloomAlpha *= rCam.voidSeaGoldFilter;
                        rCam.fullScreenEffect.color = new Color(Mathf.InverseLerp(-1200f, -6000f, vector.y) * Mathf.InverseLerp(0.9f, 0f, rCam.screenShake), 0f, 0f);
                        rCam.fullScreenEffect.isVisible = (rCam.lightBloomAlpha > 0f);
                    }
                    else
                    {
                        rCam.fullScreenEffect.color = new Color(0f, 0f, 0f);
                    }
                }
                if (rCam.fullScreenEffect != null)
                {
                    if (rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Lightning)
                    {
                        rCam.fullScreenEffect.alpha = Mathf.InverseLerp(0f, 0.2f, rCam.lightBloomAlpha) * Mathf.InverseLerp(-0.7f, 0f, num2);
                    }
                    else if (rCam.lightBloomAlpha > 0f && (rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Bloom || rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyBloom || rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyAndLightBloom || rCam.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.LightBurn))
                    {
                        rCam.fullScreenEffect.alpha = rCam.lightBloomAlpha * Mathf.InverseLerp(-0.7f, 0f, num2);
                    }
                    else
                    {
                        rCam.fullScreenEffect.alpha = rCam.lightBloomAlpha;
                    }
                }
            }
        }
        
        public static Vector2 CamPosHook(On.RoomCamera.orig_CamPos orig, RoomCamera rCam, int camPos)
        {
            if (!ShouldScroll(rCam)) return orig(rCam, camPos);
            return orig(rCam, 0);
        }
        
        public static void PreLoadTextureHook(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera rCam, Room room, int camPos)
        {
            if (!ShouldScroll(room))
            {
                orig(rCam, room, camPos);
                return;
            }
            rCam.quenedTexture = WorldLoader.FindRoomFileDirectory(room.abstractRoom.name, true) + ".png";
            rCam.www = new WWW(rCam.quenedTexture);
            orig(rCam, room, 0);
        }
        
        public static Vector2 DetermineOriginalCamPos(RoomCamera camera, Vector2 ps)
        {
            Vector2 realCameraPosition = new Vector2(0, 0);
            foreach (Vector2 camPos in origCameraPositions[camera.room.abstractRoom.name])
            {
                if (ps.x > camPos.x && ps.y > camPos.y && camPos.x + 1400 > ps.x && camPos.y + 800 > ps.y)
                {
                    realCameraPosition = camPos;
                }
            }
            return realCameraPosition;
        }
        
        public static Vector2 ApplyDepthHook(On.RoomCamera.orig_ApplyDepth orig, RoomCamera camera, Vector2 ps, float depth)
        {
            if (!ShouldScroll(camera) || !origCameraPositions.ContainsKey(camera.room.abstractRoom.name))
            {
                return orig(camera, ps, depth);
            }
            Vector2 realCameraPosition = new Vector2(0, 0);
            /*
            foreach (Vector2 camPos in origCameraPositions[camera.room.abstractRoom.name])
            {
                if (ps.x > camPos.x && ps.y > camPos.y && camPos.x + 1400 > ps.x && camPos.y + 800 > ps.y)
                {
                    realCameraPosition = camPos;
                }
            }
            */
            return Custom.ApplyDepthOnVector(ps, /*realCameraPosition*/ DetermineOriginalCamPos(camera, ps) + new Vector2(700f, 533.3334f), depth);
        }
        
        public static Dictionary<string, bool> shouldScrollRooms = new Dictionary<string, bool>();
        public static Dictionary<string, Vector2[]> origCameraPositions = new Dictionary<string, Vector2[]>();
    }
}
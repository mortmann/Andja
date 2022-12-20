using Andja.Controller;
using System.IO;
using UnityEngine;
namespace Andja.Utility {
    public class ScreenshotHelper {
        public static byte[] GetSaveGameThumbnail() {
            Camera currentCamera = Camera.main;
            float ratio = Screen.currentResolution.width / (float)Screen.currentResolution.height;
            int width = 190;
            int height = 90;
            if (ratio <= 1.34) {
                width = 200;
                height = 150;
            }
            else
            if (ratio <= 1.61) {
                width = 190;
                height = 100;
            }
            else
            if (ratio >= 1.77) {
                width = 190;
                height = 90;
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) {
                antiAliasing = 4
            };

            currentCamera.targetTexture = rt;

            currentCamera.Render();//

            //Create the blank texture container
            Texture2D thumb = new Texture2D(width, height, TextureFormat.RGB24, false);

            //Assign rt as the main render texture, so everything is drawn at the higher resolution
            RenderTexture.active = rt;

            //Read the current render into the texture container, thumb
            thumb.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);

            byte[] bytes = thumb.EncodeToJPG(90);

            //--Clean up--
            RenderTexture.active = null;
            currentCamera.targetTexture = null;
            rt.DiscardContents();
            return bytes;
        }
        public static Sprite GetSaveFileScreenShot(string saveName) {
            string filePath = Path.Combine(SaveController.GetSaveGamesPath(), saveName + SaveController.SaveFileScreenShotEnding);
            if (File.Exists(filePath) == false) {
                Debug.Log("Missing Thumbnail for savegame " + filePath);
                return null;
            }
            var fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }
    }
}

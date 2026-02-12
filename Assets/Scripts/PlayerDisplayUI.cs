using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections;

public class PlayerDisplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage avatarImage;
    public TextMeshProUGUI playerNameText;

    private ulong currentSteamID;

    public void SetPlayerInfo(ulong steamID, string playerName)
    {
        currentSteamID = steamID;
        playerNameText.text = playerName;

        // Load Steam avatar
        StartCoroutine(LoadSteamAvatar(steamID));
    }

    private IEnumerator LoadSteamAvatar(ulong steamID)
    {
        CSteamID cSteamID = new CSteamID(steamID);

        // Get the medium avatar (64x64)
        int imageID = SteamFriends.GetMediumFriendAvatar(cSteamID);

        // Wait for avatar to load
        while (imageID == -1)
        {
            yield return null;
            imageID = SteamFriends.GetMediumFriendAvatar(cSteamID);
        }

        if (imageID > 0)
        {
            // Get image size
            SteamUtils.GetImageSize(imageID, out uint width, out uint height);

            if (width > 0 && height > 0)
            {
                // Create byte array for image data
                byte[] imageData = new byte[width * height * 4];

                // Get the image data
                SteamUtils.GetImageRGBA(imageID, imageData, (int)(width * height * 4));

                // Create texture
                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(imageData);
                texture.Apply();

                // Flip texture vertically (Steam images are upside down)
                FlipTextureVertically(texture);

                // Apply to UI
                avatarImage.texture = texture;
            }
        }
    }

    private void FlipTextureVertically(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];

        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flippedPixels[x + y * width] = pixels[x + (height - 1 - y) * width];
            }
        }

        texture.SetPixels(flippedPixels);
        texture.Apply();
    }
}
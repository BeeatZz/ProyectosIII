using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections;

public class PlayerDisplayUI : MonoBehaviour
{
    public RawImage avatarImage;
    public TextMeshProUGUI playerNameText;

    private ulong currentSteamID;

    public void SetPlayerInfo(ulong steamID, string playerName)
    {
        currentSteamID = steamID;
        playerNameText.text = playerName;

        StartCoroutine(LoadSteamAvatar(steamID));
    }

    private IEnumerator LoadSteamAvatar(ulong steamID)
    {
        CSteamID cSteamID = new CSteamID(steamID);
        int imageID = SteamFriends.GetMediumFriendAvatar(cSteamID);
        while (imageID == -1)
        {
            yield return null;
            imageID = SteamFriends.GetMediumFriendAvatar(cSteamID);
        }

        if (imageID > 0)
        {
            SteamUtils.GetImageSize(imageID, out uint width, out uint height);

            if (width > 0 && height > 0)
            {
                byte[] imageData = new byte[width * height * 4];
                SteamUtils.GetImageRGBA(imageID, imageData, (int)(width * height * 4));

                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(imageData);
                texture.Apply();
                FlipTextureVertically(texture);
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
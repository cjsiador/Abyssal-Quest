using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TextureReceiver : MonoBehaviour
{
    private WebSocket websocket;
    public Material baseMat;
    public EntitiesManagerScript managerScript;
    public bool loadSavedFish = true;

    [Serializable]
    private class Submission
    {
        public string type;
        public string user;
        public int outlineId;
        public string data;
    }

    private string textureFolderPath;

    async void Start()
    {
        // Use persistent data path for saving textures
        textureFolderPath = Path.Combine(Application.persistentDataPath, "UserTextures");
        Directory.CreateDirectory(textureFolderPath);

        // Load textures from previous sessions
        if(loadSavedFish) LoadSavedTextures();

        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            var sub = JsonUtility.FromJson<Submission>(message);
            if (sub.type == "submission")
                StartCoroutine(CreateTexture(sub.user, sub.data, sub.outlineId));
        };

        await websocket.Connect();
    }

    public int numToAdd = 1;
    IEnumerator CreateTexture(string user, string base64, int id)
    {
        byte[] bytes = Convert.FromBase64String(base64.Replace("data:image/png;base64,", ""));
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        //  Save to persistent data path under outlineId folder
        string outlinePath = Path.Combine(textureFolderPath, id.ToString());
        Directory.CreateDirectory(outlinePath);

        //  Create a unique filename using timestamp
        string safeUserName = string.Join("_", user.Split(Path.GetInvalidFileNameChars())); // remove invalid chars
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{safeUserName}_{timestamp}.png";

        string savePath = Path.Combine(outlinePath, fileName);
        File.WriteAllBytes(savePath, bytes);

        // Create material and spawn
        Material mat = new Material(baseMat);
        mat.mainTexture = tex;

        Debug.Log("Spawning " + user + "'s Fish");
        managerScript.SpawnBoidsWithTexture(numToAdd, EntityType.BOID, mat, id);

        yield return null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            int a = managerScript.allFish.Length;
            managerScript.SpawnBoidsWithTexture(numToAdd, EntityType.BOID, baseMat, UnityEngine.Random.Range(0,a));
        }
        websocket.DispatchMessageQueue();

    }

    private void LoadSavedTextures()
    {
        if (!Directory.Exists(textureFolderPath))
            return;

        // Load textures from subfolders (one folder per outlineId)
        foreach (string outlineFolder in Directory.GetDirectories(textureFolderPath))
        {
            string[] textureFiles = Directory.GetFiles(outlineFolder, "*.png");
            foreach (string textureFile in textureFiles)
            {
                string userName = Path.GetFileNameWithoutExtension(textureFile);
                string outlineFolderName = Path.GetFileName(outlineFolder);
                if (int.TryParse(outlineFolderName, out int outlineId))
                {
                    Texture2D tex = new Texture2D(2, 2);
                    byte[] bytes = File.ReadAllBytes(textureFile);
                    tex.LoadImage(bytes);

                    Material mat = new Material(baseMat);
                    mat.mainTexture = tex;

                    managerScript.SpawnBoidsWithTexture(1, EntityType.BOID, mat, outlineId);
                }
            }
        }

        Debug.Log($"[Startup] Loaded saved textures from {textureFolderPath}");
    }

    private async void OnApplicationQuit() => await websocket.Close();
}

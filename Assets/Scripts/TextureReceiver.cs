using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureReceiver : MonoBehaviour
{
    private WebSocket websocket;
    public Material baseMat;
    public EntitiesManagerScript managerScript;

    [Serializable]
    private class Submission
    {
        public string type;
        public string user;
        public int outlineId;
        public string data;
    }

    async void Start()
    {
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

        Material mat = new Material(baseMat);

        mat.mainTexture = tex;
        Debug.Log("Spawning " + user + "'s Fish");
        managerScript.SpawnBoidsWithTexture(numToAdd, EntityType.BOID, mat, id);

        yield return null;
    }

    void Update() => websocket.DispatchMessageQueue();

    private async void OnApplicationQuit() => await websocket.Close();
}

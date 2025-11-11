using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class WebcamPhotoSpawner : MonoBehaviour
{
    [Header("Preview (optional)")]
    public RawImage previewUI;                 // shows live webcam

    [Header("Spawn settings")]
    public GameObject photoPrefab;             // prefab with a Renderer
    public Transform parentForInstances;       // optional parent for hierarchy cleanliness
    public int gridCols = 5;
    public float gridSpacing = 1.25f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Webcam config")]
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFPS = 30;

    [Header("Debug")]
    public bool capturePicture;
    public bool CapturePicture
    { 
        get 
        { 
            return capturePicture; 
        }  
        set
        {
            if (capturePicture != value)
            {
                capturePicture = value;
                if (value == true)
                {
                    CaptureAndSpawn();
                }
            }
        } 
    }

    WebCamTexture _webcam;
    int _spawnCount = 0;
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    IEnumerator Start()
    {
        // Ask camera permission where needed (Android, etc.)
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        StartPreview();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            CapturePicture = !CapturePicture;
        }
    }

    public void StartPreview(int deviceIndex = 0)
    {
        StopPreview();

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("No webcam devices found.");
            return;
        }

        string chosen = devices[Mathf.Clamp(deviceIndex, 0, devices.Length - 1)].name;
        _webcam = new WebCamTexture(chosen, requestedWidth, requestedHeight, requestedFPS);
        _webcam.Play();

        if (previewUI)
        {
            previewUI.texture = _webcam;

            // If the feed appears mirrored, flip the UI horizontally.
            // Some cameras report vertical mirroring; adjust if you need:
            bool mirrored = _webcam.videoVerticallyMirrored;
            previewUI.rectTransform.localScale = new Vector3(mirrored ? -1 : 1, 1, 1);
        }
    }

    public void StopPreview()
    {
        if (_webcam != null)
        {
            if (_webcam.isPlaying) _webcam.Stop();
            _webcam = null;
        }
    }

    public void CaptureAndSpawn()
    {
        if (_webcam == null || !_webcam.isPlaying)
        {
            Debug.LogWarning("Webcam not running.");
            return;
        }
        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D photo = new Texture2D(_webcam.width, _webcam.height, TextureFormat.RGBA32, false);
        photo.SetPixels32(_webcam.GetPixels32());
        photo.Apply(false, false);

        string dir = Path.Combine(Application.persistentDataPath, "Photos");
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(path, photo.EncodeToPNG());
        Debug.Log($"Saved: {path}");

        // Spawn an instance and apply texture via MaterialPropertyBlock
        GameObject go = Instantiate(photoPrefab, NextGridPosition(_spawnCount), Quaternion.identity, parentForInstances);
        
        var data = go.GetComponent<PhotoPrefabData>();
        if (data == null || data.fishRenderer == null)
        {
            Debug.LogWarning("PhotoPrefabData or fishRenderer missing.");
            yield return null;
        }

        var r = data.fishRenderer;                      
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetTexture(BaseMapId, photo);               
        r.SetPropertyBlock(mpb);

        _spawnCount++;
    }

    Vector3 NextGridPosition(int index)
    {
        int col = index % gridCols;
        int row = index / gridCols;
        return gridOrigin + new Vector3(col * gridSpacing, 0f, -row * gridSpacing);
    }

    void OnDestroy()
    {
        StopPreview();
    }
}

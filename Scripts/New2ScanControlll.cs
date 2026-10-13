using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine.Android;
using System.Threading.Tasks;

public class ScanController : MonoBehaviour
{
    [Header("Camera")]
    public RawImage rawImgCameraFeed;

    [Header("AI Models (3 YOLO Models)")]
    public ModelAsset[] yoloModelAssets = new ModelAsset[3]; 
    public string[] modelNames = new string[] { "Model v1", "Model v2", "Model v3" }; 
    private int currentModelIndex = 0;

    [Range(0.1f, 0.9f)]
    public float confidenceThreshold = 0.40f;

    // Kumpletong 11-class alphabetical list base sa Roboflow export
    private readonly string[] gymLabels = new string[]
    {
        "abductor",        // index 0
        "chest_press",     // index 1
        "lat_pulldown",    // index 2
        "leg_extension",   // index 3
        "leg_press",       // index 4
        "pec_fly",         // index 5
        "preacher_curl",   // index 6
        "seated_row",      // index 7
        "shoulder_press",  // index 8
        "smith",           // index 9
        "t_bar"            // index 10
    };

    private readonly Dictionary<string, (string id, string display)> labelMap = new Dictionary<string, (string, string)>
    {
        { "abductor",       ("abductor_machine",       "Abductor Machine") },
        { "chest_press",    ("chest_press_machine",    "Chest Press Machine") },
        { "lat_pulldown",   ("lat_pulldown_machine",   "Lat Pulldown Machine") },
        { "leg_extension",  ("leg_extension_machine",  "Leg Extension Machine") },
        { "leg_press",      ("leg_press_machine",      "Leg Press Machine") },
        { "pec_fly",        ("pec_fly_machine",        "Pec Fly Machine") },
        { "preacher_curl",  ("preacher_curl_machine",  "Preacher Curl Machine") },
        { "seated_row",     ("seated_row_machine",     "Seated Row Machine") },
        { "shoulder_press", ("shoulder_press_machine", "Shoulder Press Machine") },
        { "smith",          ("smith_machine",          "Smith Machine") },
        { "t_bar",          ("t_bar_machine",          "T-bar Machine") }
    };

    [Header("Bounding Box")]
    public GameObject       boundingBoxPrefab;
    public RectTransform    boundingBoxContainer;

    [Header("Detection Text Overlay")]
    public TMP_Text txtDetectedMachine;
    public TMP_Text txtDetectedConfidence;
    public TMP_Text txtActiveModelName; 
    [Header("Buttons")]
    public Button btnCapture;
    public Button btnFlash;
    public Button btnGallery;
    public Button btnBack;
    public Button btnSwitchModel; 

    [Header("Scene Names")]
    public string loadingSceneName = "LoadingScene";
    public string homeSceneName    = "HomeScene";

    private WebCamTexture   backCamera;
    private Model           runtimeModel;
    private Worker          engineWorker;
    private Tensor<float>   inputTensor;

    private bool flashOn   = false;
    private bool shapeOnce = false;
    private bool isInferencing = false;

    private readonly List<GameObject> boxPool = new List<GameObject>();

    private string lastMachineId      = "";
    private string lastMachineDisplay = "";
    private float  lastConfidence     = 0f;
    private float scanInterval = 0.3f; 
    private float lastScanTime = 0f;
    private TextureTransform sentisTransform;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (btnCapture     != null) btnCapture.onClick.AddListener(OnCapture);
        if (btnFlash       != null) btnFlash.onClick.AddListener(OnFlashToggle);
        if (btnGallery     != null) btnGallery.onClick.AddListener(OnGallery);
        if (btnBack        != null) btnBack.onClick.AddListener(OnBack);
        if (btnSwitchModel != null) btnSwitchModel.onClick.AddListener(OnSwitchModelClicked); // -- I-wire ang switch button

        SetOverlayText("Scanning...", "--%");
        UpdateModelUI();

        StartCoroutine(InitializeCamera());
        InitializeAI(currentModelIndex);
        StartCoroutine(InferenceLoop());
    }

    
    void OnSwitchModelClicked()
    {
        if (yoloModelAssets == null || yoloModelAssets.Length == 0) return;

        
        engineWorker?.Dispose();
        inputTensor?.Dispose();

        
        currentModelIndex = (currentModelIndex + 1) % yoloModelAssets.Length;
        shapeOnce = false; 

        // load  model
        InitializeAI(currentModelIndex);
        UpdateModelUI();

        Debug.Log("ScanController: Switched to YOLO Model index -> " + currentModelIndex);
    }

    void UpdateModelUI()
    {
        if (txtActiveModelName != null)
        {
            if (modelNames != null && currentModelIndex < modelNames.Length && !string.IsNullOrEmpty(modelNames[currentModelIndex]))
            {
                txtActiveModelName.text = modelNames[currentModelIndex];
            }
            else
            {
                txtActiveModelName.text = "Model " + (currentModelIndex + 1);
            }
        }
    }

    void OnCapture()
    {
        if (string.IsNullOrEmpty(lastMachineId)) return;

        WorkoutSession.SelectedMachineId = lastMachineId;
        WorkoutSession.SelectedMachineDisplayName = lastMachineDisplay;
        WorkoutSession.DetectionConfidence = lastConfidence;

        SceneTracker.PreviousScene = "NewScanScene";

        StopCamera();
        
        // LoadingScene 
        SceneManager.LoadScene(loadingSceneName); // Tiyaking ang loadingSceneName ay nakatakda sa "LoadingScene"
    }

    IEnumerator InitializeCamera()
    {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
        #else
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) yield break;
        #endif

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0) yield break;

        string camName = devices[0].name;
        foreach (var d in devices)
        {
            if (!d.isFrontFacing) { camName = d.name; break; }
        }

        backCamera = new WebCamTexture(camName, 640, 640, 30);
        backCamera.Play();

        float timeoutTimer = 0f;
        while (backCamera.width <= 16 && timeoutTimer < 3.0f)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }

        if (backCamera.width <= 16) yield break;

        rawImgCameraFeed.texture = backCamera;
        RectTransform rt = rawImgCameraFeed.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        AspectRatioFitter fitter = rawImgCameraFeed.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = rawImgCameraFeed.gameObject.AddComponent<AspectRatioFitter>();

        
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        
        float videoRatio = (float)backCamera.width / (float)backCamera.height;

       
        int orient = -backCamera.videoRotationAngle;
        rt.localEulerAngles = new Vector3(0, 0, orient);

        
        if (backCamera.videoRotationAngle == 90 || backCamera.videoRotationAngle == 270)
        {
            videoRatio = 1f / videoRatio;
        }
        
       
        fitter.aspectRatio = videoRatio;

        
        float scaleY = backCamera.videoVerticallyMirrored ? -1f : 1f;
        float scaleX = 1f; 

        
        rt.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    void InitializeAI(int modelIndex)
    {
        if (yoloModelAssets == null || yoloModelAssets.Length <= modelIndex || yoloModelAssets[modelIndex] == null) 
        {
            Debug.LogWarning("ScanController: YOLO Model Asset is missing at index: " + modelIndex);
            return;
        }

        runtimeModel = ModelLoader.Load(yoloModelAssets[modelIndex]);
        engineWorker = new Worker(runtimeModel, BackendType.GPUCompute);
        inputTensor  = new Tensor<float>(new TensorShape(1, 3, 640, 640));
        sentisTransform = new TextureTransform().SetDimensions(640, 640);
    }

    IEnumerator InferenceLoop()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (Time.time - lastScanTime >= scanInterval && !isInferencing)
            {
                if (backCamera != null && backCamera.isPlaying && engineWorker != null && backCamera.didUpdateThisFrame)
                {
                    lastScanTime = Time.time;
                    RunInferenceAsync();
                }
            }
        }
    }

    async void RunInferenceAsync()
    {
        if (inputTensor == null || engineWorker == null) return;
        
        isInferencing = true;

        try
        {
            TextureConverter.ToTensor(backCamera, inputTensor, sentisTransform);
            engineWorker.Schedule(inputTensor);

            var output = engineWorker.PeekOutput() as Tensor<float>;
            if (output == null) return;

            using (var cpu = await output.ReadbackAndCloneAsync() as Tensor<float>)
            {
                if (cpu == null) return;

                if (!shapeOnce)
                {
                    string s = string.Join(",", cpu.shape);
                    Debug.Log($"ScanController [Output Shape]: ({s})");
                    shapeOnce = true;
                }

                int rank = cpu.shape.rank;
                float bestConf = confidenceThreshold;
                int bestClass = -1;
                var validDetections = new List<(Rect rect, float conf, int cls)>();

                if (rank == 3)
                {
                    int dim1 = cpu.shape[1];
                    int dim2 = cpu.shape[2];

                    if (dim2 == 6 || dim2 == 7)
                    {
                        for (int i = 0; i < dim1; i++)
                        {
                            float x1 = cpu[0, i, 0];
                            float y1 = cpu[0, i, 1];
                            float x2 = cpu[0, i, 2];
                            float y2 = cpu[0, i, 3];
                            float conf = cpu[0, i, 4];
                            float cls  = cpu[0, i, 5];

                            if (conf < confidenceThreshold || x2 <= x1 || y2 <= y1) continue;

                            int classId = Mathf.RoundToInt(cls);
                            
                            Rect sr = ModelCoordsToRectTransform(x1, y1, x2, y2);
                            validDetections.Add((sr, conf, classId));
                            if (conf > bestConf) 
                            { 
                                bestConf = conf; 
                                bestClass = classId; 
                            }
                        }
                    }
                    else if (dim1 < dim2)
                    {
                        int numDetections = dim2;
                        for (int i = 0; i < numDetections; i++)
                        {
                            float cx = cpu[0, 0, i];
                            float cy = cpu[0, 1, i];
                            float w  = cpu[0, 2, i];
                            float h  = cpu[0, 3, i];

                            float maxScore = 0f;
                            int classId = -1;

                            int totalClasses = dim1 - 4;
                            for (int c = 0; c < totalClasses; c++)
                            {
                                float score = cpu[0, 4 + c, i];
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                    classId = c;
                                }
                            }

                            if (maxScore < confidenceThreshold) continue;

                            float x1 = cx - w * 0.5f;
                            float y1 = cy - h * 0.5f;
                            float x2 = cx + w * 0.5f;
                            float y2 = cy + h * 0.5f;

                            Rect sr = ModelCoordsToRectTransform(x1, y1, x2, y2);
                            validDetections.Add((sr, maxScore, classId));
                            if (maxScore > bestConf) 
                            { 
                                bestConf = maxScore; 
                                bestClass = classId; 
                            }
                        }
                    }
                }

                if (validDetections.Count == 0)
                {
                    DrawBoundingBoxes(new List<(Rect, float, int)>());
                    SetOverlayText("Scanning...", "--%");
                    lastMachineId = "";
                    lastMachineDisplay = "";
                    lastConfidence = 0f;
                }
                else
                {
                    DrawBoundingBoxes(validDetections);
                    UpdateOverlayText(bestClass, bestConf);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScanController] Inference Error: {e.Message}");
        }
        finally
        {
            isInferencing = false; 
        }
    }

    Rect ModelCoordsToRectTransform(float x1, float y1, float x2, float y2)
    {
        float camW = backCamera != null ? backCamera.width : 640f;
        float camH = backCamera != null ? backCamera.height : 480f;
        float camAspect = camW / camH;
        float imgH640 = 640f / camAspect;
        float padY = (640f - imgH640) * 0.5f;

        float nx1 = x1 / 640f;
        float nx2 = x2 / 640f;
        float ny1 = Mathf.Clamp01((y1 - padY) / imgH640);
        float ny2 = Mathf.Clamp01((y2 - padY) / imgH640);

        ny1 = 1f - ny1;
        ny2 = 1f - ny2;

        Rect cr = boundingBoxContainer != null ? boundingBoxContainer.rect : new Rect(-540, -1170, 1080, 2340);
        float boxX = nx1 * cr.width + cr.x;
        float boxY = ny2 * cr.height + cr.y;
        float boxW = (nx2 - nx1) * cr.width;
        float boxH = (ny2 - ny1) * cr.height;

        return new Rect(boxX, boxY, boxW, boxH);
    }

    void DrawBoundingBoxes(List<(Rect rect, float conf, int cls)> detections)
    {
        if (boundingBoxPrefab == null || boundingBoxContainer == null) return;
        int needed = detections.Count;

        while (boxPool.Count < needed)
        {
            var go = Instantiate(boundingBoxPrefab, boundingBoxContainer);
            go.SetActive(false);
            boxPool.Add(go);
        }

        for (int i = 0; i < boxPool.Count; i++)
        {
            if (i < needed)
            {
                var (rect, conf, classId) = detections[i];
                boxPool[i].SetActive(true);

                RectTransform rt = boxPool[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rect.x, rect.y);
                rt.sizeDelta = new Vector2(rect.width, rect.height);

                TMP_Text lbl = boxPool[i].GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                {
                    string dispName = "Machine ID: " + classId;
                    if (classId >= 0 && classId < gymLabels.Length)
                    {
                        string rawLabel = gymLabels[classId];
                        dispName = labelMap.ContainsKey(rawLabel) ? labelMap[rawLabel].display : rawLabel;
                    }

                    int pct = Mathf.RoundToInt(conf <= 1f ? conf * 100f : conf);
                    lbl.text = dispName + "\n" + pct + "%";
                }
            }
            else
            {
                boxPool[i].SetActive(false);
            }
        }
    }

    void UpdateOverlayText(int bestClass, float bestConf)
    {
        if (bestClass >= 0 && bestClass < gymLabels.Length)
        {
            string raw = gymLabels[bestClass];
            lastMachineId = labelMap.ContainsKey(raw) ? labelMap[raw].id : raw;
            lastMachineDisplay = labelMap.ContainsKey(raw) ? labelMap[raw].display : raw;
            lastConfidence = bestConf;

            int pct = Mathf.RoundToInt(bestConf <= 1f ? bestConf * 100f : bestConf);
            SetOverlayText(lastMachineDisplay, pct + "%");
        }
        else
        {
            SetOverlayText("Scanning...", "--%");
        }
    }

    void SetOverlayText(string machine, string confidence)
    {
        if (txtDetectedMachine != null) txtDetectedMachine.text = machine;
        if (txtDetectedConfidence != null) txtDetectedConfidence.text = confidence;
    }

    void OnFlashToggle()
    {
        flashOn = !flashOn;
        ToggleAndroidFlashlight(flashOn);
        Debug.Log("Flashlight status: " + flashOn);
    }

    void ToggleAndroidFlashlight(bool state)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
                var packageManager = context.Call<AndroidJavaObject>("getPackageManager");
                
                // Suriin kung may camera flash hardware ang device
                bool hasFlash = packageManager.Call<bool>("hasSystemFeature", "android.hardware.camera.flash");
                if (!hasFlash)
                {
                    Debug.LogWarning("Device does not have camera flash!");
                    return;
                }

                var cameraManager = context.Call<AndroidJavaObject>("getSystemService", "camera");
                string[] cameraIdList = cameraManager.Call<string[]>("getCameraIdList");
                
                if (cameraIdList != null && cameraIdList.Length > 0)
                {
                    string cameraId = cameraIdList[0]; // Karaniwan ay ang pangunahing likod na camera
                    cameraManager.Call("setTorchMode", cameraId, state);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to toggle flashlight: " + e.Message);
        }
#else
        Debug.Log("Flashlight is only supported on Android devices during runtime.");
#endif
    }

    void OnGallery()
    {
        Debug.Log("Opening gallery to pick an image...");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // Paggamit ng Android Native Intent para magbukas ng Gallery Picker
        try
        {
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_GET_CONTENT"));
                intentObject.Call<AndroidJavaObject>("setType", "image/*");
                
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    currentActivity.Call("startActivityForResult", intentObject, 100); // 100 ang request code
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to open gallery: " + e.Message);
        }
#else
        // Fallback Unity Editor (Testing)
        Debug.Log("Gallery selection is simulated in Unity Editor.");
#endif
    }

    void OnBack()
    {
        StopCamera();
        SceneManager.LoadScene(homeSceneName);
    }

    void StopCamera()
    {
        if (backCamera != null)
        {
            if (backCamera.isPlaying) 
            {
                backCamera.Stop(); 
            }
            Destroy(backCamera); 
            backCamera = null; 
        }
        
        if (rawImgCameraFeed != null) 
        {
            rawImgCameraFeed.texture = null;
        }
    }

    void OnDisable() => StopCamera();

    void OnDestroy()
    {
        StopCamera();
        engineWorker?.Dispose();
        inputTensor?.Dispose();
        foreach (var box in boxPool) { if (box != null) Destroy(box); }
        boxPool.Clear();
    }
}
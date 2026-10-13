using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class ScreenManager : MonoBehaviour
{
    public GameObject HomepagePanel;
    public GameObject TutorialPagePanel;
     public GameObject ScanEquipmentPanel;
    public GameObject HistoryPagePanel;
    public GameObject AboutPagePanel;

    public Button Btn_BackToHome;
    public Button Btn_StartScanning;
    public Button Btn_ScanBackToHome;
    public Button Btn_GoToHistory;
    public Button Btn_HistoryToHome;
    public Button Btn_GoToAbout;
    public Button Btn_AboutToHome;
    public Button Btn_GoToTutorial;
    public Button Btn_TutorialToHome;

    public RawImage cameraFeedDisplay;
    public Unity.InferenceEngine.ModelAsset yoloModelAsset;

    public TMP_Text Txt_BottomMachine;
    public TMP_Text Txt_BottomConfidence;

    // Make sure these match the EXACT order your YOLO model was trained on!
    private string[] gymLabels = new string[] { "Chest Press Machine", "Treadmill", "Leg Press" };

    private WebCamTexture backCamera;
    private Unity.InferenceEngine.Model runtimeModel;
    private Unity.InferenceEngine.Worker engineWorker;
    private Unity.InferenceEngine.Tensor<float> inputTensor;

    // Diagnostic tracker to prevent console spamming
    private bool shapeLogged = false;

    void Start()
    {
        // Wire up UI buttons automatically
        if (Btn_BackToHome != null) Btn_BackToHome.onClick.AddListener(ReturnToHome);
        if (Btn_StartScanning != null) Btn_StartScanning.onClick.AddListener(OpenScanPage);
        if (Btn_ScanBackToHome != null) Btn_ScanBackToHome.onClick.AddListener(ReturnToHome);
        if (Btn_GoToHistory != null) Btn_GoToHistory.onClick.AddListener(OpenHistoryPage);
        if (Btn_HistoryToHome != null) Btn_HistoryToHome.onClick.AddListener(ReturnToHome);
        if (Btn_GoToAbout != null) Btn_GoToAbout.onClick.AddListener(OpenAboutPage);
        if (Btn_AboutToHome != null) Btn_AboutToHome.onClick.AddListener(ReturnToHome);
        if (Btn_GoToTutorial != null) Btn_GoToTutorial.onClick.AddListener(OpenTutorialPage);
        if (Btn_TutorialToHome != null) Btn_TutorialToHome.onClick.AddListener(ReturnToHome);

        ReturnToHome();
        StartCoroutine(InitializeCamera());
        InitializeAI();
    }

    // --- PANEL NAVIGATION ---
    public void ReturnToHome()
    {
        if (ScanEquipmentPanel != null) ScanEquipmentPanel.SetActive(false);
        if (TutorialPagePanel != null) TutorialPagePanel.SetActive(false);
        if (HistoryPagePanel != null) HistoryPagePanel.SetActive(false);
        if (AboutPagePanel != null) AboutPagePanel.SetActive(false);
        if (HomepagePanel != null) HomepagePanel.SetActive(true);
    }

    public void OpenScanPage() { SwitchPage(ScanEquipmentPanel); }
    public void OpenHistoryPage() { SwitchPage(HistoryPagePanel); }
    public void OpenTutorialPage() { SwitchPage(TutorialPagePanel); }
    public void OpenAboutPage() { SwitchPage(AboutPagePanel); }

    private void SwitchPage(GameObject targetPanel)
    {
        if (HomepagePanel != null)
        {
            HomepagePanel.SetActive(false);
        }
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            shapeLogged = false; // Reset logging status when opening scan screen to help verify shape
        }
    }

    // --- STANDALONE AI ENGINE ---
    private IEnumerator InitializeCamera()
    {
        Debug.Log("InitializeCamera() called");

        if (cameraFeedDisplay == null)
        {
            Debug.LogError("RawImage is NULL!");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;

        Debug.Log("Camera Count: " + devices.Length);

        foreach (var d in devices)
            Debug.Log($"Camera: {d.name}");

        if (devices.Length == 0)
        {
            Debug.LogError("No webcam found!");
            yield break;
        }

        backCamera = new WebCamTexture(devices[0].name, 640, 480);

        cameraFeedDisplay.texture = backCamera;

        backCamera.Play();

        yield return new WaitUntil(() => backCamera.width > 16);

        Debug.Log("Camera Ready: " + backCamera.width + " x " + backCamera.height);

        // Wait until camera is initialized
        while (backCamera.width <= 16)
            yield return null;

        Debug.Log($"Camera Ready: {backCamera.width} x {backCamera.height}");
    }

    private void InitializeAI()
    {
        if (yoloModelAsset != null)
        {
            runtimeModel = Unity.InferenceEngine.ModelLoader.Load(yoloModelAsset);
            engineWorker = new Unity.InferenceEngine.Worker(runtimeModel, Unity.InferenceEngine.BackendType.GPUCompute);
            inputTensor = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 3, 640, 640));
            Debug.Log("Standalone AR Brain Ready!");
        }
    }

    void Update()
    {
        if (backCamera == null || !backCamera.isPlaying || engineWorker == null) return;

        if (backCamera.didUpdateThisFrame && ScanEquipmentPanel.activeSelf)
        {
            RunMobileInference();
        }
    }

    private void RunMobileInference()
    {
        if (inputTensor == null) return;

        // Convert camera frame directly inside Unity
        Unity.InferenceEngine.TextureConverter.ToTensor(backCamera, inputTensor, new Unity.InferenceEngine.TextureTransform());
        engineWorker.Schedule(inputTensor);

        // Find the most appropriate output tensor containing class data
        Unity.InferenceEngine.Tensor<float> targetTensor = null;
        var defaultOutput = engineWorker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
        if (defaultOutput == null) return;
        targetTensor = defaultOutput;

        // Multi-output safety check (if the model splits bounding box coordinates and classification scores)
        // FIXED: Changed .Length to .Count because runtimeModel.outputs is a List
        if (runtimeModel.outputs != null && runtimeModel.outputs.Count > 1)
        {
            foreach (var output in runtimeModel.outputs)
            {
                var t = engineWorker.PeekOutput(output.name) as Unity.InferenceEngine.Tensor<float>;
                if (t != null && t.shape.rank >= 2)
                {
                    // Look for an output matching our number of classes (5) or combined size (9)
                    if (t.shape[1] == gymLabels.Length || t.shape[1] == (4 + gymLabels.Length) ||
                        (t.shape.rank > 2 && (t.shape[2] == gymLabels.Length || t.shape[2] == (4 + gymLabels.Length))))
                    {
                        targetTensor = t;
                        break;
                    }
                }
            }
        }

        using (var cpuTensor = targetTensor.ReadbackAndClone())
        {
            float minConfidenceThreshold = 0.45f; // Slightly lower threshold to ensure validation drops register smoothly
            int bestClass = -1;
            float bestHighestScore = 0f;
            int rank = cpuTensor.shape.rank;

            // Log output format details to the Unity Console once for developer tracking
            if (!shapeLogged)
            {
                string shapeStr = "";
                for (int d = 0; d < rank; d++) shapeStr += cpuTensor.shape[d] + (d < rank - 1 ? ", " : "");
                Debug.Log($"[YOLO Diagnostics] Tensor Shape Matrix discovered: [{shapeStr}]");
                shapeLogged = true;
            }

            try
            {
                if (rank == 3)
                {
                    int dim1 = cpuTensor.shape[1];
                    int dim2 = cpuTensor.shape[2];

                    // FORMAT 1: Standard Combined YOLOv8 Format [1, 9, 8400]
                    if (dim1 == 4 + gymLabels.Length)
                    {
                        for (int i = 0; i < dim2; i++)
                        {
                            for (int classId = 0; classId < gymLabels.Length; classId++)
                            {
                                float score = cpuTensor[0, 4 + classId, i];
                                if (score > bestHighestScore && score >= minConfidenceThreshold)
                                {
                                    bestHighestScore = score;
                                    bestClass = classId;
                                }
                            }
                        }
                    }
                    // FORMAT 2: Transposed Combined YOLO Format [1, 8400, 9]
                    else if (dim2 == 4 + gymLabels.Length)
                    {
                        for (int i = 0; i < dim1; i++)
                        {
                            for (int classId = 0; classId < gymLabels.Length; classId++)
                            {
                                float score = cpuTensor[0, i, 4 + classId];
                                if (score > bestHighestScore && score >= minConfidenceThreshold)
                                {
                                    bestHighestScore = score;
                                    bestClass = classId;
                                }
                            }
                        }
                    }
                    // FORMAT 3: Split Score Tensor Format [1, 5, 8400]
                    else if (dim1 == gymLabels.Length)
                    {
                        for (int i = 0; i < dim2; i++)
                        {
                            for (int classId = 0; classId < gymLabels.Length; classId++)
                            {
                                float score = cpuTensor[0, classId, i];
                                if (score > bestHighestScore && score >= minConfidenceThreshold)
                                {
                                    bestHighestScore = score;
                                    bestClass = classId;
                                }
                            }
                        }
                    }
                    // FORMAT 4: Transposed Split Score Tensor Format [1, 8400, 5]
                    else if (dim2 == gymLabels.Length)
                    {
                        for (int i = 0; i < dim1; i++)
                        {
                            for (int classId = 0; classId < gymLabels.Length; classId++)
                            {
                                float score = cpuTensor[0, i, classId];
                                if (score > bestHighestScore && score >= minConfidenceThreshold)
                                {
                                    bestHighestScore = score;
                                    bestClass = classId;
                                }
                            }
                        }
                    }
                    // FORMAT 5: End-to-End Native NMS Output [1, BoxCount, 6]
                    else if (dim2 == 6 || dim2 == 7)
                    {
                        for (int i = 0; i < dim1; i++)
                        {
                            float scoreA = cpuTensor[0, i, 4];
                            float classValA = cpuTensor[0, i, 5];
                            float scoreB = cpuTensor[0, i, 5];
                            float classValB = cpuTensor[0, i, 4];

                            int classId = -1;
                            float score = 0f;

                            if (classValA >= 0 && classValA < gymLabels.Length && scoreA <= 1.01f)
                            {
                                classId = Mathf.RoundToInt(classValA);
                                score = scoreA;
                            }
                            else if (classValB >= 0 && classValB < gymLabels.Length && scoreB <= 1.01f)
                            {
                                classId = Mathf.RoundToInt(classValB);
                                score = scoreB;
                            }

                            if (classId >= 0 && classId < gymLabels.Length && score > bestHighestScore && score >= minConfidenceThreshold)
                            {
                                bestHighestScore = score;
                                bestClass = classId;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[YOLO AI Error Parsing Matrix]: {ex.Message}");
            }

            // --- UI RENDERING ENGINE ---
            if (bestClass >= 0)
            {
                Txt_BottomMachine.text = gymLabels[bestClass];

                // Decimal Format Evaluation (0.0 to 1.0)
                if (bestHighestScore >= 0.0f && bestHighestScore <= 1.0f)
                {
                    int percentage = Mathf.RoundToInt(bestHighestScore * 100f);
                    Txt_BottomConfidence.text = percentage.ToString() + "%";
                }
                // Pre-multiplied Percentage Format Evaluation (1.0 to 100.0)
                else if (bestHighestScore > 1.0f && bestHighestScore <= 100.0f)
                {
                    int percentage = Mathf.RoundToInt(bestHighestScore);
                    Txt_BottomConfidence.text = percentage.ToString() + "%";
                }
                else
                {
                    Txt_BottomConfidence.text = "--%";
                }
            }
            else
            {
                // Clears out text placeholders dynamically if nothing passes validation thresholds
                Txt_BottomMachine.text = "Scanning Area...";
                Txt_BottomConfidence.text = "--%";
            }
        }
    }

    void OnDestroy()
    {
        engineWorker?.Dispose();
        inputTensor?.Dispose();
    }
}
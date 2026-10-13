using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    [Header("--- MACHINE DATA ---")]
    public MachineGuideData[] allMachineGuides;

    [Header("AR Components")]
    [SerializeField] private ARRaycastManager arRaycastManager;

    private GameObject machinePrefab; 
    private GameObject machinePreview;
    
    private GameObject spawnedMachine; 
    private Animator machineAnimator;  
    private bool isAnimationPlaying = true; 

    private Vector3 detectedPosition;
    private Quaternion detectedRotation;
    
    public bool isPlaced = false; 
    
    // BAGO: Dito natin ise-save ang pangalan ng machine para makuha ng UI
    [HideInInspector] public string currentMachineName = "Unknown Machine"; 

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // BAGO: Ginamit ang Awake() para maunang mag-load ang data bago ang UI
    void Awake()
    {
        LoadSelectedMachinePrefab();
    }

    void Start()
    {
        if (machinePrefab != null)
        {
            machinePreview = Instantiate(machinePrefab);
            machinePreview.SetActive(false);
        }
    }

    void LoadSelectedMachinePrefab()
    {
        string id = WorkoutSession.SelectedMachineId;
        
        // Fallback para sa Editor Testing
        if (string.IsNullOrEmpty(id) && allMachineGuides.Length > 0)
        {
            machinePrefab = allMachineGuides[1].arMachinePrefab;
            currentMachineName = allMachineGuides[1].machineDisplayName; // Kunin ang pangalan
            return;
        }

        foreach (MachineGuideData m in allMachineGuides)
        {
            if (m != null && m.machineId == id)
            {
                machinePrefab = m.arMachinePrefab;
                currentMachineName = m.machineDisplayName; // Kunin ang pangalan
                break;
            }
        }

        // Kung walang nahanap, gamitin ang una sa listahan
        if (machinePrefab == null && allMachineGuides.Length > 0)
        {
            machinePrefab = allMachineGuides[0].arMachinePrefab;
            currentMachineName = allMachineGuides[0].machineDisplayName;
        }
    }

    void OnEnable() { InputHandler.OnTap += LockAndSpawnMachine; }
    void OnDisable() { InputHandler.OnTap -= LockAndSpawnMachine; }

    void Update()
    {
        if (isPlaced) return;
        UpdatePreview();
    }

    void UpdatePreview()
    {
        if (arRaycastManager == null) return;
        Vector2 middleScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (arRaycastManager.Raycast(middleScreen, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            detectedPosition = hitPose.position;
            detectedRotation = hitPose.rotation;

            if (machinePreview != null)
            {
                if (!machinePreview.activeSelf) machinePreview.SetActive(true);
                machinePreview.transform.position = detectedPosition;
                machinePreview.transform.rotation = detectedRotation;
            }
        }
        else
        {
            if (machinePreview != null && machinePreview.activeSelf)
                machinePreview.SetActive(false);
        }
    }

    void LockAndSpawnMachine(Vector2 tapPosition)
    {
        if (isPlaced || machinePreview == null || !machinePreview.activeSelf || machinePrefab == null) return;

        Destroy(machinePreview);
        spawnedMachine = Instantiate(machinePrefab, detectedPosition, detectedRotation);
        
        machineAnimator = spawnedMachine.GetComponentInChildren<Animator>();
        isAnimationPlaying = true; 
        
        isPlaced = true;
        Debug.Log("Machine locked!");
    }

    public void ToggleAnimation()
    {
        if (machineAnimator != null)
        {
            isAnimationPlaying = !isAnimationPlaying;
            machineAnimator.speed = isAnimationPlaying ? 1f : 0f;
        }
    }

    public void RemoveMachineAndUnlock()
    {
        if (!isPlaced) return;

        if (spawnedMachine != null) Destroy(spawnedMachine);
        isPlaced = false;

        if (machinePrefab != null)
        {
            machinePreview = Instantiate(machinePrefab);
            machinePreview.SetActive(false);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultController : MonoBehaviour
{
    [Header("--- UI ELEMENTS ---")]
    public TMP_Text txtMachineName;
    public TMP_Text txtConfidenceRate;
    public Image imgMachinePreview;

    [Header("--- SPRITES ---")]
    public Sprite latPulldownSprite;
    
    [Tooltip("Drag the 'leg_press_machine' sprite here")]
    public Sprite legPressSprite;
    
    [Tooltip("Drag the 'pec_fly_machine' sprite here")]
    public Sprite pecFlySprite;

    [Header("--- BUTTONS ---")]
    public Button btnRescan;
    public Button btnViewDetails;
    public Button btnBack;

    [Header("--- POPUP UI ---")]
    [Tooltip("Ang parent GameObject ng iyong popup panel")]
    public GameObject popupPanel; 
    [Tooltip("Ang TMP Text sa loob ng popup kung saan natin ilalagay ang mensahe")]
    public TMP_Text txtPopupMessage;
    [Tooltip("Button para i-dismiss o kumpirmahin ang popup nang maaga")]
    public Button btnPopupConfirm;

    [Header("--- SCENE NAMES ---")]
    public string scanSceneName    = "ScanScene";
    public string detailsSceneName = "DetailsScene";

    // Nag-iimbak ng reference sa coroutine para pwede natin itong i-stop kung kinakailangan
    private Coroutine popupCoroutine;

    void Start()
    {
        // Display results from WorkoutSession
        string machineName = WorkoutSession.SelectedMachineDisplayName;
        txtMachineName.text = machineName;

        float pct = WorkoutSession.DetectionConfidence * 100f;
        txtConfidenceRate.text = Mathf.RoundToInt(pct) + "%";

        // Dynamically assign the correct sprite based on the machine name
        SetMachineSprite(machineName);

        // Setup main button listeners
        btnRescan.onClick.AddListener(() => SceneManager.LoadScene(scanSceneName));
        btnViewDetails.onClick.AddListener(() => SceneManager.LoadScene(detailsSceneName));
        btnBack.onClick.AddListener(() => SceneManager.LoadScene(scanSceneName));

        // Setup at simulan ang Popup Message
        InitializePopup();
    }

    private void InitializePopup()
    {
        if (popupPanel == null)
        {
            Debug.LogWarning("Popup Panel is not assigned in the Inspector!");
            return;
        }

        // Aktibahin ang popup panel sa pagsisimula
        popupPanel.SetActive(true);

        // Mas pormal at malinaw na mensahe (Improved message)
        if (txtPopupMessage != null)
        {
            txtPopupMessage.text = "<b>Verification Required:</b> Please check if the actual gym machine matches the image result.\n\n• Press <b>Confirm/Continue</b> if it matches.\n• Press <b>Rescan</b> if it is incorrect.";
        }

        // Setup listener para sa Confirm button ng popup
        if (btnPopupConfirm != null)
        {
            btnPopupConfirm.onClick.AddListener(ClosePopupManually);
        }

        // Simulan ang 7-second countdown para kusang mawala ang popup
        popupCoroutine = StartCoroutine(AutoDismissPopup(7f));
    }

    private IEnumerator AutoDismissPopup(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Isasara lang ang popup kung hindi pa ito manu-manong naiclick ng user
        if (popupPanel != null && popupPanel.activeSelf)
        {
            popupPanel.SetActive(false);
            Debug.Log("Popup automatically dismissed after " + delay + " seconds.");
        }
    }

    private void ClosePopupManually()
    {
        // Kung pinindot ng user ang confirm button, i-stop ang timer at isara ang panel
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            Debug.Log("Popup manually confirmed by user.");
        }
    }

    private void SetMachineSprite(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Machine name is empty! Cannot assign sprite.");
            return;
        }

        string lowerName = name.ToLower();

        if (lowerName.Contains("lat") && lowerName.Contains("pulldown"))
        {
            imgMachinePreview.sprite = latPulldownSprite;
        }
        else if (lowerName.Contains("leg") && lowerName.Contains("press"))
        {
            imgMachinePreview.sprite = legPressSprite;
        }
        else if (lowerName.Contains("pec") && lowerName.Contains("fly"))
        {
            imgMachinePreview.sprite = pecFlySprite;
        }
        else
        {
            Debug.LogWarning($"No matching sprite found for display name: '{name}'. Leaving preview empty.");
            imgMachinePreview.sprite = null; 
        }
    }

    void OnDestroy()
    {
        // Tanggalin ang mga click listener kapag umalis sa scene upang maiwasan ang memory leaks
        if (btnPopupConfirm != null)
        {
            btnPopupConfirm.onClick.RemoveListener(ClosePopupManually);
        }
    }
}
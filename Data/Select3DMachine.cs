using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class Select3DMachine : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentPanel;          
    public GameObject machineButtonPrefab; 

    [Header("Manual Machine Guides")]
    public MachineGuideData[] manualMachineGuides; 

    [Header("Scene Navigation")]
    public string loadingSceneName = "DetailsScene";
    public Button btnBack;                  
    public string backSceneName = "StartWorkout"; 

    private HashSet<string> allowedMachineIdsForToday = new HashSet<string>();
    private HashSet<string> completedMachineIdsForToday = new HashSet<string>();
    private bool isRestrictedByProgram = false;

    void Start()
    {
        CheckCompletedMachinesToday(); 
        CheckActiveWorkoutProgram();   
        PopulateMachineList();

        if (btnBack != null)
        {
            btnBack.onClick.AddListener(OnBackButtonClicked);
        }
    }

    void CheckCompletedMachinesToday()
    {
        completedMachineIdsForToday.Clear();
        string historyJson = PlayerPrefs.GetString("UserWorkoutHistory", "");
        
        if (!string.IsNullOrEmpty(historyJson))
        {
            try
            {
                WorkoutHistoryData allData = JsonUtility.FromJson<WorkoutHistoryData>(historyJson);
                if (allData != null && allData.logs != null)
                {
                    string targetDateFormatted = DateTime.Today.ToString("MMMM dd, yyyy");
                    foreach (var log in allData.logs)
                    {
                        if (log.dateCompleted == targetDateFormatted)
                        {
                            completedMachineIdsForToday.Add(NormalizeString(log.machineName));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error reading workout history: " + e.Message);
            }
        }
    }

    // BABASAHIN NA LANG NITO ANG IPINASA NG PREVIOUS PAGE. BULLETPROOF!
    void CheckActiveWorkoutProgram()
    {
        allowedMachineIdsForToday.Clear();
        isRestrictedByProgram = false;

        int isFree = PlayerPrefs.GetInt("IsFreeWorkout", 1);
        if (isFree == 1) return; // Kung walang plan, open lahat

        isRestrictedByProgram = true;
        string allowedStr = PlayerPrefs.GetString("TodayAllowedMachines", "");

        if (!string.IsNullOrEmpty(allowedStr))
        {
            string[] machines = allowedStr.Split(',');
            foreach (string m in machines)
            {
                allowedMachineIdsForToday.Add(NormalizeString(m));
            }
        }
    }

    // TINATANGGAL ANG SPACES, UNDERSCORE, AT SALITANG "MACHINE" PARA PERFECT MATCH LAGI
    private string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("_", "").Replace(" ", "").Replace("machine", "").ToLower().Trim();
    }

    void OnMachineSelected(string machineId, string displayName)
    {
        WorkoutSession.SelectedMachineId = machineId;
        WorkoutSession.SelectedMachineDisplayName = displayName;

        SceneTracker.PreviousScene = "Select3DOnAR";
        SceneManager.LoadScene(loadingSceneName);
    }

    void PopulateMachineList()
    {
        if (contentPanel == null || machineButtonPrefab == null) return;

        foreach (Transform child in contentPanel) Destroy(child.gameObject);

        MachineGuideData[] allGuideData = manualMachineGuides;
        if (allGuideData == null || allGuideData.Length == 0)
        {
            allGuideData = Resources.LoadAll<MachineGuideData>("MachineData");
            if (allGuideData.Length == 0) allGuideData = Resources.LoadAll<MachineGuideData>("");
        }

        if (allGuideData == null || allGuideData.Length == 0) return;

        foreach (var guideData in allGuideData)
        {
            if (guideData == null) continue;

            GameObject btnObj = Instantiate(machineButtonPrefab, contentPanel);
            btnObj.SetActive(true);

            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            Button btn = btnObj.GetComponent<Button>();

            if (btnText != null) btnText.text = guideData.machineDisplayName;

            if (btn != null)
            {
                string machineId = guideData.machineId;
                string displayName = guideData.machineDisplayName;

                string normalizedBtnId = NormalizeString(machineId);
                string normalizedBtnName = NormalizeString(displayName);

                bool isAllowed = true;
                bool isCompleted = false;

                // 1. CHECK KUNG KASAMA SA ALLOWED MACHINES
                if (isRestrictedByProgram)
                {
                    isAllowed = false; 
                    foreach (string allowed in allowedMachineIdsForToday)
                    {
                        if (normalizedBtnId.Contains(allowed) || allowed.Contains(normalizedBtnId) || normalizedBtnName.Contains(allowed))
                        {
                            isAllowed = true;
                            break;
                        }
                    }
                }

                // 2. CHECK KUNG COMPLETED NA SA HISTORY
                foreach (string comp in completedMachineIdsForToday)
                {
                    if (normalizedBtnId.Contains(comp) || comp.Contains(normalizedBtnId) || normalizedBtnName.Contains(comp))
                    {
                        isCompleted = true;
                        break;
                    }
                }

                // 3. I-APPLY ANG KULAY
                if (isAllowed)
                {
                    btn.interactable = true;
                    
                    if (btnText != null)
                    {
                        if (isCompleted)
                        {
                            btnText.color = new Color32(46, 204, 113, 255); // GREEN
                        }
                        else if (isRestrictedByProgram)
                        {
                            btnText.color = new Color32(241, 196, 15, 255); // YELLOW
                        }
                        else
                        {
                            btnText.color = Color.white; // FREE WORKOUT
                        }
                    }
                    
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnMachineSelected(machineId, displayName));
                }
                else
                {
                    btn.interactable = false; // UNCLICKABLE KUNG WALA SA PROGRAM
                    
                    if (btnText != null)
                    {
                        btnText.color = new Color(1f, 1f, 1f, 0.4f); // GRAYED OUT/TRANSPARENT
                    }
                }
            }
        }
    }

    void OnBackButtonClicked()
    {
        SceneManager.LoadScene(backSceneName);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class FitnessGoalManager : MonoBehaviour
{
    [Header("--- PANELS ---")]
    public GameObject panelButtonOfGoal;
    public GameObject panelList;
    public GameObject panelDetail;
    public GameObject panelFitnessPlan;

    [Header("--- PANEL 1: USER INPUTS ---")]
    public TMP_InputField inputWeight;       
    public TMP_InputField inputHeight;       
    public TMP_InputField inputAge;          
    public TMP_InputField inputActivityLevel; 

    [Header("--- PANEL 1: GOAL BUTTONS ---")]
    public Button btnLoseWeight;
    public Button btnIncreaseStrength;
    public Button btnHypertrophy;
    public Button btnBackFromGoals;

    [Header("--- PANEL 2: PRESET LIST ---")]
    public TMP_Text txtListGoalTitle;
    public TMP_Text txtListGoalDesc;
    public TMP_Text txtBMIResult;            
    public TMP_Text txtWarningMessage;       
    
    public Transform presetScrollContent;
    public GameObject presetPlanItemPrefab;
    public Button btnBackToList;

    [Header("--- PANEL 3: DETAIL PREVIEW ---")]
    public TMP_Text txtDetailFitnessGoal;           
    public TMP_Text txtDetailDescriptionOfGoal;     
    public TMP_Text txtDetailDetails;               
    public TMP_Text txtDetailWeeklyPlan;            
    public Button btnSavePlan;
    public Button btnBackToDetail;
    
    [Header("CUSTOM REST DAY SELECTION")]
    public Button[] btnRestDays = new Button[7]; 
    public Button btnUndoReset;                  
    public TMP_Text txtRestInstruction; 

    [Header("--- PANEL 4: ACTIVE FITNESS PLAN ---")]
    public TMP_Text txtPlanFitnessGoal;             
    public Slider progressBar;                      
    public TMP_Text txtCountdown; 
    public TMP_Text txtPlanDescription;
    public TMP_Text txtPlanDetails;                 
    public Transform weeklySchedScrollContent;
    public GameObject weeklySchedItemPrefab;
    
    public Button btnCreateNew; 
    public Button btnPrev;      
    public TMP_Text txtPrevCurr; 

    public Button btnCheckCalendar; 
    public Button btnEditWeek; 
    public Button btnHome;
    public Button btnBackToPlan;

    [Header("--- SCENE NAMES ---")]
    public string homeSceneName = "HomeScene";

    private string selectedGoalId = "";
    private FitnessPlan selectedPlan;
    private string activeGoalTitle = "";
    
    private int currentTargetDays = 4;
    private int currentAdjustedWeeks = 0;
    private int currentUserAge = 25; 
    private string lastCalculatedBMI = "NO DATA"; 

    // --- CUSTOM SCHEDULE VARIABLES ---
    private int requiredRestDays = 0;
    private int selectedRestDaysCount = 0;
    private bool[] isDayMarkedAsRest = new bool[7];
    private string[] finalCustomWeeklyRoutine = new string[7];
    private List<string> extractedWorkoutTypes = new List<string>();

    private bool isViewingPrevious = false;
    private bool isEditingSchedule = false; 

    void Start()
    {
        InitializeButtons();
        CheckExistingActivePlan();
    }

    void InitializeButtons()
    {
        if (btnLoseWeight != null) btnLoseWeight.onClick.AddListener(() => SelectGoal("lose_weight"));
        if (btnIncreaseStrength != null) btnIncreaseStrength.onClick.AddListener(() => SelectGoal("increase_strength"));
        if (btnHypertrophy != null) btnHypertrophy.onClick.AddListener(() => SelectGoal("hypertrophy"));
        if (btnBackFromGoals != null) btnBackFromGoals.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));

        if (btnBackToList != null) btnBackToList.onClick.AddListener(() => ShowPanel(panelButtonOfGoal));
        if (btnSavePlan != null) btnSavePlan.onClick.AddListener(SaveActivePlan);
        
        if (btnBackToDetail != null) 
        {
            btnBackToDetail.onClick.AddListener(() => {
                if (isEditingSchedule)
                {
                    isEditingSchedule = false;
                    ShowPanel(panelFitnessPlan); 
                }
                else
                {
                    ShowPanel(panelList);
                }
            });
        }

        if (btnPrev != null) btnPrev.onClick.AddListener(ViewPreviousPlan);
        
        if (btnHome != null) btnHome.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
        if (btnBackToPlan != null) btnBackToPlan.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));

        if (btnEditWeek != null) btnEditWeek.onClick.AddListener(OnEditWeeklyScheduleClicked);

        for (int i = 0; i < btnRestDays.Length; i++)
        {
            int index = i; 
            if (btnRestDays[i] != null) btnRestDays[i].onClick.AddListener(() => OnRestDayButtonClicked(index));
        }

        if (btnUndoReset != null) btnUndoReset.onClick.AddListener(ResetRestDaySelection);
    }

    void CheckExistingActivePlan()
    {
        string savedPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(savedPlanId) && FitnessDatabaseManager.Instance != null && FitnessDatabaseManager.Instance.database != null)
        {
            selectedPlan = FitnessDatabaseManager.Instance.GetPlanById(savedPlanId);
            activeGoalTitle = PlayerPrefs.GetString("ActiveGoalTitle", "FITNESS PLAN");
            
            if (selectedPlan != null)
            {
                ViewCurrentPlan(); 
                ShowPanel(panelFitnessPlan);
                return;
            }
        }
        ShowPanel(panelButtonOfGoal);
    }

    void ShowPanel(GameObject targetPanel)
    {
        if (panelButtonOfGoal) panelButtonOfGoal.SetActive(false);
        if (panelList) panelList.SetActive(false);
        if (panelDetail) panelDetail.SetActive(false);
        if (panelFitnessPlan) panelFitnessPlan.SetActive(false);

        if (targetPanel) targetPanel.SetActive(true);
    }

    private int CalculateAdjustedWeeks(int databaseWeeks, int userDaysPerWeek, int age)
    {
        int baselineDaysPerWeek = 4; 
        int totalWorkoutsRequired = databaseWeeks * baselineDaysPerWeek;
        int calculatedWeeks = Mathf.CeilToInt((float)totalWorkoutsRequired / userDaysPerWeek);
        if (age >= 60) calculatedWeeks += 4; 
        else if (age >= 45) calculatedWeeks += 2; 
        return calculatedWeeks;
    }

    public void SelectGoal(string goalId)
    {
        isEditingSchedule = false; 

        float weight = 0f;
        float heightCm = 0f;
        currentUserAge = 25; 

        if (inputWeight != null) float.TryParse(inputWeight.text, out weight);
        if (inputHeight != null) float.TryParse(inputHeight.text, out heightCm);
        if (inputAge != null && !string.IsNullOrEmpty(inputAge.text)) int.TryParse(inputAge.text, out currentUserAge);

        float bmi = 0f;
        string bmiCategory = "NO DATA";

        if (weight > 0 && heightCm > 0)
        {
            float heightMeters = heightCm / 100f;
            bmi = weight / (heightMeters * heightMeters);
            if (bmi < 18.5f) bmiCategory = "UNDERWEIGHT";
            else if (bmi >= 18.5f && bmi <= 24.9f) bmiCategory = "NORMAL";
            else if (bmi >= 25f && bmi <= 29.9f) bmiCategory = "OVERWEIGHT";
            else bmiCategory = "OBESE";
            
            lastCalculatedBMI = $"{bmiCategory} (BMI: {bmi:F1})";
        }
        else lastCalculatedBMI = "NO DATA";

        if (txtBMIResult != null) txtBMIResult.text = (bmi > 0) ? lastCalculatedBMI : "PLEASE ENTER WEIGHT AND HEIGHT";

        currentTargetDays = 0; 
        if (inputActivityLevel != null && !string.IsNullOrEmpty(inputActivityLevel.text))
            int.TryParse(inputActivityLevel.text, out currentTargetDays);

        bool hasActivityError = (currentTargetDays < 3 || currentTargetDays > 6);
        bool hasConflict = (bmiCategory == "UNDERWEIGHT" && goalId == "lose_weight");

        selectedGoalId = goalId;
        if (FitnessDatabaseManager.Instance == null || FitnessDatabaseManager.Instance.database == null) return;
        var database = FitnessDatabaseManager.Instance.database;
        var goalData = database.fitnessGoals.Find(g => g.goalId == goalId);

        if (goalData != null)
        {
            activeGoalTitle = goalData.goalTitle;
            if (txtListGoalTitle) txtListGoalTitle.text = goalData.goalTitle;
            if (txtListGoalDesc) txtListGoalDesc.text = goalData.goalDescription;

            if (presetScrollContent != null && presetPlanItemPrefab != null)
            {
                foreach (Transform child in presetScrollContent) Destroy(child.gameObject);

                if (hasActivityError)
                {
                    if (txtWarningMessage != null) 
                    {
                        txtWarningMessage.text = "WARNING:\nPlease enter a valid workout frequency (3 to 6 days).";
                        txtWarningMessage.gameObject.SetActive(true);
                    }
                }
                else if (hasConflict)
                {
                    if (txtWarningMessage != null) 
                    {
                        txtWarningMessage.text = "WARNING:\nYou are currently classified as UNDERWEIGHT based on your BMI. Losing weight is not recommended.";
                        txtWarningMessage.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (txtWarningMessage != null) txtWarningMessage.gameObject.SetActive(false);

                    foreach (var plan in goalData.plans)
                    {
                        GameObject item = Instantiate(presetPlanItemPrefab, presetScrollContent);
                        var planRef = plan;
                        int adjustedWeeks = CalculateAdjustedWeeks(planRef.durationWeeks, currentTargetDays, currentUserAge);

                        TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
                        foreach (var t in texts)
                        {
                            if (t.gameObject.name.Contains("Description")) t.text = planRef.planDescription;
                            else if (t.gameObject.name.Contains("Details")) 
                            {
                                string ageTag = currentUserAge >= 45 ? " [AGE-ADAPTED]" : "";
                                t.text = $"{planRef.targetMetric.ToUpper()} | {currentTargetDays} DAYS/WK\n{adjustedWeeks}-WEEK PLAN{ageTag}";
                            }
                        }

                        Button btn = item.GetComponent<Button>();
                        if (btn == null) btn = item.gameObject.AddComponent<Button>();
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => 
                        {
                            currentAdjustedWeeks = adjustedWeeks; 
                            SelectPlan(planRef);
                        });
                    }
                }
            }
        }
        ShowPanel(panelList);
    }

    void SelectPlan(FitnessPlan plan)
    {
        selectedPlan = plan;

        if (txtDetailFitnessGoal) txtDetailFitnessGoal.text = activeGoalTitle;
        if (txtDetailDetails) txtDetailDetails.text = $"{lastCalculatedBMI}\n{selectedPlan.targetMetric}\n{currentAdjustedWeeks} WEEKS";
        
        if (txtDetailDescriptionOfGoal) 
        {
            if (isEditingSchedule)
                txtDetailDescriptionOfGoal.text = "<b>EDIT SCHEDULE:</b> Modify your preferred rest days for your ongoing plan. Past completed weeks will not be affected.";
            else
                txtDetailDescriptionOfGoal.text = selectedPlan.planDescription;
        }

        requiredRestDays = 7 - currentTargetDays;
        
        extractedWorkoutTypes.Clear();
        if (selectedPlan.weeks != null && selectedPlan.weeks.Count > 0)
        {
            foreach (var d in selectedPlan.weeks[0].days)
            {
                if (!d.isRestDay && !d.dayType.ToUpper().Contains("REST"))
                {
                    extractedWorkoutTypes.Add(d.dayType);
                }
            }
        }

        ResetRestDaySelection();
        ShowPanel(panelDetail);
    }

    public void OnEditWeeklyScheduleClicked()
    {
        string currentPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (string.IsNullOrEmpty(currentPlanId) || FitnessDatabaseManager.Instance == null) return;

        selectedPlan = FitnessDatabaseManager.Instance.GetPlanById(currentPlanId);
        if (selectedPlan == null) return;

        isEditingSchedule = true;

        activeGoalTitle = PlayerPrefs.GetString("ActiveGoalTitle", "FITNESS PLAN");
        currentTargetDays = PlayerPrefs.GetInt("TargetDaysPerWeek", 4);
        currentAdjustedWeeks = PlayerPrefs.GetInt("ActivePlanAdjustedWeeks", selectedPlan.durationWeeks);
        lastCalculatedBMI = PlayerPrefs.GetString("ActiveBMIStatus", "NO DATA");

        SelectPlan(selectedPlan); 
    }

    private void ResetRestDaySelection()
    {
        selectedRestDaysCount = 0;
        for (int i = 0; i < 7; i++)
        {
            isDayMarkedAsRest[i] = false;
            finalCustomWeeklyRoutine[i] = ""; 
            
            if (btnRestDays[i] != null) 
            {
                btnRestDays[i].interactable = true;
                btnRestDays[i].gameObject.SetActive(true);
            }
        }

        if (btnUndoReset != null)
        {
            TMP_Text undoTxt = btnUndoReset.GetComponentInChildren<TMP_Text>();
            if (undoTxt != null) undoTxt.text = "UNDO";
        }

        if (btnSavePlan != null) btnSavePlan.interactable = false; 
        UpdateDetailWeeklyPlanText();
    }

    private void OnRestDayButtonClicked(int dayIndex)
    {
        if (selectedRestDaysCount < requiredRestDays)
        {
            isDayMarkedAsRest[dayIndex] = true;
            selectedRestDaysCount++;
            
            if (btnRestDays[dayIndex] != null) btnRestDays[dayIndex].interactable = false;

            UpdateDetailWeeklyPlanText();
            if (selectedRestDaysCount == requiredRestDays) GenerateFinalCustomRoutine();
        }
    }

    private void UpdateDetailWeeklyPlanText()
    {
        string displayStr = "";
        for (int i = 0; i < 7; i++)
        {
            if (isDayMarkedAsRest[i]) displayStr += $"<color=#F1C40F>REST DAY</color>\n";
            else displayStr += $"<color=#95A5A6><i>Select below...</i></color>\n";
        }

        if (txtDetailWeeklyPlan != null) txtDetailWeeklyPlan.text = displayStr;

        if (txtRestInstruction != null)
        {
            int remaining = requiredRestDays - selectedRestDaysCount;
            string instruction = remaining > 0 ? $"<color=#F1C40F>□ SELECT {remaining} MORE REST DAY(S)</color>" : "<color=#2ECC71>☑ ROUTINE COMPLETE! You can now save.</color>";
            txtRestInstruction.text = instruction;
        }
    }

    private void GenerateFinalCustomRoutine()
    {
        for (int i = 0; i < 7; i++)
        {
            if (btnRestDays[i] != null) btnRestDays[i].gameObject.SetActive(false);
        }

        if (btnUndoReset != null)
        {
            TMP_Text undoTxt = btnUndoReset.GetComponentInChildren<TMP_Text>();
            if (undoTxt != null) undoTxt.text = "RESET";
        }

        int workoutIndex = 0;
        string finalDisplayStr = "";

        for (int i = 0; i < 7; i++)
        {
            if (isDayMarkedAsRest[i])
            {
                finalCustomWeeklyRoutine[i] = "REST DAY";
                finalDisplayStr += $"<color=#F1C40F>REST DAY</color>\n";
            }
            else
            {
                string assignedWorkout = extractedWorkoutTypes.Count > 0 ? extractedWorkoutTypes[workoutIndex % extractedWorkoutTypes.Count] : "FULL BODY";
                assignedWorkout = assignedWorkout.ToUpper().Replace(" WORKOUT", "").Trim();
                
                finalCustomWeeklyRoutine[i] = assignedWorkout;
                finalDisplayStr += $"<b>{assignedWorkout}</b>\n";
                workoutIndex++;
            }
        }

        if (txtDetailWeeklyPlan != null) txtDetailWeeklyPlan.text = finalDisplayStr;
        if (btnSavePlan != null) btnSavePlan.interactable = true; 
    }

    public void SaveActivePlan()
    {
        if (selectedPlan == null) return;

        if (isEditingSchedule)
        {
            if (!PlayerPrefs.HasKey("ActiveInitialRoutine")) 
            {
                PlayerPrefs.SetString("ActiveInitialRoutine", PlayerPrefs.GetString("ActiveCustomRoutine", ""));
            }

            PlayerPrefs.SetString("ActiveCustomRoutine", string.Join(",", finalCustomWeeklyRoutine));
            PlayerPrefs.Save();
            
            isEditingSchedule = false;
            ViewCurrentPlan(); 
            if (panelFitnessPlan != null) ShowPanel(panelFitnessPlan);
            return;
        }

        string currentPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(currentPlanId))
        {
            string currentGoalTitle = PlayerPrefs.GetString("ActiveGoalTitle", "");
            string currentStartDate = PlayerPrefs.GetString("ActivePlanStartDate", "");
            int currentFreq = PlayerPrefs.GetInt("TargetDaysPerWeek", 4);
            int currentAdjWeeks = PlayerPrefs.GetInt("ActivePlanAdjustedWeeks", selectedPlan.durationWeeks);
            string currentBmiStatus = PlayerPrefs.GetString("ActiveBMIStatus", "NO DATA"); 
            string currentRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");
            string currentInitialRoutine = PlayerPrefs.GetString("ActiveInitialRoutine", currentRoutine);

            int currentDaysPassed = 0;
            if (DateTime.TryParse(currentStartDate, out DateTime cDate))
                currentDaysPassed = (DateTime.Today - cDate).Days;

            PlayerPrefs.SetString("PrevFitnessPlanId", currentPlanId);
            PlayerPrefs.SetString("PrevGoalTitle", currentGoalTitle);
            PlayerPrefs.SetString("PrevPlanStartDate", currentStartDate);
            PlayerPrefs.SetInt("PrevTargetDaysPerWeek", currentFreq);
            PlayerPrefs.SetInt("PrevPlanAdjustedWeeks", currentAdjWeeks);
            PlayerPrefs.SetString("PrevBMIStatus", currentBmiStatus);
            PlayerPrefs.SetInt("PrevDaysPassed", currentDaysPassed); 
            PlayerPrefs.SetString("PrevCustomRoutine", currentRoutine);
            PlayerPrefs.SetString("PrevInitialRoutine", currentInitialRoutine); 
        }

        PlayerPrefs.SetString("ActiveFitnessPlanId", selectedPlan.planId);
        PlayerPrefs.SetString("ActivePlanStartDate", DateTime.Today.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetString("ActiveGoalTitle", activeGoalTitle);
        PlayerPrefs.SetString("ActiveBMIStatus", lastCalculatedBMI); 
        
        PlayerPrefs.SetInt("TargetDaysPerWeek", currentTargetDays);
        PlayerPrefs.SetInt("ActivePlanAdjustedWeeks", currentAdjustedWeeks);
        
        string joinedRoutine = string.Join(",", finalCustomWeeklyRoutine);
        PlayerPrefs.SetString("ActiveCustomRoutine", joinedRoutine);
        PlayerPrefs.SetString("ActiveInitialRoutine", joinedRoutine); 

        if (inputWeight != null && !string.IsNullOrEmpty(inputWeight.text)) PlayerPrefs.SetFloat("UserWeightKg", float.Parse(inputWeight.text));
        if (inputHeight != null && !string.IsNullOrEmpty(inputHeight.text)) PlayerPrefs.SetFloat("UserHeightCm", float.Parse(inputHeight.text));
        if (inputAge != null && !string.IsNullOrEmpty(inputAge.text)) PlayerPrefs.SetInt("UserAge", currentUserAge);

        PlayerPrefs.Save();
        ViewCurrentPlan(); 
        if (panelFitnessPlan != null) ShowPanel(panelFitnessPlan);
    }

    public void ContinuePreviousPlan()
    {
        string currPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        string currGoalTitle = PlayerPrefs.GetString("ActiveGoalTitle", "");
        string currStartDateStr = PlayerPrefs.GetString("ActivePlanStartDate", "");
        int currFreq = PlayerPrefs.GetInt("TargetDaysPerWeek", 4);
        int currAdjWeeks = PlayerPrefs.GetInt("ActivePlanAdjustedWeeks", 4);
        string currBmiStatus = PlayerPrefs.GetString("ActiveBMIStatus", "NO DATA");
        string currRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");
        string currInitialRoutine = PlayerPrefs.GetString("ActiveInitialRoutine", currRoutine);
        
        int currDaysPassed = 0;
        if (DateTime.TryParse(currStartDateStr, out DateTime cDate))
            currDaysPassed = (DateTime.Today - cDate).Days;

        string prevPlanId = PlayerPrefs.GetString("PrevFitnessPlanId", "");
        string prevGoalTitle = PlayerPrefs.GetString("PrevGoalTitle", "");
        int prevFreq = PlayerPrefs.GetInt("PrevTargetDaysPerWeek", 4);
        int prevAdjWeeks = PlayerPrefs.GetInt("PrevPlanAdjustedWeeks", 4);
        string prevBmiStatus = PlayerPrefs.GetString("PrevBMIStatus", "NO DATA");
        int prevDaysPassed = PlayerPrefs.GetInt("PrevDaysPassed", 0);
        string prevRoutine = PlayerPrefs.GetString("PrevCustomRoutine", "");
        string prevInitialRoutine = PlayerPrefs.GetString("PrevInitialRoutine", prevRoutine);

        PlayerPrefs.SetString("ActiveFitnessPlanId", prevPlanId);
        PlayerPrefs.SetString("ActiveGoalTitle", prevGoalTitle);
        DateTime newStartDate = DateTime.Today.AddDays(-prevDaysPassed); 
        PlayerPrefs.SetString("ActivePlanStartDate", newStartDate.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt("TargetDaysPerWeek", prevFreq);
        PlayerPrefs.SetInt("ActivePlanAdjustedWeeks", prevAdjWeeks);
        PlayerPrefs.SetString("ActiveBMIStatus", prevBmiStatus);
        PlayerPrefs.SetString("ActiveCustomRoutine", prevRoutine);
        PlayerPrefs.SetString("ActiveInitialRoutine", prevInitialRoutine);

        PlayerPrefs.SetString("PrevFitnessPlanId", currPlanId);
        PlayerPrefs.SetString("PrevGoalTitle", currGoalTitle);
        PlayerPrefs.SetString("PrevPlanStartDate", currStartDateStr); 
        PlayerPrefs.SetInt("PrevTargetDaysPerWeek", currFreq);
        PlayerPrefs.SetInt("PrevPlanAdjustedWeeks", currAdjWeeks);
        PlayerPrefs.SetString("PrevBMIStatus", currBmiStatus);
        PlayerPrefs.SetInt("PrevDaysPassed", currDaysPassed);
        PlayerPrefs.SetString("PrevCustomRoutine", currRoutine);
        PlayerPrefs.SetString("PrevInitialRoutine", currInitialRoutine);
        
        PlayerPrefs.Save();
        ViewCurrentPlan();
    }

    void ViewCurrentPlan()
    {
        string currentPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(currentPlanId) && FitnessDatabaseManager.Instance != null)
        {
            FitnessPlan currPlan = FitnessDatabaseManager.Instance.GetPlanById(currentPlanId);
            if (currPlan != null)
            {
                string currTitle = PlayerPrefs.GetString("ActiveGoalTitle", "FITNESS PLAN");
                isViewingPrevious = false;
                PopulateActiveFitnessPlanView(currPlan, currTitle, false);
            }
        }
    }

    void ViewPreviousPlan()
    {
        string prevPlanId = PlayerPrefs.GetString("PrevFitnessPlanId", "");
        if (!string.IsNullOrEmpty(prevPlanId) && FitnessDatabaseManager.Instance != null)
        {
            FitnessPlan prevPlan = FitnessDatabaseManager.Instance.GetPlanById(prevPlanId);
            if (prevPlan != null)
            {
                string prevTitle = PlayerPrefs.GetString("PrevGoalTitle", "PREVIOUS PLAN");
                isViewingPrevious = true;
                PopulateActiveFitnessPlanView(prevPlan, prevTitle, true);
            }
        }
    }

    private string GetGoalSpecificMessage(string goalTitle)
    {
        string title = goalTitle.ToUpper();
        if (title.Contains("LOSE WEIGHT")) return "Maintain a slight caloric deficit and keep rests short (30-60s). Aim for 10-15 reps per set to maximize calorie burn and preserve lean muscle mass.";
        else if (title.Contains("STRENGTH")) return "Focus on progressive overload with heavy weights. Aim for 4-6 reps per set and take longer rests (2-3 mins) to ensure full CNS recovery between sets.";
        else if (title.Contains("HYPERTROPHY") || title.Contains("MUSCLE")) return "Focus on mind-muscle connection and time under tension. Aim for 8-12 reps per set with moderate weight and 60-90 seconds of rest for optimal muscle growth.";
        return "Stay consistent with your schedule, keep pushing your limits, and track every completed set to achieve your ultimate fitness goal.";
    }

    void PopulateActiveFitnessPlanView(FitnessPlan planToDisplay, string titleToDisplay, bool isPreviousPlan)
    {
        if (planToDisplay == null) return;
        if (txtPlanFitnessGoal) txtPlanFitnessGoal.text = titleToDisplay;

        if (btnEditWeek != null) btnEditWeek.gameObject.SetActive(!isPreviousPlan);

        if (isPreviousPlan)
        {
            if (txtPrevCurr != null) txtPrevCurr.text = "PREVIOUS";
            if (btnPrev != null) btnPrev.gameObject.SetActive(false);
            if (btnCreateNew != null)
            {
                TMP_Text btnText = btnCreateNew.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "CURRENT";
                btnCreateNew.onClick.RemoveAllListeners();
                btnCreateNew.onClick.AddListener(ViewCurrentPlan);
            }
        }
        else
        {
            if (txtPrevCurr != null) txtPrevCurr.text = "CURRENT";
            if (btnPrev != null)
            {
                string prevId = PlayerPrefs.GetString("PrevFitnessPlanId", "");
                btnPrev.gameObject.SetActive(!string.IsNullOrEmpty(prevId));
            }
            if (btnCreateNew != null)
            {
                TMP_Text btnText = btnCreateNew.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "CREATE NEW";
                btnCreateNew.onClick.RemoveAllListeners();
                btnCreateNew.onClick.AddListener(() => ShowPanel(panelButtonOfGoal));
            }
        }

        string dateKeyToUse = isPreviousPlan ? "PrevPlanStartDate" : "ActivePlanStartDate";
        string adjWeeksKey = isPreviousPlan ? "PrevPlanAdjustedWeeks" : "ActivePlanAdjustedWeeks";
        string bmiKeyToUse = isPreviousPlan ? "PrevBMIStatus" : "ActiveBMIStatus";
        
        string routineKeyToUse = isPreviousPlan ? "PrevCustomRoutine" : "ActiveCustomRoutine";
        string initialRoutineKeyToUse = isPreviousPlan ? "PrevInitialRoutine" : "ActiveInitialRoutine";

        int totalWeeks = PlayerPrefs.GetInt(adjWeeksKey, planToDisplay.durationWeeks);
        string bmiStatus = PlayerPrefs.GetString(bmiKeyToUse, "NO DATA");
        
        string startDateStr = PlayerPrefs.GetString(dateKeyToUse, "");
        DateTime viewStartDate = DateTime.Today;
        if (DateTime.TryParse(startDateStr, out DateTime pDate))
            viewStartDate = pDate;

        string[] loadedCustomRoutine = new string[7];
        string savedRoutineStr = PlayerPrefs.GetString(routineKeyToUse, "");
        if (!string.IsNullOrEmpty(savedRoutineStr)) 
        {
            loadedCustomRoutine = savedRoutineStr.Split(',');
        }
        else 
        {
            for (int i = 0; i < 7; i++) loadedCustomRoutine[i] = planToDisplay.weeks[0].days[i % planToDisplay.weeks[0].days.Count].dayType;
        }

        string[] initialCustomRoutine = new string[7];
        string savedInitialStr = PlayerPrefs.GetString(initialRoutineKeyToUse, "");
        if (!string.IsNullOrEmpty(savedInitialStr)) 
        {
            initialCustomRoutine = savedInitialStr.Split(',');
        }
        else 
        {
            for (int i = 0; i < 7; i++) initialCustomRoutine[i] = planToDisplay.weeks[0].days[i % planToDisplay.weeks[0].days.Count].dayType;
        }

        int daysPassed = 0;
        if (isPreviousPlan) daysPassed = PlayerPrefs.GetInt("PrevDaysPassed", 0);
        else daysPassed = (DateTime.Today - viewStartDate).Days;

        int weeksPassed = daysPassed / 7;
        int weeksRemaining = Mathf.Max(0, totalWeeks - weeksPassed);
        float progressVal = Mathf.Clamp01((float)weeksPassed / totalWeeks);

        if (isPreviousPlan)
        {
             weeksRemaining = 0;
             progressVal = 1.0f;
             weeksPassed = totalWeeks; 
        }

        if (btnCheckCalendar != null)
        {
            TMP_Text btnText = btnCheckCalendar.GetComponentInChildren<TMP_Text>();
            btnCheckCalendar.onClick.RemoveAllListeners();
            
            if (isPreviousPlan)
            {
                if (btnText != null) btnText.text = "CONTINUE THIS";
                btnCheckCalendar.onClick.AddListener(ContinuePreviousPlan);
            }
            else
            {
                if (btnText != null) btnText.text = "CHECK CALENDAR";
                btnCheckCalendar.onClick.AddListener(() => {
                    SceneTracker.PreviousScene = "FitnessGoal"; 
                    CalendarPreviewTest.openCalendarOnStart = true;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"); 
                });
            }
        }

        if (txtCountdown != null) txtCountdown.gameObject.SetActive(true); 

        if (weeksRemaining == 0)
        {
            if (txtCountdown) txtCountdown.text = "YOU ARE DONE WITH YOUR WORKOUT";
            if (txtPlanDescription) txtPlanDescription.text = "Congratulations! You have successfully completed your fitness plan. Amazing job reaching your goal!";
            if (progressBar != null) progressBar.value = 1.0f;
        }
        else
        {
            if (txtCountdown) txtCountdown.text = $"YOU ARE {weeksRemaining} WEEKS AWAY FROM YOUR GOAL";
            if (txtPlanDescription) txtPlanDescription.text = GetGoalSpecificMessage(titleToDisplay);
            
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                var fillImage = progressBar.fillRect?.GetComponent<Image>();
                if (fillImage != null) fillImage.enabled = true;
                progressBar.value = progressVal;
            }
        }

        if (txtPlanDetails) txtPlanDetails.text = $"{bmiStatus}\n{planToDisplay.targetMetric}\n{totalWeeks} WEEKS";

        if (weeklySchedScrollContent != null && weeklySchedItemPrefab != null)
        {
            foreach (Transform child in weeklySchedScrollContent) Destroy(child.gameObject);

            // [FIXED LOGIC]: Magdedepende na ngayon ito eksakto sa mismong Calendar Status!
            Action<int, string[]> RenderWeek = (w, routineToUse) => {
                GameObject weekItem = Instantiate(weeklySchedItemPrefab, weeklySchedScrollContent);
                weekItem.transform.localScale = Vector3.one;
                
                TMP_Text weekNumberText = weekItem.transform.Find("txtWeekNumber")?.GetComponent<TMP_Text>();
                TMP_Text daysText = weekItem.transform.Find("txtDays")?.GetComponent<TMP_Text>();
                TMP_Text detailWorkoutText = weekItem.transform.Find("txtDetailWorkout")?.GetComponent<TMP_Text>();
                TMP_Text statusText = weekItem.transform.Find("txtStatus")?.GetComponent<TMP_Text>();

                if (weekNumberText) weekNumberText.text = $"WEEK {w} - WORKOUT PLAN";

                string daysStr = "";
                string workoutsStr = "";
                string statusStr = "";

                for (int i = 0; i < 7; i++)
                {
                    daysStr += $"DAY {i + 1}\n";
                    
                    bool isRestDay = routineToUse[i].ToUpper().Contains("REST");
                    if (isRestDay)
                        workoutsStr += $"<color=#F1C40F>REST DAY</color>\n";
                    else
                        workoutsStr += $"{routineToUse[i].ToUpper().Replace(" WORKOUT", "").Trim()}\n";
                    
                    // Kunin ang mismong calendar date ng araw na ito
                    DateTime targetDate = viewStartDate.AddDays((w - 1) * 7 + i);
                    
                    // Basahin sa database ang totoong status
                    int statusVal = PlayerPrefs.GetInt("Workout_Status_" + targetDate.ToString("yyyy-MM-dd"), 1);

                    if (isPreviousPlan)
                    {
                        if (isRestDay) statusStr += "<color=#F1C40F>Rest Day</color>\n";
                        else statusStr += "<color=#2ECC71>Completed</color>\n";
                    }
                    else
                    {
                        // SYNC SA CALENDAR WORKOUT STATUS
                        if (statusVal == 2) statusStr += "<color=#2ECC71>Completed</color>\n";
                        else if (statusVal == 6) statusStr += "<color=#3498DB>In Progress</color>\n";
                        else if (statusVal == 3) statusStr += "<color=#E74C3C>Missed</color>\n";
                        else if (statusVal == 5) statusStr += "<color=#95A5A6>Adjusted</color>\n";
                        else if (isRestDay || statusVal == 0 || statusVal == 4) statusStr += "<color=#F1C40F>Rest Day</color>\n";
                        else if (targetDate.Date < DateTime.Today.Date && statusVal == 1) statusStr += "<color=#E74C3C>Missed</color>\n";
                        else statusStr += "<color=#BDC3C7>Scheduled</color>\n";
                    }
                }

                if (daysText) daysText.text = daysStr;
                if (detailWorkoutText) detailWorkoutText.text = workoutsStr;
                if (statusText) statusText.text = statusStr;
            };

            if (isPreviousPlan)
            {
                for (int w = 1; w <= totalWeeks; w++) RenderWeek(w, initialCustomRoutine);
            }
            else
            {
                for (int w = 1; w <= weeksPassed; w++) RenderWeek(w, initialCustomRoutine);
                for (int w = weeksPassed + 1; w <= totalWeeks; w++) RenderWeek(w, loadedCustomRoutine);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class WorkoutHistoryController : MonoBehaviour
{
    public enum WorkoutStatus { RestDay = 0, Scheduled = 1, Completed = 2, Missed = 3, MarkedRestDay = 4, Rescheduled = 5, InProgress = 6 }

    [Header("--- MAIN TABS / PANELS ---")]
    public GameObject panelDaily;    
    public GameObject panelWeekly;   
    public GameObject panelDetail;   

    [Header("--- TAB BUTTONS ---")]
    public Button btnTabDaily;
    public Button btnTabWeekly;
    public Button btnTabMonthly; 

    [Header("--- WEEKLY UI ---")]
    public TMP_Text txtWeekNumberAndDate;
    public Transform weeklyListContent;     
    public GameObject weeklyItemPrefab;     
    public Button btnPrevWeek;
    public Button btnNextWeek;
    public TMP_Text txtSummary;

    [Header("--- DAILY LIST UI ---")]
    public Transform dailyListContent;       
    public GameObject dailyItemPrefab;   

    [Header("--- DETAIL POPUP UI ---")]
    public TMP_Text txtFitnessGoal;     
    public TMP_Text txtDayWeek;         
    public TMP_Text txtWorkoutPlan;     
    public TMP_Text txtDetailMachineName; 
    public Image imgMachineThumb;       
    public TMP_Text txtDetailDate;      
    public TMP_Text txtDetailSets;      
    public TMP_Text txtDetailReps;      
    public TMP_Text txtDetailWeight;    

    [Header("--- NAVIGATION ---")]
    public Button btnBackToList;        
    public Button btnBackToHome;        
    public Button btnNextMachine;              
    public Button btnPrevMachine;              
    public string homeSceneName = "HomeScene";

    // --- DATA STATE ---
    private List<WorkoutLog> allLogs = new List<WorkoutLog>();
    private List<WorkoutLog> currentSessionLogs = new List<WorkoutLog>();
    private int currentMachineIndex = 0;
    
    private FitnessPlan activePlan;
    private DateTime planStartDate;
    private string[] activeCustomRoutine = new string[7];
    private int adjustedDurationWeeks = 4;
    
    private int currentDisplayWeek = 1;
    private int lastActiveTab = 0;
    private string detailOverrideDayWeek = ""; 

    void Start()
    {
        LoadActivePlanData();
        EvaluateMissedWorkouts();
        LoadAllEntries();

        int daysSinceStart = (DateTime.Today.Date - planStartDate.Date).Days;
        currentDisplayWeek = (daysSinceStart >= 0) ? (daysSinceStart / 7) + 1 : 1;
        if (currentDisplayWeek < 1) currentDisplayWeek = 1;
        if (currentDisplayWeek > adjustedDurationWeeks) currentDisplayWeek = adjustedDurationWeeks;

        if (btnTabDaily != null) btnTabDaily.onClick.AddListener(() => SwitchTab(0));
        if (btnTabWeekly != null) btnTabWeekly.onClick.AddListener(() => SwitchTab(1));
        
        if (btnTabMonthly != null) btnTabMonthly.onClick.AddListener(() => {
            CalendarPreviewTest.openCalendarOnStart = true;
            SceneManager.LoadScene(homeSceneName);
        });

        if (btnPrevWeek != null) btnPrevWeek.onClick.AddListener(() => ChangeWeek(-1));
        if (btnNextWeek != null) btnNextWeek.onClick.AddListener(() => ChangeWeek(1));

        if (btnBackToHome != null) btnBackToHome.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
        if (btnBackToList != null) btnBackToList.onClick.AddListener(CloseDetailPanel);
        
        if (btnNextMachine != null) btnNextMachine.onClick.AddListener(ShowNextMachine);
        if (btnPrevMachine != null) btnPrevMachine.onClick.AddListener(ShowPrevMachine);

        SwitchTab(0);
    }

    private void LoadActivePlanData()
    {
        string savedPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(savedPlanId) && FitnessDatabaseManager.Instance != null)
        {
            activePlan = FitnessDatabaseManager.Instance.GetPlanById(savedPlanId);
        }

        string startDateStr = PlayerPrefs.GetString("ActivePlanStartDate", DateTime.Today.ToString("yyyy-MM-dd"));
        DateTime.TryParse(startDateStr, out planStartDate);

        adjustedDurationWeeks = PlayerPrefs.GetInt("ActivePlanAdjustedWeeks", activePlan != null ? activePlan.durationWeeks : 4);

        string savedRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");
        if (!string.IsNullOrEmpty(savedRoutine)) 
        {
            activeCustomRoutine = savedRoutine.Split(',');
        } 
        else if (activePlan != null && activePlan.weeks.Count > 0) 
        {
            for (int i = 0; i < 7; i++) activeCustomRoutine[i] = activePlan.weeks[0].days[i % activePlan.weeks[0].days.Count].dayType;
        }
    }

    private void EvaluateMissedWorkouts()
    {
        if (activePlan == null) return;
        
        bool requiresSave = false;
        for (DateTime d = planStartDate.Date; d < DateTime.Today.Date; d = d.AddDays(1))
        {
            int currentStatus = PlayerPrefs.GetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), 1);
            if (currentStatus == 1) 
            {
                int correctedStatus = 3; 
                PlayerPrefs.SetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), correctedStatus);
                requiresSave = true;
            }
        }
        if (requiresSave) PlayerPrefs.Save();
    }

    void LoadAllEntries()
    {
        allLogs.Clear();
        string historyJson = PlayerPrefs.GetString("UserWorkoutHistory", "");
        if (!string.IsNullOrEmpty(historyJson))
        {
            try
            {
                WorkoutHistoryData allData = JsonUtility.FromJson<WorkoutHistoryData>(historyJson);
                if (allData != null && allData.logs != null)
                {
                    // =====================================================================
                    // [AUTO-CLEANER]: Idinedetect at binubura ang mga duplicated entries 
                    // para maging maayos agad ang view mo sa History at Calendar!
                    // =====================================================================
                    int originalCount = allData.logs.Count;
                    
                    var cleanedLogs = allData.logs
                        .GroupBy(l => new { l.logId, l.machineName })
                        .Select(g => g.First())
                        .ToList();

                    allLogs = cleanedLogs;

                    // Kung may naburang duplicate, isa-save agad niya ito sa database
                    if (cleanedLogs.Count < originalCount)
                    {
                        allData.logs = cleanedLogs;
                        PlayerPrefs.SetString("UserWorkoutHistory", JsonUtility.ToJson(allData));
                        PlayerPrefs.Save();
                        Debug.Log($"Auto-Cleaned {originalCount - cleanedLogs.Count} duplicate logs!");
                    }
                }
            }
            catch {}
        }

        allLogs.Sort((a, b) => {
            DateTime dateA, dateB;
            bool validA = DateTime.TryParse(a.dateCompleted, out dateA);
            bool validB = DateTime.TryParse(b.dateCompleted, out dateB);
            if (validA && validB) return dateB.CompareTo(dateA); 
            return 0;
        });
    }

    private int GetRoutineDayIndex(DateTime date)
    {
        if (date < planStartDate) return -1; 

        int validDays = 0;
        for (DateTime d = planStartDate.Date; d < date.Date; d = d.AddDays(1))
        {
            int statusVal = PlayerPrefs.GetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), 1); 
            if (statusVal != 3 && statusVal != 5) 
            {
                validDays++;
            }
        }
        return validDays;
    }

    void SwitchTab(int tabIndex)
    {
        lastActiveTab = tabIndex; 
        
        if (panelDetail != null) panelDetail.SetActive(false);
        if (panelDaily != null) panelDaily.SetActive(tabIndex == 0);
        if (panelWeekly != null) panelWeekly.SetActive(tabIndex == 1);

        if (tabIndex == 0) BuildDailyList();
        else if (tabIndex == 1) PopulateWeeklyView();
    }

    void ChangeWeek(int direction)
    {
        currentDisplayWeek += direction;
        PopulateWeeklyView();
    }

    void PopulateWeeklyView()
    {
        if (btnPrevWeek != null) btnPrevWeek.interactable = (currentDisplayWeek > 1);
        if (btnNextWeek != null) btnNextWeek.interactable = (currentDisplayWeek < adjustedDurationWeeks);

        List<DateTime> daysInThisWeek = new List<DateTime>();
        int routineDaysCount = 0;
        int currentWk = 1;
        DateTime d = planStartDate.Date;

        while (currentWk <= currentDisplayWeek && d <= planStartDate.AddDays(365)) 
        {
            int statusVal = PlayerPrefs.GetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), 1);
            bool isValidRoutineDay = (statusVal != 3 && statusVal != 5); 

            if (currentWk == currentDisplayWeek)
            {
                daysInThisWeek.Add(d);
            }

            if (isValidRoutineDay)
            {
                routineDaysCount++;
                if (routineDaysCount >= 7)
                {
                    routineDaysCount = 0;
                    currentWk++;
                    if (currentWk > currentDisplayWeek) break; 
                }
            }
            d = d.AddDays(1);
        }

        if (daysInThisWeek.Count == 0) return; 

        DateTime weekStartDate = daysInThisWeek[0];
        DateTime weekEndDate = daysInThisWeek[daysInThisWeek.Count - 1];

        if (txtWeekNumberAndDate != null)
        {
            txtWeekNumberAndDate.text = $"WEEK {currentDisplayWeek} | {weekStartDate:MMM d} - {weekEndDate:MMM d, yyyy}".ToUpper();
        }

        foreach (Transform child in weeklyListContent) Destroy(child.gameObject);

        int totalAvgKgThisWeek = 0;
        int totalAvgRepsThisWeek = 0;
        int validCompletedDays = 0;
        int missedCountThisWeek = 0; 
        
        int validDaysInThisWeek = 0; 

        foreach (DateTime targetDate in daysInThisWeek)
        {
            WorkoutStatus status = (WorkoutStatus)PlayerPrefs.GetInt("Workout_Status_" + targetDate.ToString("yyyy-MM-dd"), 1); 
            
            GameObject item = Instantiate(weeklyItemPrefab, weeklyListContent);
            Button itemBtn = item.GetComponent<Button>();
            
            TMP_Text txtDayWork = item.transform.Find("txtDayAndWorkout")?.GetComponent<TMP_Text>();
            TMP_Text txtReps = item.transform.Find("txtRepsKG")?.GetComponent<TMP_Text>();
            TMP_Text txtStatus = item.transform.Find("txtDonePending")?.GetComponent<TMP_Text>();

            var logsToday = allLogs.Where(l => {
                if (DateTime.TryParse(l.dateCompleted, out DateTime dt)) return dt.Date == targetDate.Date;
                return false;
            }).ToList();

            int displayDayNum = validDaysInThisWeek + 1; 
            string uiDayWeekText = $"DAY {displayDayNum} - WEEK {currentDisplayWeek}";

            if (status == WorkoutStatus.Missed)
            {
                if (txtDayWork != null) txtDayWork.text = $"DAY {displayDayNum} - MISSED WORKOUT";
                if (txtStatus != null) txtStatus.text = "<color=#E74C3C>MISSED</color>";
                if (txtReps != null) txtReps.gameObject.SetActive(false);
                if (itemBtn != null) itemBtn.interactable = false; 
                
                missedCountThisWeek++;
            }
            else if (status == WorkoutStatus.Rescheduled)
            {
                if (txtDayWork != null) txtDayWork.text = $"DAY {displayDayNum} - SCHEDULE ADJUSTED";
                if (txtStatus != null) txtStatus.text = "<color=#95A5A6>ADJUSTED</color>";
                if (txtReps != null) txtReps.gameObject.SetActive(false);
                if (itemBtn != null) itemBtn.interactable = false; 
            }
            else
            {
                int rIndex = ((currentDisplayWeek - 1) * 7) + validDaysInThisWeek;
                string expectedWorkout = activeCustomRoutine[rIndex % 7];

                if (txtDayWork != null) txtDayWork.text = $"DAY {displayDayNum} - {expectedWorkout.ToUpper()}";

                bool hasLogs = logsToday.Count > 0;

                if (status == WorkoutStatus.Completed)
                {
                    if (txtStatus != null) txtStatus.text = "<color=#2ECC71>DONE</color>";
                }
                else if (status == WorkoutStatus.InProgress || hasLogs)
                {
                    if (txtStatus != null) txtStatus.text = "<color=#3498DB>IN PROGRESS</color>";
                }
                else if (status == WorkoutStatus.RestDay || expectedWorkout.ToUpper().Contains("REST"))
                {
                    if (txtStatus != null) txtStatus.text = "<color=#F1C40F>RESTDAY</color>";
                }
                else
                {
                    if (txtStatus != null) txtStatus.text = "<color=#BDC3C7>PENDING</color>";
                }

                if (status == WorkoutStatus.Completed || status == WorkoutStatus.InProgress || hasLogs)
                {
                    if (itemBtn != null) 
                    {
                        itemBtn.interactable = true;
                        var capturedLogs = logsToday;
                        itemBtn.onClick.AddListener(() => OpenDetailPanel(capturedLogs, 0, uiDayWeekText));
                    }

                    if (txtReps != null && hasLogs)
                    {
                        txtReps.gameObject.SetActive(true);
                        int sumReps = 0, sumKg = 0, count = 0;

                        foreach (var l in logsToday)
                        {
                            sumReps += ParseAverageData(l.repsPerSet);
                            sumKg += ParseAverageData(l.weightUsed);
                            count++;
                        }

                        if (count > 0)
                        {
                            int exactAvgReps = sumReps / count;
                            int exactAvgKg = sumKg / count;

                            txtReps.text = $"Average of: {exactAvgReps} REPS | {exactAvgKg} KG";
                            
                            totalAvgKgThisWeek += exactAvgKg;
                            totalAvgRepsThisWeek += exactAvgReps;
                            validCompletedDays++;
                        }
                        else
                        {
                            txtReps.text = "Average of: -- REPS | -- KG";
                        }
                    }
                    else if (txtReps != null)
                    {
                        txtReps.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (txtReps != null) txtReps.gameObject.SetActive(false);
                    if (itemBtn != null) itemBtn.interactable = false; 
                }

                validDaysInThisWeek++;
            }
        }

        string improvementsStr = "<color=yellow>Keep pushing! Log your exercises to see progress here.</color>";
        
        if (validCompletedDays > 0)
        {
            int avgKgThisWeek = totalAvgKgThisWeek / validCompletedDays;
            int avgRepsThisWeek = totalAvgRepsThisWeek / validCompletedDays;

            string performanceComment = "";

            if (avgRepsThisWeek >= 12)
            {
                performanceComment = $"You're consistently hitting high reps ({avgRepsThisWeek} Avg)! Try adding 2.5 kg to 5.0 kg next week to trigger progressive overload.";
            }
            else if (avgRepsThisWeek >= 10)
            {
                performanceComment = $"Solid control with an average of {avgRepsThisWeek} reps! Keep the {avgKgThisWeek} KG weight and aim for a strict 12 reps on all sets before going heavier.";
            }
            else
            {
                performanceComment = $"You're lifting heavy at {avgKgThisWeek} KG but averaging {avgRepsThisWeek} reps. Focus on perfect form, or slightly reduce the weight to reach 10-12 reps.";
            }

            string consistencyComment = "";

            if (missedCountThisWeek == 0)
            {
                consistencyComment = "Perfect attendance this week! Your dedication is building unstoppable momentum.";
            }
            else if (missedCountThisWeek == 1)
            {
                consistencyComment = "Only one missed session. Don't let it break your stride—let's aim for zero misses next week!";
            }
            else
            {
                consistencyComment = $"You missed {missedCountThisWeek} days. Remember, consistency is the true secret to strength. You can do this!";
            }

            improvementsStr = $"<color=yellow>{performanceComment}\n\n{consistencyComment}</color>";
        }

        string missedStr = missedCountThisWeek == 0 ? "0 missed this week. Excellent!" : $"{missedCountThisWeek} day(s) missed.";

        if (txtSummary != null)
        {
            txtSummary.text = $"IMPROVEMENTS:\n{improvementsStr}\n\nMISSED:\n<color=#E74C3C>{missedStr}</color>";
        }
    }

    private int ParseAverageData(string data)
    {
        if (string.IsNullOrEmpty(data) || data == "-") return 0;
        string clean = data.Replace("reps", "").Replace("kg", "").Trim();
        string[] parts = clean.Split('|');
        int sum = 0, count = 0;
        foreach (string p in parts)
        {
            if (int.TryParse(p.Trim(), out int val))
            {
                sum += val;
                count++;
            }
        }
        return count > 0 ? sum / count : 0;
    }

    void BuildDailyList()
    {
        foreach (Transform child in dailyListContent) Destroy(child.gameObject);

        var groupedSessions = allLogs.GroupBy(log => new { log.dateCompleted, log.workoutType }).ToList();

        foreach (var sessionGroup in groupedSessions)
        {
            GameObject item = Instantiate(dailyItemPrefab, dailyListContent);
            List<WorkoutLog> sessionLogs = sessionGroup.ToList();
            WorkoutLog firstLog = sessionLogs[0]; 

            string displayTitle = string.IsNullOrEmpty(firstLog.workoutType) ? "FREESTYLE" : firstLog.workoutType.ToUpper();
            if (!displayTitle.Contains("WORKOUT")) displayTitle += " WORKOUT";

            string rawDate = firstLog.dateCompleted ?? "";
            string formattedDate = rawDate;
            if (DateTime.TryParse(rawDate, out DateTime pDate))
            {
                formattedDate = pDate.ToString("MMMM dd, yyyy");
            }

            string dayWeekSuffix = "";
            string uiDayWeekText = ""; 
            if (DateTime.TryParse(rawDate, out DateTime logDate))
            {
                int rIndex = GetRoutineDayIndex(logDate);
                if (rIndex >= 0)
                {
                    int dNum = (rIndex % 7) + 1;
                    int wNum = (rIndex / 7) + 1;
                    dayWeekSuffix = $" | Day {dNum} | Week {wNum}";
                    uiDayWeekText = $"DAY {dNum} - WEEK {wNum}"; 
                }
            }

            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) texts[0].text = displayTitle; 
            if (texts.Length > 1) texts[1].text = formattedDate + dayWeekSuffix; 

            Button btn = item.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => OpenDetailPanel(sessionLogs, 0, uiDayWeekText));
        }
    }

    void OpenDetailPanel(List<WorkoutLog> sessionLogs, int startingIndex, string overrideDayWeekText)
    {
        currentSessionLogs = sessionLogs;
        currentMachineIndex = startingIndex;
        detailOverrideDayWeek = overrideDayWeekText; 
        
        if (panelDaily != null) panelDaily.SetActive(false);
        if (panelWeekly != null) panelWeekly.SetActive(false);
        if (panelDetail != null) panelDetail.SetActive(true);
        
        RefreshDetailView();
    }

    void CloseDetailPanel()
    {
        if (panelDetail != null) panelDetail.SetActive(false);
        SwitchTab(lastActiveTab);
    }

    void ShowNextMachine() { if (currentMachineIndex < currentSessionLogs.Count - 1) { currentMachineIndex++; RefreshDetailView(); } }
    void ShowPrevMachine() { if (currentMachineIndex > 0) { currentMachineIndex--; RefreshDetailView(); } }

    void RefreshDetailView()
    {
        if (currentSessionLogs == null || currentSessionLogs.Count == 0) return;
        WorkoutLog log = currentSessionLogs[currentMachineIndex];

        string actualWorkoutName = string.IsNullOrEmpty(log.workoutType) ? "FREESTYLE" : log.workoutType.ToUpper();
        if (!actualWorkoutName.Contains("WORKOUT")) actualWorkoutName += " WORKOUT";

        if (txtFitnessGoal != null) txtFitnessGoal.text = log.fitnessGoal ?? "FREE STYLE FITNESS";
        
        if (txtDayWeek != null) 
        {
            if (!string.IsNullOrEmpty(detailOverrideDayWeek)) txtDayWeek.text = detailOverrideDayWeek;
            else txtDayWeek.text = log.weekNumber > 0 ? $"DAY {log.dayNumber} - WEEK {log.weekNumber}" : log.dateCompleted;
        }

        if (txtWorkoutPlan != null) txtWorkoutPlan.text = actualWorkoutName;
        if (txtDetailMachineName != null) txtDetailMachineName.text = (log.machineName ?? "UNKNOWN").ToUpper();
        if (txtDetailDate != null) txtDetailDate.text = log.dateCompleted ?? ""; 
        if (txtDetailSets != null) txtDetailSets.text = log.setsCompleted ?? "";
        if (txtDetailReps != null) txtDetailReps.text = log.repsPerSet ?? "";
        if (txtDetailWeight != null) txtDetailWeight.text = log.weightUsed ?? "";

        if (imgMachineThumb != null)
        {
            Sprite machineSprite = LoadExerciseImageWithModel(log.machineName);
            if (machineSprite != null)
            {
                imgMachineThumb.sprite = machineSprite;
                imgMachineThumb.gameObject.SetActive(true);
            }
            else
            {
                imgMachineThumb.gameObject.SetActive(false); 
            }
        }

        if (btnPrevMachine != null) btnPrevMachine.interactable = (currentMachineIndex > 0);
        if (btnNextMachine != null) btnNextMachine.interactable = (currentMachineIndex < currentSessionLogs.Count - 1);
    }

    Sprite LoadExerciseImageWithModel(string machineName)
    {
        if (string.IsNullOrEmpty(machineName)) return null;
        
        string cleanId = machineName.ToLower().Replace(" machine", "").Replace(" ", "_").Trim();
        Sprite loadedSprite = null;

        List<string> possibleNames = new List<string> {
            cleanId + "withPeap",
            cleanId + "_withPeap",
            cleanId + "_machinewithPeap",
            cleanId + "_machine_withPeap",
            cleanId
        };

        if (cleanId.Contains("chest") || cleanId.Contains("chess")) 
        {
            possibleNames.InsertRange(0, new string[] {
                "chest_press_machinewithPeap", 
                "chest_press_machine_withPeap", 
                "chest_presswithPeap", 
                "chest_press_withPeap", 
                "chest_press", 
                "chest_press_machine", 
                "chess_press_machinewithPeap",
                "chess_press_machine_withPeap"
            });
        }
        
        if (cleanId.Contains("leg_extension")) possibleNames.Insert(0, "legs_extensionwithPeap");
        if (cleanId.Contains("lat_pulldown")) possibleNames.Insert(0, "lat_pulldown_machine_withPeap");

        foreach (string v in possibleNames) 
        {
            loadedSprite = Resources.Load<Sprite>("ExerciseImages/" + v);
            if (loadedSprite != null) return loadedSprite;

            loadedSprite = Resources.Load<Sprite>(v);
            if (loadedSprite != null) return loadedSprite;
        }

        foreach (string v in possibleNames) 
        {
            Texture2D tex = Resources.Load<Texture2D>("ExerciseImages/" + v);
            if (tex == null) tex = Resources.Load<Texture2D>(v);
            
            if (tex != null) {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        
        return null;
    }
}
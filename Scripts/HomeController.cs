using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeController : MonoBehaviour
{
    [Header("--- PANELS ---")]
    public GameObject panelHomePage;
    public GameObject panelDemoAcc;
    public GameObject panelCalendar;
    public GameObject panelBackupRestore;
    public GameObject panelNotification;

    [Header("--- HOME MENU BUTTONS ---")]
    public Button btnWorkoutHistory;
    public Button btnFitnessGoal;
    public Button btnCalendar;
    public Button btnDemoAccount;
    public Button btnBackupRestore;
    public Button btnNotification;
    
    // --- BAGONG IDINAGDAG NA BUTTONS PARA SA SCENES ---
    public Button btnStartWorkout;  // I-drag dito ang START WORKOUT button
    public Button btnHowToUse;      // I-drag dito ang HOW TO USE button
    public Button btnAbout;         // I-drag dito ang ABOUT button

    [Header("--- SCENE NAMES ---")]
    public string startWorkoutSceneName = "StartWorkout";
    public string howToUseSceneName = "HowToUseScene";
    public string aboutSceneName = "AboutScene";

    [Header("--- BACK / CLOSE BUTTONS ---")]
    public Button btnBackFromDemo;
    public Button btnBackFromBackup;
    public Button btnCloseNotification;

    void Start()
    {
        // Pangkalahatang button listeners para sa mga panels
        if (btnWorkoutHistory != null) btnWorkoutHistory.onClick.AddListener(() => SceneManager.LoadScene("WorkoutHistoryScene"));
        if (btnFitnessGoal != null) btnFitnessGoal.onClick.AddListener(() => SceneManager.LoadScene("FitnessGoal"));
        
        if (btnCalendar != null) btnCalendar.onClick.AddListener(() => {
            if (panelHomePage != null) panelHomePage.SetActive(false);
            if (panelCalendar != null) panelCalendar.SetActive(true);
        });

        if (btnDemoAccount != null) btnDemoAccount.onClick.AddListener(() => {
            if (panelHomePage != null) panelHomePage.SetActive(false);
            if (panelDemoAcc != null) panelDemoAcc.SetActive(true);
        });

        if (btnBackupRestore != null) btnBackupRestore.onClick.AddListener(() => {
            if (panelHomePage != null) panelHomePage.SetActive(false);
            if (panelBackupRestore != null) panelBackupRestore.SetActive(true);
        });

        if (btnNotification != null) btnNotification.onClick.AddListener(() => {
            if (panelNotification != null) panelNotification.SetActive(true);
        });

        // --- MGA BAGONG LISTENERS PARA SA SCENE NAVIGATION ---
        if (btnStartWorkout != null) btnStartWorkout.onClick.AddListener(() => SceneManager.LoadScene(startWorkoutSceneName));
        if (btnHowToUse != null) btnHowToUse.onClick.AddListener(() => SceneManager.LoadScene(howToUseSceneName));
        if (btnAbout != null) btnAbout.onClick.AddListener(() => SceneManager.LoadScene(aboutSceneName));

        // Back buttons
        if (btnBackFromDemo != null) btnBackFromDemo.onClick.AddListener(CloseAllPanels);
        if (btnBackFromBackup != null) btnBackFromBackup.onClick.AddListener(CloseAllPanels);
        if (btnCloseNotification != null) btnCloseNotification.onClick.AddListener(() => {
            if (panelNotification != null) panelNotification.SetActive(false);
        });
    }

    void CloseAllPanels()
    {
        if (panelDemoAcc != null) panelDemoAcc.SetActive(false);
        if (panelCalendar != null) panelCalendar.SetActive(false);
        if (panelBackupRestore != null) panelBackupRestore.SetActive(false);
        if (panelNotification != null) panelNotification.SetActive(false);
        if (panelHomePage != null) panelHomePage.SetActive(true);
    }
}
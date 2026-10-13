using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DataBackupManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text txtFeedback; 
    public TMP_Dropdown dropdownBackups; // [BAGO]: I-drag dito ang ginawa mong TMP_Dropdown

    private string GetFolderPath()
    {
        // Laging gagamitin ang persistentDataPath para 100% safe sa Android at PC
        return Application.persistentDataPath;
    }

    private void OnEnable()
    {
        // Kapag binuksan ang panel, i-scan agad ang mga backups para ilagay sa Dropdown
        RefreshBackupList();
    }

    // ==========================================
    // 1. EXPORT TO FILE (WITH DATE & TIME)
    // ==========================================
    public void ExportProgressToFile()
    {
        try
        {
            string historyData = PlayerPrefs.GetString("UserWorkoutHistory", "");
            string activePlan = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
            string startDate = PlayerPrefs.GetString("ActivePlanStartDate", "");
            string initialRoutine = PlayerPrefs.GetString("ActiveInitialRoutine", "");
            string customRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");

            BackupFormat backup = new BackupFormat
            {
                savedHistory = historyData,
                savedPlanId = activePlan,
                savedStartDate = startDate,
                savedInitialRoutine = initialRoutine,
                savedCustomRoutine = customRoutine
            };

            string rawJson = JsonUtility.ToJson(backup);
            string encryptedData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawJson));

            string folderPath = GetFolderPath();
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // [BAGO]: Dynamic File Naming (e.g., FitnessBackup_2026-09-10_02-30-PM.dat)
            string dateString = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-tt");
            string fileName = $"FitnessBackup_{dateString}.dat";
            string filePath = Path.Combine(folderPath, fileName);

            File.WriteAllText(filePath, encryptedData);

            ShowFeedback($"EXPORT SUCCESS!\nBackup saved as:\n{fileName}");
            Debug.Log("File Exported to: " + filePath);

            // I-refresh ang dropdown para makita agad yung bagong gawang backup!
            RefreshBackupList();
        }
        catch (Exception e)
        {
            ShowFeedback("Export Failed: " + e.Message);
            Debug.LogError(e.Message);
        }
    }

    // ==========================================
    // 2. SCAN AND POPULATE DROPDOWN
    // ==========================================
    public void RefreshBackupList()
    {
        if (dropdownBackups == null) return;

        dropdownBackups.ClearOptions();
        string folderPath = GetFolderPath();

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Kunin lahat ng .dat files sa folder
        string[] files = Directory.GetFiles(folderPath, "*.dat");
        List<string> fileNames = new List<string>();

        if (files.Length == 0)
        {
            fileNames.Add("No backups found");
            dropdownBackups.interactable = false;
        }
        else
        {
            // I-sort para yung pinakabagong backup ang nasa pinakataas ng listahan
            var sortedFiles = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.CreationTime).ToList();

            foreach (var fileInfo in sortedFiles)
            {
                fileNames.Add(fileInfo.Name); // Name lang ng file ang ipapakita (hal. FitnessBackup_2026-09-10...)
            }
            dropdownBackups.interactable = true;
        }

        dropdownBackups.AddOptions(fileNames);
    }

    // ==========================================
    // 3. IMPORT FROM SELECTED FILE IN DROPDOWN
    // ==========================================
    public void ImportProgressFromFile()
    {
        // I-check kung may napiling file sa dropdown
        if (dropdownBackups == null || !dropdownBackups.interactable || dropdownBackups.options.Count == 0)
        {
            ShowFeedback("Please select a valid backup file from the dropdown first.");
            return;
        }

        // Kunin ang pangalan ng file na pinili ng user sa Dropdown
        string selectedFileName = dropdownBackups.options[dropdownBackups.value].text;
        string filePath = Path.Combine(GetFolderPath(), selectedFileName);

        if (!File.Exists(filePath))
        {
            ShowFeedback("Selected backup file could not be found.");
            return;
        }

        try
        {
            string encryptedData = File.ReadAllText(filePath);
            string rawJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedData));
            BackupFormat backup = JsonUtility.FromJson<BackupFormat>(rawJson);

            if (backup != null)
            {
                if (!string.IsNullOrEmpty(backup.savedHistory)) PlayerPrefs.SetString("UserWorkoutHistory", backup.savedHistory);
                if (!string.IsNullOrEmpty(backup.savedPlanId)) PlayerPrefs.SetString("ActiveFitnessPlanId", backup.savedPlanId);
                if (!string.IsNullOrEmpty(backup.savedStartDate)) PlayerPrefs.SetString("ActivePlanStartDate", backup.savedStartDate);
                if (!string.IsNullOrEmpty(backup.savedInitialRoutine)) PlayerPrefs.SetString("ActiveInitialRoutine", backup.savedInitialRoutine);
                if (!string.IsNullOrEmpty(backup.savedCustomRoutine)) PlayerPrefs.SetString("ActiveCustomRoutine", backup.savedCustomRoutine);

                PlayerPrefs.Save();
                ShowFeedback($"IMPORT SUCCESS!\nProgress restored from:\n{selectedFileName}");
            }
        }
        catch (Exception e)
        {
            ShowFeedback("Backup file is corrupted or invalid.");
            Debug.LogError("Import Error: " + e.Message);
        }
    }

    private void ShowFeedback(string message)
    {
        if (txtFeedback != null)
        {
            txtFeedback.text = message;
        }
    }
}

[Serializable]
public class BackupFormat
{
    public string savedHistory;
    public string savedPlanId;
    public string savedStartDate;
    public string savedInitialRoutine;
    public string savedCustomRoutine;
}   
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class SceneTracker
{
    public static string PreviousScene = "Select3DOnAR";
}

public class DetailsController : MonoBehaviour
{
    [Header("--- MACHINE INFO UI ---")]
    public TMP_Text txtMachineName;
    public Image imgMachinePreview;      

    [Header("--- MUSCLE INFO UI ---")]
    public TMP_Text txtPrimaryMuscles;
    public TMP_Text txtSecondaryMuscles;

    [Header("--- COLOR-CODED DOTS ---")]
    public Image dotPrimary;
    public Image dotSecondary1;
    public Image dotSecondary2;

    [Header("--- BEGINNER INFO UI ---")]
    public TMP_Text txtBeginnerReco;

    [Header("--- BUTTONS ---")]
    public Button btnBack;
    public Button btn3DGuide;

    [Header("--- SCENE NAMES ---")]
    public string guideCardsSceneName = "GuideCardsScene";

    void Start()
    {
        string currentId = WorkoutSession.SelectedMachineId;
        
        Debug.Log("DetailsController: Received ID -> " + currentId);

        // 1. Kunin ang Machine Name at Data mula sa Scriptable Object
        MachineGuideData matchedGuide = LoadMachineGuideData(currentId);
        string displayName = matchedGuide != null ? matchedGuide.machineDisplayName : WorkoutSession.SelectedMachineDisplayName;

        if (txtMachineName != null)
            txtMachineName.text = displayName;

        // 2. KUNIN ANG IMAGE (Uunahin natin yung naka-assign sa Scriptable Object mo!)
        if (imgMachinePreview != null)
        {
            if (matchedGuide != null && matchedGuide.machineImage != null)
            {
                // Kukunin nito diretso yung 'chest_press_machine' sprite na nilagay mo sa Inspector
                imgMachinePreview.sprite = matchedGuide.machineImage;
                imgMachinePreview.gameObject.SetActive(true);
            }
            else
            {
                // Fallback (kung sakaling blangko sa inspector)
                Sprite loadedSprite = Resources.Load<Sprite>("MachineImages/" + currentId);
                if (loadedSprite == null) loadedSprite = Resources.Load<Sprite>(currentId); 

                if (loadedSprite != null)
                {
                    imgMachinePreview.sprite = loadedSprite;
                    imgMachinePreview.gameObject.SetActive(true);
                }
            }
        }

        // 3. GAMITIN ANG SWITCH CASE PARA SA MGA DETALYE
        SetMachineDetails(currentId);

        // 4. Dynamic Back Button Navigation
       if (btnBack != null)
        {
            btnBack.onClick.AddListener(() => {
                string targetScene = SceneTracker.PreviousScene;
                
                // --- FAILSAFE: I-skip ang Loading Scene pabalik sa Scanner ---
                if (targetScene == "LoadingScene") 
                {
                    targetScene = "NewScanScene"; // Direkta na sa Scan Scene
                }
                
                // Fallback kung walang na-record   
                if (string.IsNullOrEmpty(targetScene)) 
                {
                    targetScene = "NewScanScene"; 
                }
                
                SceneManager.LoadScene(targetScene);
            });
        }

        if (btn3DGuide != null)
        {
            btn3DGuide.onClick.AddListener(() => {
                SceneManager.LoadScene(guideCardsSceneName);
            });
        }
    }

    void SetMachineDetails(string id)
    {
        string primary = "";
        string sec1 = "";
        string sec2 = "";
        string reco = "3 sets of 10–12 repetitions";

        // Nililinis natin ang string para tanggapin kahit may "_machine" man o wala
        string cleanId = id.Replace("_machine", "").ToLower();

        switch (cleanId)
        {
            // IDINAGDAG ANG "chess_press" DITO PARA KAHIT MAY TYPO, TAMA PA RIN ANG MUSCLES NA LALABAS
            case "chest_press" or "chest press" or "chess_press":
                primary = "Chest (Pectoralis Major)";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "lat_pulldown" or "lat pulldown":
                primary = "Mid Back/Lats (Latissimus Dorsi)";
                sec1 = "Biceps";
                sec2 = "Rear Shoulders";
                break;
            case "incline_chest_press":
                primary = "Upper Chest (Pectoralis Major)";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "flat_bench_chest_press":
                primary = "Chest (Pectoralis Major)";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "pec_fly" or "pec fly":
                primary = "Chest (Pectoralis Major)";
                sec1 = "Outer Shoulders";
                sec2 = "";
                reco = "3 sets of 12–15 repetitions";
                break;
            case "shoulder_press" or "seated_shoulder_press":
                primary = "Outer Shoulders (Lateral Deltoid)";
                sec1 = "Triceps";
                sec2 = "Traps";
                break;
            case "seated_row" or "seated row":
                primary = "Mid Back/Lats (Latissimus Dorsi)";
                sec1 = "Biceps";
                sec2 = "Rear Shoulders";
                break;
            case "legs_extension" or "legs extension":
                primary = "Quads/Thighs (Quadriceps Femoris)";
                sec1 = "Hip Flexors";
                sec2 = "";
                reco = "3 sets of 12–15 repetitions";
                break;
            case "leg_press" or "leg press":
                primary = "Quads/Thighs (Quadriceps Femoris)";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                break;
            case "squat":
                primary = "Quads/Thighs (Quadriceps Femoris)";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                reco = "3 sets of 8–10 repetitions";
                break;
            case "seated_leg_curl":
                primary = "Hamstrings (Biceps Femoris)";
                sec1 = "Calves";
                sec2 = "Glutes";
                reco = "3 sets of 12–15 repetitions";
                break;
            case "preacher_curl" or "preacher curl":
                primary = "Biceps (Biceps Brachii)";
                sec1 = "Forearm Flexors";
                sec2 = "";
                break;
            case "abductor":
                primary = "Inner Thigh / Adductor (Gracilis)";
                sec1 = "Glutes";
                sec2 = "";
                reco = "3 sets of 12–15 repetitions";
                break;
            case "smith":
                primary = "Quads/Thighs (Quadriceps Femoris)";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                reco = "3 sets of 8–10 repetitions";
                break;
            case "t_bar" or "t-bar":
                primary = "Mid Back/Lats (Latissimus Dorsi)";
                sec1 = "Biceps";
                sec2 = "Traps";
                break;
            default:
                primary = "General Muscle Group";
                sec1 = "";
                sec2 = "";
                break;
        }

        // I-set ang Texts at Dots
        if (txtPrimaryMuscles != null) txtPrimaryMuscles.text = primary;
        if (dotPrimary != null)
        {
            dotPrimary.color = GetMuscleColor(primary);
            dotPrimary.gameObject.SetActive(!string.IsNullOrEmpty(primary));
        }

        if (txtSecondaryMuscles != null)
        {
            if (!string.IsNullOrEmpty(sec1) && !string.IsNullOrEmpty(sec2))
                txtSecondaryMuscles.text = sec1 + "\n" + sec2;
            else if (!string.IsNullOrEmpty(sec1))
                txtSecondaryMuscles.text = sec1;
            else
                txtSecondaryMuscles.text = "None";
        }

        if (dotSecondary1 != null)
        {
            if (!string.IsNullOrEmpty(sec1))
            {
                dotSecondary1.color = GetMuscleColor(sec1);
                dotSecondary1.gameObject.SetActive(true);
            }
            else { dotSecondary1.gameObject.SetActive(false); }
        }

        if (dotSecondary2 != null)
        {
            if (!string.IsNullOrEmpty(sec2))
            {
                dotSecondary2.color = GetMuscleColor(sec2);
                dotSecondary2.gameObject.SetActive(true);
            }
            else { dotSecondary2.gameObject.SetActive(false); }
        }

        if (txtBeginnerReco != null)
            txtBeginnerReco.text = "Recommended for Beginners\n" + reco;
    }

    Color GetMuscleColor(string muscleName)
    {
        string m = muscleName.ToLower();

        if (m.Contains("chest") || m.Contains("pectoralis") || m.Contains("upper chest"))
            return new Color32(219, 53, 39, 255);       
        else if (m.Contains("lat") || m.Contains("mid back"))
            return new Color32(84, 153, 50, 255);       
        else if (m.Contains("quad") || m.Contains("thigh"))
            return new Color32(23, 43, 77, 255);        
        else if (m.Contains("shoulder") || m.Contains("deltoid"))
            return new Color32(31, 111, 235, 255);      
        else if (m.Contains("biceps"))
            return new Color32(186, 85, 211, 255);      
        else if (m.Contains("triceps"))
            return new Color32(92, 40, 18, 255);        
        else if (m.Contains("hamstring"))
            return new Color32(24, 134, 115, 255);      
        else if (m.Contains("glute"))
            return new Color32(115, 59, 150, 255);      
        else if (m.Contains("calf") || m.Contains("calves"))
            return new Color32(139, 0, 0, 255);         
        else if (m.Contains("trap"))
            return new Color32(10, 100, 30, 255);       
        else if (m.Contains("forearm"))
            return new Color32(120, 160, 120, 255);     
        else if (m.Contains("adductor") || m.Contains("inner thigh") || m.Contains("hip flexor"))
            return new Color32(110, 90, 160, 255);      

        return Color.white;
    }

    MachineGuideData LoadMachineGuideData(string machineId)
    {
        MachineGuideData[] allGuides = Resources.LoadAll<MachineGuideData>("MachineData");
        if (allGuides.Length == 0) allGuides = Resources.LoadAll<MachineGuideData>("");

        foreach (var guide in allGuides)
        {
            if (guide != null && guide.machineId == machineId) return guide;
        }
        return null;
    }
}
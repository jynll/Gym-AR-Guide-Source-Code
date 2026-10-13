using UnityEngine;
using System.Collections.Generic;

public static class MachineDatabase
{
    public class MuscleInfo
    {
        public string name;
        public Color  color;
        public MuscleInfo(string n, Color c) { name = n; color = c; }
    }

    public class MachineInfo
    {
        public string     machineId;
        public string     displayName;
        public MuscleInfo primaryMuscle;
        public MuscleInfo secondaryMuscle1;
        public MuscleInfo secondaryMuscle2;
    }

    // Muscle colors from anatomy diagram
    public static readonly Color ColChest         = new Color(0.88f, 0.18f, 0.18f);
    public static readonly Color ColBiceps        = new Color(0.80f, 0.38f, 0.60f);
    public static readonly Color ColTriceps       = new Color(0.48f, 0.10f, 0.10f);
    public static readonly Color ColShoulders     = new Color(0.38f, 0.68f, 1.00f);
    public static readonly Color ColRearShoulders = new Color(0.20f, 0.40f, 0.90f);
    public static readonly Color ColTraps         = new Color(0.10f, 0.48f, 0.18f);
    public static readonly Color ColLats          = new Color(0.40f, 0.60f, 0.18f);
    public static readonly Color ColForearms      = new Color(0.18f, 0.68f, 0.55f);
    public static readonly Color ColQuads         = new Color(0.10f, 0.18f, 0.58f);
    public static readonly Color ColGlutes        = new Color(0.50f, 0.18f, 0.70f);
    public static readonly Color ColHamstrings    = new Color(0.10f, 0.55f, 0.55f);
    public static readonly Color ColCalves        = new Color(0.58f, 0.18f, 0.18f);
    public static readonly Color ColAdductor      = new Color(0.68f, 0.50f, 0.90f);

    private static readonly List<MachineInfo> machines = new List<MachineInfo>()
    {
        new MachineInfo {
            machineId        = "abductor_machine",
            displayName      = "Abductor Machine",
            primaryMuscle    = new MuscleInfo("Gracilis (Adductor)", ColAdductor),
            secondaryMuscle1 = new MuscleInfo("Gluteus Maximus",     ColGlutes),
            secondaryMuscle2 = null
        },
        new MachineInfo {
            machineId        = "chest_press_machine",
            displayName      = "Chest Press Machine",
            primaryMuscle    = new MuscleInfo("Pectoralis Major",  ColChest),
            secondaryMuscle1 = new MuscleInfo("Triceps Brachii",   ColTriceps),
            secondaryMuscle2 = new MuscleInfo("Lateral Deltoid",   ColShoulders)
        },
        new MachineInfo {
            machineId        = "lat_pulldown_machine",
            displayName      = "Lat Pulldown Machine",
            primaryMuscle    = new MuscleInfo("Latissimus Dorsi",  ColLats),
            secondaryMuscle1 = new MuscleInfo("Biceps Brachii",    ColBiceps),
            secondaryMuscle2 = new MuscleInfo("Posterior Deltoid", ColRearShoulders)
        },
        new MachineInfo {
            machineId        = "leg_extension_machine",
            displayName      = "Leg Extension Machine",
            primaryMuscle    = new MuscleInfo("Quadriceps Femoris", ColQuads),
            secondaryMuscle1 = null,
            secondaryMuscle2 = null
        },
        new MachineInfo {
            machineId        = "leg_press_machine",
            displayName      = "Leg Press Machine",
            primaryMuscle    = new MuscleInfo("Quadriceps Femoris", ColQuads),
            secondaryMuscle1 = new MuscleInfo("Gluteus Maximus",    ColGlutes),
            secondaryMuscle2 = new MuscleInfo("Biceps Femoris",     ColHamstrings)
        },
        new MachineInfo {
            machineId        = "pec_fly_machine",
            displayName      = "Pec Fly Machine",
            primaryMuscle    = new MuscleInfo("Pectoralis Major",  ColChest),
            secondaryMuscle1 = new MuscleInfo("Lateral Deltoid",   ColShoulders),
            secondaryMuscle2 = null
        },
        new MachineInfo {
            machineId        = "preacher_curl_machine",
            displayName      = "Preacher Curl Machine",
            primaryMuscle    = new MuscleInfo("Biceps Brachii",    ColBiceps),
            secondaryMuscle1 = new MuscleInfo("Brachioradialis",   ColForearms),
            secondaryMuscle2 = null
        },
        new MachineInfo {
            machineId        = "seated_row_machine",
            displayName      = "Seated Row Machine",
            primaryMuscle    = new MuscleInfo("Latissimus Dorsi",  ColLats),
            secondaryMuscle1 = new MuscleInfo("Biceps Brachii",    ColBiceps),
            secondaryMuscle2 = new MuscleInfo("Posterior Deltoid", ColRearShoulders)
        },
        new MachineInfo {
            machineId        = "shoulder_press_machine",
            displayName      = "Shoulder Press Machine",
            primaryMuscle    = new MuscleInfo("Lateral Deltoid",   ColShoulders),
            secondaryMuscle1 = new MuscleInfo("Triceps Brachii",   ColTriceps),
            secondaryMuscle2 = new MuscleInfo("Trapezius",         ColTraps)
        },
        new MachineInfo {
            machineId        = "smith_machine",
            displayName      = "Smith Machine",
            primaryMuscle    = new MuscleInfo("Quadriceps Femoris", ColQuads),
            secondaryMuscle1 = new MuscleInfo("Gluteus Maximus",    ColGlutes),
            secondaryMuscle2 = new MuscleInfo("Biceps Femoris",     ColHamstrings)
        },
        new MachineInfo {
            machineId        = "t_bar_machine",
            displayName      = "T-bar Machine",
            primaryMuscle    = new MuscleInfo("Latissimus Dorsi",  ColLats),
            secondaryMuscle1 = new MuscleInfo("Biceps Brachii",    ColBiceps),
            secondaryMuscle2 = new MuscleInfo("Posterior Deltoid", ColRearShoulders)
        },
    };

    public static MachineInfo Get(string machineId)
    {
        foreach (var m in machines)
            if (m.machineId == machineId) return m;
        Debug.LogWarning("MachineDatabase: No entry for '" + machineId + "'");
        return null;
    }
}
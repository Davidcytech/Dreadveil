/*using System.Collections.Generic;
using UnityEngine;

public static class CheckpointData
{
    public static bool hasCheckpoint;
    public static Vector3 position;
    public static Quaternion rotation;

    // estado do player
    public static float health, energy, energyPowerupTime;
    public static bool shield, knifeUnlocked, hasDynamite;
    public static int collectables;

    // estado do mundo
    private static HashSet<string> saved = new HashSet<string>();
    private static HashSet<string> pending = new HashSet<string>();

    public static void MarkDone(string id) { pending.Add(id); }
    public static bool IsDone(string id) { return saved.Contains(id) || pending.Contains(id); }
    public static void Commit() { saved.UnionWith(pending); pending.Clear(); }
    public static void DiscardPending() { pending.Clear(); }

    // chama ao começar um jogo novo 
    public static void ResetAll()
    {
        hasCheckpoint = false;
        saved.Clear();
        pending.Clear();
    }
}*/
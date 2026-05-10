using System.IO;
using UnityEngine;

public static class LocalSaveService
{
    public static void SaveProfile(AccountProfileSaveData data)
    {
        if (data == null)
            return;

        data.lastSavedUnixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePathUtility.GetProfilePath(data.accountId), json);
    }

    public static AccountProfileSaveData LoadProfile(string accountId)
    {
        string path = SavePathUtility.GetProfilePath(accountId);
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<AccountProfileSaveData>(json);
    }

    public static void SaveWorldRun(ActiveWorldRunSaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.ownerAccountId))
            return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePathUtility.GetWorldRunPath(data.ownerAccountId), json);
    }

    public static ActiveWorldRunSaveData LoadWorldRun(string accountId)
    {
        string path = SavePathUtility.GetWorldRunPath(accountId);
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<ActiveWorldRunSaveData>(json);
    }

    public static bool HasWorldRun(string accountId)
    {
        return File.Exists(SavePathUtility.GetWorldRunPath(accountId));
    }

    public static void DeleteWorldRun(string accountId)
    {
        string path = SavePathUtility.GetWorldRunPath(accountId);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void DeleteProfile(string accountId)
    {
        string path = SavePathUtility.GetProfilePath(accountId);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void DeleteProgressData(string accountId)
    {
        DeleteProfile(accountId);
        DeleteWorldRun(accountId);
    }
}

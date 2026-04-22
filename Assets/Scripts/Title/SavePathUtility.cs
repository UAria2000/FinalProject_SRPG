using System.IO;
using UnityEngine;

public static class SavePathUtility
{
    public static string RootDirectory
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string GetProfilePath(string accountId)
    {
        return Path.Combine(RootDirectory, $"profile_{SanitizeAccountId(accountId)}.json");
    }

    public static string GetWorldRunPath(string accountId)
    {
        return Path.Combine(RootDirectory, $"worldrun_{SanitizeAccountId(accountId)}.json");
    }

    public static string SanitizeAccountId(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return "default";

        foreach (char c in Path.GetInvalidFileNameChars())
            accountId = accountId.Replace(c, '_');

        return accountId.Trim();
    }
}

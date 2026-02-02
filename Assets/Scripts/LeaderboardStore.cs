using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LeaderboardStore
{
    [Serializable]
    public class Entry
    {
        public string mode;
        public float timeSeconds;
        public int score;
        public string grade;
        public bool success;
        public string dateUtc; // ISO string (DateTime.UtcNow.ToString("o"))
    }

    [Serializable]
    private class EntryList
    {
        public List<Entry> entries = new List<Entry>();
    }

    // Change if you want
    private const int MaxEntries = 50;

    private static readonly string FilePath =
        Path.Combine(Application.persistentDataPath, "leaderboard.json");

    // Cache so we don't read disk repeatedly
    private static EntryList cache;
    private static bool loaded;

    // ---------------- Public API ----------------

    public static void AddEntry(Entry entry)
    {
        if (entry == null) return;

        var list = LoadList();

        list.entries.Add(entry);

        // Keep it from growing forever (optional)
        SortInPlace(list.entries);
        if (list.entries.Count > MaxEntries)
            list.entries.RemoveRange(MaxEntries, list.entries.Count - MaxEntries);

        SaveList(list);
    }

    public static IEnumerable<Entry> GetEntriesSorted()
    {
        var list = LoadList();

        // Return a sorted copy (so UI can iterate safely)
        var copy = new List<Entry>(list.entries);
        SortInPlace(copy);
        return copy;
    }

    public static void Clear()
    {
        var list = LoadList();
        list.entries.Clear();
        SaveList(list);
    }

    // Optional helper if you ever want raw list
    public static List<Entry> GetEntriesRawCopy()
    {
        var list = LoadList();
        return new List<Entry>(list.entries);
    }

    // ---------------- Internals ----------------

    private static EntryList LoadList()
    {
        if (loaded && cache != null)
            return cache;

        loaded = true;

        try
        {
            if (!File.Exists(FilePath))
            {
                cache = new EntryList();
                return cache;
            }

            string json = File.ReadAllText(FilePath);
            cache = JsonUtility.FromJson<EntryList>(json);

            if (cache == null || cache.entries == null)
                cache = new EntryList();

            return cache;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardStore: Failed to load file. Creating new. Error: {e.Message}");
            cache = new EntryList();
            return cache;
        }
    }

    private static void SaveList(EntryList list)
    {
        cache = list;
        loaded = true;

        try
        {
            // Make sure folder exists (usually does)
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"LeaderboardStore: Failed to save file. Error: {e.Message}");
        }
    }

    private static void SortInPlace(List<Entry> entries)
    {
        // Example sort:
        // 1) Success before fail
        // 2) Higher score first
        // 3) Faster time first
        // 4) Newer date first (if available)
        entries.Sort((a, b) =>
        {
            int successCmp = b.success.CompareTo(a.success);
            if (successCmp != 0) return successCmp;

            int scoreCmp = b.score.CompareTo(a.score);
            if (scoreCmp != 0) return scoreCmp;

            int timeCmp = a.timeSeconds.CompareTo(b.timeSeconds);
            if (timeCmp != 0) return timeCmp;

            // Date (optional)
            if (!string.IsNullOrEmpty(a.dateUtc) && !string.IsNullOrEmpty(b.dateUtc))
                return string.CompareOrdinal(b.dateUtc, a.dateUtc);

            return 0;
        });
    }
}

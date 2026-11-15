using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "ScriptableObjects/Database/DialogueDatabase")]
public class DialogueDatabaseSO : ScriptableObject
{
    private static DialogueDatabaseSO _instance;
    public static DialogueDatabaseSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<DialogueDatabaseSO>("Database/DialogueDatabase");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueDatabaseSO>("Assets/Project/Resources/Database/DialogueDatabase.asset");
                }
#endif
            }
            return _instance;
        }
    }

    [Header("Narrative Dialogue Entries (Designer / Writer Configurable)")]
    [SerializeField] private List<DialogueEntry> entries = new List<DialogueEntry>();

    private Dictionary<string, DialogueEntry> _lookupCache;

    public List<DialogueEntry> Entries => entries;

    public void SetEntriesForBuild(List<DialogueEntry> list)
    {
        entries = list;
        _lookupCache = null;
    }

    private void BuildLookupIfNeeded()
    {
        if (_lookupCache == null)
        {
            _lookupCache = new Dictionary<string, DialogueEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && !string.IsNullOrEmpty(entries[i].ID) && !_lookupCache.ContainsKey(entries[i].ID))
                {
                    _lookupCache[entries[i].ID] = entries[i];
                }
            }
        }
    }

    public DialogueEntry GetEntry(string id)
    {
        BuildLookupIfNeeded();
        if (_lookupCache != null && _lookupCache.TryGetValue(id, out var entry))
        {
            return entry;
        }
        return null;
    }

    public List<DialogueLine> GetLines(params string[] ids)
    {
        var lines = new List<DialogueLine>();
        foreach (var id in ids)
        {
            var e = GetEntry(id);
            if (e != null)
            {
                lines.Add(e.ToDialogueLine());
            }
        }
        return lines;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public class KeybindEntry
{
    public string displayName;
    public InputActionReference actionRef;
}

[System.Serializable]
public class SavedBinding
{
    public string actionName;
    public string bindingPath;
}


public class KeybindSettingsManager : MonoBehaviour
{

    public GameObject keybindItemPrefab; // the row prefab
    public Transform keybindListParent;  // assign Content of Scroll View
    public List<KeybindEntry> keybinds = new List<KeybindEntry>();

    public static bool IsRebindingKey = false;


    void Start()
    {
        // Load saved bindings before anything else
        LoadBindings();
        // Disable template
        keybindItemPrefab.SetActive(false);

        foreach (var entry in keybinds)
        {
            GameObject itemGO = Instantiate(keybindItemPrefab, keybindListParent);
            itemGO.SetActive(true); // Ensure new instance is active
            var item = itemGO.GetComponent<KeybindItem>();
            item.Init(entry.displayName, entry.actionRef.action);
        }
    }

    public void SaveBindings()
    {
        List<SavedBinding> saved = new List<SavedBinding>();
        foreach (var entry in keybinds)
        {
            var binding = entry.actionRef.action.bindings[0]; // assuming one binding per action
            saved.Add(new SavedBinding
            {
                actionName = entry.actionRef.action.name,
                bindingPath = binding.effectivePath
            });
        }

        string json = JsonUtility.ToJson(new SerializationWrapper<SavedBinding>(saved), true);
        System.IO.File.WriteAllText(GetBindingsPath(), json);
    }

    private string GetBindingsPath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "keybinds.json");
    }

    // Helper to serialize lists
    [System.Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> list;
        public SerializationWrapper(List<T> list) => this.list = list;
    }
    
    public void LoadBindings()
    {
        string path = GetBindingsPath();
        if (!System.IO.File.Exists(path)) return;

        string json = System.IO.File.ReadAllText(path);
        var savedBindings = JsonUtility.FromJson<SerializationWrapper<SavedBinding>>(json);

        foreach (var saved in savedBindings.list)
        {
            var entry = keybinds.Find(k => k.actionRef.action.name == saved.actionName);
            if (entry != null)
            {
                entry.actionRef.action.ApplyBindingOverride(saved.bindingPath);
            }
        }
    }
}

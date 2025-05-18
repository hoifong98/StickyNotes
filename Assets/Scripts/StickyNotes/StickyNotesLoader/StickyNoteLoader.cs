using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;


[System.Serializable]
public class ShoppingItem
{
    public string name;
    public int quantity;
}

[System.Serializable]
public class StickyNoteData
{
    public string priority;
    public List<ShoppingItem> items;
}

public class StickyNoteLoader : MonoBehaviour
{
    public TextMeshProUGUI noteText;
    public GameObject noteBackground;

    private DatabaseReference dbReference;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            
            var database = FirebaseDatabase.GetInstance("https://sticky-notes-6942e-default-rtdb.asia-southeast1.firebasedatabase.app/");
            dbReference = database.RootReference;

            LoadNote("note1");
        });
    }


    void LoadNote(string noteId)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("stickynotes").Child(noteId)
            .GetValueAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    Debug.LogError("Loading Failed");
                    return;
                }

                Debug.Log("Raw JSON: " + task.Result.GetRawJsonValue());
                string json = task.Result.GetRawJsonValue();
                StickyNoteData noteData = JsonUtility.FromJson<StickyNoteData>(json);
                DisplayNote(noteData);
            });
        Debug.Log("trying to get path：stickynotes/" + noteId);
    }

    void DisplayNote(StickyNoteData data)
    {
        string result = "";

        foreach (var item in data.items)
        {
            result += $"{item.name} x {item.quantity}\n";
        }

        noteText.text = result;

        if (data.priority == "high")
            noteBackground.GetComponent<UnityEngine.UI.Image>().color = Color.red;
        else if (data.priority == "medium")
            noteBackground.GetComponent<UnityEngine.UI.Image>().color = Color.yellow;
        else
            noteBackground.GetComponent<UnityEngine.UI.Image>().color = Color.green;
    }
}


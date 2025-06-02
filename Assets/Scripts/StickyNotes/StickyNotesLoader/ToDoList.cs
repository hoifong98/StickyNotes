using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;


[System.Serializable]
public class TodoItem
{
    public string title;
    public string note;
    public bool done;
    public bool important;
    public string colorHex;
}

[System.Serializable]
public class TodoListData
{
    public List<TodoItem> items;

    public TodoListData()
    {
        items = new List<TodoItem>();
    }
}

public class ToDoList : MonoBehaviour
{
    public string noteID = "note2";

    public Transform contentContainer;
    public GameObject todoListItemPrefab;
    public GameObject todoDetailCanvas;
    public Image todoImage;
    public Image detailPanelBackground;
    public GameObject todoListCanvasRoot;

    public Button redColorButton;
    public Button orangeColorButton;
    public Button yellowColorButton;
    public Button greenColorButton;
    public Button blueColorButton;
    public Button purpleColorButton;

    private List<TodoItem> todoItems = new List<TodoItem>();
    private TodoItem currentSelectedItem;

    private DatabaseReference databaseReference;
    private bool hasSubscribed = false;
    private string currentColorHex = "#D20A2E"; 


    void Start()
    {
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForFirebaseAndLoad());

        if (redColorButton != null) redColorButton.onClick.AddListener(() => SetNoteColor("#D20A2E"));
        if (orangeColorButton != null) orangeColorButton.onClick.AddListener(() => SetNoteColor("#FF964F"));
        if (yellowColorButton != null) yellowColorButton.onClick.AddListener(() => SetNoteColor("#FFDD3C"));
        if (greenColorButton != null) greenColorButton.onClick.AddListener(() => SetNoteColor("#00BB77"));
        if (blueColorButton != null) blueColorButton.onClick.AddListener(() => SetNoteColor("#A8B5E0"));
        if (purpleColorButton != null) purpleColorButton.onClick.AddListener(() => SetNoteColor("#B19CD9"));

        if (todoDetailCanvas != null)
        {
            todoDetailCanvas.SetActive(false);

            Button saveButton = todoDetailCanvas.transform.Find("DetailPanel/SaveButton")?.GetComponent<Button>();
            Button cancelButton = todoDetailCanvas.transform.Find("DetailPanel/CancelButton")?.GetComponent<Button>();

            if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    System.Collections.IEnumerator WaitForFirebaseAndLoad()
    {
        while (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            yield return null;

        LoadNote(noteID);
    }

    void SetNoteColor(string hexColor)
    {
        currentColorHex = hexColor;
        ApplySharedColor();
        SaveColorHexToFirebase();
    }


    void OnSaveButtonClicked()
    {
        if (currentSelectedItem != null)
        {
            TMP_InputField titleInputField = todoDetailCanvas.transform.Find("DetailPanel/TitleInputField")?.GetComponent<TMP_InputField>();
            TMP_InputField notesInputField = todoDetailCanvas.transform.Find("DetailPanel/NotesInputField")?.GetComponent<TMP_InputField>();
            Toggle importantToggle = todoDetailCanvas.transform.Find("DetailPanel/ImportantToggle")?.GetComponent<Toggle>();
            Toggle doneToggle = todoDetailCanvas.transform.Find("DetailPanel/DoneToggle")?.GetComponent<Toggle>();

            if (titleInputField != null) currentSelectedItem.title = titleInputField.text;
            if (notesInputField != null) currentSelectedItem.note = notesInputField.text;
            if (importantToggle != null) currentSelectedItem.important = importantToggle.isOn;
            if (doneToggle != null) currentSelectedItem.done = doneToggle.isOn;

            todoDetailCanvas.SetActive(false);
            todoListCanvasRoot.SetActive(true);

            DisplayTodoList();
            SaveToFirebase();
        }
    }

    void OnCancelButtonClicked()
    {
        todoDetailCanvas.SetActive(false);
        todoListCanvasRoot.SetActive(true);
        ApplySharedColor();
    }

    void LoadNote(string noteId)
    {
        if (hasSubscribed) return;

        FirebaseManager.Instance.DbReference.Child("stickynotes").Child(noteId)
            .ValueChanged += (object sender, ValueChangedEventArgs args) =>
            {
                if (args.DatabaseError != null)
                {
                    Debug.LogError("Firebase error: " + args.DatabaseError.Message);
                    return;
                }

                if (!args.Snapshot.Exists)
                {
                    Debug.LogWarning("To-do list not found.");
                    return;
                }

                // Load shared colorHex
                currentColorHex = args.Snapshot.Child("colorHex").Value?.ToString() ?? "#D20A2E";
                ApplySharedColor();

                // Load item list
                todoItems.Clear();
                foreach (var child in args.Snapshot.Child("items").Children)
                {
                    TodoItem item = new TodoItem
                    {
                        title = child.Child("title").Value?.ToString() ?? "",
                        note = child.Child("note").Value?.ToString() ?? "",
                        done = bool.TryParse(child.Child("done").Value?.ToString(), out bool d) && d,
                        important = bool.TryParse(child.Child("important").Value?.ToString(), out bool i) && i
                    };
                    todoItems.Add(item);
                }

                DisplayTodoList();
            };

        hasSubscribed = true;
    }



    void DisplayTodoList()
    {
        ApplySharedColor();
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in todoItems)
        {
            GameObject listItemGO = Instantiate(todoListItemPrefab, contentContainer);
            TextMeshProUGUI titleText = listItemGO.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            Toggle doneToggle = listItemGO.transform.Find("DoneToggle")?.GetComponent<Toggle>();
            Button detailButton = listItemGO.transform.Find("DetailButton")?.GetComponent<Button>();
            Image background = todoImage.GetComponent<Image>();

            if (titleText != null) titleText.text = item.title;
            if (doneToggle != null) doneToggle.isOn = item.done;

            if (ColorUtility.TryParseHtmlString(currentColorHex, out Color sharedColor))
                background.color = sharedColor;

            if (detailButton != null)
            {
                var currentItem = item;
                detailButton.onClick.AddListener(() => OpenDetailPage(currentItem));
            }
        }
    }

    void OpenDetailPage(TodoItem item)
    {
        currentSelectedItem = item;

        TMP_InputField titleInputField = todoDetailCanvas.transform.Find("DetailPanel/TitleInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField notesInputField = todoDetailCanvas.transform.Find("DetailPanel/NotesInputField")?.GetComponent<TMP_InputField>();
        Toggle importantToggle = todoDetailCanvas.transform.Find("DetailPanel/ImportantToggle")?.GetComponent<Toggle>();
        Toggle doneToggle = todoDetailCanvas.transform.Find("DetailPanel/DoneToggle")?.GetComponent<Toggle>();

        if (titleInputField != null) titleInputField.text = item.title;
        if (notesInputField != null) notesInputField.text = item.note;
        if (importantToggle != null) importantToggle.isOn = item.important;
        if (doneToggle != null) doneToggle.isOn = item.done;

        if (detailPanelBackground != null && !string.IsNullOrEmpty(item.colorHex) &&
            ColorUtility.TryParseHtmlString(item.colorHex, out Color color))
        {
            detailPanelBackground.color = color;
            if (todoImage != null) todoImage.color = color;
        }

        todoDetailCanvas.SetActive(true);
        todoListCanvasRoot.SetActive(false);
    }

    void SaveToFirebase()
    {
        TodoListData dataToSave = new TodoListData { items = todoItems };
        string json = JsonUtility.ToJson(dataToSave);

        databaseReference.Child("stickynotes").Child(noteID).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("To-do list saved to Firebase.");
                else
                    Debug.LogError("Failed to save to-do list: " + task.Exception);
            });
    }

    void SaveColorHexToFirebase()
    {
        databaseReference.Child("stickynotes").Child(noteID).Child("colorHex")
            .SetValueAsync(currentColorHex)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("Saved shared colorHex to Firebase.");
                else
                    Debug.LogError("Failed to save colorHex: " + task.Exception);
            });
    }

    void ApplySharedColor()
    {
        if (ColorUtility.TryParseHtmlString(currentColorHex, out Color sharedColor))
        {
            if (todoImage != null)
                todoImage.color = sharedColor;

            if (detailPanelBackground != null)
                detailPanelBackground.color = sharedColor;
        }
        else
        {
            Debug.LogWarning($"Invalid currentColorHex: {currentColorHex}");
        }
    }


}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using System.Linq;

[System.Serializable]
public class ShoppingItem
{
    public string name;
    public int quantity;
    public bool done;
    public bool isVisible = true;
}

[System.Serializable]
public class StickyNoteData
{
    public string colorHex;
    public List<ShoppingItem> items;
}

public class StickyNoteLoader : MonoBehaviour
{
    public Transform itemListContainer;
    public GameObject dataLinePrefab;
    public GameObject noteBackground;
    public string noteID;
    public Button openEditPanelButton;

    public GameObject editPanelCanvas;
    public GameObject editItemRowPrefab;
    public Transform editItemListContainer;
    public GameObject editPanelContainer;
    public Button editPanelSaveButton;
    public Button editPanelCancelButton;
    public Button editPanelAddNewItemButton;
    public Button closePanelButton;
    public Button closeEditPanelButton;

    private DatabaseReference databaseReference;
    private bool hasSubscribed = false;
    private StickyNoteData currentNoteData;

    public Color undoneTextColor = Color.white;
    public Color doneTextColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    public Sprite showIcon;
    public Sprite hideIcon;

    public Button redColorButton;
    public Button orangeColorButton;
    public Button yellowColorButton;
    public Button greenColorButton;
    public Button blueColorButton;
    public Button purpleColorButton;
    private Color originalNoteColor;

    private void OnEnable()
    {
        StartCoroutine(WaitForFirebaseAndLoad());

        if (openEditPanelButton != null) openEditPanelButton.onClick.AddListener(OpenEditPanel);
        else Debug.LogError("Open Edit Panel Button not assigned!");

        if (closePanelButton != null) closePanelButton.onClick.AddListener(ClosePanel);
        else Debug.LogError("Close Panel Button not assigned!");

        if (closeEditPanelButton != null) closeEditPanelButton.onClick.AddListener(ClosePanel);
        else Debug.LogError("Close Edit Panel Button not assigned!");

        if (editPanelSaveButton != null) editPanelSaveButton.onClick.AddListener(SaveEditedItems);
        else Debug.LogError("Edit Panel Save Button not assigned!");

        if (editPanelCancelButton != null) editPanelCancelButton.onClick.AddListener(CancelEdit);
        else Debug.LogError("Edit Panel Cancel Button not assigned!");

        if (editPanelAddNewItemButton != null)
        {
            editPanelAddNewItemButton.onClick.RemoveAllListeners();
            editPanelAddNewItemButton.onClick.AddListener(AddNewItemRow);
        }
        else Debug.LogError("Edit Panel Add New Item Button not assigned!");

        if (editPanelCanvas != null) editPanelCanvas.SetActive(false);

        if (redColorButton != null) redColorButton.onClick.AddListener(() => SetNoteColor("#D20A2E"));
        else Debug.LogError("Red Color Button not assigned!");

        if (orangeColorButton != null) orangeColorButton.onClick.AddListener(() => SetNoteColor("#FF964F"));
        else Debug.LogError("Orange Color Button not assigned!");

        if (yellowColorButton != null) yellowColorButton.onClick.AddListener(() => SetNoteColor("#FFDD3C"));
        else Debug.LogError("Yellow Color Button not assigned!");

        if (greenColorButton != null) greenColorButton.onClick.AddListener(() => SetNoteColor("#00BB77"));
        else Debug.LogError("Green Color Button not assigned!");

        if (blueColorButton != null) blueColorButton.onClick.AddListener(() => SetNoteColor("#A8B5E0"));
        else Debug.LogError("Blue Color Button not assigned!");

        if (purpleColorButton != null) purpleColorButton.onClick.AddListener(() => SetNoteColor("#B19CD9"));
        else Debug.LogError("Purple Color Button not assigned!");

        Debug.Log($"dataLinePrefab on OnEnable: {dataLinePrefab}");
    }

    System.Collections.IEnumerator WaitForFirebaseAndLoad()
    {
        Debug.Log("WaitForFirebaseAndLoad coroutine started.");
        while (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            Debug.Log("FirebaseManager not initialized yet, waiting...");
            yield return null;
        }

        // Safe to access Firebase now
        databaseReference = FirebaseManager.Instance.DbReference;
        Debug.Log("FirebaseManager initialized, loading note: " + noteID);
        LoadNote(noteID);
    }


    public void SetNoteColor(string hexColor)
    {
        Color newColor;
        if (ColorUtility.TryParseHtmlString(hexColor, out newColor))
        {
            // Update the color of the main sticky note immediately
            if (noteBackground != null)
            {
                noteBackground.GetComponent<Image>().color = newColor;
            }

            // Update the color of the edit panel background immediately
            Image editPanelBackgroundImage = editPanelContainer.GetComponent<Image>();
            if (editPanelBackgroundImage == null)
            {
                editPanelBackgroundImage = editPanelCanvas.GetComponentInChildren<Image>();
            }
            if (editPanelBackgroundImage != null)
            {
                editPanelBackgroundImage.color = newColor;
            }

            // Update the color in the currentNoteData (this will be saved if the user presses "Save")
            if (currentNoteData == null)
            {
                currentNoteData = new StickyNoteData { items = new List<ShoppingItem>(), colorHex = hexColor };
            }
            else
            {
                currentNoteData.colorHex = hexColor;
            }
        }
        else
        {
            Debug.LogError($"Invalid hex color string: {hexColor}");
        }
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
                    Debug.LogWarning("Note does not exist: " + noteId);
                    return;
                }
                StickyNoteData noteData = JsonUtility.FromJson<StickyNoteData>(args.Snapshot.GetRawJsonValue());

            // Only display the note if the edit panel is not currently active
            if (editPanelCanvas == null || !editPanelCanvas.activeSelf)
                {
                    DisplayNote(noteData);
                }
                else
                {
                    Debug.Log("Edit panel is active, skipping immediate note display update.");
                    currentNoteData = noteData; // Still update the cached data
                }
            };
        hasSubscribed = true;
    }

    void DisplayNote(StickyNoteData data)
    {
        Debug.Log("DisplayNote() called.");
        currentNoteData = data;
        Debug.Log($"Is itemListContainer null? {itemListContainer == null}");
        if (itemListContainer == null)
        {
            Debug.LogError("itemListContainer is null! Cannot instantiate data line.");
            return;
        }
        foreach (Transform child in itemListContainer) Destroy(child.gameObject);

        foreach (var item in data.items)
        {
            if (item.isVisible) // Only display if isVisible is true
            {
                if (dataLinePrefab == null)
                {
                    Debug.LogError("dataLinePrefab is null! Cannot instantiate.");
                    return;
                }
                Debug.Log($"Displaying item: Name='{item.name}', Quantity={item.quantity}, Done={item.done}, Visible={item.isVisible}");
                GameObject dataLineGO = Instantiate(GetComponent<StickyNoteLoader>().dataLinePrefab, itemListContainer);
                Debug.Log("GameObjectFound");
                Toggle doneCheckbox = dataLineGO.GetComponentInChildren<Toggle>();
                Debug.Log($"Found Toggle: {doneCheckbox != null}");
                Text labelText = doneCheckbox?.transform.Find("Label")?.GetComponent<Text>();
                Debug.Log($"Found Label Text: {labelText != null}");
                string displayText = $"{item.name} x {item.quantity}";

                if (labelText != null)
                {
                    labelText.text = displayText;
                    UpdateVisualState(labelText, item.done);
                }
                else Debug.LogError("Label Text not found in prefab!");

                if (doneCheckbox != null)
                {
                    doneCheckbox.isOn = item.done;
                    var currentItem = item;
                    var currentLabelText = labelText;
                    doneCheckbox.onValueChanged.AddListener((isOn) => UpdateItemDoneState(currentItem, isOn, currentLabelText));
                }
            }
            else
            {
                Debug.Log($"Skipping item: '{item.name}' as it's not visible.");
            }
        }

        Color noteBgColor = Color.green;
        if (!string.IsNullOrEmpty(data.colorHex) && ColorUtility.TryParseHtmlString(data.colorHex, out noteBgColor))
        {
            noteBackground.GetComponent<Image>().color = noteBgColor;
        }
        noteBackground.GetComponent<Image>().color = noteBgColor;
    }

    void UpdateVisualState(Text textComponent, bool done)
    {
        if (textComponent != null)
        {
            textComponent.color = done ? doneTextColor : undoneTextColor;
        }
    }

    void UpdateItemDoneState(ShoppingItem itemToUpdate, bool isDone, Text textComponentToUpdate)
    {
        FirebaseManager.Instance.DbReference.Child("stickynotes").Child(noteID).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    StickyNoteData existingData = JsonUtility.FromJson<StickyNoteData>(task.Result.GetRawJsonValue());
                    ShoppingItem itemFound = existingData.items.FirstOrDefault(item => item.name == itemToUpdate.name);
                    if (itemFound != null) itemFound.done = isDone;
                    UpdateVisualState(textComponentToUpdate, isDone);
                    databaseReference.Child("stickynotes").Child(noteID).SetRawJsonValueAsync(JsonUtility.ToJson(existingData));
                    Debug.Log($"Updated '{itemToUpdate.name}' done state to {isDone}");
                }
                else if (task.IsFaulted)
                    Debug.LogError($"Failed to get sticky note data: {task.Exception}");
            });
    }

    private void FetchNoteDataBeforeOpeningEditPanel()
    {
        Debug.Log("Fetching note data before opening edit panel...");
        FirebaseManager.Instance.DbReference.Child("stickynotes").Child(noteID).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    if (task.Result.Exists)
                    {
                        currentNoteData = JsonUtility.FromJson<StickyNoteData>(task.Result.GetRawJsonValue());
                        Debug.Log("Note data fetched successfully.");
                        OpenEditPanelInternal(); // Call the internal method to populate and show the panel
                    }
                    else
                    {
                        Debug.LogWarning("Note does not exist: " + noteID);
                        // Handle the case where the note doesn't exist
                    }
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to fetch sticky note data: {task.Exception}");
                    // Handle the error
                }
            });
    }

    public void OpenEditPanel()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("Firebase not initialized yet.  Delaying OpenEditPanel().");
            return;  // Or you could start a short coroutine to retry later
        }

        FetchNoteDataBeforeOpeningEditPanel();
    }


    private void OpenEditPanelInternal()
    {
        Debug.Log("OpenEditPanelInternal() called.");

        if (editPanelCanvas != null)
        {
            editPanelCanvas.SetActive(true);
            noteBackground.SetActive(false);

            // Store the original color
            if (noteBackground != null)
            {
                originalNoteColor = noteBackground.GetComponent<Image>().color;
            }

            // Set the Edit Panel background color to match the current sticky note background
            Image editPanelBackgroundImage = editPanelContainer.GetComponent<Image>();
            if (editPanelBackgroundImage == null)
            {
                editPanelBackgroundImage = editPanelCanvas.GetComponentInChildren<Image>();
            }

            if (noteBackground != null && editPanelBackgroundImage != null)
            {
                editPanelBackgroundImage.color = noteBackground.GetComponent<Image>().color;
                Debug.Log("Set Edit Panel background color.");
            }
            else
            {
                Debug.LogWarning("Could not find noteBackground or Edit Panel background Image.");
            }

            foreach (Transform child in editItemListContainer) Destroy(child.gameObject);

            if (currentNoteData != null && currentNoteData.items != null)
            {
                Debug.Log($"currentNoteData.items has {currentNoteData.items.Count} items in OpenEditPanelInternal.");
                for (int i = 0; i < currentNoteData.items.Count; i++)
                {
                    var item = currentNoteData.items[i];
                    Debug.Log($"[OpenEditPanelInternal] Item: {item.name}, isVisible from data: {item.isVisible}");

                    GameObject itemRowGO = Instantiate(editItemRowPrefab, editItemListContainer);
                    TMP_InputField nameInputField = itemRowGO.transform.Find("ItemRow/ItemName")?.GetComponent<TMP_InputField>();
                    TMP_InputField quantityInputField = itemRowGO.transform.Find("ItemRow/ItemQuantity")?.GetComponent<TMP_InputField>();
                    Button removeButton = itemRowGO.transform.Find("ItemRow/DeleteButton")?.GetComponent<Button>();
                    Toggle visibilityToggle = itemRowGO.GetComponentInChildren<Toggle>();
                    Image visibilityIconImage = visibilityToggle?.transform.Find("IconImage")?.GetComponent<Image>();

                    if (nameInputField != null) nameInputField.text = item.name;
                    if (quantityInputField != null) quantityInputField.text = item.quantity.ToString();

                    if (visibilityToggle != null && visibilityIconImage != null)
                    {
                        visibilityIconImage.sprite = item.isVisible ? showIcon : hideIcon;
                        visibilityToggle.isOn = item.isVisible;

                        var currentItem = item;
                        visibilityToggle.onValueChanged.RemoveAllListeners(); // Important: Remove previous listeners
                        visibilityToggle.onValueChanged.AddListener((isOn) =>
                        {
                            currentItem.isVisible = isOn;
                            visibilityIconImage.sprite = isOn ? showIcon : hideIcon;
                            Debug.Log($"[OpenEditPanelInternal] Visibility of '{currentItem.name}' set to: {isOn}");
                        });
                    }

                    if (removeButton != null)
                    {
                        var itemRowToRemove = itemRowGO;
                        removeButton.onClick.RemoveAllListeners();
                        removeButton.onClick.AddListener(() => RemoveItemRow(itemRowToRemove));
                    }
                }
            }
            else Debug.LogWarning("currentNoteData is null or has no items to display in edit panel.");
        }
        else Debug.LogError("Edit Panel Canvas is not assigned in the Inspector!");
    }

    private void RemoveItemRow(GameObject itemRowGO) => Destroy(itemRowGO);

    public void AddNewItemRow()
    {
        if (editItemRowPrefab != null && editItemListContainer != null)
        {
            GameObject newItemRowGO = Instantiate(editItemRowPrefab, editItemListContainer);
            InputField nameInputField = newItemRowGO.transform.Find("ItemRow/ItemName")?.GetComponent<InputField>();
            InputField quantityInputField = newItemRowGO.transform.Find("ItemRow/ItemQuantity")?.GetComponent<InputField>();
            Button removeButton = newItemRowGO.transform.Find("ItemRow/DeleteButton")?.GetComponent<Button>();

            if (nameInputField != null) nameInputField.text = "";
            if (quantityInputField != null) quantityInputField.text = "1";

            if (removeButton != null)
            {
                var itemRowToRemove = newItemRowGO;
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(() => RemoveItemRow(itemRowToRemove));
            }
        }
        else Debug.LogError("Edit Item Row Prefab or Container not assigned!");
    }

    public void SaveEditedItems()
    {
        if (currentNoteData == null)
        {
            Debug.LogError("No current note data to save.");
            return;
        }

        List<Transform> itemRows = new List<Transform>();
        foreach (Transform child in editItemListContainer)
        {
            if (child != null)
            {
                itemRows.Add(child);
            }
        }

        List<ShoppingItem> newItems = new List<ShoppingItem>();
        foreach (Transform row in itemRows)
        {
            TMP_InputField nameInputField = row.transform.Find("ItemRow/ItemName")?.GetComponent<TMP_InputField>();
            TMP_InputField quantityInputField = row.transform.Find("ItemRow/ItemQuantity")?.GetComponent<TMP_InputField>();
            Toggle visibilityToggle = row.GetComponentInChildren<Toggle>(); // Get the visibility toggle

            if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            {
                string itemName = nameInputField.text.Trim();
                int itemQuantity = 0;
                int.TryParse(quantityInputField?.text, out itemQuantity);

                // Try to find the existing item to preserve its isVisible state
                ShoppingItem existingItem = currentNoteData.items.FirstOrDefault(item => item.name == itemName);
                bool isCurrentlyVisible = existingItem?.isVisible ?? true; // Default to visible if new

                // Determine the isVisible state from the UI toggle
                bool isVisibleInUI = visibilityToggle?.isOn ?? true; // Default to visible if no toggle

                bool doneStatus = existingItem?.done ?? false;

                newItems.Add(new ShoppingItem { name = itemName, quantity = itemQuantity, done = doneStatus, isVisible = isVisibleInUI });
            }
            else if (nameInputField != null)
            {
                Debug.LogWarning("Skipping item row with empty name.");
            }
        }

        currentNoteData.items = newItems;
        string updatedJson = JsonUtility.ToJson(currentNoteData);

        databaseReference.Child("stickynotes").Child(noteID).SetRawJsonValueAsync(updatedJson)
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✔️ Successfully saved edited sticky note data to Firebase.");
                editPanelCanvas.SetActive(false);
                DisplayNote(currentNoteData);
                noteBackground.SetActive(true);
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"Failed to save edited sticky note data: {task.Exception}");
            }
        });
    }

    public void CancelEdit()
    {
        if (editPanelCanvas != null)
        {
            editPanelCanvas.SetActive(false);
            noteBackground.SetActive(true);
        }

        // Revert the color of the main sticky note
        if (noteBackground != null)
        {
            noteBackground.GetComponent<Image>().color = originalNoteColor;
        }

        // Revert the color of the edit panel background as well, to be consistent
        Image editPanelBackgroundImage = editPanelContainer.GetComponent<Image>();
        if (editPanelBackgroundImage == null)
        {
            editPanelBackgroundImage = editPanelCanvas.GetComponentInChildren<Image>();
        }
        if (editPanelBackgroundImage != null)
        {
            editPanelBackgroundImage.color = originalNoteColor;
        }
    }

    public void ClosePanel()
    {
        editPanelCanvas.SetActive(false);
        noteBackground.SetActive(false);
    }
}
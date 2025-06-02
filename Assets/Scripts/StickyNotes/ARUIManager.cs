using UnityEngine;

/// <summary>
/// Singleton manager for controlling AR UI visibility.
/// Ensures only one sticky note is active at a time based on AR target detection
/// and handles explicit closing via a button.
/// </summary>
public class ARUIManager : MonoBehaviour
{
    public static ARUIManager Instance { get; private set; }

    // Reference to the currently active StickyNoteLoader (if any)
    private StickyNoteLoader activeStickyNoteLoader = null;

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this manager alive across scenes
            Debug.Log("ARUIManager: Instance created.");
        }
        else
        {
            Debug.LogWarning("ARUIManager: Duplicate instance detected, destroying self.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Attempts to show a sticky note associated with a scanned AR target.
    /// A new sticky note will only be shown if no other sticky note is currently active.
    /// </summary>
    /// <param name="loaderToShow">The StickyNoteLoader instance corresponding to the scanned target.</param>
    public void ShowStickyNote(StickyNoteLoader loaderToShow)
    {
        if (loaderToShow == null)
        {
            Debug.LogError("ARUIManager: Attempted to show a null StickyNoteLoader.");
            return;
        }

        if (activeStickyNoteLoader == null)
        {
            // No sticky note is currently active, so we can show this one.
            activeStickyNoteLoader = loaderToShow;
            if (activeStickyNoteLoader.noteBackground != null)
            {
                activeStickyNoteLoader.noteBackground.SetActive(true);
                Debug.Log($"ARUIManager: Showing sticky note for ID: {activeStickyNoteLoader.noteID}");
            }
            else
            {
                Debug.LogError($"ARUIManager: noteBackground not assigned for StickyNoteLoader ID: {activeStickyNoteLoader.noteID}");
            }

            // Ensure the edit panel is hidden when the main note is shown
            if (activeStickyNoteLoader.editPanelCanvas != null)
            {
                activeStickyNoteLoader.editPanelCanvas.SetActive(false);
            }
        }
        else if (activeStickyNoteLoader == loaderToShow)
        {
            // The same sticky note is already active, do nothing.
            Debug.Log($"ARUIManager: Sticky note for ID: {loaderToShow.noteID} is already active.");
        }
        else
        {
            // Another sticky note is already active, so do not show the new one.
            Debug.LogWarning($"ARUIManager: Another sticky note (ID: {activeStickyNoteLoader.noteID}) is already active. Ignoring scan for ID: {loaderToShow.noteID}.");
        }
    }

    /// <summary>
    /// Hides the currently active sticky note. This should be called when the "Close" button is pressed.
    /// </summary>
    public void HideActiveStickyNote()
    {
        if (activeStickyNoteLoader != null)
        {
            Debug.Log($"ARUIManager: Hiding active sticky note for ID: {activeStickyNoteLoader.noteID}.");
            // Hide both the main note background and the edit panel
            if (activeStickyNoteLoader.noteBackground != null)
            {
                activeStickyNoteLoader.noteBackground.SetActive(false);
            }
            if (activeStickyNoteLoader.editPanelCanvas != null)
            {
                activeStickyNoteLoader.editPanelCanvas.SetActive(false);
            }
            activeStickyNoteLoader = null; // Clear the reference to the active sticky note
        }
        else
        {
            Debug.Log("ARUIManager: No sticky note is currently active to hide.");
        }
    }
}

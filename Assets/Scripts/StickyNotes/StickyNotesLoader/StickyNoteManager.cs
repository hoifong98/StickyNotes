using UnityEngine;

public class StickyNoteManager : MonoBehaviour
{
    public static StickyNoteManager Instance;

    private StickyNoteLoader activeNote;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanShowNewNote()
    {
        return activeNote == null;
    }

    public void RegisterActiveNote(StickyNoteLoader note)
    {
        activeNote = note;
    }

    public void UnregisterActiveNote(StickyNoteLoader note)
    {
        if (activeNote == note)
        {
            activeNote = null;
        }
    }
}

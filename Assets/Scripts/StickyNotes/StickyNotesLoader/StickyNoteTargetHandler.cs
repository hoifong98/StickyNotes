
using UnityEngine;
using Vuforia;

public class StickyNoteTargetHandler : DefaultObserverEventHandler
{
    protected override void OnTrackingFound()
    {
        base.OnTrackingFound();
        Debug.Log(" OnTrackingFound called for: " + gameObject.name);

        StickyNoteLoader loader = GetComponent<StickyNoteLoader>();
        if (loader != null && loader.IsManuallyClosed)
        {
            Debug.Log("Reloading closed note for: " + loader.noteID);
            loader.ReloadNoteIfClosed();
        }
    }
}

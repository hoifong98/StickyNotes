using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }

    private GameObject currentPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void ShowPanel(GameObject panel)
    {
        if (currentPanel != null && currentPanel != panel)
        {
            Debug.Log("Another panel is already active. Skipping.");
            return;
        }

        if (panel != null && !panel.activeSelf)
        {
            currentPanel = panel;
            panel.SetActive(true);
        }
    }

    public void HidePanel(GameObject panel)
    {
        if (currentPanel == panel && panel.activeSelf)
        {
            panel.SetActive(false);
            currentPanel = null;
        }
    }

    public bool IsAnyPanelActive()
    {
        return currentPanel != null;
    }
}
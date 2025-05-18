using UnityEngine;
using UnityEngine.UI;

public class ScrollBar : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public RectTransform viewport;
    public Scrollbar verticalScrollbar;

    void Update()
    {
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight > viewportHeight)
        {
            scrollRect.vertical = true;
            if (verticalScrollbar != null)
                verticalScrollbar.gameObject.SetActive(true);
        }
        else
        {
            scrollRect.vertical = false;
            if (verticalScrollbar != null)
                verticalScrollbar.gameObject.SetActive(false);

            scrollRect.verticalNormalizedPosition = 1;
        }
    }
}

using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple screen-space popup for one-off announcements (a noble arriving,
/// a commission completing, etc.) - fades a message in, holds it, then
/// fades it back out. Put this on a UI Canvas object alongside a
/// CanvasGroup and a TMP_Text label (both auto-found on this object/its
/// children if left unassigned). Calling Show() again while a message is
/// already displayed interrupts it and starts the new one immediately.
/// </summary>
public class NotificationBanner : MonoBehaviour
{
    public static NotificationBanner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text label;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>Shows 'message' briefly, interrupting whatever's currently showing.</summary>
    public void Show(string message)
    {
        if (canvasGroup == null || label == null)
        {
            Debug.LogWarning("[NotificationBanner] No Canvas Group or Text label assigned/found - can't show a message.");
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        label.text = message;
        activeRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
        activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
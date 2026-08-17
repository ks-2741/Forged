using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Drives a simple two-phase day/night cycle for HDRP. Swaps the Cubemap on
/// a Volume's HDRI Sky override between a day and night sky, tracks which
/// phase you're currently in, and fires events other systems (shop,
/// crafting/sell) can hook into to gate what's allowed when.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public enum Phase { Day, Night }

    [Header("HDRP Sky")]
    [Tooltip("The Volume in your scene whose profile has a Visual Environment override (Sky Type = HDRI Sky) and an HDRI Sky override. Usually your global scene Volume.")]
    [SerializeField] private Volume skyVolume;
    [SerializeField] private Cubemap daySkyTexture;
    [SerializeField] private Cubemap nightSkyTexture;

    [Header("Cycle Length (seconds)")]
    [Tooltip("How long the Day phase lasts, in seconds.")]
    public float dayDuration = 120f;
    [Tooltip("How long the Night phase lasts, in seconds.")]
    public float nightDuration = 90f;

    [Header("Testing")]
    [Tooltip("How far into the current phase we are, in seconds. Drag this in the Inspector during Play mode to fast-forward through a phase for testing.")]
    public float currentPhaseTime = 0f;
    [Tooltip("Which phase the cycle starts in when the scene loads.")]
    [SerializeField] private Phase startingPhase = Phase.Day;

    [Header("Events")]
    [Tooltip("Fires the moment Day starts (including once at Start if starting phase is Day).")]
    public UnityEvent onDayStart;
    [Tooltip("Fires the moment Night starts (including once at Start if starting phase is Night).")]
    public UnityEvent onNightStart;

    private HDRISky hdriSky;

    public Phase CurrentPhase { get; private set; }
    public bool IsDay => CurrentPhase == Phase.Day;
    public bool IsNight => CurrentPhase == Phase.Night;

    /// <summary>0-1 progress through the current phase, handy for a UI clock/sun-arc.</summary>
    public float PhaseProgress01
    {
        get
        {
            float duration = IsDay ? dayDuration : nightDuration;
            return duration <= 0f ? 0f : Mathf.Clamp01(currentPhaseTime / duration);
        }
    }

    /// <summary>Full day+night length in seconds - one clock hand rotation equals this.</summary>
    public float TotalCycleDuration => dayDuration + nightDuration;

    /// <summary>0-1 progress through the FULL day+night cycle (not just the current phase) - Day always comes first in this ordering.</summary>
    public float CycleProgress01
    {
        get
        {
            if (TotalCycleDuration <= 0f)
            {
                return 0f;
            }

            float elapsed = IsDay ? currentPhaseTime : dayDuration + currentPhaseTime;
            return Mathf.Repeat(elapsed / TotalCycleDuration, 1f);
        }
    }

    /// <summary>
    /// Fast-forwards the clock by an arbitrary number of seconds, correctly
    /// rolling over one or more phase changes if the jump is large enough
    /// (e.g. skipping a full hour near the end of a phase). Used by the
    /// clock's click-to-skip.
    /// </summary>
    public void AdvanceTime(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        currentPhaseTime += seconds;

        float phaseDuration = IsDay ? dayDuration : nightDuration;
        while (phaseDuration > 0f && currentPhaseTime >= phaseDuration)
        {
            currentPhaseTime -= phaseDuration;
            TogglePhase();
            phaseDuration = IsDay ? dayDuration : nightDuration;
        }
    }

    private void Awake()
    {
        if (skyVolume == null)
        {
            Debug.LogError("DayNightCycle: no Sky Volume assigned. Drag your scene's global Volume into the Sky Volume field.", this);
            return;
        }

        if (!skyVolume.profile.TryGet(out hdriSky))
        {
            Debug.LogError("DayNightCycle: the assigned Volume's profile has no HDRI Sky override. Add Visual Environment (Sky Type = HDRI Sky) and HDRI Sky overrides to its profile.", this);
        }
    }

    private void Start()
    {
        CurrentPhase = startingPhase;
        ApplySky();
        InvokePhaseEvent();
    }

    private void Update()
    {
        currentPhaseTime += Time.deltaTime;

        float phaseDuration = IsDay ? dayDuration : nightDuration;
        if (currentPhaseTime >= phaseDuration)
        {
            currentPhaseTime -= phaseDuration;
            TogglePhase();
        }
    }

    private void TogglePhase()
    {
        CurrentPhase = IsDay ? Phase.Night : Phase.Day;
        ApplySky();
        InvokePhaseEvent();
    }

    /// <summary>Force-set the phase immediately, resetting phase time. Useful for a debug menu or testing button.</summary>
    public void SetPhase(Phase phase)
    {
        if (CurrentPhase == phase)
        {
            return;
        }

        CurrentPhase = phase;
        currentPhaseTime = 0f;
        ApplySky();
        InvokePhaseEvent();
    }

    private void InvokePhaseEvent()
    {
        if (IsDay)
        {
            onDayStart?.Invoke();
        }
        else
        {
            onNightStart?.Invoke();
        }
    }

    private void ApplySky()
    {
        if (hdriSky == null)
        {
            return;
        }

        hdriSky.hdriSky.value = IsDay ? daySkyTexture : nightSkyTexture;
        // Forces HDRP to actually re-render the new sky/lighting immediately
        // rather than waiting for its next internal update.
        DynamicGI.UpdateEnvironment();
    }

    // Right-click the component header in the Inspector to jump phases instantly while testing.
    [ContextMenu("Force Day")]
    private void ForceDay() => SetPhase(Phase.Day);

    [ContextMenu("Force Night")]
    private void ForceNight() => SetPhase(Phase.Night);
}
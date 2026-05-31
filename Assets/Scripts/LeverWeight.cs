using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Груз для лабораторной работы с рычагом.
/// Использует SteamVR Interaction System: Hand вызывает OnAttachedToHand /
/// OnDetachedFromHand через BroadcastMessage при захвате и отпускании.
/// Требует наличия Interactable и Throwable на том же объекте.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
public class LeverWeight : MonoBehaviour
{
    [Tooltip("Масса груза в килограммах")]
    public float massKg = 0.1f;

    [Tooltip("TextMeshPro-метка с массой (необязательно)")]
    public TextMeshPro massLabel;

    private LeverSlot _currentSlot;
    private bool _isHeld;

    public bool IsBeingHeld => _isHeld;
    public bool IsSnapped    => _currentSlot != null;

    void Awake()
    {
        GetComponent<Rigidbody>().mass = massKg;
    }

    void Start()
    {
        UpdateLabel();
    }

    // ─── SteamVR SendMessage hooks (вызываются Hand.BroadcastMessage) ────────

    private void OnAttachedToHand(Hand hand)
    {
        _isHeld = true;
        // Освобождаем слот до следующего физического кадра
        _currentSlot?.ReleaseWeight();
    }

    private void OnDetachedFromHand(Hand hand)
    {
        _isHeld = false;
    }

    // ─── Вызывается LeverSlot ─────────────────────────────────────────────────

    public void OnSnapped(LeverSlot slot)  => _currentSlot = slot;
    public void OnReleased()               => _currentSlot = null;

    // ─── Приватные ───────────────────────────────────────────────────────────

    private void UpdateLabel()
    {
        if (massLabel == null) return;
        int grams = Mathf.RoundToInt(massKg * 1000f);
        massLabel.text = grams >= 1000
            ? $"{massKg:F1} кг"
            : $"{grams} г";
    }
}

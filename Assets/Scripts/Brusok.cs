using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Брусок с известной массой для лабораторной работы «КПД наклонной плоскости».
/// Управляется через SteamVR Interaction System: Hand вызывает
/// OnAttachedToHand / OnDetachedFromHand при захвате и отпускании.
///
/// Когда брусок берут рукой, он автоматически отцепляется от крючка
/// динамометра (как груз отцепляется от слота рычага — см. LeverWeight).
///
/// Требует Rigidbody + Interactable (+ Throwable добавляется при настройке сцены).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
public class Brusok : MonoBehaviour
{
    [Tooltip("Масса бруска в килограммах")]
    public float massKg = 0.2f;

    [Tooltip("TextMeshPro-метка с массой (необязательно)")]
    public TextMeshPro massLabel;

    private SpringDynamometer _attachedTo;
    private bool _isHeld;

    public bool IsBeingHeld => _isHeld;
    public bool IsAttached  => _attachedTo != null;

    void Awake()
    {
        GetComponent<Rigidbody>().mass = massKg;
    }

    void Start()
    {
        UpdateLabel();
    }

    // ─── SteamVR SendMessage hooks (вызываются Hand.BroadcastMessage) ─────────

    private void OnAttachedToHand(Hand hand)
    {
        _isHeld = true;
        // Снимаем брусок с крючка динамометра, если он был прицеплен.
        _attachedTo?.Detach();
    }

    private void OnDetachedFromHand(Hand hand)
    {
        _isHeld = false;
    }

    // ─── Вызывается динамометром ──────────────────────────────────────────────

    public void OnHooked(SpringDynamometer dyn) => _attachedTo = dyn;
    public void OnUnhooked()                    => _attachedTo = null;

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

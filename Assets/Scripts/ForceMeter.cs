using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ForceMeter : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public UnityEvent<float> onForceChanged;

    [SerializeField] private float currentForce = 0f;

    public float CurrentForce
    {
        get => currentForce;
        set
        {
            currentForce = Mathf.Max(0, value);
            if (displayText != null)
                displayText.text = $"{currentForce:F1} N";
            onForceChanged?.Invoke(currentForce);
        }
    }

    // Пример: при захвате динамометра можно менять силу,
    // но здесь проще сделать публичный метод для внешнего управления.
    public void SetForce(float force)
    {
        CurrentForce = force;
    }
}
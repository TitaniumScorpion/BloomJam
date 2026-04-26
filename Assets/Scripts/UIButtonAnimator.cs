using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    public Vector3 clickScale = new Vector3(0.9f, 0.9f, 0.9f);
    public float animationSpeed = 15f;

    [Header("Glow/Color Settings")]
    public bool useGlowEffect = true;
    [Tooltip("If checked, the script will automatically grab the button's starting color.")]
    public bool autoGrabNormalColor = true;
    public Color normalColor = Color.white;
    [Tooltip("A brighter color to simulate a glow. Pushing values above 1 can trigger Bloom on some UI setups!")]
    public Color hoverGlowColor = new Color(1.2f, 1.2f, 1.2f, 1f); 
    
    private Graphic targetGraphic;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isHovering = false;

    private void Awake()
    {
        targetScale = normalScale;
        targetGraphic = GetComponent<Graphic>();
        
        if (targetGraphic != null && autoGrabNormalColor)
        {
            normalColor = targetGraphic.color;
        }
        targetColor = normalColor;
    }

    private void OnEnable()
    {
        // Instantly reset the button's state whenever the UI panel is turned on
        transform.localScale = normalScale;
        targetScale = normalScale;
        isHovering = false;
        
        if (targetGraphic != null)
        {
            targetGraphic.color = normalColor;
            targetColor = normalColor;
        }
    }

    private void Update()
    {
        // We use 'unscaledDeltaTime' so the animation works even when Time.timeScale is 0 (like on the Death Screen)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        if (useGlowEffect && targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetScale = hoverScale;
        if (useGlowEffect) targetColor = hoverGlowColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetScale = normalScale;
        if (useGlowEffect) targetColor = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Return to the hover scale if the mouse is still over the button, otherwise return to normal
        targetScale = isHovering ? hoverScale : normalScale;
    }
}
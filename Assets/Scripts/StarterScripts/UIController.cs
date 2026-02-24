using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    private class PlayerHudWidgets
    {
        public TextMeshProUGUI NameText;
        public Slider ExhaustionSlider;
        public TextMeshProUGUI ExhaustionText;
        public Slider HitsSlider;
        public TextMeshProUGUI HitsText;
    }

    [Header("Canvases")]
    [SerializeField] private Canvas _roundCanvas;
    [SerializeField] private Canvas _levelEndCanvas;
    [SerializeField] private Canvas _pauseMenuCanvas;

    [Header("Misc")]
    [SerializeField] private TextMeshProUGUI _winText;
    [SerializeField] private TextMeshProUGUI _roundText;

    [Header("Input")]
    [SerializeField] private InputActionReference _pauseAction;

    private bool _isPaused;

    private PlayerHudWidgets _p1Hud;
    private PlayerHudWidgets _p2Hud;

    private float _p1ExhaustionVisual;
    private float _p2ExhaustionVisual;
    private float _p1HitsVisual;
    private float _p2HitsVisual;

    private void Awake()
    {
        BuildStatusHud();
    }

    private void OnEnable()
    {
        if (_pauseAction != null && _pauseAction.action != null)
        {
            _pauseAction.action.performed += OnPausePressed;
            _pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_pauseAction != null && _pauseAction.action != null)
        {
            _pauseAction.action.performed -= OnPausePressed;
            _pauseAction.action.Disable();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Players.Count < 2 || _p1Hud == null || _p2Hud == null)
            return;

        PlayerController p1 = GameManager.Instance.Players[0];
        PlayerController p2 = GameManager.Instance.Players[1];

        _p1ExhaustionVisual = Mathf.MoveTowards(_p1ExhaustionVisual, p1.Exhaustion, Time.deltaTime * 4f);
        _p2ExhaustionVisual = Mathf.MoveTowards(_p2ExhaustionVisual, p2.Exhaustion, Time.deltaTime * 4f);
        _p1HitsVisual = Mathf.MoveTowards(_p1HitsVisual, p1.HitsTaken, Time.deltaTime * 8f);
        _p2HitsVisual = Mathf.MoveTowards(_p2HitsVisual, p2.HitsTaken, Time.deltaTime * 8f);

        UpdatePlayerHud(_p1Hud, "P1", _p1ExhaustionVisual, p1.MaxExhaustion, _p1HitsVisual, p1.MaxHitsTaken);
        UpdatePlayerHud(_p2Hud, "P2", _p2ExhaustionVisual, p2.MaxExhaustion, _p2HitsVisual, p2.MaxHitsTaken);
    }

    private void BuildStatusHud()
    {
        Canvas gameplayCanvas = GameObject.Find("Gameplay Canvas") != null
            ? GameObject.Find("Gameplay Canvas").GetComponent<Canvas>()
            : null;

        if (gameplayCanvas == null)
            return;

        GameObject root = new GameObject("StatusHUD", typeof(RectTransform));
        root.transform.SetParent(gameplayCanvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _p1Hud = BuildPlayerHud(root.transform, "P1 HUD", new Vector2(20f, -20f), TextAlignmentOptions.Left,
            new Color(0.88f, 0.26f, 0.3f, 1f));

        _p2Hud = BuildPlayerHud(root.transform, "P2 HUD", new Vector2(-20f, -20f), TextAlignmentOptions.Right,
            new Color(0.25f, 0.45f, 0.9f, 1f));
    }

    private PlayerHudWidgets BuildPlayerHud(Transform parent, string name, Vector2 anchoredPos,
        TextAlignmentOptions alignment, Color accent)
    {
        PlayerHudWidgets widgets = new PlayerHudWidgets();

        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (alignment == TextAlignmentOptions.Left)
        {
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
        }
        else
        {
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
        }

        panelRect.anchoredPosition = anchoredPos;
        panelRect.sizeDelta = new Vector2(320f, 90f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.7f);

        widgets.NameText = CreateText(panel.transform, "Name", "P", alignment, new Vector2(10f, -6f), new Vector2(300f, 18f), 16);

        widgets.ExhaustionText = CreateText(panel.transform, "ExhaustionLabel", "Exhaustion", alignment,
            new Vector2(10f, -28f), new Vector2(300f, 16f), 13);
        widgets.ExhaustionSlider = CreateSlider(panel.transform, "ExhaustionSlider", new Vector2(10f, -44f), new Vector2(300f, 14f),
            new Color(0.18f, 0.18f, 0.18f, 1f), new Color(0.95f, 0.8f, 0.35f, 1f));

        widgets.HitsText = CreateText(panel.transform, "HitsLabel", "Hits", alignment,
            new Vector2(10f, -60f), new Vector2(300f, 16f), 13);
        widgets.HitsSlider = CreateSlider(panel.transform, "HitsSlider", new Vector2(10f, -76f), new Vector2(300f, 14f),
            new Color(0.18f, 0.18f, 0.18f, 1f), accent);

        return widgets;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, TextAlignmentOptions align,
        Vector2 anchoredPos, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = align;

        return tmp;
    }

    private Slider CreateSlider(Transform parent, string objectName, Vector2 anchoredPos, Vector2 size, Color background,
        Color fill)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = anchoredPos;
        sliderRect.sizeDelta = size;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = background;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = fill;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        return slider;
    }

    private void UpdatePlayerHud(PlayerHudWidgets hud, string label, float exhaustion, float maxExhaustion, float hits,
        float maxHits)
    {
        hud.NameText.text = label;

        hud.ExhaustionSlider.maxValue = maxExhaustion;
        hud.ExhaustionSlider.SetValueWithoutNotify(exhaustion);
        hud.ExhaustionText.text = "Exhaustion " + Mathf.RoundToInt(exhaustion) + "/" + Mathf.RoundToInt(maxExhaustion);

        hud.HitsSlider.maxValue = maxHits;
        hud.HitsSlider.SetValueWithoutNotify(hits);
        hud.HitsText.text = "Hits Taken " + Mathf.RoundToInt(hits) + "/" + Mathf.RoundToInt(maxHits);
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;

        Time.timeScale = _isPaused ? 0f : 1f;

        EnablePauseMenu(_isPaused);
    }

    public void EnablePauseMenu(bool enable)
    {
        _pauseMenuCanvas.enabled = enable;
    }

    public IEnumerator ShowRoundBanner(string text, float time)
    {
        _roundText.text = text;
        _roundCanvas.enabled = true;
        yield return new WaitForSecondsRealtime(time);
        _roundCanvas.enabled = false;
    }

    public void EnableWinCanvas(bool playerWon)
    {
        if (playerWon)
        {
            _winText.color = Color.green;
            _winText.text = "You won!";
        }
        else
        {
            _winText.color = Color.red;
            _winText.text = "You died!";
        }

        _levelEndCanvas.enabled = true;
    }
}

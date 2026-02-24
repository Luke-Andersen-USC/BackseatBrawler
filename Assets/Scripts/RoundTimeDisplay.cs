using System;
using UnityEngine;
using UnityEngine.UI;

public class RoundTimeDisplay : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _punchDisplayPrefab;
    [SerializeField] private GameObject _blockDisplayPrefab;
    [SerializeField] private GameObject _blockLinePrefab;
    
    [Header("References")]
    [SerializeField] private Slider _slider;

    [SerializeField] private GameObject _sliderHandle;
    [SerializeField] private Transform _displayHolder;
    [SerializeField] private PlayerController _playerController;
    
    public Transform DisplayHolder => _displayHolder;
    
    private float _blockStartValue;
    private bool _isBlocking = false;

    private void Start()
    {
        _playerController.SetRoundTimeDisplay(this);
    }

    private void OnEnable()
    {
        _playerController.OnPunchPressed += SpawnPunch;
        _playerController.OnBlockChanged += HandleBlock;
    }

    public void UpdateDisplay(float t)
    {
        _slider.SetValueWithoutNotify(t);
    }

    private void SpawnPunch()
    {
        GameObject punchDisplay = Instantiate(_punchDisplayPrefab, _displayHolder);
        punchDisplay.transform.position = _sliderHandle.transform.position;
    }

    private void HandleBlock(bool isBlock)
    {
        if (isBlock && !_isBlocking)
        {
            _blockStartValue = _slider.value;
            _isBlocking = true;
        }
        else if (!isBlock && _isBlocking)
        {
            _isBlocking = false;

            float endValue = _slider.value;

            float min = Mathf.Min(_blockStartValue, endValue);
            float max = Mathf.Max(_blockStartValue, endValue);

            RectTransform trackRect = _slider.GetComponent<RectTransform>();
            float trackWidth = trackRect.rect.width;

            // Convert slider values to local X positions
            float startX = (_blockStartValue - _slider.minValue) / (_slider.maxValue - _slider.minValue) * trackWidth;
            float endX   = (endValue - _slider.minValue) / (_slider.maxValue - _slider.minValue) * trackWidth;

            // Spawn start marker
            GameObject startMarker = Instantiate(_blockDisplayPrefab, _displayHolder);
            startMarker.SetActive(false);
            RectTransform startRect = startMarker.GetComponent<RectTransform>();
            startRect.anchoredPosition = new Vector2(startX - trackWidth * 0.5f, 0);

            // Spawn end marker
            GameObject endMarker = Instantiate(_blockDisplayPrefab, _displayHolder);
            endMarker.SetActive(false);
            RectTransform endRect = endMarker.GetComponent<RectTransform>();
            endRect.anchoredPosition = new Vector2(endX - trackWidth * 0.5f, 0);

            // Spawn line
            GameObject lineObj = Instantiate(_blockLinePrefab, _displayHolder);
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();

            float width = Mathf.Abs(endX - startX);
            float midX = (startX + endX) * 0.5f;

            lineRect.sizeDelta = new Vector2(width, lineRect.sizeDelta.y);
            lineRect.anchoredPosition = new Vector2(midX - trackWidth * 0.5f, 0);
        }
    }

    public void ClearDisplayHolder()
    {
        foreach (Transform child in _displayHolder)
        {
            Destroy(child.gameObject);
        }
    }


}

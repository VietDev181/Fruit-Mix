using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UISetting : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Panel Animation")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button closeButton;

    private IAudioService _audio;
    private bool _panelMode; // true khi được gọi từ UIStart (có open/close animation)

    public void Initialize(IAudioService audio)
    {
        _audio = audio;

        if (bgmSlider == null || sfxSlider == null)
        {
            Debug.LogError("UISetting missing reference!");
            return;
        }

        bgmSlider.value = _audio.GetBGMVolume();
        sfxSlider.value = _audio.GetSFXVolume();

        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void Awake()
    {
        _panelMode = panel != null;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (_panelMode)
        {
            panel.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
    }

    /// <summary>Mở panel setting với animation (dùng từ UIStart hoặc UIGame).</summary>
    public void Open()
    {
        if (!_panelMode) { gameObject.SetActive(true); return; }
        gameObject.SetActive(true);
        panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    /// <summary>Đóng panel setting với animation.</summary>
    public void Close()
    {
        if (!_panelMode) { gameObject.SetActive(false); return; }
        panel.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
             .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnBGMChanged(float value) => _audio?.SetBGMVolume(value);
    private void OnSFXChanged(float value) => _audio?.SetSFXVolume(value);
}

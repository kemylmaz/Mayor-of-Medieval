using MayorOfMedieval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MayorOfMedieval.UI
{
    /// <summary>
    /// The gear button's popup: music and SFX sliders. Pauses the game while open so a
    /// player fiddling with volume on a phone doesn't lose their village in the meantime.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private bool pauseWhileOpen = true;

        private bool isOpen;

        private void Start()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            AudioManager audio = AudioManager.Instance;
            if (musicSlider != null)
            {
                musicSlider.value = audio != null ? audio.MusicVolume : 0.7f;
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = audio != null ? audio.SfxVolume : 1f;
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            // Never leave the game frozen if this object dies while the panel is open.
            if (isOpen && pauseWhileOpen) Time.timeScale = 1f;
        }

        public void Open()
        {
            isOpen = true;
            if (panelRoot != null) panelRoot.SetActive(true);
            if (pauseWhileOpen) Time.timeScale = 0f;
            AudioManager.PlaySafe(Sfx.Click);
        }

        public void Close()
        {
            isOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);
            if (pauseWhileOpen) Time.timeScale = 1f;
            AudioManager.PlaySafe(Sfx.Click);
        }

        private void OnMusicChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.SetSfxVolume(value);
            AudioManager.PlaySafe(Sfx.Toggle); // audible preview of the new level
        }
    }
}

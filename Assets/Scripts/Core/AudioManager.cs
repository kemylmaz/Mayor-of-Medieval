using UnityEngine;

namespace MayorOfMedieval.Core
{
    /// <summary>Every sound the game can fire.</summary>
    public enum Sfx
    {
        Chop, Mine, Hunt, Coins, Sale, Build, Click, Toggle, Complete, Hire
    }

    /// <summary>
    /// One music bed plus a pooled set of one-shots. Volumes persist between sessions so
    /// a player who mutes the game once stays muted.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string MusicKey = "MayorOfMedieval.Volume.Music";
        private const string SfxKey = "MayorOfMedieval.Volume.Sfx";

        public static AudioManager Instance { get; private set; }

        [Header("Clips (index matches the Sfx enum)")]
        [SerializeField] private AudioClip[] sfxClips = new AudioClip[10];
        [SerializeField] private AudioClip musicLoop;

        [Header("Mix")]
        [SerializeField] private float musicBaseVolume = 0.35f;
        [Tooltip("Same sound fired twice within this window is skipped, so a worker crowd " +
                 "chopping in unison doesn't turn into a wall of noise.")]
        [SerializeField] private float sameClipCooldown = 0.06f;

        private AudioSource music;
        private AudioSource sfx;
        private readonly float[] lastPlayed = new float[10];

        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.clip = musicLoop;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            ApplyVolumes();

            if (musicLoop != null) music.Play();
        }

        public void Play(Sfx clip)
        {
            int index = (int)clip;
            if (sfxClips == null || index < 0 || index >= sfxClips.Length) return;

            AudioClip asset = sfxClips[index];
            if (asset == null || sfx == null) return;

            // Collapse duplicate hits fired in the same instant.
            if (Time.unscaledTime - lastPlayed[index] < sameClipCooldown) return;
            lastPlayed[index] = Time.unscaledTime;

            sfx.PlayOneShot(asset, SfxVolume);
        }

        /// <summary>Safe to call from anywhere — silently does nothing before the manager exists.</summary>
        public static void PlaySafe(Sfx clip)
        {
            if (Instance != null) Instance.Play(clip);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (music != null) music.volume = MusicVolume * musicBaseVolume;
        }
    }
}

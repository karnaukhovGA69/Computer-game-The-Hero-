using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THAudioManager : MonoBehaviour
    {
        private static THAudioManager _instance;
        public static THAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THAudioManager");
                    _instance = go.AddComponent<THAudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private AudioSource _musicSource;
        private List<AudioSource> _sfxSources = new List<AudioSource>();
        private const int MaxSfxSources = 8;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1f;
        public bool SoundOn { get; private set; } = true;

        private Dictionary<string, AudioClip> _musicClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;

            for (int i = 0; i < MaxSfxSources; i++)
            {
                _sfxSources.Add(gameObject.AddComponent<AudioSource>());
            }

            LoadSettings();
            LoadClips();
        }

        private void LoadClips()
        {
            // Music
            AddMusic("MainMenu", "Audio/Music/main_menu_theme");
            AddMusic("Map", "Audio/Music/map_theme");
            AddMusic("Combat", "Audio/Music/combat_theme");
            AddMusic("Base", "Audio/Music/base_theme");

            // SFX
            AddSfx("button_click", "Audio/UI/button_click");
            AddSfx("resource_collect", "Audio/SFX/resource_collect");
            AddSfx("hero_step", "Audio/SFX/hero_step");
            AddSfx("combat_attack", "Audio/SFX/combat_attack");
            AddSfx("victory", "Audio/SFX/victory");
            AddSfx("defeat", "Audio/SFX/defeat");
            AddSfx("recruit", "Audio/SFX/recruit");
            AddSfx("upgrade", "Audio/SFX/upgrade");
            AddSfx("end_turn", "Audio/SFX/end_turn");
        }

        private void AddMusic(string key, string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null) _musicClips[key] = clip;
            else Debug.LogWarning($"[THAudio] Music clip not found: {path}");
        }

        private void AddSfx(string key, string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null) _sfxClips[key] = clip;
            else Debug.LogWarning($"[THAudio] SFX clip not found: {path}");
        }

        public void PlayMusic(string sceneName)
        {
            if (_musicClips.TryGetValue(sceneName, out AudioClip clip))
            {
                if (_musicSource.clip == clip && _musicSource.isPlaying) return;
                StartCoroutine(FadeMusic(clip));
            }
        }

        private IEnumerator FadeMusic(AudioClip newClip)
        {
            float duration = 1.0f;
            float startVol = _musicSource.volume;

            if (_musicSource.isPlaying)
            {
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(startVol, 0, t / duration);
                    yield return null;
                }
            }

            _musicSource.clip = newClip;
            if (SoundOn) _musicSource.Play();

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(0, MusicVolume * MasterVolume, t / duration);
                yield return null;
            }
            _musicSource.volume = MusicVolume * MasterVolume;
        }

        public void PlaySfx(string sfxName)
        {
            if (!SoundOn) return;

            if (_sfxClips.TryGetValue(sfxName, out AudioClip clip))
            {
                var source = GetFreeSfxSource();
                if (source != null)
                {
                    source.volume = SfxVolume * MasterVolume;
                    source.PlayOneShot(clip);
                }
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            foreach (var s in _sfxSources)
            {
                if (!s.isPlaying) return s;
            }
            return _sfxSources[0]; // Reuse first if all busy
        }

        public void SetMasterVolume(float vol)
        {
            MasterVolume = vol;
            UpdateVolumes();
            SaveSettings();
        }

        public void SetMusicVolume(float vol)
        {
            MusicVolume = vol;
            UpdateVolumes();
            SaveSettings();
        }

        public void SetSfxVolume(float vol)
        {
            SfxVolume = vol;
            SaveSettings();
        }

        public void SetSoundOn(bool isOn)
        {
            SoundOn = isOn;
            if (!isOn) _musicSource.Stop();
            else if (_musicSource.clip != null) _musicSource.Play();
            SaveSettings();
        }

        private void UpdateVolumes()
        {
            _musicSource.volume = MusicVolume * MasterVolume;
        }

        private void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("TH_MasterVolume", 1f);
            MusicVolume = PlayerPrefs.GetFloat("TH_MusicVolume", 0.8f);
            SfxVolume = PlayerPrefs.GetFloat("TH_SfxVolume", 1f);
            SoundOn = PlayerPrefs.GetInt("TH_SoundOn", 1) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("TH_MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("TH_MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("TH_SfxVolume", SfxVolume);
            PlayerPrefs.SetInt("TH_SoundOn", SoundOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

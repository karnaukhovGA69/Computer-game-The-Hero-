// Управление музыкой и звуковыми эффектами
using UnityEngine;

namespace TheHero.Infrastructure
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        public void PlayMusic(string clipName) { }
        public void PlaySFX(string clipName) { }
        public void StopMusic() => _musicSource?.Stop();

        public void SetVolume(float volume)
        {
            if (_musicSource != null) _musicSource.volume = volume;
        }
    }
}

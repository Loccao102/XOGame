using UnityEngine;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Manages runtime procedural SFX playback, ambient synthwave BGM looping, and persistent mute state.
    /// </summary>
    public sealed class XOAudioManager
    {
        private const string KeySoundMuted = "XO_SoundMuted";

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;
        private bool _enableBgm;

        public bool IsMuted
        {
            get => XOStorage.GetInt(KeySoundMuted, 0) == 1;
            set
            {
                XOStorage.SetInt(KeySoundMuted, value ? 1 : 0);
                XOStorage.Save();
                ApplyMuteState();
            }
        }

        /// <summary>Initializes SFX and ambient BGM AudioSources on the host GameObject.</summary>
        public void Initialize(GameObject host, bool enableAmbientBgm)
        {
            _enableBgm = enableAmbientBgm;

            _sfxSource = host.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _bgmSource = host.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.volume = 0.25f;

            if (_enableBgm)
            {
                _bgmSource.clip = XOProceduralAssets.GetAmbientBgmLoop();
                if (!IsMuted) _bgmSource.Play();
            }

            ApplyMuteState();
        }

        /// <summary>Plays a cached procedural sound effect if audio is not muted.</summary>
        public void PlaySfx(string key)
        {
            if (IsMuted || _sfxSource == null) return;
            AudioClip clip = XOProceduralAssets.GetSound(key);
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Toggles audio mute and plays a click cue when unmuting.</summary>
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            if (!IsMuted) PlaySfx("click");
        }

        /// <summary>Applies the persisted mute flag to SFX and ambient music AudioSources.</summary>
        public void ApplyMuteState()
        {
            bool muted = IsMuted;
            if (_sfxSource != null) _sfxSource.mute = muted;
            if (_bgmSource != null)
            {
                _bgmSource.mute = muted;
                if (!muted && _enableBgm && _bgmSource.clip != null && !_bgmSource.isPlaying)
                {
                    _bgmSource.Play();
                }
            }
        }
    }
}


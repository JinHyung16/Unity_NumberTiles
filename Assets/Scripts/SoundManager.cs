using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    public class SoundManager : SceneSingleton<SoundManager>
    {
        const int SfxSourceCount = 6;
        const string SfxResourceRoot = "Sounds/";

        const string PrefKeySfxMute = "Sound.SfxMute";
        const string PrefKeySfxVolume = "Sound.SfxVolume";

        const float MinPlayInterval = 0.02f;

        static readonly Dictionary<SoundType, string> _clipPathMap = new Dictionary<SoundType, string>
        {
            { SoundType.TileClick,   "DM-CGS-32" },
            { SoundType.NumberClear, "DM-CGS-15" },
            { SoundType.RoundClear,  "DM-CGS-26" },
            { SoundType.RoundFail,   "DM-CGS-25" },
        };

        readonly Dictionary<SoundType, AudioClip> _clipDict = new Dictionary<SoundType, AudioClip>(8);
        readonly Dictionary<SoundType, float> _lastPlayTimeDict = new Dictionary<SoundType, float>(8);

        AudioSource[] _sfxSources;
        int _nextSourceIndex;

        bool _isSfxMuted;
        float _sfxVolume = 1f;

        public bool IsSfxMuted => _isSfxMuted;
        public float SfxVolume => _sfxVolume;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            LoadPrefs();
            PreloadClips();
            InitSfxSources();
        }

        void LoadPrefs()
        {
            _isSfxMuted = PlayerPrefs.GetInt(PrefKeySfxMute, 0) != 0;
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeySfxVolume, 1f));
        }

        void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKeySfxMute, _isSfxMuted ? 1 : 0);
            PlayerPrefs.SetFloat(PrefKeySfxVolume, _sfxVolume);
            PlayerPrefs.Save();
        }

        void PreloadClips()
        {
            foreach (var kv in _clipPathMap)
            {
                var clip = Resources.Load<AudioClip>(SfxResourceRoot + kv.Value);
                if (clip == null)
                {
                    Debug.LogWarning($"[SoundManager] AudioClip 로드 실패: {SfxResourceRoot + kv.Value}");
                    continue;
                }
                _clipDict[kv.Key] = clip;
            }
        }

        void InitSfxSources()
        {
            _sfxSources = new AudioSource[SfxSourceCount];
            for (int i = 0; i < SfxSourceCount; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                source.bypassEffects = true;
                source.bypassListenerEffects = true;
                source.bypassReverbZones = true;
                source.priority = 0;
                source.reverbZoneMix = 0f;
                source.volume = 1f;
                _sfxSources[i] = source;
            }
        }

        public void PlaySfx(SoundType type)
        {
            if (_isSfxMuted)
                return;

            if (type == SoundType.None)
                return;

            if (_clipDict.TryGetValue(type, out var clip) == false || clip == null)
                return;

            float now = Time.unscaledTime;
            if (_lastPlayTimeDict.TryGetValue(type, out float last) && now - last < MinPlayInterval)
                return;
            _lastPlayTimeDict[type] = now;

            var source = _sfxSources[_nextSourceIndex];
            _nextSourceIndex = (_nextSourceIndex + 1) % SfxSourceCount;
            source.PlayOneShot(clip, _sfxVolume);
        }

        public void SetSfxMuted(bool muted)
        {
            if (_isSfxMuted == muted)
                return;
            _isSfxMuted = muted;
            SavePrefs();
        }

        public void SetSfxVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_sfxVolume, clamped))
                return;
            _sfxVolume = clamped;
            SavePrefs();
        }

        public void ToggleSfxMuted()
        {
            SetSfxMuted(!_isSfxMuted);
        }
    }
}

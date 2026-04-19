using System;
using System.Collections.Generic;
using UnityEngine;

namespace TSF
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AnimationAudioEventPlayer : MonoBehaviour
    {
        [Serializable]
        private class SoundEvent
        {
            [SerializeField] private string id;
            [SerializeField] private AudioClip[] clips;
            [SerializeField, Range(0f, 1f)] private float volume = 1f;
            [SerializeField] private Vector2 pitchRange = Vector2.one;
            [SerializeField, Min(0f)] private float cooldown = 0f;

            private float _lastPlayTime = -999f;

            public string Id => id;

            public bool CanPlay => clips != null && clips.Length > 0 && Time.time >= _lastPlayTime + cooldown;

            public bool TryGetPlayback(float volumeMultiplier, out AudioClip clip, out float playbackVolume, out float playbackPitch)
            {
                clip = null;
                playbackVolume = 0f;
                playbackPitch = 1f;

                if (!CanPlay)
                    return false;

                clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (clip == null)
                    return false;

                playbackVolume = volume * volumeMultiplier;
                playbackPitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
                _lastPlayTime = Time.time;
                return true;
            }

            public void ClampValues()
            {
                if (pitchRange == Vector2.zero)
                    pitchRange = Vector2.one;

                if (pitchRange.x <= 0f)
                    pitchRange.x = 1f;

                if (pitchRange.y <= 0f)
                    pitchRange.y = 1f;

                if (pitchRange.x > pitchRange.y)
                    pitchRange = new Vector2(pitchRange.y, pitchRange.x);
            }
        }

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool warnWhenMissingEvent = true;
        [SerializeField] private List<SoundEvent> soundEvents = new();

        private readonly Dictionary<string, SoundEvent> _eventsById = new();
        private readonly List<AudioSource> _pooledSources = new();
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            foreach (SoundEvent soundEvent in soundEvents)
                soundEvent?.ClampValues();
        }

        public void PlaySound(string eventId)
        {
            PlaySound(eventId, 1f);
        }

        public void PlaySoundEvent(AnimationEvent animationEvent)
        {
            if (animationEvent == null)
                return;

            float volumeMultiplier = animationEvent.floatParameter > 0f ? animationEvent.floatParameter : 1f;
            PlaySound(animationEvent.stringParameter, volumeMultiplier);
        }

        public void PlaySoundWithVolume(string eventId, float volumeMultiplier)
        {
            PlaySound(eventId, volumeMultiplier);
        }

        private void PlaySound(string eventId, float volumeMultiplier)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return;

            if (!_initialized)
                Initialize();

            if (_eventsById.TryGetValue(eventId, out SoundEvent soundEvent))
            {
                if (soundEvent.TryGetPlayback(Mathf.Max(0f, volumeMultiplier), out AudioClip clip, out float playbackVolume, out float playbackPitch))
                    PlayClip(clip, playbackVolume, playbackPitch);

                return;
            }

            if (warnWhenMissingEvent)
                Debug.LogWarning($"Animation audio event '{eventId}' was not found on {name}.", this);
        }

        private void Initialize()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (!_pooledSources.Contains(audioSource))
                _pooledSources.Add(audioSource);

            _eventsById.Clear();
            _initialized = true;

            foreach (SoundEvent soundEvent in soundEvents)
            {
                if (soundEvent == null || string.IsNullOrWhiteSpace(soundEvent.Id))
                    continue;

                _eventsById[soundEvent.Id] = soundEvent;
            }
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            AudioSource source = GetAvailableSource();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        private AudioSource GetAvailableSource()
        {
            foreach (AudioSource source in _pooledSources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            CopyAudioSettings(audioSource, newSource);
            _pooledSources.Add(newSource);
            return newSource;
        }

        private static void CopyAudioSettings(AudioSource source, AudioSource target)
        {
            target.outputAudioMixerGroup = source.outputAudioMixerGroup;
            target.playOnAwake = false;
            target.loop = false;
            target.priority = source.priority;
            target.spatialBlend = source.spatialBlend;
            target.reverbZoneMix = source.reverbZoneMix;
            target.dopplerLevel = source.dopplerLevel;
            target.rolloffMode = source.rolloffMode;
            target.minDistance = source.minDistance;
            target.maxDistance = source.maxDistance;
        }
    }
}

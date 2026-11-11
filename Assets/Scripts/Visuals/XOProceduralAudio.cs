using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Procedural sine-wave AudioClip synthesizer for 18 SFX effects and a 6.4s ambient Synthwave loop (0 KB on disk).
    /// </summary>
    internal static class XOProceduralAudio
    {
        private static readonly Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

        internal static AudioClip GetSound(string key)
        {
            if (_audioClips.TryGetValue(key, out AudioClip clip) && clip != null)
                return clip;

            clip = key switch
            {
                "place_x"   => CreateToneSequence("place_x", new float[] { 520f, 680f }, 0.075f, 0.25f),
                "place_o"   => CreateToneSequence("place_o", new float[] { 440f, 560f }, 0.075f, 0.25f),
                "click"     => CreateToneSequence("click", new float[] { 600f }, 0.04f, 0.18f),
                "undo"      => CreateToneSequence("undo", new float[] { 587f, 440f }, 0.09f, 0.25f),
                "hint"      => CreateToneSequence("hint", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.08f, 0.28f),
                "win"       => CreateToneSequence("win", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.11f, 0.32f),
                "lose"      => CreateToneSequence("lose", new float[] { 392f, 349.23f, 311.13f }, 0.14f, 0.28f),
                "reward"    => CreateToneSequence("reward", new float[] { 587.33f, 783.99f, 987.77f, 1174.66f }, 0.09f, 0.30f),
                "bomb"      => CreateToneSequence("bomb", new float[] { 240f, 160f, 95f }, 0.09f, 0.38f),
                "freeze"    => CreateToneSequence("freeze", new float[] { 880f, 1174.66f, 1567.98f }, 0.07f, 0.28f),
                "swap"      => CreateToneSequence("swap", new float[] { 440f, 659.25f, 880f, 1174.66f }, 0.065f, 0.30f),
                "lightning" => CreateToneSequence("lightning", new float[] { 783.99f, 1046.5f, 1567.98f }, 0.055f, 0.32f),
                "callout"   => CreateToneSequence("callout", new float[] { 659.25f, 880f, 1046.5f }, 0.07f, 0.28f),
                "bot_chat"  => CreateToneSequence("bot_chat", new float[] { 480f, 620f }, 0.045f, 0.20f),
                "chest"     => CreateToneSequence("chest", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1567.98f }, 0.09f, 0.34f),
                "gobble"    => CreateToneSequence("gobble", new float[] { 349.23f, 523.25f, 698.46f }, 0.065f, 0.32f),
                "spin_tick" => CreateToneSequence("spin_tick", new float[] { 820f }, 0.028f, 0.22f),
                "jackpot"   => CreateToneSequence("jackpot", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1567.98f, 2093f }, 0.095f, 0.35f),
                _           => CreateToneSequence("default", new float[] { 500f }, 0.05f, 0.2f)
            };

            _audioClips[key] = clip;
            return clip;
        }

        /// <summary>
        /// Generates a seamless 6.4-second chill Synthwave / Lo-Fi Pentatonic Ambient BGM Loop in RAM.
        /// </summary>
        internal static AudioClip GetAmbientBgmLoop()
        {
            if (_audioClips.TryGetValue("bgm_loop", out AudioClip cached) && cached != null)
                return cached;

            int sampleRate = 22050;
            float[] melodyNotes =
            {
                261.63f, 329.63f, 392.00f, 440.00f,
                392.00f, 329.63f, 293.66f, 329.63f,
                261.63f, 329.63f, 392.00f, 523.25f,
                440.00f, 392.00f, 329.63f, 293.66f
            };
            float[] bassRoots = { 130.81f, 110.00f, 146.83f, 98.00f };

            float stepDuration = 0.40f;
            int samplesPerStep = Mathf.RoundToInt(sampleRate * stepDuration);
            int totalSamples = samplesPerStep * melodyNotes.Length;
            float[] data = new float[totalSamples];

            for (int step = 0; step < melodyNotes.Length; step++)
            {
                float melFreq = melodyNotes[step];
                float bassFreq = bassRoots[(step / 4) % bassRoots.Length];
                int startSample = step * samplesPerStep;

                for (int i = 0; i < samplesPerStep; i++)
                {
                    float tGlobal = (startSample + i) / (float)sampleRate;
                    float progress = i / (float)samplesPerStep;

                    float melEnv = Mathf.Clamp01(progress * 10f) * Mathf.Exp(-3.2f * progress);
                    float melWave = Mathf.Sin(2f * Mathf.PI * melFreq * tGlobal) + 0.3f * Mathf.Sin(4f * Mathf.PI * melFreq * tGlobal);
                    float bassWave = Mathf.Sin(2f * Mathf.PI * bassFreq * tGlobal) * 0.55f;
                    data[startSample + i] = (melWave * melEnv * 0.055f) + (bassWave * 0.035f);
                }
            }

            AudioClip bgm = AudioClip.Create("XO_AmbientBGM", totalSamples, 1, sampleRate, false);
            bgm.SetData(data, 0);
            _audioClips["bgm_loop"] = bgm;
            return bgm;
        }

        private static AudioClip CreateToneSequence(string name, float[] frequencies, float noteDuration, float volume)
        {
            int sampleRate = 22050;
            int samplesPerNote = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteDuration));
            int totalSamples = samplesPerNote * frequencies.Length;
            float[] data = new float[totalSamples];

            for (int n = 0; n < frequencies.Length; n++)
            {
                float freq = frequencies[n];
                int startSample = n * samplesPerNote;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesPerNote;
                    float env = Mathf.Clamp01(progress * 14f) * Mathf.Pow(1f - progress, 1.5f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                    data[startSample + i] = wave * env * volume;
                }
            }

            AudioClip clip = AudioClip.Create("XO_" + name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}


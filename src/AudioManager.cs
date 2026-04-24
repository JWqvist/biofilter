using System;
using Godot;

namespace BioFilter;

/// <summary>
/// Manages all synthesized game audio.
/// Sounds are generated procedurally as float sample arrays and streamed via
/// AudioStreamGenerator — no external audio files needed.
/// Wire signals from TowerManager, ParticleManager, and WaveManager in Main.cs.
/// </summary>
public partial class AudioManager : Node
{
    private const int SampleRate = 22050;

    // One streaming player per event so concurrent sounds don't cut each other off
    private StreamingPlayer _placementPlayer = null!;
    private StreamingPlayer _killPlayer      = null!;
    private StreamingPlayer _waveStartPlayer = null!;
    private StreamingPlayer _waveEndPlayer   = null!;

    // ── Inner helper ──────────────────────────────────────────────────────────

    private sealed partial class StreamingPlayer : Node
    {
        private AudioStreamPlayer _player = null!;
        private float[] _samples = Array.Empty<float>();
        private int _sampleIndex = -1;
        private AudioStreamGeneratorPlayback? _playback;

        public static StreamingPlayer Create(float[] samples, float volumeDb = -10f)
        {
            var sp = new StreamingPlayer();
            sp._samples = samples;

            var gen = new AudioStreamGenerator();
            gen.MixRate   = SampleRate;
            gen.BufferLength = 0.1f; // 100ms buffer

            sp._player = new AudioStreamPlayer();
            sp._player.Stream   = gen;
            sp._player.VolumeDb = volumeDb;
            sp.AddChild(sp._player);
            return sp;
        }

        /// <summary>Queue playback from the beginning of the sample buffer.</summary>
        public void Trigger()
        {
            _sampleIndex = 0;
            _player.Play();
            _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }

        public override void _Process(double _delta)
        {
            if (_sampleIndex < 0 || _playback == null) return;

            int available = _playback.GetFramesAvailable();
            while (available-- > 0 && _sampleIndex < _samples.Length)
            {
                float v = _samples[_sampleIndex++];
                _playback.PushFrame(new Vector2(v, v)); // mono → stereo
            }

            if (_sampleIndex >= _samples.Length)
            {
                _player.Stop();
                _sampleIndex = -1;
                _playback    = null;
            }
        }
    }

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        _placementPlayer = StreamingPlayer.Create(GenerateBeep(880f, 0.08f, 0.35f), -8f);
        _killPlayer      = StreamingPlayer.Create(GenerateBeep(440f, 0.06f, 0.25f), -14f);
        _waveStartPlayer = StreamingPlayer.Create(GenerateChime(new[] { 523f, 659f, 784f }, 0.12f), -6f);
        _waveEndPlayer   = StreamingPlayer.Create(GenerateChime(new[] { 784f, 659f, 523f }, 0.15f), -5f);

        AddChild(_placementPlayer);
        AddChild(_killPlayer);
        AddChild(_waveStartPlayer);
        AddChild(_waveEndPlayer);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayTowerPlaced()    => _placementPlayer.Trigger();
    public void PlayParticleKilled() => _killPlayer.Trigger();
    public void PlayWaveStarted()    => _waveStartPlayer.Trigger();
    public void PlayWaveComplete()   => _waveEndPlayer.Trigger();

    // ── Sample generators ─────────────────────────────────────────────────────

    private static float[] GenerateBeep(float frequency, float duration, float amplitude = 0.5f)
    {
        int count = (int)(SampleRate * duration);
        float[] buf = new float[count];
        float fadeIn  = 0.005f;
        float fadeOut = 0.02f;
        for (int i = 0; i < count; i++)
        {
            float t  = (float)i / SampleRate;
            float env = MathF.Min(MathF.Min(t / fadeIn, 1f), MathF.Min((duration - t) / fadeOut, 1f));
            buf[i] = MathF.Sin(t * frequency * MathF.PI * 2f) * env * amplitude;
        }
        return buf;
    }

    private static float[] GenerateChime(float[] frequencies, float noteDuration, float amplitude = 0.4f)
    {
        int noteSamples = (int)(SampleRate * noteDuration);
        float[] buf = new float[noteSamples * frequencies.Length];
        float fadeIn  = 0.005f;
        float fadeOut = 0.02f;
        for (int n = 0; n < frequencies.Length; n++)
        {
            float freq = frequencies[n];
            for (int i = 0; i < noteSamples; i++)
            {
                float t  = (float)i / SampleRate;
                float env = MathF.Min(MathF.Min(t / fadeIn, 1f), MathF.Min((noteDuration - t) / fadeOut, 1f));
                buf[n * noteSamples + i] = MathF.Sin(t * freq * MathF.PI * 2f) * env * amplitude;
            }
        }
        return buf;
    }
}

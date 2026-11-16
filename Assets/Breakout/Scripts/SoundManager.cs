using UnityEngine;

/// <summary>
/// Efeitos sonoros gerados por código (estilo retrô), sem precisar de arquivos de áudio.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 0.45f;

    const int SampleRate = 44100;

    AudioSource source;
    AudioClip paddleClip, wallClip, brickClip, brickHardClip, launchClip;
    AudioClip loseClip, powerUpClip, levelClip, gameOverClip;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        paddleClip    = Sweep("Paddle", 520f, 420f, 0.07f);
        wallClip      = Sweep("Wall", 300f, 260f, 0.05f);
        brickClip     = Sweep("Brick", 880f, 1100f, 0.06f);
        brickHardClip = Sweep("BrickHard", 330f, 300f, 0.08f);
        launchClip    = Sweep("Launch", 400f, 800f, 0.10f);
        loseClip      = Sweep("Lose", 500f, 90f, 0.50f);
        powerUpClip   = Notes("PowerUp", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.06f);
        levelClip     = Notes("Level", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f }, 0.10f);
        gameOverClip  = Notes("GameOver", new[] { 392f, 329.63f, 261.63f, 196f }, 0.18f);
    }

    public void PlayPaddle()        => Play(paddleClip, 0.05f);
    public void PlayWall()          => Play(wallClip, 0.05f);
    public void PlayBrick()         => Play(brickClip, 0.08f);
    public void PlayBrickHard()     => Play(brickHardClip, 0.05f);
    public void PlayLaunch()        => Play(launchClip);
    public void PlayLose()          => Play(loseClip);
    public void PlayPowerUp()       => Play(powerUpClip);
    public void PlayLevelComplete() => Play(levelClip);
    public void PlayGameOver()      => Play(gameOverClip);

    void Play(AudioClip clip, float pitchVariation = 0f)
    {
        if (clip == null || source == null) return;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }

    /// <summary>Tom com frequência variando de f0 até f1 (mistura de onda senoidal e quadrada).</summary>
    static AudioClip Sweep(string name, float f0, float f1, float duration)
    {
        int n = Mathf.CeilToInt(SampleRate * duration);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            data[i] = Wave(phase) * Envelope(i, n);
        }
        var clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Sequência de notas curtas (arpejo).</summary>
    static AudioClip Notes(string name, float[] freqs, float noteDuration)
    {
        int perNote = Mathf.CeilToInt(SampleRate * noteDuration);
        var data = new float[perNote * freqs.Length];
        for (int k = 0; k < freqs.Length; k++)
        {
            float phase = 0f;
            for (int i = 0; i < perNote; i++)
            {
                phase += 2f * Mathf.PI * freqs[k] / SampleRate;
                data[k * perNote + i] = Wave(phase) * Envelope(i, perNote);
            }
        }
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Wave(float phase)
    {
        float sine = Mathf.Sin(phase);
        float square = sine >= 0f ? 1f : -1f;
        return Mathf.Lerp(sine, square, 0.35f) * 0.5f;
    }

    static float Envelope(int i, int n)
    {
        float attack = Mathf.Min(1f, i / (SampleRate * 0.004f));
        float release = 1f - (float)i / n;
        return attack * release;
    }
}

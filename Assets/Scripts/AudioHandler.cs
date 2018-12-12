using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioHandler : Singleton<AudioHandler>
{
    public enum Size
    {
        TWOx01  = 2,
        TWOx02  = 4,
        TWOx03  = 8,
        TWOx04  = 16,
        TWOx05  = 32,
        TWOx06  = 64,
        TWOx07  = 128,
        TWOx08  = 256,
        TWOx09  = 512,
        TWOx10  = 1024,
        TWOx11  = 2048,
        TWOx12  = 4096
    }

    struct ClipData
    {
        public AudioClip Clip;
        public string ID;
    }

    [SerializeField] Size m_size;
    [SerializeField] Size m_frequencyBandSize;
    [SerializeField] [Range(0.0f, 1.0f)] float m_bufferPercentSpeed = 0.5f;

    public AudioSource SoundSource { get; private set; }
    public float MaxVolume { get { return m_maxVolume; } set { m_maxVolume = Mathf.Clamp01(value); } }
    public Size SampleSize
    {
        get { return m_size; }
        set
        {
            m_size = value;
            m_samples = new float[(int)SampleSize];
        }
    }
    public Size FrequencyBandSize
    {
        get { return m_frequencyBandSize; }
        set
        {
            m_frequencyBandSize = value;
            m_frequencyBands = new float[(int)FrequencyBandSize];
            m_bandBuffer = new float[(int)FrequencyBandSize];
            m_bufferGravity = new float[(int)FrequencyBandSize];
            m_audioBand = new float[(int)FrequencyBandSize];
            m_audioBandBuffer = new float[(int)FrequencyBandSize];
            m_highestFrequency = new float[(int)FrequencyBandSize];
        }
    }

    public float[] m_audioBand;
    public float[] m_audioBandBuffer;

    float[] m_samples;
    float[] m_frequencyBands;
    float[] m_bandBuffer;
    float[] m_bufferGravity;
    float[] m_highestFrequency;

    public delegate void BeatTrigger();
    public BeatTrigger BeatNotifier;
    
    List<string> m_validExtensions = new List<string> { ".ogg", ".wav" };
    List<ClipData> m_clipData;
    float m_maxVolume;

    private void Awake()
    {
        SampleSize = m_size;
        FrequencyBandSize = m_frequencyBandSize;
        m_clipData = new List<ClipData>();
        SoundSource = GetComponent<AudioSource>();
        MaxVolume = 1.0f;
    }

    private void Start()
    {
        LoadAudioClips("C:/Users/colin/Music");
        //LoadAudioClip("", "Music");
    }

    private void Update()
    {
        if (!SoundSource.isPlaying)
            return;

        GetSpectrumData();
        MakeFrequencyBands();
        CreateBandBuffer();
        CreateAudioBands();
    }
    
    void GetSpectrumData()
    {
        SoundSource.GetSpectrumData(m_samples, 0, FFTWindow.BlackmanHarris);
    }

    void MakeFrequencyBands()
    {
        /*
         * 0 - 60 hertz
         * 60 - 250 hertz
         * 0 - 60 hertz
         * 0 - 60 hertz
         * 0 - 60 hertz
         * 0 - 60 hertz
         */

        /*
         * 0 - 2
         * 1
         * 2
         * 3
         * 4
         * 5
         * 6
         * 7
         * 8
         * 9
         * 10
         * 11
         * 12
         * 13
         * 14
         * 15 - 256
         * 
         */

        float scaleFactor = (int)FrequencyBandSize / 8.0f;
        float sampleFactor = (int)SampleSize / 512.0f;
        for (int i = 0; i < m_frequencyBands.Length; i++)
        {
            int sampleCount = (int)(Mathf.Pow(2, (i / scaleFactor)) * 2.0f * sampleFactor);
            float average = 0.0f;
            for (int j = 0; j < sampleCount; j++)
            {
                average += m_samples[j] * (j + 1);
            }

            average /= sampleCount;
            m_frequencyBands[i] = average * 10.0f;
        }
    }

    void CreateBandBuffer()
    {
        float delta = Time.deltaTime;
        for (int i = 0; i < m_frequencyBands.Length; i++)
        {
            if (m_bandBuffer[i] < m_frequencyBands[i])
            {
                m_bandBuffer[i] = m_frequencyBands[i];
                m_bufferGravity[i] = m_bandBuffer[i] * 0.01f;
            }
            else if (m_bandBuffer[i] > m_frequencyBands[i])
            {
                m_bandBuffer[i] -= m_bufferGravity[i] * delta;
                m_bufferGravity[i] += m_bandBuffer[i] * m_bufferPercentSpeed;
                if (m_bandBuffer[i] < 0.01f)
                    m_bandBuffer[i] = 0.01f;
            }
        }
    }

    void CreateAudioBands()
    {
        for (int i = 0; i < m_frequencyBands.Length; i++)
        {
            if (m_frequencyBands[i] > m_highestFrequency[i])
            {
                m_highestFrequency[i] = m_frequencyBands[i];
            }
            if (m_highestFrequency[i] == 0.0f)
                m_highestFrequency[i] = 0.01f;
            m_audioBand[i] = (m_frequencyBands[i] / m_highestFrequency[i]);
            m_audioBandBuffer[i] = (m_bandBuffer[i] / m_highestFrequency[i]);
        }
    }

    public void LoadAudioClips(string directory)
    {
        var info = new DirectoryInfo(directory);
        var soundFiles = info.GetFiles()
            .Where(f => IsValidMusicType(f.Name))
            .ToArray();

        foreach (var s in soundFiles)
        {
            string name = s.Name.Remove(s.Name.LastIndexOf('.'));
            LoadAudioClip(s.FullName, name);
        }
    }

    public bool IsValidMusicType(string fileName)
    {
        return m_validExtensions.Contains(Path.GetExtension(fileName));
    }

    public void LoadAudioClip(string path, string id)
    {
        StartCoroutine(LoadAudio(path, id));
    }

    IEnumerator LoadAudio(string path, string id)
    {
        WWW www = new WWW(path);

        AudioClip clip = www.GetAudioClip(false);
        while (clip.loadState != AudioDataLoadState.Loaded)
            yield return www;

        clip.name = Path.GetFileName(path);
        m_clipData.Add(new ClipData() { Clip = clip, ID = id });
        if (m_clipData.Count == 1)
            PlayAudioClip(id, 0.5f);
    }

    public void PlayAudioClip(string id, float fadeTime = 0.0f)
    {
        AudioClip clip = null;
        foreach (ClipData data in m_clipData)
        {
            if (data.ID == id)
            {
                clip = data.Clip;
                break;
            }
        }

        StartCoroutine(SwitchSong(clip, fadeTime));
    }

    IEnumerator SwitchSong(AudioClip clip, float fadeTime)
    {
        float time = fadeTime / 2.0f;
        FadeVolume(false, time);

        yield return new WaitForSeconds(time);

        SoundSource.clip = clip;
        SoundSource.Play();
        FadeVolume(true, time);
    }

    public void FadeVolume(bool fadeIn, float time, float maxVolume = -1.0f)
    {
        if (maxVolume >= 0.0f && maxVolume <= 1.0f)
            MaxVolume = maxVolume;
        if (fadeIn)
            SoundSource.volume = 0.0f;
        else
            SoundSource.volume = MaxVolume;

        StartCoroutine(Fade(fadeIn, time));
    }

    IEnumerator Fade(bool fadeIn, float time)
    {
        float speed = MaxVolume / time;
        if (fadeIn)
        {
            for (float i = 0.0f; i < MaxVolume; i += Time.deltaTime * speed)
            {
                SoundSource.volume = i;
                yield return null;
            }
            SoundSource.volume = MaxVolume;
        }
        else
        {
            for (float i = MaxVolume; i > 0.0f; i -= Time.deltaTime * speed)
            {
                SoundSource.volume = i;
                yield return null;
            }
            SoundSource.volume = 0.0f;
        }
    }
}

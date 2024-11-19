using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class ThinkOutLoudManager : MonoBehaviour
{
    private AudioClip audioClip;
    private bool isRecording = false;
    private const int samplingRate = 44100;
    private const float silenceThreshold = 0.02f; // Adjust based on environment
     
    private float silenceTimer = 0f;
    private bool savedLocally = false;
    private bool uploadedToServer = false;

    public GameObject promptText; 


    [Header("Recording Settings")]

    [Tooltip("Start recording as soon as the game starts. If false, you can call StartRecording() manually with a unityEvent")]
    [SerializeField] bool startRecordingAtGameStart = true; // Start recording as soon as the game starts


    [SerializeField] float silenceDuration = 0.5f; // Duration of silence before prompting user to speak
    
    [Tooltip("Stop recording when the game ends. If false, you can call StopRecordingAndSave() manually with a unityEvent")]
    [SerializeField] bool stopRecordingAtGameEnd = true; // Stop recording when the game ends


    [Tooltip("Save the audio file locally")]
    [SerializeField] bool saveLocally = true; // Save the audio file locally


    [Tooltip("Upload the audio file to a server")]
    [SerializeField] bool uploadToServer = true; // Upload the audio file to a server

    

    void Start()
    {
        if (startRecordingAtGameStart)
        {
            StartRecording();
        }
        // Initially hide the prompt text
        promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isRecording)
        {
            CheckSilence();
        }
    }

    // Start recording audio from the microphone
    public void StartRecording()
    {
        if (isRecording) return;

        audioClip = Microphone.Start(null, true, 60, samplingRate);
        isRecording = true;
        silenceTimer = 0f;
        Debug.Log("Recording started.");
    }

    // Stop recording, save audio to file, and reset state
    public bool StopRecordingAndSave()
    {
        Microphone.End(null);
        isRecording = false;
        promptText.gameObject.SetActive(false);
        Debug.Log("Recording stopped. Saving to file...");

        // Save the audio to a file
        string path = Path.Combine(Application.persistentDataPath, "recording.wav");
        if (saveLocally)
        {
            SaveAudioToFile(audioClip);
        }

        if (uploadToServer) {
            GetComponent<AudioUpload>().UploadAudioFile(path);
        }

        return true;
    }

    // Check if there's silence, and prompt user if necessary
    private void CheckSilence()
    {
        // Read a small amount of audio data to calculate average volume
        float[] samples = new float[128];
        audioClip.GetData(samples, Microphone.GetPosition(null) - samples.Length);

        float averageVolume = 0f;
        foreach (float sample in samples)
        {
            averageVolume += Mathf.Abs(sample);
        }
        averageVolume /= samples.Length;

        // If average volume is below threshold, consider it silence
        if (averageVolume < silenceThreshold)
        {
            silenceTimer += Time.deltaTime;

            // Trigger prompt if silence exceeds duration
            if (silenceTimer >= silenceDuration)
            {
                PromptUserToSpeak();
            }
        }
        else
        {
            // Reset timer if user is speaking
            silenceTimer = 0f;
            promptText.gameObject.SetActive(false);
        }
    }


    void OnApplicationPause(bool pauseStatus)
    {
        if (!stopRecordingAtGameEnd) return;
        if (!pauseStatus) return;
        StopRecordingAndSave();
    }

    private void PromptUserToSpeak()
    {
        
        promptText.gameObject.SetActive(true);
        Debug.Log("User has been silent for too long. Prompting to speak.");
    }

    private void SaveAudioToFile(AudioClip clip)
    {
        string path = Path.Combine(Application.persistentDataPath, "recording.wav");
        byte[] wavFile = ConvertAudioClipToWav(clip);

        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            fileStream.Write(wavFile, 0, wavFile.Length);
        }

        Debug.Log("Audio saved to: " + path);

        savedLocally = true;
    }

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = new byte[samples.Length * 2];
        int sampleIndex = 0;
        foreach (float sample in samples)
        {
            short intSample = (short)(sample * short.MaxValue);
            pcmData[sampleIndex++] = (byte)(intSample & 0xFF);
            pcmData[sampleIndex++] = (byte)((intSample >> 8) & 0xFF);
        }

        int fileSize = 44 + pcmData.Length;

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write("RIFF".ToCharArray());                      // Chunk ID
            writer.Write(fileSize - 8);                              // Chunk Size
            writer.Write("WAVE".ToCharArray());                      // Format
            writer.Write("fmt ".ToCharArray());                      // Subchunk1 ID
            writer.Write(16);                                        // Subchunk1 Size (PCM)
            writer.Write((short)1);                                  // Audio Format (1 for PCM)
            writer.Write((short)clip.channels);                      // Number of Channels
            writer.Write(clip.frequency);                            // Sample Rate
            writer.Write(clip.frequency * clip.channels * 2);        // Byte Rate
            writer.Write((short)(clip.channels * 2));                // Block Align
            writer.Write((short)16);                                 // Bits Per Sample
            writer.Write("data".ToCharArray());                      // Subchunk2 ID
            writer.Write(pcmData.Length);                            // Subchunk2 Size
            writer.Write(pcmData);                                   // PCM Data

            return stream.ToArray();
        }
    }
}

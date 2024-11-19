using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

public class AudioUpload : MonoBehaviour
{

    
    [SerializeField] private string port;

    [SerializeField] private string serverIP;


    public void UploadAudioFile(string filePath)
    {
        StartCoroutine(UploadFileCoroutine(filePath));
    }

    private IEnumerator UploadFileCoroutine(string filePath)
    {
        string serverUrl = "http://" + serverIP + ":" + port + "/upload"; // Use your local IP address here
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("File uploaded successfully.");
            }
            else
            {
                Debug.LogError("File upload failed: " + www.error);
            }
        }
    }
}

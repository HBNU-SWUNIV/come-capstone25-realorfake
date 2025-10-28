using UnityEngine;
using UnityEngine.Networking;
using System.Collections;  // ← 이게 반드시 있어야 합니다
using System.IO;
public class FileManager : MonoBehaviour
{
    private string path = PlayerDataManager._serverUrl;
    private string oid = "1"; // oid 테스트

    public void StartUpload()
    {
        string filePath = Path.Combine(Application.persistentDataPath, oid+".glb");
        if (File.Exists(filePath))
        {
            StartCoroutine(UploadFile(filePath, true));
            //StartCoroutine(UploadFile(filePath, false));
        }
        else
        {
            Debug.LogError("파일이 존재하지 않습니다: " + filePath);
        }
    }

    IEnumerator UploadFile(string filePath, bool isImage)
    {
        string uploadUrl;
        if (!isImage)
            uploadUrl = $"{path}/instance/upload/file";
        else
            uploadUrl = $"{path}/instance/upload/img";

        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "application/octet-stream");

        UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("업로드 실패: " + www.error);
        }
        else
        {
            Debug.Log("업로드 성공: " + www.downloadHandler.text);
        }
    }

    public void DownloadFile(string oid)
    {
        StartCoroutine(DownloadAndSave(oid, true));
        StartCoroutine(DownloadAndSave(oid, false));
    }

    public void DownloadImage(string oid)
    {
        StartCoroutine(DownloadAndSave(oid, true));
    }

    IEnumerator DownloadAndSave(string oid, bool isImage)
    {
        string url;
        if (!isImage)
            url = $"{path}/instance/download/file/" + oid; // 서버는 {oid}.glb를 내부에서 처리
        else
            url = $"{path}/instance/download/img/" + oid;

        Debug.Log("url: " + url);
        string savePath;
        if (!isImage)
             savePath = Path.Combine(Application.persistentDataPath, "objects", oid + ".glb");
        else
             savePath = Path.Combine(Application.persistentDataPath, "images", oid + ".jpg");

        Debug.Log("savePath: " + savePath);

        if (File.Exists(savePath))
        {
            // 이미 파일 존재
            yield break;
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("다운로드 실패: " + www.error);
        }
        else
        {
            File.WriteAllBytes(savePath, www.downloadHandler.data);
            Debug.Log("파일 저장 완료: " + savePath);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // StartUpload();
        // StartCoroutine(DownloadAndSave(oid));
        //DownloadFile();
    }
}

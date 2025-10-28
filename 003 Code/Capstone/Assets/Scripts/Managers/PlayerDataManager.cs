using AG.Network.AGLobby;
using NUnit.Framework;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerDataManager : MonoBehaviour
{
    /*
 
Dont Destroy on Load 이용해서 유저 데이터 객체 만들어야함

들고 있어야 하는 데이터
- UID
- SID / EXPIRE
- LEVEL
- EXP
- MONEY
- BOX_SIZE

    GETTER/SETTER 만들지 말고 그냥 전역 변수로 접근
 
 */

    public GameObject _invalidSessionUI;
    
    public static int _uid;
    public static string _sid;
    public static DateTime _expire;
    public static int _level;
    public static int _exp;
    public static int _money;
    public static int _box_size;
    public static string _oids;

    public static string _serverUrl = "https://dfe92cde1caa.ngrok-free.app";

    private static string _url = $"{_serverUrl}/instance";
    private static string _filePath;

    private static string _objPath;

    private static string _sessionUrl = $"{_serverUrl}/session/view";

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        ClearData();
        _filePath = Path.Combine(Application.persistentDataPath, "myJson.json");
        if (!File.Exists(_filePath))
        {
            File.Create(_filePath);
        }

        _objPath = Path.Combine(Application.persistentDataPath, "objects");
    }

    public static void ClearData()
    {
        _uid = 0;
        _sid = "";
        _expire = DateTime.Now;
        _level = 0;
        _exp = 0;
        _money = 0;
        _box_size = 0;
    }

    public void StartSaveItemCoroutine()
    {
        StartCoroutine(SaveMyItemJson());
    }

    public IEnumerator SaveMyItemJson()
    {
        JSONArray array = new JSONArray();

        FileManager fileManager = GameObject.FindAnyObjectByType<FileManager>();
        Debug.Log($"Before Trime : {_oids}");
        string trimmed = _oids.Trim('[', ']');
        Debug.Log($"After Trime : {trimmed}");
        List<int> _oidList = trimmed.Split(',').Select(s=>s.Trim()).Where(s=>!string.IsNullOrEmpty(s)).Select(int.Parse).ToList();
        Debug.Log($"oidList {_oidList.Count}");

        foreach (int oid in _oidList)
        {
            // path : /instance/oid
            if (fileManager != null)
            {
                fileManager.DownloadFile(oid.ToString());
                Debug.Log($"oid{oid} Download");
            }

            string url = $"{_url}/{oid}";

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("정보 로드 실패: " + www.error);
            }
            else
            {
                JSONNode n = JSON.Parse(www.downloadHandler.text);
                array.Add(n);
            }
        }

        File.WriteAllText(_filePath, array.ToString());

        while (true)
        {
            int cnt = 0;
            foreach (int oid in _oidList)
            {
                string path = $"{_objPath}/{oid}.glb";
                if (File.Exists(path))
                    cnt++;
            }

            if (cnt >= _oidList.Count)
            {
                break;
            }

            yield return new WaitForSeconds(1);
        }

        StartSceneManager.Instance.LoadMainScene();
    }

    public void StartCompareSessionCoroutine(System.Action<int> callback)
    {
        StartCoroutine(CompareSession(callback));
    }

    public IEnumerator CompareSession(System.Action<int> callback)
    {
        var json = new JSONObject();
        json["uid"] = _uid.ToString();
        json["sid"] = _sid.ToString();

        Debug.Log(json);

        UnityWebRequest request = new UnityWebRequest(_sessionUrl, "GET");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString());

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler.contentType = "application/json";
        yield return request.SendWebRequest();
        Debug.Log(request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Response 데이터

            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                var retJson = JSON.Parse(request.downloadHandler.text);
                Debug.Log(retJson.ToString());

                DateTime cmp = DateTime.Parse(retJson["expire"]);
                DateTime now = DateTime.Now;
                if (now > cmp)
                {
                    // 세션 유효 시간 지남

                    InvalidSession("Expired Session");
                } else
                {
                    // 세션 시간 정상, 정상 작업 실행
                    callback?.Invoke(1);
                }

            } else
            {
                // 세션 정보 없음

                InvalidSession("Invalid Session");
            }
        }
        else
        {
            // 서버 url이 잘못 됐거나,, 아예 통신 실패
            InvalidSession("Invalid Url or Server Down");
        }
    }

    public void InvalidSession(string reason)
    {
        // 메인씬 이동
        // 모든 데이터 초기화

        ClearData();
        LobbySingleton.instance.ClearSession();

        GameObject _ui = Instantiate(_invalidSessionUI);
        _ui.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        _ui.transform.rotation = Quaternion.LookRotation(_ui.transform.position - Camera.main.transform.position);
        _ui.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
        StartCoroutine(GoMainScene(_ui, reason));
    }

    IEnumerator GoMainScene(GameObject ui, string reason)
    {
        // 세션 DB 정보 제거
        var json = new JSONObject();
        json["uid"] = _uid.ToString();
        json["sid"] = _sid.ToString();

        UnityWebRequest www = new UnityWebRequest($"{_serverUrl}/user/logout", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString());

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.uploadHandler.contentType = "application/json";
        yield return www.SendWebRequest();

        Debug.Log(www.downloadHandler.text);

        GameObject.Find("reasonText").GetComponent<Text>().text += $"{reason}";
        Text timeText = GameObject.Find("timeText").GetComponent<Text>();

        int time = 5;
        while (time > 0)
        {
            timeText.text = $"Logout In {time} Seconds...";
            yield return new WaitForSeconds(1);
            time--;
        }

        FindAnyObjectByType<SceneChanger>().ChangeScene("StartScene");
    }
}

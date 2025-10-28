using Meta.WitAi.TTS.Integrations;
using SimpleJSON;
using System;
using System.Collections;
using System.Numerics;
using System.Text;
using Unity.Services.Lobbies.Http;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class LoginUI : MonoBehaviour
{
    public static Text _id;
    public Text _password;
    public Button _loginButton;
    public Button _registerButton;
    public Text _result;
    public GameObject _registerUI;
    public delegate void LoginUIDelegate();
    public LoginUIDelegate _fp;

    string url = "/user/login";

    void Start()
    {
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
        _registerButton.onClick.AddListener(OnRegisterButtonClicked);
        _id = GameObject.Find("_idText").GetComponent<Text>();
    }

    void OnLoginButtonClicked()
    {
        var json = new JSONObject();

        json["id"] = _id.text;
        json["password_hash"] = _password.text;

        //json["id"] = "admin";
        //json["password_hash"] = "1q2w3e4r!";

        _loginButton.interactable = false;
        StartCoroutine(WaitLogin(json.ToString()));
    }

    void OnRegisterButtonClicked()
    {
        GameObject backup = _registerUI;
        _registerUI = Instantiate(_registerUI);
        _registerUI.GetComponent<RegisterUI>().SetLoginUI(this.gameObject);
        _registerUI.transform.position = this.transform.position;
        _registerUI.transform.rotation = this.transform.rotation;
        _registerUI.transform.localScale = new UnityEngine.Vector3(0.003f, 0.003f, 0.003f);
        _registerUI = backup;
        this.gameObject.SetActive(false);
    }

    IEnumerator WaitLogin(string json)
    {
        Debug.Log(json);
        UnityWebRequest request = new UnityWebRequest(PlayerDataManager._serverUrl + url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler.contentType = "application/json";
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Response ������
            Debug.Log(request.downloadHandler.text);

            /*
             OUTPUT : 
            success(���� ����), 
            sid(���� ID), 
            expire(���� ���� �ð�), 
            uid(���� ID), 
            level(���� ����), 
            exp(���� ����ġ), 
            money(���� ��ȭ), 
            box_size(������ ĭ)
             */
            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                var retJson = JSON.Parse(request.downloadHandler.text);
                if (retJson["success"] == "true")
                {
                    PlayerDataManager._sid = BigInteger.Parse(retJson["sid"]).ToString();
                    PlayerDataManager._expire = DateTime.Parse(retJson["expire"]);
                    PlayerDataManager._uid = int.Parse(retJson["uid"]);
                    PlayerDataManager._level = int.Parse(retJson["level"]);
                    PlayerDataManager._level = int.Parse(retJson["level"]);
                    PlayerDataManager._exp = int.Parse(retJson["exp"]);
                    PlayerDataManager._money = int.Parse(retJson["money"]);
                    PlayerDataManager._box_size = int.Parse(retJson["box_size"]);
                    PlayerDataManager._oids = retJson["oid"].ToString();

                    _result.text = "Result : Success";
                    _fp();
                } else
                {
                    // �α��� ���� (��Ʈ��ũ ���� X (��� Ʋ�Ȱų�, ���� ���̵�ų�
                    _result.text = "Result : Fail";
                }
            }
        }
        else
        {

            Debug.Log(request.downloadHandler.text);
            _result.text = "Result : Failed To Login";
            _loginButton.interactable = true;
        }
    }

    IEnumerator WaitRegister(string json)
    {
        //REGISTER (IN : name, email, pass / OUT : success, fail)

        UnityWebRequest request = new UnityWebRequest(PlayerDataManager._serverUrl + url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {

            // Response ������
            Debug.Log(request.downloadHandler.text);

            /*
             OUTPUT : 
            success(���� ����), 
            fail(���� ����)
             */

        }
        else
        {

        }
    }

    IEnumerator WaitLogout(string json)
    {
        //LOGOUT (IN : sid / OUT : success)

        UnityWebRequest request = new UnityWebRequest(PlayerDataManager._serverUrl + url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            _result.text = "Result : Success";
            // Response ������
            Debug.Log(request.downloadHandler.text);

            /*
             OUTPUT : 
            success(���� ����)
             */
            PlayerDataManager.ClearData();

        }
        else
        {
            // ���� �ص� ���� �α��� �ϰų� ������ ���Ŷ� ��� �����
        }
    }

    IEnumerator WaitIncreaseExp(string json)
    {
        //INCREASEEXP (IN : sid, uid, exp / OUT : success, sid, expire, uid, level, exp, money, box_size)

        UnityWebRequest request = new UnityWebRequest(PlayerDataManager._serverUrl + url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            _result.text = "Result : Success";
            // Response ������
            Debug.Log(request.downloadHandler.text);
            var retJson = JSON.Parse(request.downloadHandler.text);
            /*
             OUTPUT : 
            success(���� ����)
            sid(���� ID) 
            expire(���� ���� �ð�)
            uid
            level
            exp
            money
            box_size
             */
            PlayerDataManager._sid = retJson["sid"];
            PlayerDataManager._expire = DateTime.Parse(retJson["expire"]);
            PlayerDataManager._uid = int.Parse(retJson["uid"]);
            PlayerDataManager._level = int.Parse(retJson["level"]);
            PlayerDataManager._exp = int.Parse(retJson["exp"]);
            PlayerDataManager._money = int.Parse(retJson["money"]);
            PlayerDataManager._box_size = int.Parse(retJson["box_size"]);

        }
        else
        {

        }
    }

    IEnumerator WaitMoney(string json)
    {
        //MONEY (IN : sid, uid, money / OUT : success, sid, expire, uid, level, exp, money, box_size)

        UnityWebRequest request = new UnityWebRequest(PlayerDataManager._serverUrl + url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            _result.text = "Result : Success";
            // Response ������
            Debug.Log(request.downloadHandler.text);
            var retJson = JSON.Parse(request.downloadHandler.text);

            /*
             OUTPUT : 
             OUTPUT : 
            success(���� ����)
            sid(���� ID) 
            expire(���� ���� �ð�)
            uid
            level
            exp
            money
            box_size
             */
            PlayerDataManager._sid = retJson["sid"];
            PlayerDataManager._expire = DateTime.Parse(retJson["expire"]);
            PlayerDataManager._uid = int.Parse(retJson["uid"]);
            PlayerDataManager._level = int.Parse(retJson["level"]);
            PlayerDataManager._exp = int.Parse(retJson["exp"]);
            PlayerDataManager._money = int.Parse(retJson["money"]);
            PlayerDataManager._box_size = int.Parse(retJson["box_size"]);

        }
        else
        {

        }
    }
}

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegisterUI : MonoBehaviour
{
    public Text _id;
    public Text _email;
    public Text _password;
    public Button _registerButton;
    public Button _backButton;
    public Text _result;
    public GameObject _loginUI;

    string url = PlayerDataManager._serverUrl + "/user/register";

    void Start()
    {
        _registerButton.onClick.AddListener(OnRegisterButtonClicked);
        _backButton.onClick.AddListener(OnBackButtonClicked);
    }

    void OnRegisterButtonClicked()
    {
        var json = new JSONObject();
        json["id"] = _id.text;
        json["email"] = _email.text;
        json["password_hash"] = _password.text;

        _registerButton.interactable = false;
        StartCoroutine(WaitRegister(json.ToString()));
    }

    void OnBackButtonClicked()
    {
        _loginUI.SetActive(true);
        Destroy(this.gameObject);
    }

    public void SetLoginUI(GameObject loginUI)
    {
        _loginUI = loginUI;
    }

    IEnumerator WaitLogin(string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
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
                    // �α��� ���� (��Ʈ��ũ ���� X (��� Ʋ�Ȱų�, ���� ���̵�ų�
                }
            }
        }
        else
        {
            _result.text = "Result : Failed To Login";
        }
    }

    IEnumerator WaitRegister(string json)
    {
        //REGISTER (IN : name, email, pass / OUT : success, fail)

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler.contentType = "application/json";
        yield return request.SendWebRequest();

        Debug.Log(request.downloadHandler.text);
        if (request.result == UnityWebRequest.Result.Success)
        {

            // Response ������
            var retJson = JSON.Parse(request.downloadHandler.text);
            /*
             OUTPUT : 
            success(���� ����), 
            fail(���� ����)
             */
            if (retJson["success"] == "true")
                _result.text = "SUCCESS";
            else
                _result.text = "FAIL";
        }
        else
        {
            _result.text = $"FAIL";
        }
    }

    IEnumerator WaitLogout(string json)
    {
        //LOGOUT (IN : sid / OUT : success)

        UnityWebRequest request = new UnityWebRequest(url, "POST");
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

        UnityWebRequest request = new UnityWebRequest(url, "POST");
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

        UnityWebRequest request = new UnityWebRequest(url, "POST");
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

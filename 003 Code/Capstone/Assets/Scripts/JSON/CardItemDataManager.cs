using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Threading;
using Unity.Services.Relay.Models;
using UnityEngine.UI;
using System.Text;
using System.Resources;
using UnityEngine.Networking;
using System;
using SimpleJSON;

// 카드 아이템 데이터 관리 클래스
// 서버와 통신하여 카드 정보를 받아오고, 카드 리스트를 관리함
public class CardItemDataManager : MonoBehaviour
{
    // 카드 아이템 리스트 (JSON 변환용)
    CardItemDataList itemList = new CardItemDataList();
    // 실제 카드 데이터 리스트
    public List<CardItemData> cardList = new List<CardItemData>();
    // 이미지 분류 서버에 전송할 이미지 경로
    string imagePath;
    // 3D 모델링 저장 서버에 저장될 위치 (glb 파일 경로)
    string filePath;
    // 파일 체크 중복 방지 플래그
    static bool isCheckRun = false;


    CardItemData data;

    const string requestURL = "http://182.210.118.115:3333";
    string DBURL = PlayerDataManager._serverUrl;


    public static string lastCreatedOID; // 어디서든 접근 가능

    public string bigClass;


    // 서버로부터 카드 정보 수신 시 호출되는 콜백 함수
    // isSuccess: 파일 감지 성공 여부, big: bigClass 정보

    // ✅ 중복 저장 방지용 static 변수 추가
    private static bool isAlreadySaved = false;

    public async void OnCardInfoReceived(bool isSuccess, string big)
    {
        if (!isSuccess)
        {
            isCheckRun = false;
            StartCoroutine(Check(filePath, (isSuccess, _) => OnCardInfoReceived(isSuccess, big)));
            return;
        }

        if (isAlreadySaved)
        {
            Debug.LogWarning("OnCardInfoReceived가 이미 실행되었으므로 중복 저장을 방지합니다.");
            return;
        }

        isAlreadySaved = true;
        Debug.Log("카드 정보 수신 시작: " + big);

        try
        {
            // 1. OID 요청
            int oidTask;
            if (!string.IsNullOrEmpty(lastCreatedOID))
            {
                oidTask = int.Parse(lastCreatedOID);
                Debug.Log($"기존 OID 사용: {oidTask}");
            }
            else
            {
                oidTask = await GetOID();
                lastCreatedOID = oidTask.ToString();
                Debug.Log($"서버에서 OID 발급 완료: {oidTask}");
            }

            // 2. 이미지 분류 요청
            Debug.Log("이미지 분류 시작...");
            string smallClassTask = await ImageClassification(imagePath, GetTypeCode(big));
            Debug.Log($"이미지 분류 완료: {smallClassTask}");
            await Task.Delay(500);

            // 3. 카드 데이터 생성
            data = new CardItemData
            {
                oid = oidTask.ToString(),
                uid = PlayerDataManager._uid.ToString(), // TODO: 실제 사용자 ID로 대체
                bigClass = big,
                smallClass = smallClassTask,
                abilityType = GetAbilityType(smallClassTask),
            };
            data.stat = GenerateRandomStat();
            data.grade = GetGrade(data.stat);
            data.cost = GetCost(data.stat, data.grade);

            // 4. 카드 데이터 생성 완료 후 자동으로 저장 진행
            Debug.Log("카드 데이터 생성 완료. 저장을 시작합니다.");
            bool success = await AddAndSaveCardData(true);
            if (success)
            {
                Debug.Log("카드 저장 완료");
            }
            else
            {
                Debug.LogError("카드 저장 실패");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"카드 정보 처리 중 예외 발생: {e.Message}");
            ShowErrorUI();
        }
    }

    public async Task<bool> AddAndSaveCardData(bool showUI = true)
    {
        // data가 null인지 확인
        if (data == null)
        {
            Debug.LogError("카드 데이터가 null입니다. OnCardInfoReceived가 먼저 호출되어야 합니다.");
            if (showUI)
            {
                ShowErrorUI();
            }
            return false;
        }

        // 1. 카드 리스트에 추가
        cardList.Add(data);
        Debug.Log($"[카드 추가] oid: {data.oid}, smallClass: {data.smallClass}");

        // 2. DB 저장 요청
        Debug.Log("[DB 저장] 요청 시작...");
        bool isSaved = await CreateInstance(data);

        // 3. GLB 파일 저장 요청
        bool isUploadedFile = false;
        bool isUploadedImage = false;
        filePath = Path.Combine(Application.persistentDataPath, "objects", data.oid + ".glb");
        if (File.Exists(filePath))
        {
            Debug.Log($"[DB 저장] GLB 파일 경로: {filePath}");
            isUploadedFile = await UploadFile(filePath, false);
            isUploadedImage = await UploadFile(imagePath, true);
        }
        else
        {
            Debug.LogError("파일이 존재하지 않습니다: " + filePath);
        }

        if (isSaved && isUploadedFile && isUploadedImage)
        {
            Debug.Log("[DB 저장] 성공");
        }
        else
        {
            Debug.LogError("[DB 저장] 실패");
        }


        // 4. UI 표시 (옵션)
        if (showUI)
        {
            if (isSaved && isUploadedFile && isUploadedImage)
            {
                ShowSuccessUI(data);
            }
            else
            {
                ShowErrorUI();
            }
        }

        return isSaved && isUploadedFile && isUploadedImage;
    }

    // 카드 정보를 DB 서버에 저장 (비동기)
    async Task<bool> CreateInstance(CardItemData data)
    {
        Debug.Log("CreateInstance 호출됨");

        string jsonData = JsonConvert.SerializeObject(data);

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{DBURL}/instance/create", content); // Post
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException e)
            {
                Debug.Log(e.Message);
                return false;
            }
        }
    }

    public async Task<bool> UploadFile(string filePath, bool isImage)
    {
        string uploadUrl;
        if (!isImage)
            uploadUrl= $"{DBURL}/instance/upload/file";
        else
            uploadUrl = $"{DBURL}/instance/upload/img";

        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "application/octet-stream");

        using (UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("업로드 실패: " + www.error);
                return false;
            }
            else
            {
                Debug.Log("업로드 성공: " + www.downloadHandler.text);
                return true;
            }
        }
    }

    // <<-- [수정됨] 4개의 인수를 받도록 시그니처 변경
    public async void RegisterAuctionItem(string oid, string cost, bool sellState)
    {
        var data = new JSONObject();
        data["cost"] = int.Parse(cost);
        data["sellState"] = sellState;


        // UpdateItemInfo는 oid와 변경할 데이터(data)를 서버로 전송합니다.
        bool success = await UpdateItemInfo(oid, data.ToString());

        if (success)
        {
            Debug.Log("카드 경매장 등록 완료");
        }
        else
        {
            Debug.LogError("카드 경매장 등록 실패");
        }
    }

    public async Task<bool> UpdateItemInfo(string oid, string data)
    {
        string uploadUrl = $"{DBURL}/instance/{oid}";

        // PUT 요청을 위한 UnityWebRequest 객체 생성
        using (UnityWebRequest request = new UnityWebRequest(uploadUrl, "PUT"))
        {
            // JSON 데이터를 바이트 배열로 변환
            byte[] bodyRaw = Encoding.UTF8.GetBytes(data);

            // 요청에 데이터와 헤더 추가
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 서버에 요청 전송 및 응답 대기
            await request.SendWebRequest();

            // 응답 결과 확인
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 에러가 발생한 경우
                Debug.LogError($"[오류] {request.error}");
                Debug.LogError($"서버 응답 코드: {request.responseCode}");
                Debug.LogError($"서버 메시지: {request.downloadHandler.text}");
                return false;
            }
            else
            {
                // 성공적으로 응답을 받은 경우
                Debug.Log($"[성공] 서버 응답 코드: {request.responseCode}");
                Debug.Log($"서버 메시지: {request.downloadHandler.text}");
                return true;
            }
        }
    }

    // bigClass 문자열에 따라 type 코드 반환 (0: kitchen, 1: clean, 2: writeInst)
    int GetTypeCode(string big)
    {
        if (big.Contains("kitchen")) return 0;
        else if (big.Contains("clean")) return 1;
        else if (big.Contains("write")) return 2;
        return 0; // 기본값
    }

    // 유니티 Start 함수: 초기화 및 파일 체크 코루틴 시작
    void Start()
    {
        // MainScene에서만 실행
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene")
        {
            return;
        }

        // 이미지 및 GLB 파일 경로 설정
        if (!string.IsNullOrEmpty(WebcamToUI.currentTimestamp) && !string.IsNullOrEmpty(WebcamToUI.firstCapturedFileName))
        {
            imagePath = Path.Combine("C:/RecordedFrames", WebcamToUI.currentTimestamp, WebcamToUI.firstCapturedFileName);
            filePath = Path.Combine(Application.persistentDataPath, "objects", "{oid}.glb"); // 멤버 변수에 저장

            // bigClass가 외부에서 전달되었는지 확인
            if (!string.IsNullOrEmpty(bigClass))
            {
                Debug.Log($"[Start] BigClass 설정됨: {bigClass}");
                StartCoroutine(Check(filePath, (isSuccess, _) => OnCardInfoReceived(isSuccess, bigClass)));
            }
            else
            {
                Debug.LogWarning("[Start] bigClass가 설정되지 않았습니다. GLBViewerUI에서 먼저 설정해주세요.");
            }
        }
        else
        {
            Debug.LogWarning("WebcamToUI.currentTimestamp 또는 firstCapturedFileName이 설정되지 않았습니다.");
        }

        // 카드 리스트를 JSON으로 저장
        itemList.items = cardList;
        string json = JsonUtility.ToJson(itemList, true);
        Debug.Log(json);

        string path = Path.Combine(Application.persistentDataPath, "items.json");
        File.WriteAllText(path, json);
        Debug.Log("저장된 파일의 경로: " + path);
    }


    // 0~99 사이의 랜덤 스탯 생성
    string GenerateRandomStat()
    {
        int stat = UnityEngine.Random.Range(0, 100);
        return stat.ToString();
    }
    // 소분류명에 따라 능력 타입 반환
    string GetAbilityType(string smallClass)
    {
        smallClass = smallClass.ToLower();

        // Throw 타입
        if (
            smallClass.Contains("ballpen") ||
            smallClass.Contains("glue") ||
            smallClass.Contains("scissors") ||
            smallClass.Contains("ruler") ||
            smallClass.Contains("pencil") ||
            smallClass.Contains("crayon") ||
            smallClass.Contains("knife") ||
            smallClass.Contains("ladle") ||
            smallClass.Contains("mbowl") ||
            smallClass.Contains("mcup") ||
            smallClass.Contains("cup") ||
            smallClass.Contains("spoon") ||
            smallClass.Contains("spray") ||
            smallClass.Contains("squeezer") ||
            smallClass.Contains("mop") ||
            smallClass.Contains("duster") ||
            smallClass.Contains("broom") ||
            smallClass.Contains("airgun")
        )
            return "A";

        // Consume 타입
        if (
            smallClass.Contains("tape") ||
            smallClass.Contains("highlighterpen") ||
            smallClass.Contains("fountainpen") ||
            smallClass.Contains("eraser") ||
            smallClass.Contains("mspoon") ||
            smallClass.Contains("microwave") ||
            smallClass.Contains("ptowel") ||
            smallClass.Contains("scale") ||
            smallClass.Contains("spatula") ||
            smallClass.Contains("strainer") ||
            smallClass.Contains("tongs") ||
            smallClass.Contains("container") ||
            smallClass.Contains("vacuum") ||
            smallClass.Contains("toiletbrush") ||
            smallClass.Contains("sponge") ||
            smallClass.Contains("mopsqueezer") ||
            smallClass.Contains("gloves") ||
            smallClass.Contains("dustpan") ||
            smallClass.Contains("dishcloth") ||
            smallClass.Contains("bucket")
        )
            return "B";

        // Install 타입
        if (
            smallClass.Contains("compass") ||
            smallClass.Contains("coffeepot") ||
            smallClass.Contains("cookpot") ||
            smallClass.Contains("pan") ||
            smallClass.Contains("plate") ||
            smallClass.Contains("toaster") ||
            smallClass.Contains("tapecleaner")
        )
            return "C";

        // 그 외는 D
        return "D";
    }
    // 스탯에 따라 등급 반환
    string GetGrade(string statStr)
    {
        int stat = int.Parse(statStr);
        if (stat >= 90) return "Epic";
        if (stat >= 60) return "SuperRare";
        if (stat >= 40) return "Rare";
        if (stat >= 10) return "Normal";
        return "Normal";
    }
    // 스탯과 등급에 따라 비용 산출
    string GetCost(string statStr, string grade)
    {
        int stat = int.Parse(statStr);
        int baseCost = grade switch
        {
            "Epic" => 100,
            "SuperRare" => 80,
            "Rare" => 60,
            "Normal" => 30,
            _ => 10
        };
        int finalCost = stat * baseCost / 10;
        return finalCost.ToString();
    }

    // 이미지 분류 서버에 이미지 전송 후 소분류명 반환 (비동기)
    public static async Task<string> ImageClassification(string filePath, int type)
    {
        using (var client = new HttpClient())
        using (var form = new MultipartFormDataContent())
        {
            var fileName = Path.GetFileName(filePath);
            using (var fileStream = File.OpenRead(filePath))
            {
                var fileContent = new StreamContent(fileStream);
                form.Add(fileContent, "image", fileName);

                string url = requestURL;

                // type에 따라 분류 서버 엔드포인트 결정
                switch (type)
                {
                    case 0:
                        url += "/kitchen";
                        break;
                    case 1:
                        url += "/clean";
                        break;
                    case 2:
                        url += "/write";
                        break;
                    default:
                        // 오류 처리
                        break;
                }

                var response = await client.PostAsync(url, form);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var jObj = JObject.Parse(json);
                return jObj["predicted_class"]?.ToString();
            }
        }
    }
    // 서버로부터 OID(고유 식별자) 받아오기 (비동기)
    public static async Task<int> GetOID()
    {
        using (var client = new HttpClient())
        using (var form = new MultipartFormDataContent())
        {
            var response = await client.GetAsync(requestURL + "/oid");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(json);
            return jObj["oid"].Value<int>();
        }
    }

    // 파일 존재 여부를 주기적으로 체크하는 코루틴
    // filePath: 감지할 파일 경로
    // onFileDetected: 파일 감지 시 호출될 콜백 (성공/실패, 파일경로)
    public static IEnumerator Check(string filePath, System.Action<bool, string> onFileDetected)
    {
        if (isCheckRun)
            yield return null;

        isCheckRun = true;
        int count = 0;

        while (true)
        {
            if (count > 120)
            {
                // 20분(10초*120) 동안 파일이 없으면 실패 처리
                onFileDetected?.Invoke(false, filePath);
                break;
            }
            Debug.Log($"result ; {File.Exists(filePath)}, PATH ; {filePath}");
            if (File.Exists(filePath))
            {
                Debug.Log("in");
                onFileDetected?.Invoke(true, filePath);
                break;
            }
            yield return new WaitForSeconds(10.0f); // 10초마다 체크
        }
    }

    // 성공 UI 표시
    private void ShowSuccessUI(CardItemData data)
    {
        Debug.Log($"카드 저장 성공! OID: {data.oid}, 분류: {data.smallClass}, 등급: {data.grade}");
        
        // 성공 메시지를 콘솔에 출력 (나중에 UI로 변경 가능)
        // TODO: 실제 UI 구현 시 여기에 성공 UI 표시 로직 추가
    }

    // 실패 UI 표시
    private void ShowErrorUI()
    {
        Debug.LogError("카드 저장에 실패했습니다.");
        
        // 실패 메시지를 콘솔에 출력 (나중에 UI로 변경 가능)
        // TODO: 실제 UI 구현 시 여기에 실패 UI 표시 로직 추가
    }
}
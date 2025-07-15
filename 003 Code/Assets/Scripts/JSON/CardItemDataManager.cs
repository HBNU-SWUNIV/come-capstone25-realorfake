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
    // 3D 모델링 저장 서버에 저장될 위치 (obj 파일 경로)
    string filePath;
    // 파일 체크 중복 방지 플래그
    static bool isCheckRun = false;

    const string requestURL = "221.158.33.236:3333";


    // 서버로부터 카드 정보 수신 시 호출되는 콜백 함수
    // isSuccess: 파일 감지 성공 여부, big: bigClass 정보
    public async void OnCardInfoReceived(bool isSuccess, string big)
    {
        if (!isSuccess)
        {
            // 검색 실패 시 재시도
            isCheckRun = false;
            StartCoroutine(Check(filePath, OnCardInfoReceived));
            return;
        }

        Debug.Log("카드 정보 수신 시작: " + big);

        try
        {
            // 1. 서버로부터 OID(고유 식별자) 비동기 요청
            Debug.Log("OID 요청 시작...");
            int oidTask = await GetOID();
            Debug.Log($"OID 발급 완료: {oidTask}");

            // 2. 이미지 분류 서버에 이미지 전송 후 smallClass(소분류) 비동기 요청
            Debug.Log("이미지 분류 시작...");
            string smallClassTask = await ImageClassification(imagePath, GetTypeCode(big));
            Debug.Log($"이미지 분류 완료: {smallClassTask}");

            // 비동기 처리로 인한 오류 방지 (임시 대기)
            await Task.Delay(500);

            // 3. 카드 정보 생성 및 리스트에 추가
            CardItemData data = new CardItemData();

            data.oid = oidTask.ToString(); // OID 할당
            data.bigClass = big;
            data.smallClass = smallClassTask; // 소분류 할당
            data.abilityType = GetAbilityType(smallClassTask); // 능력 타입 할당
            data.stat = GenerateRandomStat(); // 랜덤 스탯 생성
            data.grade = GetGrade(data.stat); // 등급 산출
            data.cost = GetCost(data.stat, data.grade); // 비용 산출

            cardList.Add(data);
            
            // 4. DB 서버에 아이템 정보 전달 및 저장
            Debug.Log("DB 저장 시작...");
            bool CreateSuccess = await CreateInstance(data);
            if (CreateSuccess) 
            {
                Debug.Log($"카드 정보 생성이 완료되었습니다: oid={data.oid}, smallClass={data.smallClass}");
                
                // 5. 성공 UI 표시
                ShowSuccessUI(data);
            }
            else 
            {
                Debug.LogError("카드 정보 생성 실패: DB 저장 중 오류 발생");
                ShowErrorUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"카드 정보 처리 중 오류 발생: {e.Message}");
            ShowErrorUI();
        }
    }

    // 카드 정보를 DB 서버에 저장 (비동기)
    async Task<bool> CreateInstance(CardItemData data)
    {
        Debug.Log("CreateUser 호출됨");

        string jsonData = JsonConvert.SerializeObject(data);

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://19a4-203-230-102-162.ngrok-free.app/instance/create", content); // Post
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException e)
            {
                Debug.Log(e.Message);
                return false;
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
        
        // 실제 구현 시 이미지 경로 및 obj 파일 경로 할당
        if (!string.IsNullOrEmpty(WebcamToUI.currentTimestamp))
        {
            imagePath = WebcamToUI.currentTimestamp;
            filePath = $"C:/RecordedFrames/{WebcamToUI.currentTimestamp}/output/texturedMeshed.obj";
            
            // obj 파일 생성 감지 코루틴 시작
            StartCoroutine(Check(filePath, OnCardInfoReceived));
        }
        else
        {
            Debug.LogWarning("WebcamToUI.currentTimestamp가 설정되지 않았습니다.");
        }

        // 카드 리스트를 JSON으로 변환 후 저장
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
        int stat = Random.Range(0, 100);
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

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

public class GetItemInfo : MonoBehaviour
{
    /*
    함수 설명 :
    이미지 경로를 받아서 분류를 수행하는 함수

    Task<string> task = ImageClassification("경로");
    -> 함수 호출만으로 작업이 시작됨

    string result = task.Result;
    -> 함수가 끝나지 않았는데 여기서 대기하면 예외 발생 (프로그램 멈춤 현상)
    -> 비동기 처리를 해야 함

    0 : kitchen (주방 용품)
    1 : clean (욕실 용품)
    2 : writeInst (기본 설명)

     */
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

                string url = "http://218.158.75.83:3333";

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
                        // 경로 오류
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

    /*
    비동기 함수로 구현됨.

     Task<int> tasks = Requests.GetOID();

     int results = tasks.Result;

     */
    public static async Task<int> GetOID()
    {
        using (var client = new HttpClient())
        using (var form = new MultipartFormDataContent())
        {
            var response = await client.GetAsync("http://218.158.75.83:3333/oid");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(json);
            return jObj["oid"].Value<int>();
        }
    }

    /*
    StartCoroutine(Check(filePath, MakeObject));
    -> 파일이 생성될 때까지 대기, 파일이 생성되면 생성된 파일의 경로를 함수(콜백 함수)로 전달

     */
    public static IEnumerator Check(string filePath, System.Action<bool, string> onFileDetected)
    {
        int count = 0;

        while (count <= 100)
        {
            if (File.Exists(filePath))
            {
                onFileDetected?.Invoke(true, filePath);
                break;
            }
            yield return new WaitForSeconds(10.0f);
        }

        onFileDetected?.Invoke(false, filePath);
    }

    private void MakeObject(bool isExist, string filePath)
    {
        /*
         
        파일 존재 여부를 체크하는 부분에서 파일이 없으면
        파일이 생성될 때까지 대기

        + 비동기 처리를 해야 함 
        하나의 작업이 끝나지 않았는데 다른 작업을 시작하면 안됨
         
         */
        if (!isExist)
        {
            // 파일이 생성될 때까지 대기
            StartCoroutine(Check(filePath, MakeObject));
            return;
        }

        /*
         
        파일이 생성되면 실행
        여기서 다른 함수 호출하면 됨

         */
    }
}

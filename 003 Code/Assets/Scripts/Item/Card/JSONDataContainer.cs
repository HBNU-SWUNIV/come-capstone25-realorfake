using UnityEngine;

public class JSONDataContainer : MonoBehaviour
{
    [TextArea(5, 20)]
    public string data; // 여기에 JSON 문자열을 저장

    public void SaveData(string json)
    {
        data = json;
    }
}

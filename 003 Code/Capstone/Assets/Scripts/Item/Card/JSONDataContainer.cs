using UnityEngine;

public class JSONDataContainer : MonoBehaviour
{
    [TextArea(5, 20)]
    public string data; // ���⿡ JSON ���ڿ��� ����

    public void SaveData(string json)
    {
        data = json;
    }
}

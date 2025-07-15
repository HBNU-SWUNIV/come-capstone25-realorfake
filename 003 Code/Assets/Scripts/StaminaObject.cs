using System.Drawing;
using UnityEngine;

public class StaminaObject : MonoBehaviour
{

    private GameObject[] staminaObjects;
    private int stamina;

    /// <summary>
    /// 
    /// 스테미너 업데이트
    /// 
    /// NetworkVariable로 stamina 변수로 관리 후 참조해서 업데이트 예정
    /// 테스트로 만든거라 따로 안건들어도 되고
    /// 테스트 할 때 빼고 하면 됨
    /// 
    /// </summary>

    void Start()
    {
        Init();
        SetStamina(10);
    }

    void FixedUpdate()
    {
        UpdateStamina();

        if (Input.GetMouseButtonDown(0))
            stamina--;
    }

    void Init()
    {
        staminaObjects = new GameObject[10];
        for (int i = 0; i < 10; i ++)
        {
            staminaObjects[i] = GameObject.Find($"Stamina{i + 1}").gameObject;
        }
    }

    void SetStamina(int dStamina)
    {
        stamina = dStamina;
    }

    void UpdateStamina()
    {
        for (int i = 0; i < stamina; i++)
        {
            Renderer[] renderers = staminaObjects[i].GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Material mat = renderer.material;
                mat.color = UnityEngine.Color.green;
            }
        }

        for (int i = stamina; i < 10; i++)
        {
            Renderer[] renderers = staminaObjects[i].GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Material mat = renderer.material;
                mat.color = UnityEngine.Color.red;
            }
        }
    }
}

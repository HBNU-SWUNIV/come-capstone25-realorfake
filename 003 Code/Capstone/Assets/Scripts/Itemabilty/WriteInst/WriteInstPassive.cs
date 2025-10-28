using UnityEngine;

public class WriteInstPassive : MonoBehaviour
{
    /*
     
    (패시브) 모든 문구류 아이템의 데미지 추가 데미지 5% (중첩)
     
     */

    private static WriteInstPassive _instance;
    private float _additionalDamage = 1.0f;

    private WriteInstPassive() { }

    public void UseWriteInstItem()
    {
        _additionalDamage += 0.05f;
    }

    public static WriteInstPassive GetInstance()
    {
        if (_instance == null)
            _instance = new WriteInstPassive();

        return _instance;
    }

    public float AdditionalDamage()
    {
        return _additionalDamage;
    }
}

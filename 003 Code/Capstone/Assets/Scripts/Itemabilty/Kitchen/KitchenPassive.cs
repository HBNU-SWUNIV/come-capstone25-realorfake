using Unity.Netcode;
using UnityEngine;

public class KitchenPassive : MonoBehaviour
{

    /*
     
    (체력) 기본 공격력 +20%, 5회 이상 사용 시 스태미나 소모 없이 공격 가능(단 한번), 다음 턴 공격 불가(다시 해야함)
    -> 다음 턴 스태미나 0으로 설정
     
     */

    private float _additionalStat = 0.2f;
    private static int _useCount = 0;
    private const int _maxUseCount = 5;

    private static KitchenPassive _instance;

    private KitchenPassive() { }

    public static KitchenPassive GetInstance()
    {
        if (_instance == null)
            _instance = new KitchenPassive();
        return _instance;
    }

    public bool UseKitchenItem()
    {
        if (_useCount >= _maxUseCount)
        {
            // 스태미나 0으로 설정된 상태에서 공격이 가능한 상태가 아니다.
            // 공격이 불가능한 상태로 설정
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ZEROSTAMINA, 1, 100, NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.ZEROSTAMINA, 1, 100, NetworkManager.Singleton.IsHost);

            return false;
        }
        _useCount++;
        Debug.Log($"UseKitchenItem {_useCount}");
        return true;
    }

    public float AdditionalDamage()
    {
        return 1 + _additionalStat;
    }

    public void ResetUseCount()
    {
        _useCount = 0;
    }
}

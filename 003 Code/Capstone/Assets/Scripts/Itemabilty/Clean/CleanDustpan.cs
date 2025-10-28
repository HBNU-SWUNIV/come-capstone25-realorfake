using Unity.Netcode;
using UnityEngine;

public class CleanDustpan : CleanItem
{

    /*
     
    쓰레받기(1) : (액티브, 소모) 30%의 확률로 가드를 1회 부여하고 소멸한다
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_09_GuardianShield";
        _particleScale = 0.2f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
    }
    public override void Use()
    {
        base.Use();

        if (Random.Range(0, 10) < 3)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 100, NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.GUARD, 1, 100, NetworkManager.Singleton.IsHost);
        }
    }
}

using Unity.Netcode;
using UnityEngine;

public class CleanGloves : CleanItem
{
    /*
     
    장갑(2) : (패시브, 소모) 가드 1회
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_09_GuardianShield";
        _particleScale = 0.2f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
    }
    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);

    }
}

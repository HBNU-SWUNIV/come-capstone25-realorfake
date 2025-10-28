using Unity.Netcode;
using UnityEngine;

public class KitchenPan : KitchenItem
{
    /*
     
    프라이팬 (2) : (스태미나, 설치) 적의 공격을 반사, 2회 사용 후 사라짐
     
     */
    protected override void Awake()
    {

        _installParticlePath = "Effect/Effect_38_GloryBoundary";
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _expireCount = 2;
        _interactionType = InteractionType.Install;
    }

    public override void InstallPassive()
    {
        --_expireCount;

        // 반사 버프적용..
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.REFLECT, 1, 2, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.REFLECT, 1, 2, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        base.Use();
    }
}

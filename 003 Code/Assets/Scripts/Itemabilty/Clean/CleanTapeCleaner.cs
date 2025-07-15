using Unity.Netcode;
using UnityEngine;

public class CleanTapeCleaner : CleanItem
{
    /*
     * 테이프 클리너(1) : (공격력, 설치) 5%의 확률로 모든 데미지를 막음
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Install;  // 설치형으로 설정
        base.Awake();
    }

    public void InstallPassive()
    {
        if (Random.Range(0, 20) < 1)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 1;

        GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina);
    }
}

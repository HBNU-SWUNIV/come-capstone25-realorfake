using System.Collections.Generic;
using UnityEngine;

public class DescriptionManager : MonoBehaviour
{
    public static DescriptionManager Instance;

    // 설명을 저장할 딕셔너리(Dictionary)
    private Dictionary<string, string> bigClassDescriptions;
    private Dictionary<string, string> smallClassDescriptionFormats;

    void Awake()
    {
        // --- [오류 해결] 싱글턴 인스턴스를 초기화하는 부분입니다. ---
        // Instance가 아직 설정되지 않았다면 자기 자신을 할당합니다.
        if (Instance == null)
        {
            Instance = this;
        }
        // 만약 Instance가 이미 다른 객체로 설정되어 있다면, 이 객체는 파괴합니다.
        // (씬에 DescriptionManager가 두 개 이상 존재하는 것을 방지)
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // 게임 시작 시 설명 데이터를 초기화합니다.
        InitializeDescriptions();
    }

    /// <summary>
    /// 설명 데이터를 초기화하는 함수.
    /// </summary>
    private void InitializeDescriptions()
    {
        // BigClass 공통 정보 초기화
        bigClassDescriptions = new Dictionary<string, string>
        {
            { "Kitchen", "기본 공격력 +20%, 5회 이상 사용 시 스태미나 소모 없이 공격 가능(단 한번), 다음 턴 공격 불가" },
            { "WriteInst", "필기구 아이템의 스탯을 10% 향상시킵니다." },
            { "Clean", "청소 아이템을 사용하지 않으면 공격력 +20%, 청소 아이템을 사용하면 공격력 -50%" }
            // 여기에 더 많은 bigClass 정보 추가...
        };

        // SmallClass 개별 정보 포맷 초기화 (데이터를 동적으로 조합)
        // {0}에는 stat, {1}에는 grade 등의 데이터가 들어갑니다.
        smallClassDescriptionFormats = new Dictionary<string, string>
        {
            { "ballpen", "스탯의 120% 피해로 공격" },
            { "compass", "매 턴 60%의 피해로 공격, 피해를 흡수하는 보호막 무시" },
            { "crayon", "랜덤 적 설치 아이템 2개 파괴" },
            { "eraser", "자신의 모든 디버프 효과 제거" },
            { "fountainpen", "전투 내 모든 아이템 스탯 +25%(영구)" },
            { "glue", "..." },
            { "highlighterpen", "3턴 간 내 방어력 +20%, 공격력 +15%" },
            { "pencil", "스탯의 100% 피해로 공격, 3회 공격 시 마다 스탯 반감" },
            { "ruler", "스탯의 80% 피해로 2회 공격" },
            { "scissors", "스탯의 90% 피해로 공격, 상대방 다음 턴 회복 불가" },
            { "tape", "상대 다음 턴 행동 불가, 자신의 다음 턴 스태미나 최대 4 고정" },
            { "coffeepot", "턴 당 스탯의 50% 피해로 공격, 2턴 후 파괴" },
            { "container", "3턴 간 스탯의 50% 만큼 피해를 흡수하는 보호막 생성" },
            { "cookpot", "턴 당 스탯의 20% 피해로 공격, 3턴 후 파괴, 3턴 간 방어력 +30%" },
            { "cup", "스탯의 70% 피해로 공격, 20% - 상대방 다음 턴 행동 불가" },
            { "knife", "스탯의 150% 피해로 공격, 20% -3턴 간  출혈(스탯의 10% 피해)" },
            { "ladle", "스탯의 80% 데미지 + 30% - 상대 스태미나 1 감소" },
            { "mbowl", "스탯의 60% 피해로 공격, 50% - 다음 턴 적 공격 실패" },
            { "mcup", "스탯의 100% 피해로 공격, 2턴 간 공격력 +15%" },
            { "microwave", "다음 사용하는 주방 아이템의 스탯을 2배 적용(영구)" },
            { "mspoon", "현재 적용 중인 회복 버프에 대한 회복량 +30%" },
            { "pan", "매 턴 적의 공격을 1회 반사, 2턴 후 제거" },
            { "plate", "스탯의 100% 만큼 피해를 흡수하는 보호막 생성" },
            { "ptowel", "출혈 제거, 2턴 간 현재 체력의 10% 체력 회복" },
            { "scale", "모든 설치형 아이템의 사용 횟수 +1" },
            { "spatula", "상대방의 공격 및 방어 수치의 +, -를 바꾼다" },
            { "spoon", "스탯의 90% 피해로 공격, 자신의 현재 체력의 10% 체력 회복" },
            { "strainer", "다음 턴 자신의 방어력 +50%" },
            { "toaster", "턴 당 스탯의 40% 피해로 공격, 3턴 후 파괴" },
            { "tongs", "상대의 다음 턴 스태미나 2 감소, 내 다음 턴 스태미나 2 증가" },
            { "airgun", "상호작용 시 스태미나 4 소모, 공격을 한 번 막는 보호막을 생성한다 (최대 2회)" },
            { "broom", "스탯 100%의 피해로 공격" },
            { "busket", "3턴 동안 받는 피해량 -20%" },
            { "dishCloth", "자신의 공격력을 영구적으로 +2%" },
            { "duster", "스탯 70%의 피해로 공격, , 다음 턴 내 피해량 +30%, 받는 피해량 +30%" },
            { "dustpan", "30%의 확률로 공격을 한 번 막는 보호막을 생성한다" },
            { "gloves", "100%의 확률로 공격을 한 번 막는 보호막을 생성한다" },
            { "mop", "스탯 100%의 피해로 공격, 5%-2배 피해, 5%-공격 실패" },
            { "mopSqueezer", "다음 턴 자신의 공격력을 2배로 증가" },
            { "sponge", "자신의 방어력을 영구적으로 +2%" },
            { "spray", "50% - 적 공격력 10% 감소, 50% - 적 방어력 10% 감소" },
            { "squeezer", "스탯 70%의 피해로 공격, 다음 턴 내 방어력 +30%" },
            { "tapeCleaner", "매 공격 5%의 확률로 공격을 막는다" },
            { "toiletBrush", "스탯 200%의 피해로 공격, 나 자신도 스탯 50%의 피해를 입음" },
            { "Vacuum", "상대방의 공격력을 10% 감소한다" },
        };
    }

    /// <summary>
    /// BigClass 키를 받아 포증가맷팅된 설명을 반환하는 함수
    /// </summary>
    public string GetBigClassDescription(string bigClassKey)
    {
        if (string.IsNullOrEmpty(bigClassKey)) return "분류 정보 없음";

        if (bigClassDescriptions.TryGetValue(bigClassKey, out string description))
        {
            return $"{bigClassKey} : {description}";
        }
        return $"{bigClassKey} : 등록된 정보가 없습니다.";
    }

    /// <summary>
    /// CardDisplay 컴포넌트를 받아 포맷팅된 SmallClass 설명을 반환하는 함수
    /// </summary>
    public string GetSmallClassDescription(CardDisplay card)
    {
        string smallClassKey = card.GetSmallClass();
        if (string.IsNullOrEmpty(smallClassKey)) return "개별 정보 없음";

        if (smallClassDescriptionFormats.TryGetValue(smallClassKey, out string format))
        {
            // string.Format을 이용해 카드 데이터를 설명에 동적으로 삽입합니다.
            string finalDescription = string.Format(format, card.GetStat(), card.GetGrade());
            return $"{smallClassKey} : {finalDescription}";
        }
        return $"{smallClassKey} : 등록된 정보가 없습니다.";
    }
}
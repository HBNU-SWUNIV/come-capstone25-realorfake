using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{

    static Text _turnText;
    static Text _timeText;
    static Text _staminaText;
    static Text _hpText;
    static Text _shieldText;
    static Text _attackText;
    static Text _defenseText;
    static Text _buffText;
    static Text _usedItemText;

    void Start()
    {
        _turnText = GameObject.Find("TurnText").GetComponent<Text>();
        _timeText = GameObject.Find("TimeText").GetComponent<Text>();
        _staminaText = GameObject.Find("StaminaText").GetComponent<Text>();
        _hpText = GameObject.Find("HpText").GetComponent<Text>();
        _shieldText = GameObject.Find("ShieldText").GetComponent<Text>();
        _attackText = GameObject.Find("AttackText").GetComponent<Text>();
        _defenseText = GameObject.Find("DefenseText").GetComponent<Text>();
        _buffText = GameObject.Find("BuffText").GetComponent<Text>();
        _usedItemText = GameObject.Find("UsedItemText").GetComponent<Text>();
    }

    public static void SetTurnText(string t)
    {
        _turnText.text = t;
    }

    public static void SetTimeText(float t)
    {
        
        _timeText.text = $"Time : {t.ToString("F1")}";
    }

    public static void SetStaminaText(int t)
    {
        _staminaText.text = $"Stamina : {t}";
    }

    public static void SetHpText(int t)
    {
        _hpText.text = $"HP : {t}";
    }

    public static void SetShieldText(int t)
    {
        _shieldText.text = $"Shield : {t}";
    }

    public static void SetAttackText(int t)
    {
        _attackText.text = $"Attack : {t}";
    }

    public static void SetDefenseText(int t)
    {
        _defenseText.text = $"Defense : {t}";
    }

    public static void AddBuffText(string t)
    {
        _buffText.text += $"\n{t}";
    }

    public static void ClearBuffText()
    {
        _buffText.text = $"BUFF";
    }

    public static void AddUsedItemText(string t)
    {
        _usedItemText.text += $"\n{t}";
    }
}

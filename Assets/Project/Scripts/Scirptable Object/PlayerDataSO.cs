using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData_Default", menuName = "ScriptableObjects/Character/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
    [Header("----Player Base Identity & Element----")]
    public string playerName = "Hero";
    public ElementType element = ElementType.Wood;

    [Header("----Base Stats (Level 1 GDD)----")]
    public float maxHP = 500f;
    public float atk = 200f;
    public float def = 0f;
    [Range(0f, 1f)] public float critRate = 0.10f; // 10%
    public float critDmg = 1.0f;                   // +100% khi bạo kích (x2.0)
    public float speed = 0.8f;
    public int initialEnergyCores = 50;
}

using UnityEngine;

[CreateAssetMenu(fileName = "ActiveSkillData", menuName = "GameData/Skill/ActiveSkillData")]
public class ActiveSkillDataSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _skillId;
    [SerializeField] private string _skillName = "Kỹ Năng Chủ Động";
    [SerializeField] private ActiveSkillType _skillType = ActiveSkillType.MeteorStrike;
    [SerializeField] private ElementType _element = ElementType.Fire;
    [SerializeField] private Sprite _icon;

    [Header("Stats")]
    [SerializeField] private float _cooldownTime = 12f;
    [SerializeField] private float _skillDamageMultiplier = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _dropRateFromBoss = 0.5f; // Tỉ lệ rơi từ Boss wave theo GDD

    [Header("Description")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    public string SkillId => _skillId;
    public string SkillName => _skillName;
    public ActiveSkillType SkillType => _skillType;
    public ElementType Element => _element;
    public Sprite Icon => _icon;
    public float CooldownTime => _cooldownTime;
    public float SkillDamageMultiplier => _skillDamageMultiplier;
    public float DropRateFromBoss => _dropRateFromBoss;
    public string Description => _description;

    public void Initialize(string id, string name, ActiveSkillType type, ElementType elem, float cd, float dmgMul, float dropRate)
    {
        _skillId = id;
        _skillName = name;
        _skillType = type;
        _element = elem;
        _cooldownTime = cd;
        _skillDamageMultiplier = dmgMul;
        _dropRateFromBoss = dropRate;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "SkillPassiveData", menuName = "GameData/Skill/SkillPassiveData")]
public class SkillPassiveDataSO : ScriptableObject
{
    [Header("Skill Identity")]
    [SerializeField] private string _skillId;
    [SerializeField] private string _skillName;
    [SerializeField] private TypeStone _stoneType;
    [SerializeField] private ElementType _element;
    [SerializeField] private Sprite _icon;

    [Header("Combat Parameters")]
    [SerializeField] private GameObject _skillPrefab;
    [SerializeField] private float _baseDamage = 25f;
    [SerializeField] private float _timeToLive = 5f;
    [SerializeField] private float _moveSpeed = 7f;

    [Header("Description")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    public string SkillId => _skillId;
    public string SkillName => _skillName;
    public TypeStone StoneType => _stoneType;
    public ElementType Element => _element;
    public Sprite Icon => _icon;
    public GameObject SkillPrefab => _skillPrefab;
    public float BaseDamage => _baseDamage;
    public float TimeToLive => _timeToLive;
    public float MoveSpeed => _moveSpeed;
    public string Description => _description;

    public void Initialize(string id, string skillName, TypeStone stoneType, ElementType element, GameObject prefab, float baseDamage)
    {
        Initialize(id, skillName, stoneType, element, prefab, null, baseDamage);
    }

    public void Initialize(string id, string skillName, TypeStone stoneType, ElementType element, GameObject prefab, Sprite icon = null, float baseDamage = 25f)
    {
        _skillId = id;
        _skillName = skillName;
        _stoneType = stoneType;
        _element = element;
        _skillPrefab = prefab;
        _icon = icon;
        _baseDamage = baseDamage;
    }
}

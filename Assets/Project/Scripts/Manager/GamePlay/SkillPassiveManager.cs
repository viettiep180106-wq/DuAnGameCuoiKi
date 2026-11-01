using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

// 
[Serializable]
public class QueuedSkill
{
    public TypeStone stoneType;
    public int matchCount;

    public QueuedSkill(TypeStone type, int count)
    {
        this.stoneType = type;
        this.matchCount = count;
    }
}

public class SkillPassiveManager : MonoBehaviour
{
    public static SkillPassiveManager instance;

    [Header("Settings")]
    [SerializeField]private Player player;
    [SerializeField]private float space = 0;
    [Header("Skill Prefabs")]
    private Dictionary<TypeStone,SkillPassive> listSkill = new Dictionary<TypeStone, SkillPassive>();

    private Queue<SkillPassive> skillQueue = new Queue<SkillPassive>();
    private bool isCasting = false;

    private void Awake()
    {
        instance = this;
        string path = "Prefab/Base/Skill/Passive";
        SkillPassive [] skills = Resources.LoadAll<SkillPassive>(path);
        foreach(SkillPassive skill in skills)
        {
            listSkill.Add(skill.GetTypeStone(),skill);
        }
    }
    public void CaculatorSkill(HashSet<Vector2Int> listMatch,List<List<SlotStoneBase>> grid)
    {
        Dictionary<TypeStone, int> matchCounts = new Dictionary<TypeStone, int>();
        foreach (Vector2Int e in listMatch)
        {
            TypeStone type = grid[e.x][e.y].GetStone().nameStone;
            if (matchCounts.ContainsKey(type))
            {
                matchCounts[type]++;
            }
            else
            {
                matchCounts.Add(type,1);

            }
        }

        foreach (var kvp in matchCounts)
        {
            GameObject newObj = ObjectPoolingManager.Instance.Spawn(listSkill[kvp.Key].gameObject,transform.position+ new Vector3(-2.15f+space,0,0),Quaternion.identity);
            newObj.transform.SetParent(transform);
            SkillPassive skill = newObj.GetComponent<SkillPassive>();
            skill.GetComponent<Transform>().localScale = new Vector3(0.4f,0.4f,0.4f);
            skill.Initialize(kvp.Value);
            skillQueue.Enqueue(skill);
            space+=0.3f;
        }
    }
    public SkillPassive GetSkill()
    {
        SkillPassive currentSkill = null;
        player.transform.GetComponent<PlayerAnimationController>().ActiveAnimation(TypeAnimationAtivity.isFly);
        
        if (skillQueue.Count > 0)
        {
            currentSkill = skillQueue.Dequeue();
        }
        return currentSkill;
    }
   
    
}
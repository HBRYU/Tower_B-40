using System.Collections;
using System.Collections.Generic;
using Player.Skills;
using UnityEngine;

public class PlayerSkillManager : MonoBehaviour
{
    public List<Skill> activeSkills;

    public void TriggerSkill(List<int> indexes)
    {
        GlobalSkillManager.SkillStatus skillStatus = GlobalSkillManager.SkillStatusList.Find(
            ss => ss.unlocked && ss.skill.ActivationPattern(indexes));
        
        if (skillStatus == null)
        {
            print("Skill issue :P");
            return;
        }
        
        var skill = skillStatus.skill;
        if (activeSkills.Contains(skill))
        {
            print("Skill already active");
            return;
        }
        skill.Activate(gameObject);
        activeSkills.Add(skill);
        
    }

    void Update()
    {
        foreach (var skill in activeSkills)
        {
            skill.Update();
        }
    }

    public void DeactivateSkill(Skill skill)
    {
        activeSkills.Remove(skill);
    }
}

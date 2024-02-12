using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Player.Skills;
using UnityEngine;

public class GlobalSkillManager : MonoBehaviour
{
    [Serializable]
    public class SkillStatus
    {
        public Skill skill;
        public bool unlocked;
    }

    public List<SkillStatus> setSkillStatusList;
    public static List<SkillStatus> SkillStatusList { get; private set; }
    //public static List<Skill> Skills { get; private set; }

    public static GlobalSkillManager GlobalSkillManagerInstance { get; private set; }

    private void Awake()
    {
        //Skills ??= SkillStatusList.Select(sp => sp.skill).ToList();  // checks null. Singleton? fuck
        SkillStatusList ??= setSkillStatusList;
        if(GlobalSkillManagerInstance == null)
            GlobalSkillManagerInstance = this;
    }

    public static void UpdateSkillUnlock(int index, bool value)
    {
        if(index < SkillStatusList.Count)
            SkillStatusList[index].unlocked = value;
    }
    
    public static void UpdateSkillUnlock(string skillName, bool value)
    {
        var status = SkillStatusList.Find(ss => ss.skill.skillName == skillName);
        if (status != null)
        {
            status.unlocked = value;
        }
    }
}

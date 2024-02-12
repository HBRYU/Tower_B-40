using System.Collections.Generic;
using UnityEngine;

namespace Player.Skills
{
    public abstract class Skill : ScriptableObject
    {
        public string skillName;
        public abstract bool ActivationPattern(List<int> indexes);
    
        // Should be initialized in editor
        public float energyCost;
        public bool overrideMovement, overrideWings;
        public AudioClip activationAudio;
        
        public abstract void Activate(GameObject playerObject);
        public abstract void Update();
        public abstract void Deactivate();
    }
}

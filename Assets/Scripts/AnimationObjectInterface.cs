using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AnimationObjectInterface : MonoBehaviour
{
    public Action endAction;

    // Try and fix my lazy ass
    public void End()
    {
        endAction();
    }
}

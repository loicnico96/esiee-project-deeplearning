using UnityEngine;
using UnityEditor;

public enum EActionCode : int
{
    ActionCodeNoAction,             // Idle, disabled, moving, dead
    ActionCodeRoll,                 // Rolling
    ActionCodeAttackLight,          // Light attack
    ActionCodeAttackHeavy,          // Charged attack
    ActionCodeGuard,                // Guard
    Count
}

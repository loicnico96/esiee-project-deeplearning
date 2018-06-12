using UnityEngine;
using UnityEditor;

public enum EEventType
{
    EventTypeGameEnd,           // The game ended (call at least once before exiting to save network state)
    EventTypeDamageDealt,       // Damage was dealt (requires "damage", "target_id" and "caster_id")
    EventTypeCharacterDeath,    // A character died (requires "caster_id", "target_id")
    EventTypeActionFinished,    // A character finished an action (requires "caster_id")
    EventTypeActionStarted,     // A character started an action (requires "caster_id", "action_code")
    Count
}
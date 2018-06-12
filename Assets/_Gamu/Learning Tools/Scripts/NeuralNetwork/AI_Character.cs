using UnityEngine;
using UnityEditor;

public class AI_Character
{
    public int CharacterID { get; private set; }
    public ECharacterType CharacterType { get; private set; }
    public AI_Action CurrentAction { get; private set; }
    public AI_Action PreviousAction { get; private set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float LastSeenTime { get; private set; }
    public bool IsAlive { get; private set; }
    public bool IsIdle { get { return (this.CurrentAction == null); } }
    public bool IsPlayer { get { return (this.CharacterType == ECharacterType.CharacterTypePlayer); } }

    public AI_Character(int character_id, ECharacterType character_type)
    {
        this.CharacterID = character_id;
        this.CharacterType = character_type;
        this.CurrentAction = null;
        this.PreviousAction = null;
        this.Direction = Vector3.zero;
        this.Position = Vector3.zero;
        this.Velocity = Vector3.zero;
        this.LastSeenTime = 0.0f;
        this.IsAlive = false;
    }

    /// <summary>
    /// Extrapolates the position of the character after a delay, considering current position and velocity. 
    /// </summary>
    /// <param name="delay">Delay (in seconds)</param>
    /// <returns></returns>
    public Vector3 PositionIn(float delay)
    {
        return this.Position + this.Velocity * delay;
    }

    /// Character is visible by AI (by definition AI are always visible by AI)
    public void OnSeenByAI(float game_time, Vector3 position, Vector3 direction, Vector3 velocity)
    {
        this.Direction = direction;
        this.Position = position;
        this.Velocity = velocity;
        this.LastSeenTime = game_time;
    }

    /// Character started an action
    public AI_Action OnActionStarted(float game_time, EActionCode action_code, AI_Character target)
    {
        this.CurrentAction = new AI_Action(action_code, this, target);
        this.CurrentAction.OnActionStarted(game_time);
        return this.CurrentAction;
    }

    // Character finished his current action
    public AI_Action OnActionFinished(float game_time)
    {
        this.PreviousAction = this.CurrentAction;
        if (this.PreviousAction != null)
        {
            this.PreviousAction.OnActionFinished(game_time);
        }
        return this.PreviousAction;
    }

    // Character dealt damage
    public void OnDamageDealt(AI_Character target, float damage_dealt)
    {
        if (this.CurrentAction != null)
        {
            this.CurrentAction.OnActionDamageDealt(damage_dealt);
        }
    }

    // Character took damage
    public void OnDamageTaken(AI_Character caster, float damage_taken)
    {
        if (this.CurrentAction != null)
        {
            this.CurrentAction.OnActionDamageTaken(damage_taken);
        }
    }

    // Character died
    public void OnDeath()
    {
        this.IsAlive = true;
    }
}

using UnityEngine;
using UnityEditor;

public class AI_Action
{
    public AI_Character Caster { get; private set; }
    public AI_Character Target { get; private set; }
    public EActionCode ActionCode { get; private set; }
    public float CastTime { get; private set; }
    public float CastDistance { get; private set; }
    public float CastAngleForCaster { get; private set; }
    public float CastAngleForTarget { get; private set; }
    public float DamageDealt { get; private set; }
    public float DamageTaken { get; private set; }
    public EActionCode TargetActionCode { get; private set; }
    public float TargetActionAdvantage { get; private set; }
    public bool Success { get; private set; }

    public AI_Action(EActionCode action_code, AI_Character caster, AI_Character target)
    {
        this.ActionCode = action_code;
        this.Caster = caster;
        this.Target = target;
        this.Success = false;
    }

    public void OnActionStarted(float game_time)
    {
        this.CastTime = game_time;
        this.DamageDealt = 0.0f;
        this.DamageTaken = 0.0f;
        if (this.Target != null)
        {
            this.CastDistance = Vector3.Distance(this.Caster.Position, this.Target.Position);
            this.CastAngleForCaster = Vector3.Angle(this.Caster.Direction, this.Target.Position - this.Caster.Position);
            this.CastAngleForTarget = Vector3.Angle(this.Target.Direction, this.Caster.Position - this.Target.Position);
            if (this.Target.CurrentAction != null)
            {
                this.TargetActionCode = this.Target.CurrentAction.ActionCode;
                this.TargetActionAdvantage = this.Target.CurrentAction.CastTime;
            }
            else
            {
                this.TargetActionCode = EActionCode.ActionCodeNoAction;
                this.TargetActionAdvantage = 0.0f;
            }
        }
    }

    public void OnActionFinished(float game_time)
    {
        // Nothing to do for now
    }

    public void OnActionDamageDealt(float damage_dealt)
    {
        this.DamageDealt += damage_dealt;
        this.Success = true;
    }

    public void OnActionDamageTaken(float damage_taken)
    {
        this.DamageTaken += damage_taken;
    }

    public void OnTargetActionStarted(AI_Action action)
    {
        if (this.Target != null && this.Target.CharacterID == action.Caster.CharacterID && (this.TargetActionCode == EActionCode.ActionCodeNoAction))
        {
            this.TargetActionCode = action.ActionCode;
            this.TargetActionAdvantage = this.CastTime - action.CastTime;

            switch (action.ActionCode)
            {
                case EActionCode.ActionCodeGuard:
                case EActionCode.ActionCodeRoll:
                    this.Success = true;
                    break;
            }
        }
    }
}

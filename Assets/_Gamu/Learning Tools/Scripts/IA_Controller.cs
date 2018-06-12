using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IA_Controller : ICharacter_Controller{
        
    public Transform target;

    protected NavMeshAgent agent;    
    protected MyStateMachine fsm;

    public float Weapon_range { get; set; }
    public float HeavyAttack { get; set; }
    public float WeakAttack { get; set; }
    public int Munitions { get; set; }

    public float PlayerAnimationTimeLeft { get; set; }

    private float weapon_range_off;
    private Animator playerAnim;    

    // Update is called once per frame
    void Update ()
    {
        UpdatePlayerAnimationTimeLeft();
        if (!IsBusy())
            fsm.FireStateMachine();        
    }

    void Start()
    {
        Init();
        Munitions = 3;
        weapon_range_off = (3 / 4) * Weapon_range;
        RuntimeAnimatorController ac = Anim.runtimeAnimatorController;    //Get Animator controller
        for(int i = 0; i<ac.animationClips.Length; i++)                 //For all Animations
        {
            if(ac.animationClips[i].name == "WeakAttack")        //If it has the same name as your clip
            {
                WeakAttack = ac.animationClips[i].length;
            }
            else if (ac.animationClips[i].name == "HeavyAttack")        //If it has the same name as your clip
            {
                HeavyAttack = ac.animationClips[i].length;
            }
        }

        agent = GetComponent<NavMeshAgent>();
        playerAnim = GameObject.FindWithTag("Player").GetComponent<Animator>();

        fsm = new MyStateMachine(this);
        
        fsm.States.Add(new StateChase());
        fsm.States.Add(new StateMeleeAtk());
        fsm.States.Add(new StateDistAtk());
        fsm.States.Add(new StateReact());
        fsm.States.Add(new StateDash());

        fsm.Initialize(State.eStates.Chase);
    }

    public bool IsBusy()
    {
        return Anim.GetBool("AtkMelee") || Anim.GetBool("AtkDistance") || Anim.GetBool("Dashing") || Anim.GetBool("TakeDamage");
    }

    public void UpdatePlayerAnimationTimeLeft()
    {
        var state = playerAnim.GetCurrentAnimatorStateInfo(atkLayer);
        var t = state.length - Mathf.Floor(state.length);
        var l = state.normalizedTime;
        PlayerAnimationTimeLeft = l * t;
    }

    public float GetDistance()
    {
        return Vector3.Distance(target.position, agent.transform.position);
    }

    public bool IsInRange()
    {
        if(GetDistance() <= weapon_range_off)
        {
            return true;
        }
        return false;
    }

    public void GetInRange()
    {
        while (GetDistance() <= weapon_range_off) //l'offset permet de ne pas être à la limite de la range avant d'attaquer.
        {
            agent.destination = target.position;
        }
    }    
}

public class MyStateMachine : MonoBehaviour
{
    State.eStates curState;  
   
    IA_Controller c;
    
    public MyStateMachine(IA_Controller c)
    {
        this.c = c;
    }

    public State.eStates CurState
    {
        get
        {
            return curState;
        }
        set
        {
            if(value!=curState)
               curState = value;
        }
    }

    public List<State> States { set; get; }

    public void Initialize(State.eStates start)
    {
        CurState = start;
    }

    public void FireStateMachine()
    {
        CurState = States[(int)CurState].Execute(c); //Les états sont listés dans le même ordre que dans l'enum State.eStates
    }
}

public abstract class State : MonoBehaviour
{
    public enum eStates { Chase, MeleeAtk, DistAtk, React, Dash }
    public abstract eStates Execute(IA_Controller c);
}

public class StateChase : State
{
    public override eStates Execute(IA_Controller c)
    {
        c.GetInRange();
        return eStates.React;
    }
}

public class StateMeleeAtk : State
{
    public override eStates Execute(IA_Controller c)
    {
        if (c.PlayerAnimationTimeLeft >= c.HeavyAttack)
            c.Anim.SetTrigger("HeavyAttack");
        else if (c.PlayerAnimationTimeLeft >= c.WeakAttack)      
            c.Anim.SetTrigger("WeakAttack");
        return eStates.React;
    }
}

public class StateDistAtk : State
{
    public override eStates Execute(IA_Controller c)
    {
        c.Anim.SetTrigger("WeakDistance");
        return eStates.React;
    }
}

public class StateReact : State
{
    public override eStates Execute(IA_Controller c)
    {
        if (c.IsInRange())
        {
            if (c.PlayerAnimationTimeLeft >= c.WeakAttack)
                return eStates.MeleeAtk;
            else
                return eStates.Dash;
        }
        else
        {
            if (c.Munitions > 0)
                return eStates.DistAtk;
            else
                return eStates.Chase;
        }
    }
}

public class StateDash : State
{ 
    public override eStates Execute(IA_Controller c)
    {
        c.Anim.SetTrigger("Dash");
        return eStates.React;
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// HOW TO USE:
///  - No instantiation, no initialization, only use static methods. 
///  - Call AI_Params.PushEvent(...) for each event (action started/released/finished, damage dealt, maybe more?). 
///  - Call AI_Params.PushCharacter(...) for each character. Do this right before sending (so it contains the most up-to-date info). 
///  - Call AI_Params.Send(...) to send the information to the AI thread. 
/// </summary>
public class AI_Params
{
    /// <summary>
    /// Constant representing the ID of the player. 
    /// </summary>
    public static readonly int PlayerID = -1;

    /// <summary>
    /// Constant representing an undetermined character ID.
    /// </summary>
    public static readonly int UnknownID = -2;

    /// <summary>
    /// [Game thread] Adds information about the current status of a character.
    /// </summary>
    /// <param name="id">ID of the character</param>
    /// <param name="type">Type of the character (refer to the ECharacterType enum for valid character types)</param>
    /// <param name="position">Current position of the character</param>
    /// <param name="direction">Current direction the character is facing</param>
    /// <param name="velocity">Current velocity of the character</param>
    /// <param name="visible">Is the character visible for the AI? (optional, true by default)</param>
    public static void PushCharacter(int id, ECharacterType type, Vector3 position, Vector3 direction, Vector3 velocity, bool visible)
    {
        Instance.Characters.Add(new AI_ParamCharacter(id, type, position, direction, velocity, visible));
    }
    public static void PushCharacter(int id, ECharacterType type, Vector3 position, Vector3 direction, Vector3 velocity)
    {
        PushCharacter(id, type, position, direction, velocity, true);
    }

    /// <summary>
    /// [Game thread] Adds information about an event that occured since the last update. 
    /// </summary>
    /// <param name="type">Type of event (refer to the EEventType enum for valid event types)</param>
    /// <param name="caster_id">ID of the caster (optional, AI_Params.UnknownID if undetermined)</param>
    /// <param name="target_id">ID of the target (optional, AI_Params.UnknownID if undetermined)</param>
    /// <param name="action_code">Code of the action (optional, refer to the EActionCode enum for valid action codes)</param>
    /// <param name="damage">Damage dealt (optional)</param>
    public static void PushEvent(EEventType type, int caster_id, int target_id, EActionCode action_code, float damage)
    {
        Instance.Events.Add(new AI_ParamEvent(type, caster_id, target_id, action_code, damage));
    }
    public static void PushEvent(EEventType type, int caster_id, int target_id, EActionCode action_code)
    {
        PushEvent(type, caster_id, target_id, action_code, 0.0f);
    }
    public static void PushEvent(EEventType type, int caster_id, int target_id)
    {
        PushEvent(type, caster_id, target_id, EActionCode.ActionCodeNoAction);
    }
    public static void PushEvent(EEventType type, int caster_id)
    {
        PushEvent(type, caster_id, AI_Params.UnknownID);
    }
    public static void PushEvent(EEventType type)
    {
        PushEvent(type, AI_Params.UnknownID);
    }

    /// <summary>
    /// [Game thread] Clears the current parameters. Called automatically by Send(). 
    /// </summary>
    public static void Clear()
    {
        Instance = new AI_Params();
    }

    /// <summary>
    /// [Game thread] Sends the parameters to the AI thread. 
    /// </summary>
    /// <param name="game_time">Current in-game time (in seconds)</param>
    public static void Send(float game_time)
    {
        Instance.CurrentGameTime = game_time;
        lock (ThreadQueue) { ThreadQueue.Enqueue(Instance); }
        ThreadSignal.Set(); // Waking up the Get() function
        Clear(); // Resetting the instance for next update
    }

    /// <summary>
    /// [AI thread] Retrieves the last parameters sent  to the AI thread.
    /// </summary>
    /// <returns></returns>
    public static AI_Params Get()
    {
        AI_Params p;
        ThreadSignal.WaitOne(); // Waiting for a Send() call
        lock (ThreadQueue) { p = ThreadQueue.Dequeue(); }
        if (ThreadQueue.Count == 0)
        {
            ThreadSignal.Reset(); // Going to sleep if nothing else in the queue
        }
        return p;
    }



    /// Multi-threading variables (private)
    private static AI_Params Instance = new AI_Params();
    private static readonly Queue<AI_Params> ThreadQueue = new Queue<AI_Params>();
    private static readonly ManualResetEvent ThreadSignal = new ManualResetEvent(false);



    /// The actual AI_Params object (private)
    public float CurrentGameTime { get; private set; }
    public List<AI_ParamCharacter> Characters { get; private set; }
    public List<AI_ParamEvent> Events { get; private set; }

    private AI_Params()
    {
        this.Characters = new List<AI_ParamCharacter>();
        this.Events = new List<AI_ParamEvent>();
    }

    public class AI_ParamCharacter
    {
        public int CharacterID { get; private set; }
        public ECharacterType CharacterType { get; private set; }
        public Vector3 Direction { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool VisibleByAI { get; private set; }

        public AI_ParamCharacter(int id, ECharacterType type, Vector3 position, Vector3 direction, Vector3 velocity, bool visible)
        {
            this.CharacterID = id;
            this.CharacterType = type;
            this.Direction = direction;
            this.Position = position;
            this.Velocity = velocity;
            this.VisibleByAI = visible;
        }
    }

    public class AI_ParamEvent
    {
        public EEventType EventType { get; private set; }
        public int CasterID { get; private set; }
        public int TargetID { get; private set; }
        public EActionCode ActionCode { get; private set; }
        public float Damage { get; private set; } // ignored for events other than EventTypeDamageDealt

        public AI_ParamEvent(EEventType type, int caster_id, int target_id, EActionCode action_code, float damage)
        {
            this.ActionCode = action_code;
            this.EventType = type;
            this.CasterID = caster_id;
            this.TargetID = target_id;
            this.Damage = damage;
        }
    }
}

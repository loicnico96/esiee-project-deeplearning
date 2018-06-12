using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

public class AI_DeepLearningSystem
{
    public bool IsRunning { get; private set; }
    public float LastUpdateTime { get; private set; }

    private Dictionary<int, AI_Character> Characters;
    private Dictionary<EActionCode, AI_DeepLearningNetwork> Networks_Action;                                // Is this action going to be effective ?
    private Dictionary<EActionCode, Dictionary<EActionCode, AI_DeepLearningNetwork>> Networks_Reaction;     // Is this action going to beat the player's action ?
    private Dictionary<EActionCode, AI_DeepLearningNetwork> Networks_Anticipation_1;                        // What action is the player going to do in this situation ? (from player's current/previous action)
    private Dictionary<EActionCode, AI_DeepLearningNetwork> Networks_Anticipation_2;                        // What action is the player going to do in this situation ? (from AI's action action)
    private EActionCode Anticipation_ActionCode;
    private float Anticipation_ActionTime;
    private float Anticipation_Probability;
    private static string NetworkDirectory = "AI_Network";

    public AI_DeepLearningSystem()
    {
        this.IsRunning = false;
        this.LastUpdateTime = 0.0f;
        this.Characters = new Dictionary<int, AI_Character>();
        this.Anticipation_ActionCode = EActionCode.ActionCodeNoAction;
        this.Anticipation_ActionTime = 0.0f;
        this.Anticipation_Probability = 0.0f;
        this.LoadNetworkState();
    }

    /// <summary>
    /// Starts the AI process, waiting for updates and saving network state upon exiting
    /// </summary>
    public void Run()
    {
        this.IsRunning = true;
        while (this.IsRunning)
        {
            Debug.Log("[AI] Waiting for update...");
            AI_Params p = AI_Params.Get();
            Debug.Log("[AI] Update received (at time " + p.CurrentGameTime + "s).");
            this.LastUpdateTime = p.CurrentGameTime;
            this.UpdateCharacters(p);
            this.UpdateEvents(p);
            this.ThinkAnticipation();
            this.ThinkAction();
        }
        Debug.Log("[AI] Terminating...");
        this.SaveNetworkState();
    }

    /// <summary>
    /// Update all characters.
    /// </summary>
    /// <param name="p">Update parameters</param>
    public void UpdateCharacters(AI_Params p)
    {
        foreach (AI_Params.AI_ParamCharacter c in p.Characters)
        {
            // Create a new character if ID doesn't already exist
            if (!this.Characters.ContainsKey(c.CharacterID))
            {
                Debug.Log("Adding new character (Character ID: " + c.CharacterID + ").");
                this.Characters[c.CharacterID] = new AI_Character(c.CharacterID, c.CharacterType);
            }

            // Update character information
            AI_Character character = this.Characters[c.CharacterID];
            character.OnSeenByAI(this.LastUpdateTime, c.Position, c.Direction, c.Velocity);
        }
    }

    /// <summary>
    /// Update all events. 
    /// </summary>
    /// <param name="p">Update parameters</param>
    public void UpdateEvents(AI_Params p)
    {
        for (EEventType t = 0; t < EEventType.Count; t++)
        {
            foreach (AI_Params.AI_ParamEvent e in p.Events)
            {
                if (e.EventType == t)
                {
                    switch (e.EventType)
                    {
                        // A character started an action
                        case EEventType.EventTypeActionStarted:
                            Debug.Assert(this.Characters.ContainsKey(e.CasterID), "[AI] Invalid caster ID (" + e.CasterID + ").");
                            this.OnCharacterActionStarted(this.Characters[e.CasterID], e.ActionCode);
                            break;
                        // A character finished an action
                        case EEventType.EventTypeActionFinished:
                            Debug.Assert(this.Characters.ContainsKey(e.CasterID), "[AI] Invalid caster ID (" + e.CasterID + ").");
                            this.OnCharacterActionFinished(this.Characters[e.CasterID]);
                            break;
                        // A character took damage
                        case EEventType.EventTypeDamageDealt:
                            Debug.Assert(this.Characters.ContainsKey(e.CasterID), "[AI] Invalid caster ID (" + e.CasterID + ").");
                            Debug.Assert(this.Characters.ContainsKey(e.TargetID), "[AI] Invalid target ID (" + e.TargetID + ").");
                            this.OnCharacterDamageDealt(this.Characters[e.CasterID], this.Characters[e.TargetID], e.Damage);
                            break;
                        // A character died
                        case EEventType.EventTypeCharacterDeath:
                            Debug.Assert(this.Characters.ContainsKey(e.CasterID), "[AI] Invalid caster ID (" + e.CasterID + ").");
                            Debug.Assert(this.Characters.ContainsKey(e.TargetID), "[AI] Invalid target ID (" + e.TargetID + ").");
                            this.OnCharacterDeath(this.Characters[e.TargetID]);
                            break;
                        // The game session ended
                        case EEventType.EventTypeGameEnd:
                            this.IsRunning = false;
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when a character starts a new action.
    /// </summary>
    /// <param name="caster">Caster</param>
    /// <param name="action_code">Code of the action (see EActionCode)</param>
    public void OnCharacterActionStarted(AI_Character caster, EActionCode action_code)
    {
        Debug.Log("[AI] Action started (Caster ID: " + caster.CharacterID + ", Action Code: " + action_code + ").");
        AI_Character target = null;

        // Determining the target
        switch (caster.CharacterType)
        {
            case ECharacterType.CharacterTypeEnemy:
                Debug.Assert(this.Characters.ContainsKey(AI_Params.PlayerID), "[AI] Invalid player ID (" + AI_Params.PlayerID + ").");
                target = this.Characters[AI_Params.PlayerID];
                break;
            case ECharacterType.CharacterTypePlayer:
                float distance = 10000.0f;
                foreach (AI_Character c in this.Characters.Values)
                {
                    if (distance > Vector3.Distance(c.Position, caster.Position) + Mathf.Abs(Vector3.Angle(caster.Direction, c.Position - caster.Position)))
                    {
                        distance = Vector3.Distance(c.Position, caster.Position) + Mathf.Abs(Vector3.Angle(caster.Direction, c.Position - caster.Position));
                        target = c;
                    }
                }
                break;
        }

        // Registering the action
        AI_Action action = caster.OnActionStarted(this.LastUpdateTime, action_code, target);
        foreach (AI_Character c in this.Characters.Values)
        {
            if ((c.CurrentAction != null) && (c.CurrentAction.Target.CharacterID == caster.CharacterID))
            {
                c.CurrentAction.OnTargetActionStarted(action);
            }
        }

        // Training anticipation network
        if (caster.CharacterType == ECharacterType.CharacterTypePlayer)
        {
            this.TrainAnticipation(caster, action_code);
        }
    }

    /// <summary>
    /// Called when a character finishes its current action.
    /// </summary>
    /// <param name="caster">Caster</param>
    public void OnCharacterActionFinished(AI_Character caster)
    {
        Debug.Log("[AI] Action finished (Caster ID: " + caster.CharacterID + ").");
        AI_Action action = caster.OnActionFinished(this.LastUpdateTime);
        this.TrainAction(action);
    }

    /// <summary>
    /// Called when a character takes damage.
    /// </summary>
    /// <param name="caster">Caster</param>
    /// <param name="target">Target</param>
    /// <param name="damage">Damage dealt</param>
    public void OnCharacterDamageDealt(AI_Character caster, AI_Character target, float damage)
    {
        Debug.Log("[AI] Damage dealt (Caster ID: " + caster.CharacterID + ", Target ID: " + target.CharacterID + ", Damage: " + damage + ").");
        caster.OnDamageDealt(target, damage);
        target.OnDamageTaken(caster, damage);
    }

    /// <summary>
    /// Called when a character dies.
    /// </summary>
    /// <param name="caster">Character</param>
    public void OnCharacterDeath(AI_Character character)
    {
        Debug.Log("[AI] Character died (Character ID: " + character.CharacterID + ").");
        character.OnDeath();
    }

    /// <summary>
    /// Loads all neural networks. 
    /// </summary>
    public void LoadNetworkState()
    {
        Debug.Log("[AI] Loading network state...");

        // Loading network - action
        this.Networks_Action = new Dictionary<EActionCode, AI_DeepLearningNetwork>();
        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
        {
            this.Networks_Action[code] = new AI_DeepLearningNetwork("AI_Action_" + code, new float[3, 2] {
                { 0.0f, 10000.0f },             // Input 1: Distance to player
                { -180.0f, +180.0f },           // Input 2: Angle to player from caster
                { -180.0f, +180.0f }            // Input 3: Angle to caster from player
            }, new float[1, 2] {
                { 0.0f, 1.0f }                  // Output 1: Probability of hitting
            });
        }

        // Loading network - reaction
        this.Networks_Reaction = new Dictionary<EActionCode, Dictionary<EActionCode, AI_DeepLearningNetwork>>();
        for (EActionCode code1 = EActionCode.ActionCodeNoAction; code1 < EActionCode.Count; code1++)
        {
            this.Networks_Reaction[code1] = new Dictionary<EActionCode, AI_DeepLearningNetwork>();
            for (EActionCode code2 = EActionCode.ActionCodeNoAction; code2 < EActionCode.Count; code2++)
            {
                this.Networks_Reaction[code1][code2] = new AI_DeepLearningNetwork("AI_Reaction_" + code1 + "_" + code2, new float[4, 2] {
                    { 0.0f, 10000.0f },         // Input 1: Distance to player
                    { -180.0f, +180.0f },       // Input 2: Angle to caster from player
                    { -180.0f, +180.0f },       // Input 3: Angle to caster from player
                    { -3.0f, +3.0f }            // Input 4: Time advantage for player
                }, new float[2, 2] {
                    { -3.0f, +3.0f },           // Output 1: Average damage dealt
                    { -3.0f, +3.0f }            // Output 2: Average damage taken
                });
            }
        }

        // Loading network - reaction - roll direction



        // Loading network - anticipation
        float[,] outputs = new float[(int)EActionCode.Count, 2];
        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
        {
            outputs[(int)code, 0] = 0.0f;
            outputs[(int)code, 1] = 1.0f;
        }
        this.Networks_Anticipation_1 = new Dictionary<EActionCode, AI_DeepLearningNetwork>();
        this.Networks_Anticipation_2 = new Dictionary<EActionCode, AI_DeepLearningNetwork>();
        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
        {
            this.Networks_Anticipation_1[code] = new AI_DeepLearningNetwork("AI_Anticipation_2_" + code, new float[4, 2] {
                { 0.0f, 10000.0f },             // Input 1: Distance to player
                { -180.0f, +180.0f },           // Input 2: Angle to caster from player
                { -180.0f, +180.0f },           // Input 3: Angle to player from caster
                { -3.0f, +3.0f }                // Input 4: Time since last action started
            }, outputs);                        // Output 1: Probability of hitting
            this.Networks_Anticipation_2[code] = new AI_DeepLearningNetwork("AI_Anticipation_1_" + code, new float[4, 2] {
                { 0.0f, 10000.0f },             // Input 1: Distance to player
                { -180.0f, +180.0f },           // Input 2: Angle to caster from player
                { -180.0f, +180.0f },           // Input 3: Angle to player from caster
                { -3.0f, +3.0f }                // Input 4: Caster action advantage
            }, outputs);                        // Output 1: Probability of hitting
        }

        Debug.Log("[AI] Loading network state - Done.");
    }

    /// <summary>
    /// Saves the state of all neural networks. 
    /// </summary>
    public void SaveNetworkState()
    {
        Debug.Log("[AI] Saving network state...");

        // Creating directory
        try
        {
            System.IO.Directory.CreateDirectory(NetworkDirectory);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[System] Error creating directory: " + ex);
        }

        // Saving network - action
        foreach (AI_DeepLearningNetwork n in this.Networks_Action.Values)
        {
            if (n.Samples > 0)
            {
                Debug.Log("[AI] Saving " + n.Filename + "...");
                n.Save(NetworkDirectory);
            }
        }

        // Saving network - reaction
        foreach (Dictionary<EActionCode, AI_DeepLearningNetwork> dict in this.Networks_Reaction.Values)
        {
            foreach (AI_DeepLearningNetwork n in dict.Values)
            {
                if (n.Samples > 0)
                {
                    Debug.Log("[AI] Saving " + n.Filename + "...");
                    n.Save(NetworkDirectory);
                }
            }
        }

        // Saving network - anticipation
        foreach (AI_DeepLearningNetwork n in this.Networks_Anticipation_1.Values)
        {
            if (n.Samples > 0)
            {
                Debug.Log("[AI] Saving " + n.Filename + "...");
                n.Save(NetworkDirectory);
            }
        }
        foreach (AI_DeepLearningNetwork n in this.Networks_Anticipation_2.Values)
        {
            if (n.Samples > 0)
            {
                Debug.Log("[AI] Saving " + n.Filename + "...");
                n.Save(NetworkDirectory);
            }
        }

        Debug.Log("[AI] Saving network state - Done.");
    }

    /// <summary>
    /// Trains the anticipation network.
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="action_code">Code of the action performed</param>
    public void TrainAnticipation(AI_Character player, EActionCode action_code)
    {
        AI_DeepLearningNetwork network;
        float[] outputs = new float[(int)EActionCode.Count];
        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
        {
            outputs[(int)code] = (code == action_code) ? 1.0f : 0.0f;
        }

        foreach (AI_Character c in this.Characters.Values)
        {
            if (c.CharacterType == ECharacterType.CharacterTypeEnemy && c.IsAlive)
            {
                if (player.PreviousAction != null)
                {
                    network = this.Networks_Anticipation_1[player.PreviousAction.ActionCode];
                    network.Learn(new float[] {
                        Vector3.Distance(c.Position, player.Position),
                        Vector3.Angle(player.Direction, c.Position - player.Position),
                        Vector3.Angle(c.Direction, player.Position - c.Position),
                        this.LastUpdateTime - player.PreviousAction.CastTime
                    }, outputs);
                }
                if (c.CurrentAction != null)
                {
                    network = this.Networks_Anticipation_2[c.CurrentAction.ActionCode];
                    network.Learn(new float[] {
                        Vector3.Distance(c.Position, player.Position),
                        Vector3.Angle(player.Direction, c.Position - player.Position),
                        Vector3.Angle(c.Direction, player.Position - c.Position),
                        this.LastUpdateTime - c.CurrentAction.CastTime
                    }, outputs);
                }
                else
                {
                    network = this.Networks_Anticipation_2[EActionCode.ActionCodeNoAction];
                    network.Learn(new float[] {
                        Vector3.Distance(c.Position, player.Position),
                        Vector3.Angle(player.Direction, c.Position - player.Position),
                        Vector3.Angle(c.Direction, player.Position - c.Position),
                        this.LastUpdateTime - c.CurrentAction.CastTime
                    }, outputs);
                }
            }
        }
    }

    /// <summary>
    /// Thinks the next player action. 
    /// </summary>
    public void ThinkAnticipation()
    {
        Debug.Assert(this.Characters.ContainsKey(AI_Params.PlayerID), "[AI] Invalid player ID (" + AI_Params.PlayerID + ").");
        AI_Character player = this.Characters[AI_Params.PlayerID];
        bool changed = false;

        // Training guard
        if ((player.CurrentAction != null) && (player.CurrentAction.ActionCode == EActionCode.ActionCodeGuard))
        {
            this.TrainAnticipation(player, EActionCode.ActionCodeGuard);
        }

        // Training idle
        if ((player.CurrentAction == null) || (player.CurrentAction.ActionCode == EActionCode.ActionCodeNoAction))
        {
            this.TrainAnticipation(player, EActionCode.ActionCodeNoAction);
        }

        // Decaying anticipation probability
        if (this.LastUpdateTime > this.Anticipation_ActionTime)
        {
            this.Anticipation_Probability -= this.LastUpdateTime - this.Anticipation_ActionTime;
            this.Anticipation_ActionTime = this.LastUpdateTime;
            // Reset anticipation after a while
            if (this.Anticipation_Probability < 0.0f)
            {
                this.Anticipation_Probability = 0.0f;
                this.Anticipation_ActionCode = EActionCode.ActionCodeNoAction;
            }
        }

        float[] outputs;
        AI_DeepLearningNetwork network;
        // Trying different anticipation timings
        for (float t = 0.1f; t <= 1.0f; t += 0.1f)
        {

            // We will add the anticipation results from each AI for each action in this array
            float[] actions = new float[(int)EActionCode.Count];
            for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
            {
                actions[(int)code] = 0.0f;
            }

            // Each AI character participates in the anticipation
            foreach (AI_Character c in this.Characters.Values)
            {
                if (c.CharacterType == ECharacterType.CharacterTypeEnemy && c.IsAlive)
                {

                    // Anticipation from current/previous player action
                    if (player.CurrentAction != null)
                    {
                        network = this.Networks_Anticipation_1[player.CurrentAction.ActionCode];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.PositionIn(t), player.PositionIn(t)),
                            Vector3.Angle(player.Direction, c.PositionIn(t) - player.PositionIn(t)),
                            Vector3.Angle(c.Direction, player.PositionIn(t) - c.PositionIn(t)),
                            t + this.LastUpdateTime - player.CurrentAction.CastTime
                        }, network.Variance);
                        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                        {
                            actions[(int)code] += outputs[(int)code];
                        }
                    }
                    else if (player.PreviousAction != null)
                    {
                        network = this.Networks_Anticipation_1[player.PreviousAction.ActionCode];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.PositionIn(t), player.PositionIn(t)),
                            Vector3.Angle(player.Direction, c.PositionIn(t) - player.PositionIn(t)),
                            Vector3.Angle(c.Direction, player.PositionIn(t) - c.PositionIn(t)),
                            t + this.LastUpdateTime - player.PreviousAction.CastTime
                        }, network.Variance);
                        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                        {
                            actions[(int)code] += outputs[(int)code];
                        }
                    }

                    // Anticipation from current AI action
                    if (c.CurrentAction != null)
                    {
                        network = this.Networks_Anticipation_2[c.CurrentAction.ActionCode];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.PositionIn(t), player.PositionIn(t)),
                            Vector3.Angle(player.Direction, c.PositionIn(t) - player.PositionIn(t)),
                            Vector3.Angle(c.Direction, player.PositionIn(t) - c.PositionIn(t)),
                            t + this.LastUpdateTime - c.CurrentAction.CastTime
                        }, network.Variance);
                        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                        {
                            actions[(int)code] += outputs[(int)code];
                        }
                    }
                    else
                    {
                        network = this.Networks_Anticipation_2[EActionCode.ActionCodeNoAction];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.PositionIn(t), player.PositionIn(t)),
                            Vector3.Angle(player.Direction, c.PositionIn(t) - player.PositionIn(t)),
                            Vector3.Angle(c.Direction, player.PositionIn(t) - c.PositionIn(t)),
                            t
                        }, network.Variance);
                        for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                        {
                            actions[(int)code] += outputs[(int)code];
                        }
                    }
                }
            }

            // Choosing the most likely action
            for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
            {
                if (this.Anticipation_Probability < actions[(int)code])
                {
                    this.Anticipation_Probability = actions[(int)code];
                    this.Anticipation_ActionCode = code;
                    this.Anticipation_ActionTime = this.LastUpdateTime + t;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            Debug.Log("[AI] Anticipating player action " + this.Anticipation_ActionCode + " in " + (this.Anticipation_ActionTime - this.LastUpdateTime) + "s.");
        }
    }

    /// <summary>
    /// Trains the action/reaction network. 
    /// </summary>
    /// <param name="action">Action performed</param>
    public void TrainAction(AI_Action action)
    {
        AI_DeepLearningNetwork network;

        // Training network - action
        network = this.Networks_Action[action.ActionCode];
        network.Learn(new float[] {
            action.CastDistance,
            action.CastAngleForCaster,
            action.CastAngleForTarget
        }, new float[] {
            action.Success ? 1.0f : 0.0f
        });

        // Training network - reaction
        network = this.Networks_Reaction[action.ActionCode][action.TargetActionCode];
        network.Learn(new float[] {
            action.CastDistance,
            action.CastAngleForCaster,
            action.CastAngleForTarget,
            action.TargetActionAdvantage
        }, new float[] {
            action.DamageDealt,
            action.DamageTaken
        });
    }

    /// <summary>
    /// Thinks the orders for AI characters. 
    /// </summary>
    public void ThinkAction()
    {
        Debug.Assert(this.Characters.ContainsKey(AI_Params.PlayerID), "[AI] Invalid player ID (" + AI_Params.PlayerID + ").");
        AI_Character player = this.Characters[AI_Params.PlayerID];

        foreach (AI_Character c in this.Characters.Values)
        {
            if (!c.IsPlayer && c.IsAlive && c.IsIdle)
            {
                AI_DeepLearningNetwork network;
                float[] outputs;

                float[] actions = new float[(int)EActionCode.Count];
                for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                {
                    actions[(int)code] = 0.0f;
                }

                for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                {
                    // Action
                    network = this.Networks_Action[code];
                    outputs = network.Think(new float[] {
                        Vector3.Distance(c.Position, player.Position),
                        Vector3.Angle(c.Direction, player.Position - c.Position),
                        Vector3.Angle(player.Direction, c.Position - player.Position)
                    }, network.Variance);
                    actions[(int)code] = outputs[0];

                    // Reaction
                    if (player.CurrentAction != null)
                    {
                        network = this.Networks_Reaction[code][player.CurrentAction.ActionCode];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.Position, player.Position),
                            Vector3.Angle(c.Direction, player.Position - c.Position),
                            Vector3.Angle(player.Direction, c.Position - player.Position),
                            this.LastUpdateTime - player.CurrentAction.CastTime
                        }, network.Variance);
                        actions[(int)code] = actions[(int)code] * outputs[0] - outputs[1];
                    }
                    else
                    {
                        network = this.Networks_Reaction[code][this.Anticipation_ActionCode];
                        outputs = network.Think(new float[] {
                            Vector3.Distance(c.Position, player.Position),
                            Vector3.Angle(c.Direction, player.Position - c.Position),
                            Vector3.Angle(player.Direction, c.Position - player.Position),
                            this.LastUpdateTime - this.Anticipation_ActionTime
                        }, network.Variance);
                        actions[(int)code] = actions[(int)code] * outputs[0] - outputs[1];
                    }
                }

                // Choosing the best action
                float best_action_value = 0.0f;
                EActionCode best_action_code = EActionCode.ActionCodeNoAction;
                for (EActionCode code = EActionCode.ActionCodeNoAction; code < EActionCode.Count; code++)
                {
                    if (best_action_value < actions[(int)code])
                    {
                        best_action_value = actions[(int)code];
                        best_action_code = code;
                    }
                }

                // Setting the new order
                switch (best_action_code)
                {
                    case EActionCode.ActionCodeAttackLight:
                    case EActionCode.ActionCodeAttackHeavy:
                    case EActionCode.ActionCodeGuard:
                        Debug.Log("[AI] Set order for character " + c.CharacterID + " : " + best_action_code + ".");
                        AI_Orders.SetOrderForCharacter(c.CharacterID, best_action_code, c.Position, Vector3.Normalize(player.Position - c.Position));
                        break;
                    case EActionCode.ActionCodeRoll:
                        Debug.Log("[AI] Set order for character " + c.CharacterID + " : " + best_action_code + ".");
                        float left_or_right = (UnityEngine.Random.value >= 0.5) ? 1.0f : -1.0f;
                        Vector3 direction_player = Vector3.Normalize(player.Position - c.Position);
                        Vector3 direction_roll = new Vector3(-direction_player.x, direction_player.y, direction_player.z) * left_or_right;
                        AI_Orders.SetOrderForCharacter(c.CharacterID, best_action_code, c.Position, direction_roll);
                        break;
                    case EActionCode.ActionCodeNoAction:
                        if (Vector3.Distance(player.Position, c.Position) < 100.0f)
                        {
                            Debug.Log("[AI] Set order for character " + c.CharacterID + " : " + best_action_code + " (stay in position).");
                            AI_Orders.SetOrderForCharacter(c.CharacterID, EActionCode.ActionCodeNoAction, c.Position, Vector3.Normalize(player.Position - c.Position));
                        }
                        else
                        {
                            Debug.Log("[AI] Set order for character " + c.CharacterID + " : " + best_action_code + " (move towards player).");
                            AI_Orders.SetOrderForCharacter(c.CharacterID, EActionCode.ActionCodeNoAction, player.Position, Vector3.Normalize(player.Position - c.Position));
                        }
                        break;
                }
            }
        }
    }
}


using System.Threading;
using UnityEngine;

/// <summary>
/// Script créant et appelant le système d'IA deep learning
/// Ajouter simplement ce script (et seulement une fois) à un élément présent dans la scène. Il est sur la caméra pour l'instant.
/// </summary>
public class AI_DeepLearning : MonoBehaviour
{
    private static int InstanceCount = 0; // Make sure we don't have more than 1
    private AI_DeepLearningSystem AISystem;
    private Thread AIThread;

    public void Start()
    {
        Debug.Log("[Game] Launching AI thread...");
        InstanceCount++;
        if (InstanceCount < 2)
        {
            AIThread = new Thread(() => {
                Debug.Log("[AI] New thread launched (ID: " + Thread.CurrentThread.ManagedThreadId + ").");
                AISystem = new AI_DeepLearningSystem();
                AISystem.Run();
            });
            AIThread.Start();
        }
        else
        {
            Debug.LogError("[AI] AI thread already exists. Don't create more than one.");
            MonoBehaviour.Destroy(this);
        }
    }

    public void Update()
    {
        AI_Params.PushCharacter(AI_Params.PlayerID, ECharacterType.CharacterTypePlayer, Vector3.zero, Vector3.zero, Vector3.zero, true);
        //AI_Params.PushEvent(EEventType.EventTypeActionStarted, AI_Params.PlayerID, AI_Params.UnknownID, EActionCode.ActionCodeAttackLight); 
        //if (Time.time > 2.0) AI_Params.PushEvent(EEventType.EventTypeGameEnd); 
        AI_Params.Send(Time.time);
        AI_Order order = AI_Orders.GetOrderForCharacter(AI_Params.PlayerID);
        if (order != null)
            Debug.Log("[Game] Order received (id: " + order.CharacterID + ", code: " + order.ActionCode + ").");
    }

    public void OnDestroy()
    {
        if (AIThread != null)
        {
            AI_Params.PushEvent(EEventType.EventTypeGameEnd);
            AI_Params.Send(Time.time);
            AIThread.Join(); // Waiting for the AI thread to finish saving
            AIThread = null;
        }
        InstanceCount--;
    }
}
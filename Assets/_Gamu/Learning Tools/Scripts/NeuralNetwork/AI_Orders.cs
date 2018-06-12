using UnityEngine;
using UnityEditor;
using System.Collections;

public class AI_Order
{
    public int CharacterID;
    public EActionCode ActionCode;
    public Vector3 Direction;
    public Vector3 Position;
    public AI_Order(int id, EActionCode code, Vector3 position, Vector3 direction)
    {
        this.CharacterID = id;
        this.ActionCode = code;
        this.Direction = direction;
        this.Position = position;
    }
}

public class AI_Orders
{
    private static Hashtable Orders = new Hashtable();

    /// <summary>
    /// Retrieves the current order for a character. The order is then consumed. 
    /// </summary>
    /// <param name="id">ID of the character</param>
    /// <returns>Order to perform</returns>
    public static AI_Order GetOrderForCharacter(int id)
    {
        lock (Orders)
        {
            AI_Order order = (AI_Order)Orders[id];
            Orders[id] = null;
            return order;
        }
    }

    /// <summary>
    /// Gives a new order to a character. Previous orders that were not consumed are overriden. 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="code">Action code</param>
    /// <param name="direction">Target direction of the action (optional)</param>
    /// <param name="position">Target position of the action (optional)</param>
    /// <returns>Order to perform</returns>
    public static AI_Order SetOrderForCharacter(int id, EActionCode code, Vector3 position, Vector3 direction)
    {
        lock (Orders)
        {
            AI_Order order = new AI_Order(id, code, position, direction);
            Orders[id] = order;
            return order;
        }
    }
    public static AI_Order SetOrderForCharacter(int id, EActionCode code, Vector3 position)
    {
        return SetOrderForCharacter(id, code, position, Vector3.zero);
    }
    public static AI_Order SetOrderForCharacter(int id, EActionCode code)
    {
        return SetOrderForCharacter(id, code, Vector3.zero);
    }

    /// <summary>
    /// Checks if a character currently has an order to perform. 
    /// </summary>
    /// <param name="id">ID of the character</param>
    /// <returns>True if an order exists, false otherwise</returns>
    public static bool HasOrderForCharacter(int id)
    {
        lock (Orders)
        {
            AI_Order order = (AI_Order)Orders[id];
            return (order != null);
        }
    }
}
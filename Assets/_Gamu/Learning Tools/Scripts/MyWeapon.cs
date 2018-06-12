using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyWeapon : MonoBehaviour {

    private int damage;
    private Transform transf;
    //list of collision sensors   
    private Vector3 LastPos;

    public int Damage
    {
        get
        {
            return damage;
        }

        set
        {
            damage = value;
        }
    }

    void Start()
    {
        LastPos = transf.position;
    }

    void FixedUpdate()
    {
        var curPos = transf.position;
        CheckRays(LastPos, curPos);
        LastPos = curPos;
    }

    public void CheckRays(Vector3 last, Vector3 cur)
    {
        RaycastHit hit;       
        if (Physics.Raycast(last, cur - last, out hit, 0.2f))
            foreach(string name in ICharacter_Controller.Characters.Keys)
            {
                if(hit.collider.CompareTag(name))
                    ICharacter_Controller.Characters[name].TakeDamage(Damage);
            }                
    }
}

public class MyWeaponSensors : MyWeapon
{
    private List<Vector3> LastPos { get; set; }

    private void Start()
    {
        InitCollisionSensors();
    }
    public void InitCollisionSensors()
    {
        foreach (Transform child in transform)
            LastPos.Add(child.position);
    }

    public void FixedUpdate()
    {
        UpdateCollisionSensors();
    }
    public void UpdateCollisionSensors()
    {
        int i = 0;
        Vector3 pos;
        foreach (Transform child in transform)
        {
            pos = child.position;
            CheckRays(LastPos[i], pos);
            LastPos[i++] = pos;
        }
    }
}

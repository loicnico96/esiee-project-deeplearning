using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ICharacter_Controller : MonoBehaviour
{
    public static Dictionary<string, ICharacter_Controller> Characters = new Dictionary<string, ICharacter_Controller>();

    public GameObject weapon;

    private Animator anim;
    private Rigidbody rb;
    private GameObject bulletPrefab;
    private float health;

    protected int atkLayer;  

    public void Init()
    {
        Anim = GetComponent<Animator>();
        BulletPrefab = GameObject.Find("BulletPrefab");
        Rb = GetComponent<Rigidbody>();
        atkLayer = Anim.GetLayerIndex("Attacks");      
        /*  
        wmHash = Animator.StringToHash("WeakAttack");
        hmHash = Animator.StringToHash("HeavyAttack");
        wdHash = Animator.StringToHash("WeakDistance");
        hdHash = Animator.StringToHash("HeavyDistance");
        */
    }

    public float Health
    {
        get
        {
            return health;
        }

        set
        {
            health = value;
        }
    }

    public Animator Anim
    {
        get
        {
            return anim;
        }

        set
        {
            anim = value;
        }
    }

    public Rigidbody Rb
    {
        get
        {
            return rb;
        }

        set
        {
            rb = value;
        }
    }

    public GameObject BulletPrefab
    {
        get
        {
            return bulletPrefab;
        }

        set
        {
            bulletPrefab = value;
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        if(Health < 0)
        {
            //ded
        }
    }

    public void FireWeakDistance()
    {
        Vector3 pos = Rb.transform.position;
        var bullet = Instantiate(BulletPrefab, Rb.transform.forward, Rb.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6;
        bullet.GetComponent<MyWeapon>().Damage = 1;
        Destroy(bullet, 2.0f);
    }
}
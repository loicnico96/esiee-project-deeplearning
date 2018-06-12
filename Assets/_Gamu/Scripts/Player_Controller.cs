using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : ICharacter_Controller {

    //set in editor
    public Transform target; 

    public float weapon_range;    

    private float weapon_range_off;
    private float speed;

    //vars related to IsBusy()
    private float x;
    private float y;
    private string nextAction;
    private bool isFiring;

    private float heavyCharge;
    private float meleeWeak;
    private float chargeTime;
    private float chargeOver;
    private bool firstFrame;

    //Anim

    // Use this for initialization
    void Start ()
    {
        Init();
        isFiring = false;
        firstFrame = true;
        atkLayer = Anim.GetLayerIndex("Attacks");            
        Rb = GetComponent<Rigidbody>();
        weapon_range = 0.5f;
        weapon_range_off = (3 / 4) * weapon_range;
        speed = 3.0f;

        RuntimeAnimatorController ac = Anim.runtimeAnimatorController;    //Get Anim controller
        for (int i = 0; i < ac.animationClips.Length; i++)                 //For all animations
        {
            if (ac.animationClips[i].name.Equals("WeakAttack"))        //If it has the same name as your clip
            {
                meleeWeak = ac.animationClips[i].length;
            }            
        }

        heavyCharge = 2 * meleeWeak;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!IsBusy()) //si le joueur n'est pas occupé
        {
            UpdateMovement();
            UpdateAction();
            Apply();               
        }        
	}

    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        Rb.velocity = movement * speed * Time.deltaTime;
    }   
    

    public bool IsBusy()
    {
        var b = Anim.GetBool("AtkMelee") || Anim.GetBool("AtkDistance") || Anim.GetBool("Dashing") || Anim.GetBool("TakeDamage");
        Anim.SetBool("Busy", b);
        return b;
    }

    public void UpdateMovement()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");        
    }
    
    public void UpdateAction()
    {
        var xr = Input.GetAxis("X360_RAnalog_X");
        var yr = Input.GetAxis("X360_RAnalog_Y");

        nextAction = "";

        if (Input.GetButtonDown("X360_A"))
        {
            nextAction = "Dash";
        } 
        else if(Input.GetButtonDown("X360_B"))
        {
            nextAction = "Parry";
        }
        else if(Input.GetButtonDown("X360_X"))
        {
            firstFrame = true;
        }
        else if(Input.GetButton("X360_X"))
        {
            float overcharged=0;
            if(firstFrame)
            {
                chargeTime = Time.deltaTime + heavyCharge;
                overcharged = chargeTime + chargeTime / 2;
                firstFrame = false;
            }
            if (Input.GetButtonUp("X360_X") && chargeTime >= Time.deltaTime)
                nextAction = "HeavyAttack";
            else
                nextAction = "WeakAttack";
            if(Time.deltaTime >= overcharged)
            {
                nextAction = "HeavyAttack";
            }
        }

        if ( (System.Math.Abs(xr) >= 0.25f || System.Math.Abs(yr) >= 0.25f) && nextAction.Equals(""))
        {
            //InvokeRepeating("FireWeakDistance", 0.15f, 0.15f);
            //isFiring = true;
        }
        else
        {
            if(isFiring)
            {
                //CancelInvoke("FireWeakDistance");
                //isFiring = false;
            }
        }
    }

    public void Apply()
    {               
        float sp;
        if ( x != 0 || y != 0)        
            sp = speed;         
        else        
            sp = 0.0f;       
        Anim.SetFloat("Speed", sp);
        //var move = new Vector3(x, 0.0f, y);
        //Rb.AddForce(move * speed);
        if(!nextAction.Equals(""))
            Anim.SetTrigger(nextAction);        
    }
}
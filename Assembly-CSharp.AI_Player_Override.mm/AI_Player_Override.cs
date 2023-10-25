#pragma warning disable CS0626

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rewired;
using UnityEngine;
using MonoMod;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.InlineRT;
using Mono.Cecil.Cil;
using Miniscript;

public class playerInput
{
    public Player player;
    public float leftRight, forwardBack;
    public bool button1, button2, button3, button4;
    public bool toggle1, toggle2, toggle3, toggle4;
    public float analog1, analog2;

    public playerInput()
    {
        this.zero();
    }
    public playerInput(Player newPlayer)
    {
        this.setPlayer(newPlayer);
        this.zero();
    }

    public void setPlayer(Player newPlayer)
    {
        if (newPlayer != null) this.player = newPlayer;
    }

    public void zero()
    {
        this.leftRight = 0f;
        this.forwardBack = 0f;
        this.button1 = false;
        this.button2 = false;
        this.button3 = false;
        this.button4 = false;
        this.toggle1 = false;
        this.toggle2 = false;
        this.toggle3 = false;
        this.toggle4 = false;
        this.analog1 = 0f;
        this.analog2 = 0f;
    }

    public bool updateState()
    {   // returns true if player isn't null
        if (this.player == null) return false;
        this.forwardBack = (this.player.GetAxis("LeftTankDrive") + this.player.GetAxis("RightTankDrive")) * 0.5f + this.player.GetAxis("ForwardsBackwards");
        this.leftRight = (this.player.GetAxis("LeftTankDrive") - this.player.GetAxis("RightTankDrive")) * 0.5f + this.player.GetAxis("LeftwardsRightwards");
        if (!this.button1 && this.player.GetButton("Button1")) this.toggle1 = !this.toggle1;
        if (!this.button2 && this.player.GetButton("Button2")) this.toggle2 = !this.toggle2;
        if (!this.button3 && this.player.GetButton("Button3")) this.toggle3 = !this.toggle3;
        if (!this.button4 && this.player.GetButton("Button4")) this.toggle4 = !this.toggle4;
        this.button1 = this.player.GetButton("Button1");
        this.button2 = this.player.GetButton("Button2");
        this.button3 = this.player.GetButton("Button3");
        this.button4 = this.player.GetButton("Button4");
        this.analog1 = this.player.GetAxis("AnalogLever1");
        this.analog2 = this.player.GetAxis("AnalogLever2");
        return true;
    }
}

public class patch_PlayerInputReader : PlayerInputReader
{
    public playerInput pInput;
    [MonoModIgnore]
    public patch_AIController aiController;

    private extern void orig_Start();
    private void Start()
    {
        this.pInput = new playerInput(this.player);
        orig_Start();
    }

    private extern void orig_Update();
    private void Update()
    {
        if (this.player == null) this.player = ReInput.players.GetPlayer(this.playerId);
        if (this.pInput == null) this.pInput = new playerInput(this.player);
        if (!this.pInput.updateState())
        {
            this.pInput.setPlayer(this.player);
            this.pInput.zero();
        }
        orig_Update();
    }
}

public class patch_AIMiniscriptHandler : AIMiniscriptHandler
{
    // gotta make the compiler happy
    [MonoModIgnore]
    private Interpreter interpreter;
    [MonoModIgnore]
    private Code_Editor_Value_Set debugInputs;
    [MonoModIgnore]
    private patch_PlayerInputReader PIR;
    public bool hasBeenReset = false;
    
    //public extern orig_Code_Editor_Value_Set;
    
    public Interpreter getInterpreter()
    {
        return this.interpreter;
    }

    public void printKeyValsFromInterpreter(ValMap vars, string title)
    {
        if (vars != null && vars is ValMap)
        {
            this.debugInputs.AddOrUpdate("Map " + title + ":", vars.Keys.Count);
            foreach(var i in vars.Keys)
            {
                if (vars.Lookup(i) is ValMap)
                {
                    this.printKeyValsFromInterpreter((ValMap)vars.Lookup(i), i.ToString());
                }
                else if (vars.Lookup(i) is ValList)
                {
                    this.debugInputs.AddOrUpdate("List " + i.ToString() + ":", ((ValList)vars.Lookup(i)).values.Count);
                }
                else
                {
                    this.debugInputs.AddOrUpdate(title + "." + i.ToString(), vars.Lookup(i).ToString());
                }
            }
            this.debugInputs.AddOrUpdate("End map " + title, "");
        }
    }
    public void resetDebugInputStuff()
    {
        this.hasBeenReset = true;
        this.debugInputs.keys.Clear();
        this.debugInputs.values.Clear();
    }
    public extern void orig_UpdateScriptFromSelf();
    public void UpdateScriptFromSelf()
    {
        orig_UpdateScriptFromSelf();
        if (this.PIR != null)
        {
            this.interpreter.SetGlobalValue("pForwardBack", new ValNumber(this.PIR.pInput.forwardBack));
            this.interpreter.SetGlobalValue("pLeftRight", new ValNumber(this.PIR.pInput.leftRight));
            this.interpreter.SetGlobalValue("pBtn1", new ValNumber(this.PIR.pInput.button1 ? 1 : 0));
            this.interpreter.SetGlobalValue("pBtn2", new ValNumber(this.PIR.pInput.button2 ? 1 : 0));
            this.interpreter.SetGlobalValue("pBtn3", new ValNumber(this.PIR.pInput.button3 ? 1 : 0));
            this.interpreter.SetGlobalValue("pBtn4", new ValNumber(this.PIR.pInput.button4 ? 1 : 0));
            this.interpreter.SetGlobalValue("pTgl1", new ValNumber(this.PIR.pInput.toggle1 ? 1 : 0));
            this.interpreter.SetGlobalValue("pTgl2", new ValNumber(this.PIR.pInput.toggle2 ? 1 : 0));
            this.interpreter.SetGlobalValue("pTgl3", new ValNumber(this.PIR.pInput.toggle3 ? 1 : 0));
            this.interpreter.SetGlobalValue("pTgl4", new ValNumber(this.PIR.pInput.toggle4 ? 1 : 0));
            this.interpreter.SetGlobalValue("pAna1", new ValNumber(this.PIR.pInput.analog1));
            this.interpreter.SetGlobalValue("pAna2", new ValNumber(this.PIR.pInput.analog2));
            /*this.debugInputs.AddOrUpdate("pForwardBack", this.PIR.pInput.forwardBack);
            this.debugInputs.AddOrUpdate("pLeftRight", this.PIR.pInput.leftRight);
            this.debugInputs.AddOrUpdate("pBtn1", this.PIR.pInput.button1);
            this.debugInputs.AddOrUpdate("pBtn2", this.PIR.pInput.button2);
            this.debugInputs.AddOrUpdate("pBtn3", this.PIR.pInput.button3);
            this.debugInputs.AddOrUpdate("pBtn4", this.PIR.pInput.button4);
            this.debugInputs.AddOrUpdate("pTgl1", this.PIR.pInput.toggle1);
            this.debugInputs.AddOrUpdate("pTgl2", this.PIR.pInput.toggle2);
            this.debugInputs.AddOrUpdate("pTgl3", this.PIR.pInput.toggle3);
            this.debugInputs.AddOrUpdate("pTgl4", this.PIR.pInput.toggle4);
            this.debugInputs.AddOrUpdate("pAna1", this.PIR.pInput.analog1);
            this.debugInputs.AddOrUpdate("pAna2", this.PIR.pInput.analog2);*/
        }
        // print variable state
        this.printKeyValsFromInterpreter(this.interpreter.vm.globalContext.variables, "variables");
        //this.printKeyValsFromInterpreter((ValMap)this.interpreter., "Local vars");
    }
}

[MonoModIgnore]
public class patch_AIController : AIController
{
    public patch_AIMiniscriptHandler miniscript;
}

public class patch_TestCageObjectSpawn : TestCageObjectSpawn
{
    [MonoModIgnore]
    public patch_AIMiniscriptHandler miniscriptHandler;
    public extern void orig_saveAICode();
    public void saveAICode()
    {
        this.miniscriptHandler.resetDebugInputStuff();
        this.miniscriptHandler.getInterpreter().Restart();
        orig_saveAICode();
    }
}

public class patch_Code_Editor_Inputs_Display_Controller : Code_Editor_Inputs_Display_Controller
{
    [MonoModIgnore]
    public patch_AIMiniscriptHandler aIMiniscriptHandler;
    [MonoModIgnore]
    public patch_Value_Set_Controller valueSetDisplay;
    private extern void orig_Update();
    private void Update()
    {
        if(this.aIMiniscriptHandler != null && this.valueSetDisplay != null && this.aIMiniscriptHandler.hasBeenReset)
        {
            this.valueSetDisplay.reset();
            this.aIMiniscriptHandler.hasBeenReset = false;
        }
        orig_Update();
    }
}

public class patch_Value_Set_Controller : Value_Set_Controller
{
    [MonoModIgnore]
    private List<RectTransform> rows;
    public void reset()
    {
        foreach(var item in rows)
        {
            item.gameObject.SetActive(false);
            Destroy(item.GetChild(0).GetChild(0).gameObject);
            Destroy(item.GetChild(1).GetChild(0).gameObject);
            Destroy(item.GetChild(0).gameObject);
            Destroy(item.GetChild(1).gameObject);
        }
        rows.Clear();
    }
}
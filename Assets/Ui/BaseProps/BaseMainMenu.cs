using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMainMenu : BaseMenu
{
    protected static BaseMainMenu instance = null;
    public static BaseMainMenu Instance
    {
        get { return instance; }
    }
    public override void Setup()
    {
        base.Setup();
        BaseMenu.MainMenu = this;
        BaseMainMenu.instance = this;
        this.isSelected = true;
        //BaseMenu.allMenus.Clear();
        //BaseMenu.allMenuTypes.Clear();
    }

}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseMenu : DeclaredProp
{
    public static HashSet<BaseMenu> allMenus = new HashSet<BaseMenu>();
    public static HashSet<Type> allMenuTypes = new HashSet<Type>();
    public static Stack<BaseMenu> lastMenu = new Stack<BaseMenu>();

    public CallbackArray selectedCallbacks = new CallbackArray(true);
    public CallbackArray deselectedCallbacks = new CallbackArray(true);

    //protected Action[] selectedCallbacks = new Action[] { };
    //protected Action[] deselectedCallbacks = new Action[] { };

    private bool _isSelected = false;
    public bool isSelected
    {
        get { return this._isSelected; }
        set
        {
            this._isSelected = value;

            if (!value)
            {
                this.deselectedCallbacks.Invoke();
                //foreach (Action c in this.deselectedCallbacks)
                //{
                //    c();
                //}
            }
            else
            {
                print($"{this.GetType()} is being selected");
                this.selectedCallbacks.Invoke();
                //foreach (Action c in this.selectedCallbacks)
                //{
                //    c();
                //}
            }
            this.gameObject.SetActive(value);

        }
    }

    private static BaseMenu mainMenu;
    protected static BaseMenu MainMenu
    {
        get { return BaseMenu.mainMenu; }
        set
        {
            if (BaseMenu.mainMenu is not null)
            {
                throw new Exception("Cannot assign main menu item twice!!");
            }
            BaseMenu.mainMenu = value;
        }
    }

    public override void Setup()
    {
        //Type MyType = GetType();
        //Debug.Assert(MyType != typeof(MenuBase));
        //if (allMenuTypes.Contains(MyType))
        BaseMenu.allMenus.Add(this);
        BaseMenu.allMenuTypes.Add(this.GetType());
    }

    protected virtual void Start()
    {
        this.gameObject.SetActive(this.isSelected);
    }

    public static void SwitchToMenu(Type menuType, bool switchingBack = false)
    {
        bool menuExists = BaseMenu.allMenuTypes.Contains(menuType);
        if (!menuExists && menuType != null)
        {
            GameObject newMenuObject = GameObject.Instantiate(UIUtils.Panel);
            newMenuObject.transform.SetParent(BaseMenu.mainMenu.transform.parent);
            newMenuObject.RT().anchoredPosition = Vector2.zero;
            BaseMenu newMenu = newMenuObject.AddComponent(menuType) as BaseMenu;
            newMenu.gameObject.SetActive(false);
        }
        // switchingBack => true: remember this menu when switching back to last menu, or false: skip remembering this menu
        foreach (BaseMenu menu in BaseMenu.allMenus)
        {
            bool activation = menu.GetType() == menuType;
            if (menu.gameObject.activeSelf && switchingBack)
            {
                //print("last menu: " + menu);
                BaseMenu.lastMenu.Push(menu);
            }
            //menu.gameObject.SetActive(activation);
            menu.isSelected = activation;
        }
    }

    public static void SwitchToPreviousMenu()
    {
        if (BaseMenu.lastMenu.Count > 0)
        {
            BaseMenu.SwitchToMenu(BaseMenu.lastMenu.Pop().GetType(), true);
        }
    }

    // InputReceiver methods:
    //bool InputReceiver.IsActive()
    //{
    //    return this.gameObject.activeSelf;
    //}

    //void InputReceiver.HandleInputs() { }
    //void InputReceiver.HandleInputs()
    //{

    //    if (!this.gameObject.activeSelf)
    //    {
    //        Debug.LogError($"Handling inputs on inactive object {this.GetType()} SHOULD HANDLE IF SEEN MORE THAN A FEW TIMES");
    //        //throw new InvalidOperationException($"Cannot handle inputs on inactive UI object!!!");
    //    }
    //    //print($"HANDLING ON {this.GetType()}");
    //    //Debug.LogError($"MY TYPE: {this.GetType()} USE OVERRIDE INSTEAD OF NEW");
    //    //throw new NotImplementedException();
    //}

}

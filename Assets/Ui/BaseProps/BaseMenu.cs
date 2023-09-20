using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMenu : DeclaredProp
{
    public static HashSet<BaseMenu> allMenus = new HashSet<BaseMenu>();
    public static HashSet<Type> allMenuTypes = new HashSet<Type>();
    public static Stack<BaseMenu> lastMenu = new Stack<BaseMenu>();

    protected Action[] selectedCallbacks = new Action[] { };
    protected Action[] deselectedCallbacks = new Action[] { };

    private bool _isSelected = false;
    public bool isSelected 
    {
        get { return this._isSelected; }
        set
        {
            this._isSelected = value;

            if (!value)
            {
                foreach (Action c in this.deselectedCallbacks)
                {
                    c();
                }
            }
            else
            {
                print($"{this.GetType()} is being selected");
                foreach (Action c in this.selectedCallbacks)
                {
                    c();
                }
            }

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

    public void SetThisMenuSelectedCallbacks(Action[] actions)
    {
        this.selectedCallbacks = actions;
    }

    public void SetThisMenuDeselectedCallbacks(Action[] actions)
    {
        this.deselectedCallbacks = actions;
    }

    protected override void Setup()
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

    public static void SwitchToMenu(Type menuType, bool switchingBack=false) {
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
        foreach(BaseMenu menu in BaseMenu.allMenus)
        {
            bool activation = menu.GetType() == menuType;
            if (menu.gameObject.activeSelf && !switchingBack)
            {
                //print("last menu: " + menu);
                BaseMenu.lastMenu.Push(menu);
            }
            menu.gameObject.SetActive(activation);
            menu.isSelected = activation;
        }
    }
    public static void SwitchToPreviousMenu()
    {
        BaseMenu.SwitchToMenu(BaseMenu.lastMenu.Pop().GetType(), true);
    }
}

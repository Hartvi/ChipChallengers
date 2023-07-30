using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMenu : TopProp
{
    public static HashSet<BaseMenu> allMenus = new HashSet<BaseMenu>();
    public static HashSet<Type> allMenuTypes = new HashSet<Type>();
    public static Stack<BaseMenu> lastMenu = new Stack<BaseMenu>();
    public bool isSelected = false;
    private static BaseMenu mainMenu;
    protected static BaseMenu MainMenu
    {
        get { return mainMenu; }
        set
        {
            //if (mainMenu is not null)
            //{
            //    throw new Exception("Cannot assign main menu item twice!!");
            //}
            mainMenu = value;
        }
    }
    protected override void Setup()
    {
        //Type MyType = GetType();
        //Debug.Assert(MyType != typeof(MenuBase));
        //if (allMenuTypes.Contains(MyType))
        allMenus.Add(this);
        allMenuTypes.Add(this.GetType());
    }
    protected virtual void Start()
    {
        gameObject.SetActive(isSelected);
    }
    public static void SwitchToMenu(Type menuType, bool switchingBack=false) {
        bool menuExists = allMenuTypes.Contains(menuType);
        if (!menuExists && menuType != null)
        {
            GameObject newMenuObject = Instantiate(UIUtils.FullscreenPanel);
            newMenuObject.transform.SetParent(mainMenu.transform.parent);
            newMenuObject.RT().anchoredPosition = Vector2.zero;
            BaseMenu newMenu = newMenuObject.AddComponent(menuType) as BaseMenu;
            newMenu.gameObject.SetActive(false);
        }
        foreach(var menu in allMenus)
        {
            bool activation = menu.GetType() == menuType;
            if (menu.gameObject.activeSelf && !switchingBack)
            {
                //print("last menu: " + menu);
                lastMenu.Push(menu);
            }
            menu.gameObject.SetActive(activation);
            menu.isSelected = activation;
        }
    }
    public static void SwitchToPreviousMenu()
    {
        SwitchToMenu(lastMenu.Pop().GetType(), true);
    }
}

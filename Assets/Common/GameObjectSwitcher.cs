using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectSwitcher
{
    GameObject[] gameObjects;

    public GameObjectSwitcher(GameObject[] gameObjects)
    {
        this.gameObjects = gameObjects;
    }

    public void Switch(Func<GameObject, bool> action)
    {
        for(int i = 0; i < this.gameObjects.Length; ++i)
        {
            this.gameObjects[i].SetActive(action(this.gameObjects[i]));
        }
    }
}

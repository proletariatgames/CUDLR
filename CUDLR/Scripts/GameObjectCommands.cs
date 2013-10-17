using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

/**
 * Example console commands for getting information about GameObjects
 */
public class GameObjectCommands : MonoBehaviour {

  private static Queue<string> m_runQueue;

  public virtual void Awake() {
    Console.GetInstance().RegisterCommand("object list", "lists all the game objects in the scene", ListGameObjects );
    Console.GetInstance().RegisterCommand("object print", "lists properties of the object", PrintGameObject );
  }

  public void ListGameObjects(List<string> args) {
    UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
    foreach (UnityEngine.Object obj in objects) {
      Console.GetInstance().Log(obj.name);
    }
  }

  public void PrintGameObject(List<string> args) {
    if (args.Count < 1) {
      Console.GetInstance().Log( "expected : object print <Object Name>" );
      return;
    }

    GameObject obj = GameObject.Find( args[0] );
    if (obj == null) {
      Console.GetInstance().Log("GameObject not found : "+args[0]);
    } else {
      Console.GetInstance().Log("Game Object : "+obj.name);
      foreach (Component component in obj.GetComponents(typeof(Component))) {
        Console.GetInstance().Log("  Component : "+component.GetType());
        foreach (FieldInfo f in component.GetType().GetFields()) {
          Console.GetInstance().Log("    "+f.Name+" : "+f.GetValue(component));
        }
      }
    }

  }

}

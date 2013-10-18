using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

/**
 * Example console commands for getting information about GameObjects
 */
public static class GameObjectCommands {

  [ConsoleCommand("object list", "lists all the game objects in the scene")]
  public static void ListGameObjects() {
    UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
    foreach (UnityEngine.Object obj in objects) {
      Console.Log(obj.name);
    }
  }

  [ConsoleCommand("object print", "lists properties of the object")]
  public static void PrintGameObject(string[] args) {
    if (args.Length < 1) {
      Console.Log( "expected : object print <Object Name>" );
      return;
    }

    GameObject obj = GameObject.Find( args[0] );
    if (obj == null) {
      Console.Log("GameObject not found : "+args[0]);
    } else {
      Console.Log("Game Object : "+obj.name);
      foreach (Component component in obj.GetComponents(typeof(Component))) {
        Console.Log("  Component : "+component.GetType());
        foreach (FieldInfo f in component.GetType().GetFields()) {
          Console.Log("    "+f.Name+" : "+f.GetValue(component));
        }
      }
    }
  }
}



/**
 * Example console route for getting information about GameObjects
 *

// FIXME need main thread support

public static class GameObjectRoutes {
  
  [ConsoleRoute("^/object/list.json$")]
  public static bool ListGameObjects(HttpListenerContext context) {
    string json = "[";
    UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
    foreach (UnityEngine.Object obj in objects) {
      // FIXME object names need to be escaped.. use minijson or similar
      json += string.Format("\"{0}\", ", obj.name);
    }
    json = json.TrimEnd(new char[]{',', ' '}) + "]";

    context.Response.WriteString(json, "application/json");
    return true;
  }
}
*/

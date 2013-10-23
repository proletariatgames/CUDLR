using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System;

/**
 * Example console commands for getting information about GameObjects
 */
public static class GameObjectCommands {

  [CUDLR.Command("object list", "lists all the game objects in the scene")]
  public static void ListGameObjects() {
    Action<Transform, int> deepList = (Transform xform, int indentLevel) => {
      string listing = "- " + xform.name;
      CUDLR.Console.Log(listing.PadLeft(listing.Length + indentLevel * 2));

      int childCount = xform.childCount;
      for (int child = 0; child < childCount; child++)
        deepList(xform.GetChild(child), indentLevel + 1);
    };

    foreach (Transform xform in GameObject.FindObjectsOfType(typeof(Transform))) {
      if (xform.parent == null)
        deepList(xform, 0);
    }
  }

  [CUDLR.Command("object print", "lists properties of the object")]
  public static void PrintGameObject(string[] args) {
    if (args.Length < 1) {
      CUDLR.Console.Log("expected: object print <Object Name>");
      return;
    }

    GameObject obj = GameObject.Find( args[0] );
    if (obj == null) {
      CUDLR.Console.Log("GameObject not found: "+args[0]);
    } else {
      CUDLR.Console.Log("Game Object: "+obj.name);
      foreach (Component component in obj.GetComponents(typeof(Component))) {
       CUDLR.Console.Log("   + Component: "+component.GetType());
        foreach (FieldInfo f in component.GetType().GetFields()) {
          CUDLR.Console.Log("     - "+f.Name+":  "+f.GetValue(component));
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

  [CUDLR.Route("^/object/list.json$")]
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

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Linq;
using MiniJSON;

/**
 * Example console commands for getting information about GameObjects
 */
public static class GameObjectCommands {

  [CUDLR.Command("object list", "lists all the game objects in the scene")]
  public static void ListGameObjects() {
    UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
    foreach (UnityEngine.Object obj in objects) {
      CUDLR.Console.Log(obj.name);
    }
  }

  [CUDLR.Command("object print", "lists properties of the object")]
  public static void PrintGameObject(string[] args) {
    if (args.Length < 1) {
      CUDLR.Console.Log( "expected : object print <Object Name>" );
      return;
    }

    GameObject obj = GameObject.Find( args[0] );
    if (obj == null) {
      CUDLR.Console.Log("GameObject not found : "+args[0]);
    } else {
      CUDLR.Console.Log("Game Object : "+obj.name);
      foreach (Component component in obj.GetComponents(typeof(Component))) {
       CUDLR.Console.Log("  Component : "+component.GetType());
        foreach (FieldInfo f in component.GetType().GetFields()) {
          CUDLR.Console.Log("    "+f.Name+" : "+f.GetValue(component));
        }
      }
    }
  }
}



/**
 * Example console route for getting information about GameObjects
 *
 */
public static class GameObjectRoutes {

  [CUDLR.Route("^/objects.json$", @"(GET|HEAD)", true)]
  public static void ListGameObjects(CUDLR.RequestContext context) {
    string[] objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)).Select(x => x.name).ToArray();
    context.Response.WriteString(Json.Serialize(objects), "application/json");
  }


  [CUDLR.Route("^/object/([\\w\\s]+).json$", @"(GET|HEAD)", true)]
  public static void PrintGameObject(CUDLR.RequestContext context) {
    string obj_name = context.match.Groups[1].Value;

    GameObject obj = GameObject.Find( obj_name );
    if (obj == null) {
      context.pass = true;
      return;
    }

    Dictionary<string, object> components = new Dictionary<string, object>();
    foreach (Component component in obj.GetComponents(typeof(Component))) {
      Dictionary<string, object> members = new Dictionary<string, object>();
      foreach (MemberInfo member in component.GetType().GetMembers(BindingFlags.Public|BindingFlags.Instance)) {
        if (member.MemberType == MemberTypes.Field)
          members[member.Name] = ((FieldInfo)member).GetValue(component);

        if (member.MemberType == MemberTypes.Property)
          members[member.Name] = ((PropertyInfo)member).GetValue(component, null);
      }
      components[component.GetType().ToString()] = members;
    }

    context.Response.WriteString(Json.Serialize(components), "application/json");
  }
}

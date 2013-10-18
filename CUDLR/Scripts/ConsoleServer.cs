using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Reflection;

public static class ResponseExtension {
  public static void WriteString(this HttpListenerResponse response, string input, string type = "text/plain")
  {
    response.StatusCode = (int)HttpStatusCode.OK;
    response.StatusDescription = "OK";

    if (!string.IsNullOrEmpty(input)) {
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(input);
      response.ContentLength64 = buffer.Length;
      response.ContentType = type;
      response.OutputStream.Write(buffer,0,buffer.Length);
    }
  }

  public static void WriteFile(this HttpListenerResponse response, string path, bool download = false)
  {
    using (FileStream fs = File.OpenRead(path)) {
      response.StatusCode = (int)HttpStatusCode.OK;
      response.StatusDescription = "OK";
      response.ContentLength64 = fs.Length;
      // FIXME content type response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
      if (download)
        response.AddHeader("Content-disposition", string.Format("attachment; filename={0}", Path.GetFileName(path)));

      byte[] buffer = new byte[64 * 1024];
      int read;
      while ((read = fs.Read(buffer, 0, buffer.Length)) > 0) {
        // FIXME required?
        System.Threading.Thread.Sleep(0);
        response.OutputStream.Write(buffer, 0, read);
      }
    }
  }
}

public class ConsoleServer : MonoBehaviour {

  [SerializeField]
  public int Port = 55055;

  private static HttpListener listener = new HttpListener();
  private static string filePath;
  private static Regex fileRegex;

  public delegate void RouteCallback(HttpListenerContext context);
  private static ConsoleRouteAttribute[] registeredRoutes;

  public virtual void Awake() {
    // Set file path based on targeted platform
    switch (Application.platform) {
      case RuntimePlatform.OSXEditor:
      case RuntimePlatform.WindowsEditor:
      case RuntimePlatform.WindowsPlayer:
        filePath = Application.dataPath + "/StreamingAssets/CUDLR/";
        break;
      case RuntimePlatform.OSXPlayer:
        filePath = Application.dataPath + "/Data/StreamingAssets/CUDLR/";
        break;
      case RuntimePlatform.IPhonePlayer:
        filePath = Application.dataPath + "/Raw/CUDLR/";
        break;
      case RuntimePlatform.Android:
        filePath = "jar:file://" + Application.dataPath + "!/assets/CUDLR/";
        break;
      default:
        Debug.Log("Error starting CUDLR: Unsupported platform.");
        return;
    }

    // List of supported files
    // FIXME - add content types for these files
    fileRegex = new Regex(@"^.*\.(jpg|gif|png|css|htm|html|ico)$", RegexOptions.IgnoreCase);

    RegisterRoutes();

    // Start server
    Debug.Log("Starting Debug Console on port : " + Port);
    listener.Prefixes.Add("http://*:"+Port+"/");
    listener.Start();
    listener.BeginGetContext(ListenerCallback, null);
  }

  private void RegisterRoutes() {

    List<ConsoleRouteAttribute> found = new List<ConsoleRouteAttribute>();

    foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {

      // FIXME add support for non-static methods (FindObjectByType?)
      foreach(MethodInfo method in type.GetMethods(BindingFlags.Public|BindingFlags.Static)) {
        ConsoleRouteAttribute[] attrs = method.GetCustomAttributes(typeof(ConsoleRouteAttribute), true) as ConsoleRouteAttribute[];
        if (attrs.Length == 0)
          continue;

        RouteCallback cb = (RouteCallback) Delegate.CreateDelegate(typeof(RouteCallback), method, false);
        if (cb == null)
        {
          Debug.LogError(string.Format("Method {0}.{1} takes the wrong arguments for a console route.", type, method.Name));
          continue;
        }

        // try with a bare action
        foreach(ConsoleRouteAttribute route in attrs) {
          if (string.IsNullOrEmpty(route.m_route)) {
            Debug.LogError(string.Format("Method {0}.{1} needs a valid route name.", type, method.Name));
            continue;
          }

          route.m_callback = cb;
          found.Add(route);
        }
      }
    }

    // FIXME sort and binary search to match
    registeredRoutes = found.ToArray();
  }

  void OnEnable() {
    // Capture Console Logs
    Application.RegisterLogCallback(HandleLog);
  }

  void OnDisable() {
    Application.RegisterLogCallback(null);
  }

  void Update() {
    Console.Update();
  }

  void ListenerCallback(IAsyncResult result) {
    HttpListenerContext context = listener.EndGetContext(result);

    string path = context.Request.Url.AbsolutePath;
    if (path == "/")
      path = "index.html";

    try {
      bool handled = false;
      foreach (ConsoleRouteAttribute route in registeredRoutes) {
        if (string.Compare(route.m_route, path, true) != 0)
          continue;

        route.m_callback(context);
        handled = true;
        break;
      }

      if (!handled && fileRegex.IsMatch(path))
      {
        path = filePath + path;

        if (File.Exists(path)) {
          context.Response.WriteFile(path);
          handled = true;
        }
      }

      if (!handled) {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.StatusDescription = "Not Found";
      }
    }
    catch (Exception exception) {
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      context.Response.StatusDescription = string.Format("Fatal error:\n{0}", exception);

      Debug.LogException(exception);
    }

    context.Response.OutputStream.Close();

    listener.BeginGetContext(new AsyncCallback(ListenerCallback), null);
  }

  private void HandleLog(string logString, string stackTrace, LogType type) {
    Console.Log(logString);
    if (type != LogType.Log) {
      Console.Log(stackTrace);
    }
  }
}

static class ConsoleRoutes 
{
  [ConsoleRoute("/console/out")]
  public static void Output(HttpListenerContext context) {
    context.Response.WriteString(Console.Output());
  }

  [ConsoleRoute("/console/run")]
  public static void Run(HttpListenerContext context) {
    string command = context.Request.QueryString.Get("command");
    if (!string.IsNullOrEmpty(command))
      Console.Run(command);

    context.Response.StatusCode = (int)HttpStatusCode.OK;
    context.Response.StatusDescription = "OK";
  }

  [ConsoleRoute("/console/commandHistory")]
  public static void History(HttpListenerContext context) {
    string index = context.Request.QueryString.Get("index");

    string previous = null;
    if (!string.IsNullOrEmpty(index))
      previous = Console.PreviousCommand(System.Int32.Parse(index));

    context.Response.WriteString(previous);
  }


  [ConsoleRoute("/console/complete")]
  public static void Complete(HttpListenerContext context) {
    string partialCommand = context.Request.QueryString.Get("command");

    string found = null;
    if (partialCommand != null)
      found = Console.Complete(partialCommand);

    context.Response.WriteString(found);
  }
}

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleRouteAttribute : Attribute
{
    public ConsoleRouteAttribute(string route, string methods = null)
    {
      m_route = route;
      if (methods != null)
        m_methods = methods.Split('|');
    }

    public string m_route;
    public string[] m_methods;
    public ConsoleServer.RouteCallback m_callback;
}

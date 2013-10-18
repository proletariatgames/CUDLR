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
      // FIXME - add content types for supported types
      // response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
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

  private static List<ConsoleRouteAttribute> registeredRoutes;

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

    RegisterRoutes();
    RegisterFileHandlers();

    // Start server
    Debug.Log("Starting Debug Console on port : " + Port);
    listener.Prefixes.Add("http://*:"+Port+"/");
    listener.Start();
    listener.BeginGetContext(ListenerCallback, null);
  }

  private void RegisterRoutes() {

    registeredRoutes = new List<ConsoleRouteAttribute>();

    foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {

      // FIXME add support for non-static methods (FindObjectByType?)
      foreach(MethodInfo method in type.GetMethods(BindingFlags.Public|BindingFlags.Static)) {
        ConsoleRouteAttribute[] attrs = method.GetCustomAttributes(typeof(ConsoleRouteAttribute), true) as ConsoleRouteAttribute[];
        if (attrs.Length == 0)
          continue;

        ConsoleRouteAttribute.Callback cbm = (ConsoleRouteAttribute.Callback) Delegate.CreateDelegate(typeof(ConsoleRouteAttribute.Callback), method, false);
        if (cbm == null)
        {
          ConsoleRouteAttribute.CallbackSimple cb = (ConsoleRouteAttribute.CallbackSimple) Delegate.CreateDelegate(typeof(ConsoleRouteAttribute.CallbackSimple), method, false);
          if (cb != null) {
            cbm = delegate(HttpListenerContext context, Match match) {
              return cb(context);
            };
          }
        }

        if (cbm == null) {
          Debug.LogError(string.Format("Method {0}.{1} takes the wrong arguments for a console route.", type, method.Name));
          continue;
        }

        // try with a bare action
        foreach(ConsoleRouteAttribute route in attrs) {
          if (route.m_route == null) {
            Debug.LogError(string.Format("Method {0}.{1} needs a valid route regexp.", type, method.Name));
            continue;
          }

          route.m_callback = cbm;
          registeredRoutes.Add(route);
        }
      }
    }
  }

  static bool FileHandler(HttpListenerContext context, Match match, bool download) {
    string path = filePath + match.Groups[1].Value;
    if (!File.Exists(path))
        return false;

    context.Response.WriteFile(path, download);
    return true;
  }

  static void RegisterFileHandlers() {
    // List of supported files
    ConsoleRouteAttribute downloadRoute = new ConsoleRouteAttribute(@"^/download/(.*\.(jpg|gif|png|css|htm|html|ico))$");
    ConsoleRouteAttribute fileRoute = new ConsoleRouteAttribute(@"^/(.*\.(jpg|gif|png|css|htm|html|ico))$");

    downloadRoute.m_callback = delegate(HttpListenerContext context, Match match) { return FileHandler(context, match, true); };
    fileRoute.m_callback = delegate(HttpListenerContext context, Match match) { return FileHandler(context, match, false); };

    registeredRoutes.Add(downloadRoute);
    registeredRoutes.Add(fileRoute);
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
      path = "/index.html";

    // FIXME filter routes on method
    try {
      bool handled = false;
      foreach (ConsoleRouteAttribute route in registeredRoutes) {
        Match match = route.m_route.Match(path);
        if (!match.Success)
          continue;

        if (route.m_methods != null && !route.m_methods.IsMatch(context.Request.HttpMethod))
          continue;

        if (route.m_callback(context, match)) {
          handled = true;
          break;
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
  [ConsoleRoute("^/console/out$")]
  public static bool Output(HttpListenerContext context) {
    context.Response.WriteString(Console.Output());
    return true;
  }

  [ConsoleRoute("^/console/run$")]
  public static bool Run(HttpListenerContext context) {
    string command = context.Request.QueryString.Get("command");
    if (!string.IsNullOrEmpty(command))
      Console.Run(command);

    context.Response.StatusCode = (int)HttpStatusCode.OK;
    context.Response.StatusDescription = "OK";
    return true;
  }

  [ConsoleRoute("^/console/commandHistory$")]
  public static bool History(HttpListenerContext context) {
    string index = context.Request.QueryString.Get("index");

    string previous = null;
    if (!string.IsNullOrEmpty(index))
      previous = Console.PreviousCommand(System.Int32.Parse(index));

    context.Response.WriteString(previous);
    return true;
  }


  [ConsoleRoute("^/console/complete$")]
  public static bool Complete(HttpListenerContext context) {
    string partialCommand = context.Request.QueryString.Get("command");

    string found = null;
    if (partialCommand != null)
      found = Console.Complete(partialCommand);

    context.Response.WriteString(found);
    return true;
  }
}

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleRouteAttribute : Attribute
{
    public delegate bool CallbackSimple(HttpListenerContext context);
    public delegate bool Callback(HttpListenerContext context, Match match);

    public ConsoleRouteAttribute(string route, string methods = @"(GET|HEAD)")
    {
      m_route = new Regex(route, RegexOptions.IgnoreCase);
      if (methods != null)
        m_methods = new Regex(methods);
    }

    public Regex m_route;
    public Regex m_methods;
    public Callback m_callback;
}

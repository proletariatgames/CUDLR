using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;

namespace CUDLR {

  public class Server : MonoBehaviour {

    [SerializeField]
    public int Port = 55055;

    private static HttpListener listener = new HttpListener();
    private static string filePath;

    private static List<RouteAttribute> registeredRoutes;

    // List of supported files
    // FIXME add an api to register new types
    private static Dictionary<string, string> fileTypes = new Dictionary<string, string> {
      {"js",   "application/javascript"},
      {"json", "application/json"},
      {"jpg",  "image/jpeg" },
      {"jpeg", "image/jpeg"},
      {"gif",  "image/gif"},
      {"png",  "image/png"},
      {"css",  "text/css"},
      {"htm",  "text/html"},
      {"html", "text/html"},
      {"ico",  "image/x-icon"}, 
    };

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
      Debug.Log("Starting CUDLR Server on port : " + Port);
      listener.Prefixes.Add("http://*:"+Port+"/");
      listener.Start();
      listener.BeginGetContext(ListenerCallback, null);
    }

    private void RegisterRoutes() {

      registeredRoutes = new List<RouteAttribute>();

      foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {

        // FIXME add support for non-static methods (FindObjectByType?)
        foreach(MethodInfo method in type.GetMethods(BindingFlags.Public|BindingFlags.Static)) {
          RouteAttribute[] attrs = method.GetCustomAttributes(typeof(RouteAttribute), true) as RouteAttribute[];
          if (attrs.Length == 0)
            continue;

          RouteAttribute.Callback cbm = Delegate.CreateDelegate(typeof(RouteAttribute.Callback), method, false) as RouteAttribute.Callback;
          if (cbm == null)
          {
            RouteAttribute.CallbackSimple cb = Delegate.CreateDelegate(typeof(RouteAttribute.CallbackSimple), method, false) as RouteAttribute.CallbackSimple;
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
          foreach(RouteAttribute route in attrs) {
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

      string type;
      string ext = Path.GetExtension(path).ToLower().TrimStart(new char[] {'.'});
      if (download || !fileTypes.TryGetValue(ext, out type))
        type = "application/octet-stream";

      context.Response.WriteFile(path, type, download);
      return true;
    }

    static void RegisterFileHandlers() {
      string pattern = string.Format("({0})", string.Join("|", fileTypes.Select(x => x.Key).ToArray()));
      RouteAttribute downloadRoute = new RouteAttribute(string.Format(@"^/download/(.*\.{0})$", pattern));
      RouteAttribute fileRoute = new RouteAttribute(string.Format(@"^/(.*\.{0})$", pattern));

      downloadRoute.m_callback = delegate(HttpListenerContext context, Match match) { return FileHandler(context, match, true); };
      fileRoute.m_callback = delegate(HttpListenerContext context, Match match) { return FileHandler(context, match, false); };

      registeredRoutes.Add(downloadRoute);
      registeredRoutes.Add(fileRoute);
    }

    void OnEnable() {
      // Capture Console Logs
      Application.RegisterLogCallback(Console.LogCallback);
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
        foreach (RouteAttribute route in registeredRoutes) {
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

#if UNITY_3_5
        Debug.LogError("Handling: " + path + ", crashed: " + exception);
#else
        Debug.LogException(exception);
#endif
      }

      context.Response.OutputStream.Close();

      listener.BeginGetContext(new AsyncCallback(ListenerCallback), null);
    }
  }
}

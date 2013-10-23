using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.Threading;

namespace CUDLR {

  public class RequestContext
  {
    public HttpListenerContext context;
    public Match match;
    public bool pass;
    public string path;
    public int currentRoute;

    public HttpListenerRequest Request { get { return context.Request; } }
    public HttpListenerResponse Response { get { return context.Response; } }

    public RequestContext(HttpListenerContext ctx)
    {
      context = ctx;
      match = null;
      pass = false;
      path = WWW.UnEscapeURL(context.Request.Url.AbsolutePath);
      if (path == "/")
        path = "/index.html";
      currentRoute = 0;
    }
  }


  public class Server : MonoBehaviour {

    [SerializeField]
    public int Port = 55055;

    private static Thread mainThread;
    private static string fileRoot;
    private static HttpListener listener = new HttpListener();
    private static List<RouteAttribute> registeredRoutes;
    private static Queue<RequestContext> mainRequests = new Queue<RequestContext>();

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
      mainThread = Thread.CurrentThread;
      fileRoot = Path.Combine(Application.streamingAssetsPath, "CUDLR");

      RegisterRoutes();
      RegisterFileHandlers();

      // Start server
      Debug.Log("Starting CUDLR Server on port : " + Port);
      listener.Prefixes.Add("http://*:"+Port+"/");
      listener.Start();
      listener.BeginGetContext(ListenerCallback, null);

      StartCoroutine(HandleRequests());
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

    static void FindFileType(RequestContext context, bool download, out string path, out string type) {
      path = Path.Combine(fileRoot, context.match.Groups[1].Value);

      string ext = Path.GetExtension(path).ToLower().TrimStart(new char[] {'.'});
      if (download || !fileTypes.TryGetValue(ext, out type))
        type = "application/octet-stream";
    }


    public delegate void FileHandlerDelegate(RequestContext context, bool download);
    static void WWWFileHandler(RequestContext context, bool download) {
      string path, type;
      FindFileType(context, download, out path, out type);

      WWW req = new WWW(path);
      while (!req.isDone) {
        Thread.Sleep(0);
      }

      if (string.IsNullOrEmpty(req.error)) {
        context.Response.ContentType = type;
        if (download)
          context.Response.AddHeader("Content-disposition", string.Format("attachment; filename={0}", Path.GetFileName(path)));

        context.Response.WriteBytes(req.bytes);
        return;
      }

      if (req.error.StartsWith("Couldn't open file")) {
        context.pass = true;
      }
      else {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.StatusDescription = string.Format("Fatal error:\n{0}", req.error);
      }
    }

    static void FileHandler(RequestContext context, bool download) {
      string path, type;
      FindFileType(context, download, out path, out type);

      if (File.Exists(path)) {
        context.Response.WriteFile(path, type, download);
      }
      else {
        context.pass = true;
      }
    }

    static void RegisterFileHandlers() {
      string pattern = string.Format("({0})", string.Join("|", fileTypes.Select(x => x.Key).ToArray()));
      RouteAttribute downloadRoute = new RouteAttribute(string.Format(@"^/download/(.*\.{0})$", pattern));
      RouteAttribute fileRoute = new RouteAttribute(string.Format(@"^/(.*\.{0})$", pattern));

      bool needs_www = fileRoot.Contains("://");
      downloadRoute.m_runOnMainThread = needs_www;
      fileRoute.m_runOnMainThread = needs_www;

      FileHandlerDelegate callback = FileHandler;
      if (needs_www)
        callback = WWWFileHandler;

      downloadRoute.m_callback = delegate(RequestContext context) { callback(context, true); };
      fileRoute.m_callback = delegate(RequestContext context) { callback(context, false); };

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
      RequestContext context = new RequestContext(listener.EndGetContext(result));

      HandleRequest(context);

      listener.BeginGetContext(new AsyncCallback(ListenerCallback), null);
    }

    void HandleRequest(RequestContext context) {
      try {
        bool handled = false;

        for (; context.currentRoute < registeredRoutes.Count; ++context.currentRoute) {
          RouteAttribute route = registeredRoutes[context.currentRoute];
          Match match = route.m_route.Match(context.path);
          if (!match.Success)
            continue;

          if (!route.m_methods.IsMatch(context.Request.HttpMethod))
            continue;

          // Upgrade to main thread if necessary
          if (route.m_runOnMainThread && Thread.CurrentThread != mainThread) {
            lock (mainRequests) {
              mainRequests.Enqueue(context);
            }
            return;
          }

          context.match = match;
          route.m_callback(context);
          handled = !context.pass;
          if (handled)
            break;
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
    }

    IEnumerator HandleRequests() {
      while (true) {
        while (mainRequests.Count == 0) {
          yield return new WaitForEndOfFrame();
        }

        RequestContext context = null;
        lock (mainRequests) {
            context = mainRequests.Dequeue();
        }

        HandleRequest(context);
      }
    }
  }
}

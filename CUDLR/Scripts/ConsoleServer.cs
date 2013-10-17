using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

public class ConsoleServer : MonoBehaviour {

  [SerializeField]
  public int Port = 55055;

  private static HttpListener listener = new HttpListener();
  private static string filePath;
  private static Regex fileRegex;

  public virtual void Awake() {
    // Set file path based on targeted platform
    switch (Application.platform) {
      case RuntimePlatform.OSXEditor:
      case RuntimePlatform.WindowsEditor:
        filePath = Application.dataPath + "/StreamingAssets/WWW/";
        break;
      case RuntimePlatform.IPhonePlayer:
        filePath = Application.dataPath + "/Raw/WWW/";
        break;
      case RuntimePlatform.Android:
        filePath = "jar:file://" + Application.dataPath + "!/assets/WWW/";
        break;
    }

    // List of supported files
    fileRegex = new Regex(@"^.*\.(jpg|gif|png|css|htm|html|ico)$");

    // Start server
    Debug.Log("Starting Debug Console on port : " + Port);
    listener.Prefixes.Add("http://*:"+Port+"/");
    listener.Start();
    listener.BeginGetContext(ListenerCallback, null);
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

  public void ListenerCallback(IAsyncResult result) {
    HttpListenerContext context = listener.EndGetContext(result);
    HttpListenerRequest request = context.Request;
    HttpListenerResponse response = context.Response;

    string responseString = LoadPage(request.RawUrl, request.QueryString);
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    response.ContentLength64 = buffer.Length;
    System.IO.Stream output = response.OutputStream;
    output.Write(buffer,0,buffer.Length);
    output.Close();

    listener.BeginGetContext(new AsyncCallback(ListenerCallback), null);
  }

  public string LoadPage(string url, NameValueCollection args) {
    string[] tokens = url.Split('?');
    url = tokens[0];
    string file = null;

    // TODO register endpoints outside of server class.
    switch (url) {
      case "/console/out":
        return Console.Output();
      case "/console/run":
        string command = args.Get("command");
        if (command != null) { Console.Run(command); }
        return "";
      case "/console/commandHistory":
        string index = args.Get("index");
        string previousCommand = null;
        if (index != null) {
          previousCommand = Console.PreviousCommand(System.Int32.Parse(index));
        }
        return previousCommand == null ? "" : previousCommand;
      case "/console/complete":
        string partialCommand = args.Get("command");
        if (partialCommand != null) { return Console.Complete(partialCommand); }
        return "";
      default:
        file = fileRegex.IsMatch(url) ? url : "index.html";
        break;
    }

    try {
      StreamReader reader = new StreamReader(filePath + file);
      Debug.Log("reader : " +  reader);
      string responseString = "";
      string text = null;
      while ( (text = reader.ReadLine()) != null ) {
        responseString += text + "\n";
      }
      return responseString;
    } catch (Exception exception) {
      Debug.Log("Console Error : " + exception);
      return "";
    }
  }

  private void HandleLog(string logString, string stackTrace, LogType type) {
    Console.Log(logString);
    if (type != LogType.Log) {
      Console.Log(stackTrace);
    }
  }

}

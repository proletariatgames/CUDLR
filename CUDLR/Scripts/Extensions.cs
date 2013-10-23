using UnityEngine;
using System.Net;
using System.IO;

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

  public static void WriteBytes(this HttpListenerResponse response, byte[] bytes)
  {
      response.StatusCode = (int)HttpStatusCode.OK;
      response.StatusDescription = "OK";
      response.ContentLength64 = bytes.Length;
      response.OutputStream.Write(bytes, 0, bytes.Length);
  }

  public static void WriteFile(this HttpListenerResponse response, string path, string type = "application/octet-stream", bool download = false)
  {
    using (FileStream fs = File.OpenRead(path)) {
      response.StatusCode = (int)HttpStatusCode.OK;
      response.StatusDescription = "OK";
      response.ContentLength64 = fs.Length;
      response.ContentType = type;
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

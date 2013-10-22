using UnityEngine;
using System.Net;
using System.IO;

namespace CUDLR {
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

    public static void WriteBytes(this HttpListenerResponse response, string path, byte[] bytes, string type = "application/octet-stream", bool download = false)
    {
        response.StatusCode = (int)HttpStatusCode.OK;
        response.StatusDescription = "OK";
        response.ContentLength64 = bytes.Length;
        response.ContentType = type;
        if (download)
          response.AddHeader("Content-disposition", string.Format("attachment; filename={0}", Path.GetFileName(path)));

        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

  }
}

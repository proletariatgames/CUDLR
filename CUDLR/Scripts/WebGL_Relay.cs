using AOT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace CUDLR
{
    /// <summary>
    /// Relay between browser console and CUDLR.Console.
    /// </summary>
    public static class WebGL_Relay
    {
        /// <summary>
        /// Initialize by setting callback to a javascript on the browser.
        /// </summary>
        public static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogFormat("Initialize CUDLR WebGL Relay");
            SetCudlrCallbackAndCreateInput(RunCommand);
            Console.Update();
#endif
        }

        public static void Log(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShowLog(text);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// General command callback.
        /// </summary>
        /// <param name="commandline"></param>
        [MonoPInvokeCallback(typeof(Func<string,string>))]
        static private string RunCommand(string commandline)
        {
            Console.Run(commandline);
            return Console.GetResult();
        }

        [DllImport("__Internal")]
        static extern void SetCudlrCallbackAndCreateInput(Func<string,string> callback);

        [DllImport("__Internal")]
        static extern string ShowLog(string text);
#endif
    }
}

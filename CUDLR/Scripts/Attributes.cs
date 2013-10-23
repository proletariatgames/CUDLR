using System;
using System.Net;
using System.Text.RegularExpressions;

namespace CUDLR {

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public delegate void CallbackSimple();
        public delegate void Callback(string[] args);

        public CommandAttribute(string cmd, string help, bool runOnMainThread = true)
        {
          m_command = cmd;
          m_help = help;
          m_runOnMainThread = runOnMainThread;
        }

        public string m_command;
        public string m_help;
        public bool m_runOnMainThread;
        public Callback m_callback;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        public delegate void Callback(RequestContext context);

        public RouteAttribute(string route, string methods = @"(GET|HEAD)", bool runOnMainThread = true)
        {
          m_route = new Regex(route, RegexOptions.IgnoreCase);
          m_methods = new Regex(methods);
          m_runOnMainThread = runOnMainThread;
        }

        public Regex m_route;
        public Regex m_methods;
        public bool m_runOnMainThread;
        public Callback m_callback;
    }
}
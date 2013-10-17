using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

struct QueuedCommand {
  public Console.CommandCallback command;
  public List<string> args;
}

public class Console {

  // Max number of lines in the console output
  const int MAX_LINES = 100;

  // Maximum number of commands stored in the history
  const int MAX_HISTORY = 50;

  // Prefix for user inputted command
  const string COMMAND_OUTPUT_PREFIX = "> ";

  private static Console instance;
  private CommandTree m_commands;
  private List<string> m_output;
  private List<string> m_history;
  private string m_help;
  private Queue<QueuedCommand> m_commandQueue;

  public delegate void CommandCallback(List<string> args);

  private Console() {
    m_commands = new CommandTree();
    m_output = new List<string>();
    m_history = new List<string>();
    m_commandQueue = new Queue<QueuedCommand>();

    RegisterAttributes();
  }

  public static Console GetInstance() {
    if (instance == null) instance = new Console();
    return instance;
  }

  public void Update() {
    while (m_commandQueue.Count > 0) {
      QueuedCommand cmd = m_commandQueue.Dequeue();
      cmd.command( cmd.args );
    }
  }

  /* Queue a command to be executed on update on the main thread */
  public void Queue(CommandCallback command, List<string> args) {
    QueuedCommand queuedCommand = new QueuedCommand();
    queuedCommand.command = command;
    queuedCommand.args = args;
    m_commandQueue.Enqueue( queuedCommand );
  }

  /* Execute a command */
  public void Run(string str) {
    if (str.Length > 0) {
      LogCommand(str);
      RecordCommand(str);
      m_commands.Run(str);
    }
  }

  /* Clear all output from console */
  public void Clear() {
    m_output.Clear();
  }

  /* Print a list of all console commands */
  public void PrintCommands() {
    Log( string.Format("Commands:{0}", m_help) );
  }

  /* Find command based on partial string */
  public string Complete(string partialCommand) {
    return m_commands.Complete( partialCommand );
  }

  /* Logs user input to output */
  public void LogCommand(string cmd) {
    Log(COMMAND_OUTPUT_PREFIX+cmd);
  }

  /* Logs string to output */
  public void Log(string str) {
    m_output.Add(str);
    if (m_output.Count > MAX_LINES) m_output.RemoveAt(0);
  }

  /* Returns the output */
  public string Output() {
    return string.Join("\n", m_output.ToArray());
  }

  /* Register a new console command */
  public void RegisterCommand(string command, string desc, CommandCallback callback, bool runOnMainThread = true) {
    if (command == null || command.Length == 0) {
      throw new Exception("Command String cannot be empty");
    }
    m_commands.Add(command, callback, runOnMainThread);
    m_help += string.Format("\n{0} : {1}", command, desc);
  }

  private void RegisterAttributes() {
    foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {

      // FIXME add support for non-static methods (FindObjectByType?)
      foreach(MethodInfo method in type.GetMethods(BindingFlags.Public|BindingFlags.Static)) {
        ConsoleCommandAttribute[] attrs = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true) as ConsoleCommandAttribute[];
        if (attrs.Length == 0)
          continue;

        CommandCallback action = (CommandCallback) Delegate.CreateDelegate(typeof(CommandCallback), method, false);
        if (action == null)
          continue;

        foreach(ConsoleCommandAttribute cmd in attrs) {
          if (cmd.m_command == null || cmd.m_command.Length == 0)
            continue;

          m_commands.Add(cmd.m_command, action, cmd.m_runOnMainThread);
          m_help += string.Format("\n{0} : {1}", cmd.m_command, cmd.m_help);
        }
      }
    }
  }

  /* Get a previously ran command from the history */
  public string PreviousCommand(int index) {
    return index >= 0 && index < m_history.Count ? m_history[index]  : null;
  }

  /* Update history with a new command */
  private void RecordCommand(string command) {
    m_history.Insert(0, command);
    if (m_history.Count > MAX_HISTORY) m_history.RemoveAt(m_history.Count - 1);
  }
}


[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandAttribute : Attribute
{
    public ConsoleCommandAttribute(string cmd, string help, bool runOnMainThread = true)
    {
      m_command = cmd;
      m_help = help;
      m_runOnMainThread = runOnMainThread;
    }

    public string m_command;
    public string m_help;
    public bool m_runOnMainThread;
}

class CommandTree {

  private Dictionary<string, CommandTree> m_subcommands;
  private Console.CommandCallback m_command;
  private bool m_runOnMainThread;

  public CommandTree() {
    m_subcommands = new Dictionary<string, CommandTree>();
  }

  public void Add(string str, Console.CommandCallback cmd, bool runOnMainThread) {
    _add(new List<string>(str.ToLower().Split(' ')), cmd, runOnMainThread);
  }

  private void _add(List<string> commands, Console.CommandCallback cmd, bool runOnMainThread) {
    if (commands.Count == 0) {
      m_runOnMainThread = runOnMainThread;
      m_command = cmd;
      return;
    }

    string token = commands[0];
    commands.RemoveAt(0);
    if (!m_subcommands.ContainsKey(token)){
      m_subcommands[token] = new CommandTree();
    }
    m_subcommands[token]._add(commands, cmd, runOnMainThread);
  }

  public string Complete(string partialCommand) {
    return _complete( new List<string>(partialCommand.Split(' ')), "");
  }

  public string _complete(List<string> partialCommand, string result) {
    if (partialCommand.Count == 0 && m_command != null) {
      // this is a valid command... so we do nothing
      return result;
    } else if (partialCommand.Count == 0) {
      // This is valid but incomplete.. print all of the subcommands
      Console.GetInstance().LogCommand(result);
      foreach (string key in m_subcommands.Keys) {
        Console.GetInstance().Log( result + " " + key);
      }
      return result + " ";
    } else if (partialCommand.Count == 1) {
      string partial = partialCommand.Last();
      if (m_subcommands.ContainsKey(partial)) {
        result += partial;
        partialCommand.RemoveAt(0);
        return m_subcommands[partial]._complete(partialCommand, result);
      }

      // Find any subcommands that match our partial command
      List<string> matches = new List<string>();
      foreach (string key in m_subcommands.Keys) {
        if (key.StartsWith(partial)) {
          matches.Add(key);
        }
      }

      if (matches.Count == 1) {
        // Only one command found, log nothing and return the complete command for the user input
        return result + matches[0] + " ";
      } else if (matches.Count > 1) {
        // list all the options for the user and return partial
        Console.GetInstance().LogCommand(result + partial);
        foreach (string match in matches) {
          Console.GetInstance().Log( result + match);
        }
      }
      return result + partial;
    }

    string token = partialCommand[0];
    if (!m_subcommands.ContainsKey(token)) {
      return result;
    }
    partialCommand.RemoveAt(0);
    result += token + " ";
    return m_subcommands[token]._complete( partialCommand, result );
  }

  public void Run(string commandStr) {
    // Split user input on spaces ignoring anything in qoutes
    Regex regex = new Regex(@""".*?""|[^\s]+");
    List<string> tokens = new List<string>();
    MatchCollection matches = regex.Matches(commandStr);
    foreach (Match match in matches) {
      tokens.Add(match.Value.Replace("\"",""));
    }
    _run(tokens);
  }

  private void _run(List<string> commands) {
    if (commands.Count == 0) {
      RunCommand(commands);
      return;
    }

    string token = commands[0].ToLower();
    if (!m_subcommands.ContainsKey(token)) {
      RunCommand(commands);
      return;
    }
    commands.RemoveAt(0);
    m_subcommands[token]._run (commands);
  }

  private void RunCommand(List<string> args) {
    if (m_command == null) {
      Console.GetInstance().Log("command not found");
    } else if (m_runOnMainThread) {
      Console.GetInstance().Queue( m_command, args );
    } else {
      m_command(args);
    }
  }

}


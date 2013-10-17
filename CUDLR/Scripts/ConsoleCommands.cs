using UnityEngine;
using System;
using System.Collections.Generic;

/**
 * Default console commands.
 */
public class ConsoleCommands : MonoBehaviour {

  public virtual void Awake() {
    Console.GetInstance().RegisterCommand("clear", "clears console output", Clear, false );
    Console.GetInstance().RegisterCommand("help", "prints commands", Help, false );
  }

  public void Clear(List<string> args) {
    Console.GetInstance().Clear();
  }

  public void Help(List<string> args) {
    Console.GetInstance().PrintCommands();
  }

}

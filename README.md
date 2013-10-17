CUDLR
=====

Console for Unity Debugging and Logging Remotely
----
CUDLR is an open source remote developer console for Unity. CUDDLE starts a webserver on the client to host static
files and exposes a HTTP API for executing commands.

### Features
* Supports Unity Editor, iOS, and Android (PC/Mac support coming soon).
* Console runs in any browser
* Command tab completion
* Arrow keys to cycle through previous commands

How do I use CUDLR?
----
* Download the unitypackage from github or the Unity Asset Store and import it in to your project.
* Create an empty GameObject and add the "Console Server" component.
* Set the port on the component (default value is 55055).
* Add the command component "Console Commands" and any custom command components.
* Run the game and connect to http://localhost:55055 with your browser.

Examples
----

An example Console Server GameObject prefab is located in Assets/CUDLR/Examples. Add the GameObject to the scene,
run the game, and connect to the console with your browser.

An example of adding commands is available [here](https://github.com/proletariatgames/CUDLR/blob/master/CUDLR/Scripts/GameObjectCommands.cs).

Adding Additional Commands
----

Create a new component and register commands on Awake. When the Command String is entered in to the console the
Delegate will be called passing in any additional arguments used in the console.

```
Console.GetInstance().RegisterCommand(<Command String>, <Description>, <Delegate>, <Optional: flag to run on main thread> );
```

The Command Delegate returns void and takes in a list of strings.

```
public delegate void CommandCallback(List<string> args);
```


Delegate functions can output data to the console by calling the Console Log function.

```
Console.GetInstance().Log( <Log String> );
```

License
---
CUDLR is distributed under The MIT License (MIT), see [LICENSE](https://github.com/proletariatgames/CUDLR/blob/master/LICENSE).

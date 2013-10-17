CUDLR
=====

Console for Unity Debugging and Logging Remotely
----
CUDLR is a remote developer console for Unity. Instead of struggling to enter console commands on a mobile devices or having to constantly export debugging logs from a device, CUDLR lets you use your development machine to enter debug commands and see their output or any log messages or stack traces. 

To do this, CUDLR starts a webserver on the target device to host static files and exposes a HTTP API for executing commands which can interface with your project. 

We wrote CUDLR to use in [Proletariat's](http://www.proletariat.com) upcoming game, [World Zombination](http://www.worldzombination.com). For more info on why we wrote it and other tools we've released, check out our [blog](http://blog.proletariat.com).

### Features
* Supports iOS, Android, and the Unity Editor (PC/Mac support coming soon)
* Capture Unity log messages and stack traces
* Console runs in any browser
* Copy/paste from/to the console
* Tab completion
* Command history
* Standard text-entry shortcuts (ctrl-a, ctrl-e, etc)
* Uses standard HTML/CSS for layout
 
How do I use CUDLR?
----
* Download the unitypackage from github or the [Unity Asset Store](https://www.assetstore.unity3d.com/#/content/XXX) and import it in to your project.
* Create an empty GameObject in the scene and add the "Console Server" component.
* Set the port on the component (default value is 55055).
* Add the ConsoleCommand attribute to your code.
* Run the game and connect to http://localhost:55055 with your browser.

An example Console Server GameObject prefab is located in Assets/CUDLR/Examples. Add the GameObject to the scene,
run the game, and connect to the console with your browser.

An example of adding commands is available [here](https://github.com/proletariatgames/CUDLR/blob/master/CUDLR/Scripts/GameObjectCommands.cs).

Adding Additional Commands
----

Add a ConsoleCommand attribute to any static method. When the Command String is entered into the console the
Delegate will be called passing in any additional arguments used in the console.

```
[ConsoleCommand(<Command String>, <Description>, <Optional: flag to run on main thread>)]
```

The Command Delegate returns void and takes in a list of strings.

```
public delegate void CommandCallback(List<string> args);
```


Delegate functions can output data to the console by calling the Console Log function or using Unity's built-in logging.

```
Console.Log( <Log String> );
```

License
---
CUDLR is distributed under The MIT License (MIT), see [LICENSE](https://github.com/proletariatgames/CUDLR/blob/master/LICENSE).

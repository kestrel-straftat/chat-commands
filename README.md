# Chat Commands

This mod adds a command system to the chat, with syntax similar to that of the console in source games.

## What does the mod do?

This mod is mostly intended to be a library mod for other mod developers to add their own commands as an easy
to implement way to run code in their mods. However, I have included a few builtin commands! Some features of
them you might find helpful include:

- Spawning any weapon while in exploration mode
- Ignoring players, hiding their chat messages from you
- Banning players from your lobbies, instantly kicking them if they join
- Changing a few settings like brightness or sensitivity without opening the settings menu~ and setting them to exact
values instead of using a slider
- Binding any command or sequence of commands to a key **or the mousewheel**

## Syntax and basic guide to commands

To begin a command, enter a slash (/) into chat. you can also hit the slash key and the chat box will open with
a slash already in it. After the slash, enter the command name, then the command's parameters **seperated by
spaces**.

To view a list of all commands currently registered, run the command `help` (or its alias `h`). if you want to
see help text for a particular command, or a particular category of commands, enter the command name or category
name as a parameter after the command name.

If you want to enter something with spaces in it as a parameter, e.g. a sentence, surround the parameter with
quotation marks (" or ') to create a string (a sequence of
characters). If you want to enter another string inside a string, you can either use the type of quotation mark you did not use for the outer string, or enter a
backslash (\\) before quotation marks you use inside the string.

Some commands may have optional parameters. These parameters have a default value that will be chosen if you
do not enter a value for them. **Optional parameters will always appear at the end of a command's parameter list.**

You can run multiple commands in one go by separating them with a semicolon (;). You can also take the output of
a command and directly append it to the parameters of the following command by putting a pipe (|) after the first
command. This allows for many complicated and advanced setups, for example `gamma | add 0.5 | gamma | concat
'gamma: ' | echo`. This command will get the current brightness setting (`gamma` with no parameters), send it
to `add` as the second parameter, adding 0.5 to the value, send the value back to `gamma` as a parameter, setting
the brightness to the value, then send the new brightness value to `concat` as the second parameter, joining it to
the string `'gamma: '`, and finally send the output of that to `echo`, printing the result. Altogether, this results
in the brightness setting being incremented by 0.5 and some handy feedback being sent in the chat to show the new
value. I personally have this command bound to a key, as well as one that reduces the brightness by 0.5 (i'll leave
that one to you to figure out :3), as a handy way to quickly change brightness while in a game.

For any further help, you can find me @sneaky_kestrel in the #modding channel in the official straftat discord!

## For mod developers~ getting started

- Download the zip from the Thunderstore page, take out `ChatCommands.dll`, put it somewhere safe and then add
a reference to it in your project.
- Add a `[BepInDependency(ChatCommands.PluginInfo.PLUGIN_GUID)]` attribute to your main plugin class
- If you are planning to upload your mod to Thunderstore, add the package's dependency string, that can be found
on the Thunderstore page, to your manifest.json.
- Call `CommandRegistry.RegisterCommandsFromAssembly()` in your plugin's startup logic.

#### Creating Commands

To create a command, add the `[Command]` attribute to any **static** method. The attribute has a few parameters:

- `name`: Self explanatory. the name of your command.
- `description`: A short description of your command and what it does~ this will be shown in the output of `help`.
- `flags`: Flags that define extra behaviour your command requires, or how it should be used. Not required, and
defaults to `CommandFlags.None`. This is a flags enum, so multiple flags can be applied with a logical or.

The return value of a command will be output to chat by default~ if you'd like to return a value but wouldn't
like it to be printed you can add `CommandFlags.Silent` to your command.

By default, the following types are supported as parameters for commands. See below on how to add support for
custom types.

- Any system type that implements `IConvertible`
- `UnityEngine.KeyCode`~ also supports the standard source engine key names
- `bool`~ a custom parser that also parses numbers i.e. 1 -> true, 0 -> false.
- `Vector2` & `Vector3`

#### Aliases

Aliases can be added to your commands with the `[CommandAliases]` attribute. Apply this to the method and enter
any aliases you want to be associated with your command as parameters. 

#### Categories

By default, your command will be in the category `Misc`. If you wish to specify a category name for your commands,
put a `[CommandCategory]` attribute on the class containing them. This attribute has one argument, the category name.

#### Exceptions

If you'd like to cause a command to fail with some feedback for the user on why, you can throw a `CommandException`.
The command evaluator will catch these and display an error in chat, as well as the message contained in the exception.

#### Command Overriding

You can also override existing commands from other mods. All commands have a `registryPriority` associated with them.
If any command is registered that shares the name or any aliases of another command, the command with the higher priority
will be registered, and the other will be removed from the registry. All commands have a priority of 0 by default.
To set the priority of your command, add a `[CommandOverride]` attribute to it, with the parameter being the new
priority of the command. If you want your command to always override its target, i'd recommend int.MaxValue here. Please
note that **both names and aliases** are taken into account when commands are overriden, so check your aliases carefully
on any commands with above normal priority. It's also worth mentioning that by setting a command's priority to a negative value, you can force it to be overriden
by any other commands by default. I have no idea when this might be useful. It's a thing though.

#### Custom Parsers

If you'd like to use types that don't have built in parsers as parameters to commands, you can implement `ParserBase`
and create your own! You'll need to implement two members: `ParseResultType`, a property that returns the type your
parser converts to, and `object Parse(string value)`, a method that parses a string input to your desired type and
returns the value boxed to an object. After implementing the class for all types you wish to convert to, call
`ParserLocator.RegisterParsersFromAssembly()` in your plugin's startup logic to register all your parsers.
Also worth noting is the fact that `ParserLocator`'s `ParseTo()`, `TryParseTo()` and their generic counterparts are
public for your utility.

## Manual Installation Instructions

_**(you should probably just use a mod manager like [r2modman](https://thunderstore.io/c/straftat/p/ebkr/r2modman/)
or [gale](https://thunderstore.io/c/straftat/p/Kesomannen/GaleModManager/) though)**_

!!! You need [Bepinex 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) !!!

<small>(if you have no idea what the versions mean try BepInEx_x64_5.4.21.0 and it might work. maybe)</small>

Once BepInEx is installed, extract the zip into the BepInEx/plugins folder in the game's root directory.

have fun :3

<img src ="https://files.catbox.moe/vb78bw.jpg" width="250" alt="kity">
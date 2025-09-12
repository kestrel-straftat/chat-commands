### v1.2.5

#### General

- Added a `prop` command (alias `p`) that allows you to spawn props (barrels, aboubi heads) in exploration mode
  - Works identically to the `weapon` command, but with names of props instead
- Added teleportation commands (in the new category `Teleportation`, all are exploration only)
  - `saveloc`: saves your location (alias `tps`)
  - `loadloc`: loads your saved location again (alias `tpl`, useful for movement routing in exploration mode!)
  - `teleport`: teleports you to a specific location with a specific rotation (alias `tp`)
  - `teleportplayer`: teleports a specified player to another player (alias `tpp`). Requires the target player to have chat
commands installed.
- Commands that would previously have used a Steam ID parameter (`ban`, `ignore`, etc.) now allow you to enter either
any Steam ID or the username (not case-sensitive) of a player in your lobby.
- Fixed the `help` command not including some parameters
- Fixed the `help` command not including all command flags

#### API Changes ~ For Mod Developers (some potentially breaking changes this time! please read!)

- Nullable types are now properly supported in command parameters
  - This means you can now create optional parameters with no default value (previously you'd have to use a special value
as the default).
- The `Parsers` namespace has been moved to `Parsing`
- `ParserBase` is now the interface `IParsingExtension`
- `ParserLocator.RegisterParsersFromAssembly()` is now `ParserLocator.RegisterExtensionsFromAssembly()`
- Returning custom messages from parsing extensions is now possible through the message of an `InvalidCastException`
  - If this exception is thrown while parsing, and a message is present, the message will be shown to the user in chat.
  - For an example of how this could be used, check `Utils.ParameterTypes.PlayerParser`.
- Added a new `Player` helper type in `Utils.ParameterTypes`
  - This type has a custom parser that allows both usernames and steam ids to parse to it
  - Useful for commands that need to take in a player as a parameter
  - It is **up to you** to verify that a given player is actually in the lobby with `Player.InCurrentLobby`

### v1.1.5

#### General

- `weapon`, `randomweapon`, `weaponrain` and `clearweapons` can now be run by non-hosts, as long as the host has chat commands installed.

#### API Changes ~ For Mod Developers

- Added a new command flag: `TryRunOnHost`
  - Commands with this flag will be run on the host's machine if possible.
  - The output of commands with this flag **will** be shown to the user, but **will not** be sent for piping to other commands
    (because i can't be bothered to implement that)
  - It's therefore best to only use this flag on commands that **do not return a value**.
  - To get the steam id of the person requesting the command's execution, add a CSteamID parameter called `requester` with a 
  default value (`CSteamID requester = default`) as the **last parameter** of your command.

### v1.0.5

- Fixed `fov` command

### v1.0.4

- Fixed `say` command

### v1.0.3

- Fixed for straftat 1.3.3c
- Improved `ban` command: the mod now warns you when joining the lobby of someone on your banlist
- Removed `weapons` command
- `weapon` command now takes an extra optional parameter: `count`
- `randomweapon` command now takes an extra optional parameter: `count`

### v1.0.2

- Fixed `help` command returning help text for some commands multiple times when getting help by command category

### v1.0.1

- Fixed `jump` command not working when run from chat
- Added Github repo url to Thunderstore page

### v1.0.0

- Initial release
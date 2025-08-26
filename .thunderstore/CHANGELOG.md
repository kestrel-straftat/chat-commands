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
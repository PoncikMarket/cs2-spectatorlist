<p align="center">
  <img src="https://github.com/PoncikMarket/speedmeter-icons/blob/main/speclist.PNG" />
</p>

<p align="center">
  <img src="https://github.com/PoncikMarket/speedmeter-icons/blob/main/speclist1.PNG" />
</p>

<p align="center">
  <img src="https://github.com/PoncikMarket/speedmeter-icons/blob/main/speclist2.PNG" />
</p>

A Counter-Strike 2 plugin that displays a real-time spectator list, allowing players to toggle and customize the display of spectators watching them. Integrated with dynamic command registration and multi-language support.

## Features
- Toggle the spectator list on or off using !css_spectatorlist or !css_speclist.
- Customize the display to show or hide spectator names via !css_spectatorlistedit or !izleyiciayar.
- Automatically creates a default configuration file (cs2-spectatorlist.json) if none exists.
- Supports configurable chat prefix, interval, and command aliases via JSON.
- Displays spectator count or names in the game UI with a timer-based update.
- Multi-language support with localization files (English, Turkish, and more).

## Requirements
- [CounterStrikeSharp API](https://github.com/roflmuffin/CounterStrikeSharp)

## Configuration
The plugin uses a JSON configuration file `(cs2-spectatorlist.json)` with the following options:

- ChatPrefix: The chat prefix for plugin messages (default: "[Spectatorlist]").
- ChatInterval: The interval (in seconds) for updating the spectator list (default: 3.0 seconds).
- Commands: List of commands to toggle the spectator list (default: ["css_spectatorlist", "css_speclist"]).
- SettingsCommands: List of commands to open the settings menu (default: ["css_spectatorlistedit", "css_speclistedit"]).

Example `Config File`:
```bash
json{
  "ChatPrefix": "[Spectatorlist]",
  "ChatInterval": 3.0,
  "Commands": ["css_spectatorlist", "css_speclist"],
  "SettingsCommands": ["css_spectatorlistedit", "css_speclistedit"]
}
```
## Commands

- `!spectatorlist` or `!speclist`: Toggle the spectator list display on or off.
- `!spectatorlistedit` or `!speclistedit`: Open the settings menu to show or hide spectator names.

## Localization
- The plugin supports multiple languages via JSON files in the lang/ directory. Supported languages:

- English (`en.json`)
- Turkish (`tr.json`)
- (Add more languages as needed, e.g., Russian, German)
  
- Each file contains key-value pairs for in-game messages, such as settings prompts, spectator status, and errors.
## Author
- PoncikMarket (Discord: `poncikmarket`)

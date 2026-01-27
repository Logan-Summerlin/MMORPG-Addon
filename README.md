## Main Plugin Template Features
* Simple functional plugin
  * Slash command
  * Main UI
  * Settings UI
  * Image loading
  * Plugin json
* Simple, slightly-improved plugin configuration handling
* Project organization
  * Copies all necessary plugin files to the output directory
    * Does not copy dependencies that are provided by dalamud
    * Output directory can be zipped directly and have exactly what is required
  * Hides data files from visual studio to reduce clutter
    * Also allows having data files in different paths than VS would usually allow if done in the IDE directly

The intention is less that any of this is used directly in other projects, and more to show how similar things can be done.

### Reconfiguring for your own uses

Replace all references to `SamplePlugin` in all the files and filenames with your desired name, then start building the plugin of your dreams. You'll figure it out 

Dalamud will load the JSON file (by default, `SamplePlugin/SamplePlugin.json`) next to your DLL and use it for metadata, including the description for your plugin in the Plugin Installer. Make sure to update this with information relevant to _your_ plugin!

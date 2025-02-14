# ComponentSearchWizard

![20230527025150_1](https://github.com/Nytra/NeosComponentSearchWizard/assets/14206961/2238238c-3f73-423f-824f-1360624918a4)

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that provides a wizard for searching for components by type and/or by name, displaying them in a list, and then provides buttons to batch enable, disable or destroy them.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
2. Place [ComponentSearchWizard.dll](https://github.com/Nytra/NeosComponentSearchWizard/releases/latest) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Neos logs.

## Usage
The wizard can be found in the DevTip 'Create New' menu under Editors/Component Wizard (Mod). <br>

You can drop a component reference into the Component Type field to search for components with that exact type, or check Ignore Type Arguments to search for components with the same base type only. For example, if you drop a ValueField\<bool\>, then with Ignore Type Arguments it will search for all ValueField components. <br>
  
The Search Nice Name option will take the component name as, for example, ValueField\<bool\> as opposed to ValueField\`1. <br>
  
You can check Exact Match to match the whole string as you typed it, instead of checking if the component name only contains that string. <br>
  
You can check Spawn Detail Text which gives a text with the component names, their Enabled state, and the hierarchy path to get to them. <br>

## Notes

This mod should be used carefully as it allows manipulating a large number of components at once. <br>

The mod now supports undo! <br>

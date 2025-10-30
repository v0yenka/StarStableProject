# Star Stable Project

**Experimental Project** – Use with caution!  
This tool is designed for testing and experimenting with the old Star Stable game engine (Seasonal Riders series). The game is very old (2007) and may behave glitchy or laggy. Backup your files before use. Been tested with SS Autumn Riders and SS Spring Riders.

---

## What is this program?

The **Star Stable Project** is a small console application that allows you to:

- Swap in-game scripts for testing purposes (check out the scripts.txt file)
- Backup and restore original game files automatically  
- May experiment with new functionality, like adding forces or modifying events  
- Prepare the project for safe collaboration and versioning via GitHub

This tool is intended **for educational and testing purposes only** and should only be used with your own legal copies of the game.

---

> **Important note about the default script:**  
> The currently implemented “natural” script makes the horse **fly**, so this tool is mostly for testing behaviors and experimenting with game physics. You can easily change the script in the program itself.
> Have fun experimenting with it! Just click the in-game map to impelment ypur chosen script.

> **Warning:** The game is very old (2007). Sometimes:  
> - The map may disappear  
> - Physics or scripts may glitch  
> - You may need to repeat the process to see changes  
> Use a virtual machine if needed to avoid restarting your PC frequently.

## How to Run the Program

### 1. Using the `.exe` (recommended for teammates)

1. Copy `TempProj.exe` to your desired folder  
2. Run `TempProj.exe`  
3. The program will prompt you for:
   - Your **game executable path** (`PXStudioEngine.exe`)  
   - Your **data file path** (`data.csa`)  

Example input:

Type your game path: C:\Games\StarStable\PXStudioEngine.exe
Type your data path: C:\Games\StarStable\data.csa


---

### 2. Building and running from source

If you want to build yourself:

```bash
git clone https://github.com/v0yenka/StarStableProject.git
cd TempProj
dotnet build -c Release
dotnet run
```
Make sure to enter your own file paths for the game executable and data file.

## Future Development

> **Important:** Since the game engine is very unstable there are no incoming future updates.
> I may add a little GUI to make the scripting process more convenient.

# Star Stable Project (v0yenka injector)

**Experimental Project** – Use with caution!  
This tool is designed for testing and experimenting with the old Star Stable game engine (Seasonal Riders series). The game is very old (2007) and may behave glitchy or laggy. Backup your files before use.

---

## Project Status: Final Version / Discontinued
**Important Note:**
This project has been completely refactored to run as a console application. While I initially planned to add a database for script management, I have decided to stop further development. Working with a game engine from 2007 is a total nightmare, and I’ve reached a point where the effort is no longer worth the struggle. Consider this the final, "as-is" release.

This tool is intended **for educational and testing purposes only** and should only be used with your own legal copies of the game.

---

> **Important note about the default script:**  
> The currently implemented “natural” script makes the horse **fly**, so this tool is mostly for testing behaviors and experimenting with game physics. You can easily change the script in the program itself.
> Don't hesitate to experiment with it!

> **Warning:** Since the game engine is a crap sometimes:  
> - The map may disappear  
> - Physics or scripts may glitch  
> - You may need to repeat the process to see changes  
> Use a virtual machine if needed to avoid restarting your PC frequently.

## How to Run

### Prerequisites
* **Star Stable (Seasonal Riders)** must be running in the background.
* The tool requires **Administrator Privileges** to access the game process memory.

### 1. Using the `.exe`
1. Start your game
2. Run `v0yenka-injector.exe` as Administrator.
3. The console will automatically search for the game process and initialize the script swapping (Remember that the deafult script is flying).

### 2. Building from Source
If you want to compile and run the project manually:
```bash
# Clone the repository
git clone [https://github.com/v0yenka/StarStableProject.git](https://github.com/v0yenka/StarStableProject.git)](https://github.com/v0yenka/StarStableProject)
cd StarStableProject
```
Build and run (with game running in the background):
```bash
dotnet run --project v0yenka-injector
```

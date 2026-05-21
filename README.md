# Go Board Game
This project is a desktop implementation of the traditional board game **Go** written in C# using the modern cross-platform framework **Avalonia UI**.
The application is currently in the state of **fully functional prototype**.

## 🚀 Main features
* **Local game (1v1):** Ability to play on one computer against another player.
* **Play against AI (KataGo):** Integration with one of the best open-source Go engines.
* **Game Settings:**
    * Choice of game board size (standard 19x19, 13x13, 9x9).
    * Choice of stone color (black/white) when playing against a bot.
    * Setting the level/difficulty of the KataGo bot. 
* **Scoring:** The calculation of the game result is delegated directly to the KataGo engine, which ensures more accurate determination of territory and dead stones than a purely local algorithm.

## 🛠️ Technologies used
* **Language:** C#
* **GUI Framework:** [Avalonia UI](https://avaloniaui.net/) (XAML / MVVM)
* **AI Engine:** [KataGo](https://github.com/lightvector/KataGo)

---
## 📸 Screenshots
| Home | Results |
| :---: | :---: |
| ![Home](Preview/Home.png) | ![Results](Preview/Results.png) |
| PvE mode | PvP mode |
| ![PvE](Preview/PvE.png) | ![PvP](Preview/PvP.png) |

---

## ⚠️ Current Project Status & Notice

**The current application is only a prototype**
Since this is a development version, there are some bugs, inconsistencies, and technical debt in the application. The project was created within the framework of a C# and was primarily used to test more complex game logic and integration with an external engine.
### For developers and contributors:
I am open to any modifications, forks, or optimizations to this code. However, please note the following:
* **All modifications and execution of the code are at your own risk.**
* Due to the bugs present, I recommend **first thoroughly checking and testing** the code before any deployment or modification.


This repository currently serves mainly as an archive.

---
 
## ⚙️ How to start a project
1. Clone the repository.
 ```bash
git clone https://github.com/Zero89-sys/Go-BoardGame
```
2. **Important step for playing against bot:** Due to GitHub's file size limits, the trained neural network for KataGo is not included in the repository.
    * Download any network model (`.bin.gz` file) from the official KataGo website.
    * Name the downloaded file exactly `KataGo.bin.gz`.
    * Put it in the `Engine/` folder next to the executable program.
3. Open the project in the IDE and run the application.

## 📄 License
Distributed under the GPL-3.0 license. See `LICENSE` for more information.
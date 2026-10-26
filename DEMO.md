# Web demo

The demo combines the time and daily economy feature with the latest main assets and UI.
It starts at the main menu, continues through Dawny's intro, then opens SampleScene.

## Build

Use Unity 6000.6.2f1 with Web Build Support installed. Run `git lfs pull` after
cloning or pulling to download the actual font and image files.

Choose **Elysium > Build Web Demo**. This uses the **Web - Desktop - Development**
profile and writes to `Builds/WebGL-Demo`. The build command checks the three
enabled scenes, missing scripts, placeable graphics, and clock/economy behavior.

## Run locally

Run `./Start-WebDemo.ps1` from PowerShell in the project folder, then open
http://localhost:8080. Keep that PowerShell window open during the demo; Ctrl+C
stops the server. Use `-Port 8081` if port 8080 is occupied.

The script uses Python bundled with Unity Web Build Support, so no separate
Python installation is required. Alternatively, serve the entire
`Builds/WebGL-Demo` folder using a local web server. Opening index.html directly
from File Explorer will not work reliably with WebGL.

## Demo scope

Use the on-screen tutorial for placement and demolition controls. The top-center clock shows the day and time. Prices and balance are shown in the building store. Progress lasts for the current session.

The shop is connected to apartment, road, Mayor's Office, and market placement. Publishing the build to a public website is a separate step.

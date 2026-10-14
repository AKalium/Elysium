# Web demo

The demo combines `feature/time-and-daily-economy` with `origin/missing-graphics`.
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

Use the on-screen tutorial for placement and demolition controls. The economy
HUD tracks time, balance, and daily income/upkeep; its speed button cycles
1x, 2x, and 4x. Progress lasts for the current session.

The merged branch includes the UI lead's shop/card prefab and scripts, but those
are not wired into SampleScene. The demo retains the existing numbered building
selection controls. Publishing the build to a public website is a separate step.

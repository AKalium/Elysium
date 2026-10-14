param([int]$Port = 8080)
$ErrorActionPreference = 'Stop'
$demoPath = Join-Path $PSScriptRoot 'Builds/WebGL-Demo'
if (-not (Test-Path (Join-Path $demoPath 'index.html'))) {
    throw 'Build the demo first using Elysium > Build Web Demo in Unity.'
}
$versionLine = Get-Content (Join-Path $PSScriptRoot 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$unityVersion = ($versionLine -split ': ', 2)[1]
$python = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$unityVersion/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/python/python.exe"
if (-not (Test-Path $python)) {
    throw 'Unity Web Build Support was not found in the default installation folder. Use Unity Build And Run, or serve Builds/WebGL-Demo with a local web server.'
}
Write-Host "Open http://localhost:$Port in your browser. Keep this window open; press Ctrl+C to stop the demo server."
& $python -m http.server $Port --bind 127.0.0.1 --directory $demoPath

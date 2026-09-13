$ErrorActionPreference = 'Stop'
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskOutput = Join-Path $PSScriptRoot 'GH3-HoldToHit.exe'
$taskSource = Join-Path $PSScriptRoot 'HoldToHit.cs'
$taskPayload = Join-Path $PSScriptRoot 'Payload.cs'
$taskBinary = Join-Path $PSScriptRoot 'hold_hook.bin'
& $taskCompiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$taskOutput" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/resource:$taskBinary,HoldHook" $taskSource $taskPayload
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built $taskOutput"

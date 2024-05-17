$CustomizationScriptsDir = "C:\DevBoxCustomizations"
$LockFile = "lockfile"
$SetVariablesScript = "setVariables.ps1"
$RunAsUserScript = "runAsUser.ps1"
$CleanupScript = "cleanup.ps1"
$RunAsUserTask = "DevBoxCustomizations"
$CleanupTask = "DevBoxCustomizationsCleanup"

if (!(Test-Path "$($CustomizationScriptsDir)\$($LockFile)")) {
    Unregister-ScheduledTask -TaskName $RunAsUserTask -Confirm:$false
    Unregister-ScheduledTask -TaskName $CleanupTask -Confirm:$false
    Remove-Item $CustomizationScriptsDir -Force -Recurse
}

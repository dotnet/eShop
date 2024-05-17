param(
    [Parameter()]
    [string]$Command,
    [Parameter()]
    [string]$WorkingDirectory
 )

# Check if workingDirectory is set and not empty and if so, change to it.
if ($WorkingDirectory -and $WorkingDirectory -ne "") {
    # Check if the working directory exists.
    if (-not (Test-Path $WorkingDirectory)) {
        # Create the working directory if it does not exist.
        Write-Output "Creating working directory $WorkingDirectory"
        New-Item -ItemType Directory -Force -Path $WorkingDirectory
    }

    Write-Output "Changing to working directory $WorkingDirectory"
    Set-Location $WorkingDirectory
}

# Note we're calling powershell.exe directly, instead
# of running Invoke-Expression, as suggested by
# https://learn.microsoft.com/en-us/powershell/scripting/learn/deep-dives/avoid-using-invoke-expression?view=powershell-7.3
# Note that this will run powershell.exe
# even if the system has pwsh.exe.
Write-Output "Running command $Command"
powershell.exe -Command $Command
$CommandExitCode = $LASTEXITCODE
Write-Output "Command exited with code $CommandExitCode"

# Task powershell scripts should always end with an
# exit code reported up to the runner agent.
# This is how the runner agent knows whether the
# command succeeded or failed.
exit $CommandExitCode

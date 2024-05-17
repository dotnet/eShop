<#
.SYNOPSIS
    Install an extension defined by the given item name to the latest Visual Studio instance
.PARAMETER MarketplaceItemName 
    Markplace Item Name (as used in the URI of a given Visual Studio Extension Maketplace entry)
#>
param (
        [Parameter(Mandatory)]
        [string]$MarketplaceItemName
    )

<#
.SYNOPSIS
    Download an extension defined by the given item name
.PARAMETER MarketplaceItemName 
    Markplace Item Name (as used in the URI of a given Visual Studio Extension Maketplace entry)
#>

function Get-VSIXFromMarketplace
{
    param (
        [Parameter(Mandatory)]
        [string]$MarketplaceItemName
    )

    # Declare exit code. Default to failure and set to 0 when operation succeeds.
    $exitCode = 1

    # Turn off progress to fix speed bug in Invoke-WebRequest
    $ProgressPreference = 'SilentlyContinue'

    $vswhereResult = Invoke-VsWhere "-prerelease -latest -format json" | ConvertFrom-Json
    
    $instanceVersion = $vswhereResult.InstallationVersion

    $isolationInfo = ConvertFrom-StringData((Get-Content "$($vswhereResult.ProductPath -replace ".exe",".isolation.ini")" | Select-Object -Skip 2) -replace "\\","\\\\" -join "`n")
    $arch = If ($isolationInfo.ProductArch -eq "x64") { "amd64" } else { $isolationInfo.ProductArch }

    $marketplaceQueryBody = 
@"
{"flags":"673","filters":[{"criteria":[{"filterType":"7","value":"$MarketplaceItemName"},{"filterType":"15","value":"$instanceVersion"},{"filterType":"22","value":"$arch"},{"filterType":"8","value":"Microsoft.VisualStudio.Enterprise"},{"filterType":"8","value":"Microsoft.VisualStudio.Ultimate"},{"filterType":"8","value":"Microsoft.VisualStudio.Pro"},{"filterType":"8","value":"Microsoft.VisualStudio.Community"},{"filterType":"8","value":"Microsoft.VisualStudio.IntegratedShell"}],"sortBy":"4","sortOrder":"2","pageSize":"2","pageNumber":"1"}]}
"@

    $requestParams = @{
        Uri = "https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery"
        Method = "Post"
        Headers = @{"Accept" = 'application/json;api-version=3.2-preview.1'}
        ContentType = "application/json"
        Body = $marketplaceQueryBody
        UseBasicParsing = $true
    }

    $jsonResponse = (Invoke-WebRequest @requestParams).Content | ConvertFrom-Json 
    $extensions = $jsonResponse.results.extensions

    if(-not $extensions -or ($extensions -is [array] -and $extensions.length -le 0)) {
        Write-Warning "No extension was found for item: $MarketplaceItemName. Specify a valid extension."
        exit $exitcode
    }

    if($extensions -is [array] -and $extensions.length -gt 1) {
        Write-Warning "Multiple extensions ($($extensions.length)) found for the given item name $MarketplaceItemName"
        exit $exitcode
    }

    $cdnUrl = "$($extensions.versions[0].fallbackAssetUri)/Microsoft.VisualStudio.IDE.Payload?redirect=true&install=true"
    $downloadFilePath =  Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::ChangeExtension("VSIX$([IO.Path]::GetRandomFileName())", ".vsix"))
    $extensionName = $extensions.displayName
    Write-Host "Downloading $extensionName"

    Invoke-WebRequest $cdnUrl -UseBasicParsing -OutFile $downloadFilePath

    # Turn progress back on
    $ProgressPreference = 'Continue'

    return $downloadFilePath
}

<#
.SYNOPSIS
    Invokes Visual Studio Locator, if it exists, with the provided arguments.
.DESCRIPTION
    Invokes Visual Studio Locator (vswhere.exe) with the provided arguments.
    If this script is run without the locator present, it will fail.
.PARAMETER Arguments
    Arguments to pass onwards to Visual Studio Locator.
.LINK
    https://learn.microsoft.com/en-us/visualstudio/install/tools-for-managing-visual-studio-instances#using-vswhereexe
#>
function Invoke-VsWhere
{
    param
    (
        [Parameter(Mandatory)]
        [string]$Arguments
    )

    Assert-VsWherePresent

    return Invoke-Expression -Command "&'$(Get-VsWherePath)' $Arguments"
}

<#
.SYNOPSIS
    Returns the default path of Visual Studio Locator (vswhere.exe).
#>
function Get-VsWherePath
{
    return Join-Path -Path "${env:ProgramFiles(x86)}" -ChildPath "Microsoft Visual Studio\Installer\vswhere.exe"
}

<#
.SYNOPSIS
    Throws an exception if Visual Studio Locator (vswhere.exe) is not present in the default location.
#>
function Assert-VsWherePresent
{
    if(-not (Test-Path (Get-VsWherePath)))
    {
        throw "Visual Studio Locator not found."
        exit $exitcode
    }
}

<#
.SYNOPSIS
    Invokes VSIX Installer, if it exists, with the provided arguments.
.DESCRIPTION
    Invokes VSIX Installer with the provided arguments.
    If this script is run without VSIX Installer present, it will fail.
.PARAMETER Arguments
    Arguments to pass onwards to VSIX Installer.
#>
function Invoke-VsixInstaller
{
    param
    (
        [Parameter(Mandatory)]
        [string]$Arguments
    )

    Assert-VsixInstallerPresent

    return Invoke-Expression -Command "&'$(Get-VsixInstallerPath)' $Arguments"
}

<#
.SYNOPSIS
    Returns the default path of VSIX Installer.
#>
function Get-VsixInstallerPath
{
    return Join-Path -Path "${env:ProgramFiles(x86)}" -ChildPath "Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VSIXInstaller.exe"
}

<#
.SYNOPSIS
    Throws an exception if VSIX Installer is not present in the default location.
#>
function Assert-VsixInstallerPresent
{
    if(-not (Test-Path (Get-VsixInstallerPath)))
    {
        throw "VSIX Installer not found."
        exit $exitcode
    }
}

# ---- Main Script Start ----

$pathToVSIX = Get-VSIXFromMarketplace $MarketplaceItemName

if(-not $pathToVsix -or -not (Test-Path $pathToVSIX -PathType Leaf)) {
    Write-Warning "Issue with VSIX path for item $MarketplaceItemName. No extension downloaded; skipping install."
    exit $exitcode
}

Write-Host "Invoking VSIX Installer for downloaded VSIX at $pathToVsix..."

try {
    Invoke-VsixInstaller "/a /enableUpdate /q /f /sp $pathToVSIX" | Out-Null
    Wait-Process (Get-Process VsixInstaller).id -Timeout 600
}
catch {
    Write-Warning "VSIX Installer failed with error: $_"
    exit $exitcode
}

Write-Host "VSIX Installer Completed."
Write-Host "$MarketplaceItemName Successfully installed."

$exitcode = 0
exit $exitCode

# ---- Main Script End ----

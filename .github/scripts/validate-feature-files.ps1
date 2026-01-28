#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates naming conventions for agent feature files.

.DESCRIPTION
    This script checks that all files in .github/features follow the correct naming conventions.

.PARAMETER FeatureName
    The name of the specific feature to validate. If omitted, validates all features.

.PARAMETER Fix
    Automatically fix common naming issues (kebab-case conversion).

.EXAMPLE
    .\validate-feature-files.ps1

.EXAMPLE
    .\validate-feature-files.ps1 -FeatureName promotional-discounts

.EXAMPLE
    .\validate-feature-files.ps1 -Fix
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$FeatureName,
    
    [Parameter(Mandatory = $false)]
    [switch]$Fix
)

$ErrorActionPreference = "Stop"

# Valid file patterns for feature folders
$validPatterns = @(
    '^[a-z0-9-]+\.findings\.md$',
    '^[a-z0-9-]+\.plan\.md$',
    '^[a-z0-9-]+\.memory\.md$',
    '^README\.md$'
)

$script:invalidFileCount = 0
$script:fixCount = 0

function Convert-ToKebabCase {
    param([string]$text)
    
    $result = $text.ToLower() -replace '[_\s]+', '-'
    $result = $result -replace '[^a-z0-9-]', ''
    $result = $result -replace '-+', '-'
    $result = $result.Trim('-')
    
    return $result
}

function Test-FileName {
    param(
        [string]$filePath,
        [string]$featureName
    )
    
    $fileName = Split-Path -Leaf $filePath
    $isValid = $false
    
    foreach ($pattern in $validPatterns) {
        if ($fileName -match $pattern) {
            if ($fileName -ne "README.md") {
                $fileFeatureName = ($fileName -split '\.')[0]
                if ($fileFeatureName -eq $featureName) {
                    $isValid = $true
                    break
                } else {
                    Write-Warning "File '$fileName' feature name mismatch. Expected: '$featureName', Got: '$fileFeatureName'"
                    
                    if ($Fix) {
                        $extension = ($fileName -split '\.' | Select-Object -Skip 1) -join '.'
                        $newFileName = "$featureName.$extension"
                        $newPath = Join-Path (Split-Path $filePath) $newFileName
                        
                        Write-Host "  Renaming: $fileName -> $newFileName" -ForegroundColor Yellow
                        Move-Item -Path $filePath -Destination $newPath
                        $script:fixCount++
                        $isValid = $true
                        break
                    }
                }
            } else {
                $isValid = $true
                break
            }
        }
    }
    
    return $isValid
}

function Test-FeatureFolder {
    param([string]$folderPath)
    
    $featureName = Split-Path -Leaf $folderPath
    
    if ($featureName -notmatch '^[a-z0-9-]+$') {
        Write-Warning "Feature folder name '$featureName' is not in kebab-case"
        
        if ($Fix) {
            $newFeatureName = Convert-ToKebabCase $featureName
            $newFolderPath = Join-Path (Split-Path $folderPath) $newFeatureName
            
            if ($newFeatureName -ne $featureName) {
                Write-Host "  Renaming folder: $featureName -> $newFeatureName" -ForegroundColor Yellow
                Move-Item -Path $folderPath -Destination $newFolderPath
                $script:fixCount++
                $featureName = $newFeatureName
                $folderPath = $newFolderPath
            }
        } else {
            $script:invalidFileCount++
        }
    }
    
    $files = Get-ChildItem -Path $folderPath -File
    
    Write-Host "`nValidating feature: $featureName" -ForegroundColor Cyan
    Write-Host "Location: $folderPath"
    
    foreach ($file in $files) {
        $isValid = Test-FileName -filePath $file.FullName -featureName $featureName
        
        if (-not $isValid) {
            Write-Host "  X Invalid: $($file.Name)" -ForegroundColor Red
            Write-Host "    Expected: $featureName.findings.md, $featureName.plan.md, $featureName.memory.md or README.md" -ForegroundColor Gray
            $script:invalidFileCount++
        } else {
            Write-Host "  + Valid: $($file.Name)" -ForegroundColor Green
        }
    }
    
    $requiredFiles = @("findings", "plan", "memory")
    $foundFiles = @()
    foreach ($file in $files) {
        if ($file.Name -match "^$featureName\.(.+)\.md$") {
            $foundFiles += $matches[1]
        }
    }
    
    foreach ($required in $requiredFiles) {
        if ($required -notin $foundFiles) {
            Write-Host "  ! Missing: $featureName.$required.md" -ForegroundColor Yellow
        }
    }
}

$featuresPath = Join-Path $PSScriptRoot "..\features"

if (-not (Test-Path $featuresPath)) {
    Write-Host "`nNo features folder found at: $featuresPath" -ForegroundColor Yellow
    Write-Host "Creating features folder..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $featuresPath -Force | Out-Null
    exit 0
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Feature Files Naming Convention Validator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($FeatureName) {
    $featureFolder = Join-Path $featuresPath $FeatureName
    if (Test-Path $featureFolder) {
        Test-FeatureFolder -folderPath $featureFolder
    } else {
        Write-Error "Feature folder not found: $featureFolder"
        exit 1
    }
} else {
    $featureFolders = Get-ChildItem -Path $featuresPath -Directory
    
    if ($featureFolders.Count -eq 0) {
        Write-Host "`nNo feature folders found in: $featuresPath" -ForegroundColor Yellow
        exit 0
    }
    
    foreach ($folder in $featureFolders) {
        Test-FeatureFolder -folderPath $folder.FullName
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($Fix) {
    Write-Host "Files fixed: $fixCount" -ForegroundColor Yellow
}

if ($invalidFileCount -gt 0) {
    Write-Host "Invalid files found: $invalidFileCount" -ForegroundColor Red
    Write-Host "`nRun with -Fix parameter to automatically fix naming issues." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "All files follow naming conventions!" -ForegroundColor Green
    exit 0
}

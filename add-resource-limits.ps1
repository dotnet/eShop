# PowerShell script to add resource limits to all API deployments
# This script adds CPU and memory limits (1 CPU, 200MB) to all APIs

param(
    [string]$ManifestPath = "aspirate-output"
)

Write-Host "Adding resource limits to API deployments..." -ForegroundColor Green

# Find all kustomization.yaml files in the output directory
$kustomizationFiles = Get-ChildItem -Path $ManifestPath -Name "kustomization.yaml" -Recurse

foreach ($kustomizationFile in $kustomizationFiles) {
    $fullPath = Join-Path $ManifestPath $kustomizationFile
    $directory = Split-Path $fullPath -Parent
    
    Write-Host "Processing directory: $directory" -ForegroundColor Yellow
    
    # Find deployment YAML files in this directory
    $deploymentFiles = Get-ChildItem -Path $directory -Filter "*.yaml" | Where-Object { $_.Name -ne "kustomization.yaml" }
    
    foreach ($deploymentFile in $deploymentFiles) {
        $content = Get-Content $deploymentFile.FullName -Raw
        
        # Check if this is a Deployment manifest for an API service
        if ($content -match "kind: Deployment" -and ($deploymentFile.BaseName -match "api$" -or $deploymentFile.BaseName -match "bff$" -or $deploymentFile.BaseName -match "processor$")) {
            Write-Host "  Adding resource limits to: $($deploymentFile.Name)" -ForegroundColor Cyan
            
            # Parse YAML content and add resource limits
            $lines = Get-Content $deploymentFile.FullName
            $newContent = @()
            $inContainerSpec = $false
            $resourcesAdded = $false
            
            for ($i = 0; $i -lt $lines.Count; $i++) {
                $line = $lines[$i]
                $newContent += $line
                
                # Look for container spec section
                if ($line -match "^\s*containers:\s*$") {
                    $inContainerSpec = $true
                }
                
                # Add resources after ports section or before env section
                if ($inContainerSpec -and !$resourcesAdded -and 
                    (($line -match "^\s*ports:\s*$" -and $i + 2 -lt $lines.Count -and $lines[$i + 2] -match "^\s*env") -or
                     ($line -match "^\s*env:\s*$") -or
                     ($line -match "^\s*envFrom:\s*$"))) {
                    
                    # Determine indentation level
                    $indent = ""
                    if ($line -match "^(\s*)") {
                        $indent = $Matches[1]
                    }
                    
                    # Add resources section
                    $newContent += "$indent" + "resources:"
                    $newContent += "$indent" + "  limits:"
                    $newContent += "$indent" + "    cpu: `"1000m`""
                    $newContent += "$indent" + "    memory: `"200Mi`""
                    $newContent += "$indent" + "  requests:"
                    $newContent += "$indent" + "    cpu: `"100m`""
                    $newContent += "$indent" + "    memory: `"100Mi`""
                    $resourcesAdded = $true
                }
                
                # Reset flag when we exit container spec
                if ($line -match "^\s*volumes:\s*$" -or $line -match "^\s*restartPolicy:\s*$") {
                    $inContainerSpec = $false
                }
            }
            
            # Write the modified content back to file
            if ($resourcesAdded) {
                $newContent | Set-Content $deploymentFile.FullName -Encoding UTF8
                Write-Host "    ✓ Resource limits added" -ForegroundColor Green
            } else {
                Write-Host "    ⚠ Could not add resource limits - container structure not recognized" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host "Resource limits processing completed!" -ForegroundColor Green
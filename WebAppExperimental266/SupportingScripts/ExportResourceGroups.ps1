#GPT-40 generated powershell script, modified 08OCT2025

#Connect to Azure, authenticate in
# Connect-AzAccount -DeviceCode
# Used the recommended website, https://microsoft.com/devicelogin
# Provided the code shown in the output window into the dialog in the browser.
# Saw in powershell that the authentication completed and put us in the correct tenant.

# We did not need to Set-AzContext -SubscriptionId because we were already there.
# Set-AzContext -SubscriptionId "Your Subscription ID"
# Get-AzContext

# The script will not export resources of some types, like KeyVault.

#
#$resourceGroups = Get-AzResourceGroup
#foreach ($rg in $resourceGroups) {
#    $template = Export-AzResourceGroup -ResourceGroupName $rg.ResourceGroupName
#    $template | ConvertTo-Json -Depth 100 | Out-File "$($rg.ResourceGroupName)-template.json"
#}

<#
function Export-AzResourceGroupsToJson { Get-AzResourceGroup | `
ForEach-Object { $template = Export-AzResourceGroup `
-ResourceGroupName $_.ResourceGroupName; $template | `
ConvertTo-Json -Depth 100 | Out-File "$(ProjectDir)ConfigurationsAndCodeChecks\$($_.ResourceGroupName)-template.json"} };
Export-AzResourceGroupsToJson
#>

# Commit 4bad2746bf55af277abdc4efbf76c2a815bcdfa1 worked successfully.

<#
function Export-AzResourceGroupsToJson {
    $outputDir = "$($env:ProjectDir)ConfigurationsAndCodeChecks"

    # Create the directory if it doesn't exist
    if (-not (Test-Path -Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir
    }

    Get-AzResourceGroup | `
    ForEach-Object {
        $template = Export-AzResourceGroup -ResourceGroupName $_.ResourceGroupName
        $template | `
        ConvertTo-Json -Depth 100 | Out-File -FilePath "$outputDir/$($_.ResourceGroupName)-template.json" -Encoding utf8 
        Write-Output "$outputDir/$($_.ResourceGroupName)-template.json"
    }
}
Export-AzResourceGroupsToJson
#>

<#
function Export-AzResourceGroupsToJson {
    $outputDir = "$($env:ProjectDir)ConfigurationsAndCodeChecks"
    
    # Create the directory if it doesn't exist
    if (-not (Test-Path -Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir
    }

    Get-AzResourceGroup | ForEach-Object {
        $template = Export-AzResourceGroup -ResourceGroupName $_.ResourceGroupName
        $jsonFileName = "$($_.ResourceGroupName)-template.json"
        $jsonFilePath = "$outputDir\$jsonFileName"

        # Export the JSON to the output directory
        $template | ConvertTo-Json -Depth 100 | Out-File -FilePath $jsonFilePath -Encoding utf8
        
        # Additional code to move any existing JSON related files if necessary
        $existingJsonPath = "$($env:ProjectDir)$jsonFileName"
        
        if (Test-Path -Path $existingJsonPath) {
            Move-Item -Path $existingJsonPath -Destination $outputDir -Force
        }
    }
}
Export-AzResourceGroupsToJson
#>


function Export-AzResourceGroupsToJson {
    $outputDir = "$($env:ProjectDir)ConfigurationsAndCodeChecks"
    
    # Create the directory if it doesn't exist
    if (-not (Test-Path -Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force
    }

    Get-AzResourceGroup | ForEach-Object {
        $template = Export-AzResourceGroup -ResourceGroupName $_.ResourceGroupName
        $jsonFileName = "$($_.ResourceGroupName)-template.json"
        $jsonFilePath = "$outputDir\$jsonFileName"

        # Export the JSON to the output directory
        $template | ConvertTo-Json -Depth 100 | Out-File -FilePath $jsonFilePath -Encoding utf8

        # Check if the JSON file exists and if not, log a message
        if (Test-Path -Path $jsonFilePath) {
            Write-Host "Successfully created: $jsonFilePath"
        } else {
            Write-Host "Failed to create: $jsonFilePath"
        }
        
        # Move existing JSON files from the main directory to the output directory
        # Adjusted jsonFileName to pick up the files I wanted moved.
        $jsonFileName = "$($_.ResourceGroupName).json"
        $existingJsonPath = "$($env:ProjectDir)$jsonFileName"
        
        if (Test-Path -Path $existingJsonPath) {
            try {
                Move-Item -Path $existingJsonPath -Destination $outputDir -Force
                Write-Host "Moved existing file: $existingJsonPath to $outputDir"
            } catch {
                Write-Host "Error moving file: $_"
            }
        } else {
            Write-Host "No existing file to move: $existingJsonPath"
        }
    }
}
Export-AzResourceGroupsToJson




<#

<ProjectDir>C:\Users\gladi\source\repos\RFID-RED-01SEP2025</ProjectDir>


function Export-AzResourceGroupsToJson {
    $outputDir = "$($env:ProjectDir)ConfigurationsAndCodeChecks"
    
    # Create the directory if it doesn't exist
    if (-not (Test-Path -Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force
    }

    Get-AzResourceGroup | ForEach-Object {
        $template = Export-AzResourceGroup -ResourceGroupName $_.ResourceGroupName
        $jsonFileName = "$($_.ResourceGroupName)-template.json"
        $jsonFilePath = "$outputDir\$jsonFileName"

        # Export the JSON to the output directory
        $template | ConvertTo-Json -Depth 100 | Out-File -FilePath $jsonFilePath -Encoding utf8

        # Log the path of the created JSON file
        Write-Host "Created JSON file at: $jsonFilePath"
        
        # Check the main directory directly
        $existingJsonPath = Join-Path -Path $env:ProjectDir -ChildPath $jsonFileName
        
        # Log the constructed path for debugging
        Write-Host "Checking for existing file at: $existingJsonPath"

        if (Test-Path -Path $existingJsonPath) {
            try {
                Move-Item -Path $existingJsonPath -Destination $outputDir -Force
                Write-Host "Moved existing file: $existingJsonPath to $outputDir"
            } catch {
                Write-Host "Error moving file: $_"
            }
        } else {
            Write-Host "No existing file to move: $existingJsonPath"
        }
    }
}

Export-AzResourceGroupsToJson
#>
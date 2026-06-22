#REF:  https://learn.microsoft.com/en-us/powershell/module/az.keyvault/set-azkeyvaultsecret?view=azps-14.4.0



# Define variables
$vaultName = ""
$certificateName = "server"
$pfxFilePath = ""  # Update with your file path
$plainPassword = ""  # Update with your PFX password

# Convert the plain text password to SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Read the PFX file and convert to Base64
#$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
#$base64Pfx = [Convert]::ToBase64String($pfxBytes)

# THIS STEP WAS CRITIAL TO SUCCESS
#$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force

Import-AzKeyVaultCertificate -VaultName $vaultName -Name $certificateName -FilePath $pfxFilePath -Password $securePassword

# Upload the Base64 string as a secret to Azure Key Vault
#Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret


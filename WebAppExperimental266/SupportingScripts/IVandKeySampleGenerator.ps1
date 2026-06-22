# GPT-40 mini script to generate a random IV and output as Hex.

# Generate a random IV of 12 bytes (96 bits)
$iv = New-Object byte[] 12
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($iv)

# Convert the byte array to a hex string
$hexIv = [BitConverter]::ToString($iv) -replace '-', ''

# Output the results
Write-Host "Random IV (Bytes): $($iv -join ', ')"
Write-Host "IV in Hex: $hexIv"


# GPT-40 mini script to generate a random 256-bit key in Hex
# Generate a random 256-bit key (32 bytes)
$key = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)

# Convert the byte array to a hex string
$hexKey = [BitConverter]::ToString($key) -replace '-', ''

# Output the results
Write-Host "Random 256-bit Key (Bytes): $($key -join ', ')"
Write-Host "Key in Hex: $hexKey"

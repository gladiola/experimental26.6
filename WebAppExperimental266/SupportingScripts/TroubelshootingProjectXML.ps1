;
			Get-ChildItem -Path '$(ProjectDir)wwwroot\js\*.js' | 
			ForEach-Object { 
				$hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
				$bytes = [byte[]]::new($hash.Hash.Length / 2)
				for ($i = 0; $i -lt $hash.Hash.Length; $i += 2) {
					$bytes[$i / 2] = [Convert]::ToByte($hash.Hash.Substring($i, 2), 16)
				}
				$base64Hash = [Convert]::ToBase64String($bytes)
				'sha256-' + $base64Hash
			} | Out-File -FilePath '$(ProjectDir)\PrivateFiles\CSPScriptHashes.txt' -Encoding utf8 
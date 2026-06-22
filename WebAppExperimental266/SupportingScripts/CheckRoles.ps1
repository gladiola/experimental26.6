# Troubleshooting application's access to CosmosDB

Get-AzADServicePrincipal -SearchString ""

Get-AzRoleDefinition | Where-Object { $_.Name -like "*Cosmos*" } | Format-Table -Property Name, IsCustom, Id


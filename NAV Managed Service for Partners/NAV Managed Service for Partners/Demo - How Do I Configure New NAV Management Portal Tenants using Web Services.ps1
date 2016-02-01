$MyCompany = "Gesellschaft GmbH"
$UserName = "ADMIN"
$Password = "Ad.12345!"

Set-ManagedService `
    -TenantName "partnertenant" `
    -UrlBase "" ` #Specify your partner tenant URL
    -UserName "" ` #Specify your partner tenant username
    -Password "" ` #Specify your partner tenant password
    -ServiceName "" #Specify your application service name

Create-ApplicationTenant -Name $MyCompany -Country "Germany" `
  | Provision-ApplicationTenant `
  | Set-Tenant `
  | Create-ApplicationTenantUser `
        -UserName $UserName `
        -FullName "Administrator" `
        -EMail "mail@domain.com" ` #Put your e-mail here
        -IsAdmin $true `
  | Provision-ApplicationTenantUser -Password $Password `
  | Set-TenantUser -Password $Password

$UserName = "USER"
$Password = "Usr.12345!"
Create-ApplicationTenantUser `
    -UserName $UserName `
    -FullName "John Doe" `
    -EMail "john@doe.com" ` #Put actual e-mail here
  | Provision-ApplicationTenantUser -Password $Password

Get-TenantUser -UserName $UserName `
  | Set-UserPermissionSet -PermissionSets `
    "BASIC", "S&R-CUSTOMER, EDIT", "S&R-JOURNAL, POST", "S&R-POSTED S/I/R/C ", `
    "S&R-Q/O/I/R/C, POST", "S&R-REGISTER", "S&R-SETUP"

Configure-TenantCompany `    -Name $MyCompany `
    -Address "Addressenstraﬂe 1" `    -City "Munchen" `    -PostCode "81030" `
    -CountryCode "DE" `
    -VATRegNo "DE123456789"

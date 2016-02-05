Set-ManagedServiceScope `
    -TenantName "" `
    -UrlBase "" `
    -UserName "" `
    -Password "" `
    -ServiceName ""


Get-ApplicationTenant "Gesellschaft GmbH" `
    | Get-ApplicationTenantUser `
    | Where-Object { !$_.Administrator } `
    | Disable-ApplicationTenantUser


Create-ApplicationTenantUser `
    -TenantName "Gesellschaft GmbH" `
    -UserName "PSUSER" `
    -FullName "PowerShell User" `
    -EMail "user@psrocks.com"



Get-ApplicationTenant `
    | Create-ApplicationTenantUser `
        -UserName "TEST" `
        -FullName "Test Automation User" `
        -EMail "test@contosonav.com"


Get-ApplicationTenant `
    | Create-ApplicationTenantUser `
        -UserName "PARTNER" `
        -FullName "Partner Admin" `
        -EMail "partner@contosonav.com" `
        -Administrator $true `
    | Provision-ApplicationTenantUser `
    | Set-ApplicationTenantUserPassword `
        -Password "Ad.12345!"


Get-ApplicationTenant | Set-TenantWebServiceCredentials -UserName "PARTNER" -Password "Ad.12345!"


Get-ApplicationTenantUser `
    | Get-NAVUser `
    | Set-NAVUserPermissionSet `
        -PermissionSets BASIC, FOUNDATION

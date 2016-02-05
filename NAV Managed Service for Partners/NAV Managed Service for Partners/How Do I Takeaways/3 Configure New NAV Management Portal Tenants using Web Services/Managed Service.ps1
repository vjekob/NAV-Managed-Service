# Global constants
$ManagedService = @{}
$State = @{}
$Proxies = @{}

Function Get-Credential()
{
    Param(
        $UserName,
        $Password
    )
    $SecurePassword = ConvertTo-SecureString $Password -AsPlainText -Force
    $Credential = New-Object System.Management.Automation.PSCredential($UserName, $SecurePassword)

    Return $Credential
}

Function Create-WebServiceProxy()
{
    Param (
        [Parameter(Mandatory = $true)] [string] $ServiceName,
        [Parameter(Mandatory = $true)] [string] $Namespace
    )

    Assert-ManagedServiceSet

    If(-not $global:Proxies.ContainsKey($ServiceName))
    {
        $Proxy = New-WebServiceProxy `
                    -uri ($global:ManagedService.UrlBase -f $ServiceName) `
                    -Credential $global:ManagedService.Credential `
                    -Namespace $Namespace
        $Proxy.Timeout = 600000
        $global:Proxies.Add($ServiceName, $Proxy)
    }

    Return $global:Proxies.Get_Item($ServiceName)
}

Function Create-TenantWebServiceProxy()
{
    Param (
        [Parameter(Mandatory = $true)] [string] $ServiceName,
        [string] $Company,
        [Parameter(Mandatory = $true)] [string] $Namespace
    )

    Assert-TenantUserSet

    if ([string]::IsNullOrWhiteSpace($Company))
    {
        $Company = $global:ManagedService.CompanyName
    }

    $Url = ("{0}:7047/NAV/WS/{1}{2}" -f $global:State.Tenant.URL, [Uri]::EscapeUriString($Company), $ServiceName)
    If(-not $global:Proxies.ContainsKey($ServiceName))
    {
        $Proxy = New-WebServiceProxy `
                    -uri $Url `
                    -Credential $global:State.TenantCredential `
                    -Namespace $Namespace
        $Proxy.Timeout = 600000
        $global:Proxies.Add($ServiceName, $Proxy)                
    }

    Return $global:Proxies.Get_Item($ServiceName)
}

Function Assert-ManagedServiceSet()
{
    if ([string]::IsNullOrWhiteSpace($global:ManagedService.ServiceName))
    {
        Throw "Managed Service is not initialized. Call Set-ManagedService first."
    }
}

Function Assert-TenantUserSet()
{
    if ($global:State.TenantUser -eq $null)
    {
        Throw "Tenant user is not set. Call Set-TenantUser first."
    }
}

Function Set-ManagedService() 
{
    Param(
        [Parameter(Mandatory = $true)] [string] $TenantName,
        [Parameter(Mandatory = $true)] [string] $UrlBase,
        [Parameter(Mandatory = $true)] [string] $UserName,
        [Parameter(Mandatory = $true)] [string] $Password,
        [Parameter(Mandatory = $true)] [string] $ServiceName,
        [string] $CompanyName
    )

    $global:ManagedService.TenantName = $TenantName
    $global:ManagedService.UserName = $UserName
    $global:ManagedService.Password = $Password
    $global:ManagedService.Credential = Get-Credential -UserName "$TenantName\$UserName" -Password $Password
    $global:ManagedService.ServiceName = $ServiceName
    $global:ManagedService.UrlBase = $UrlBase + ":7047/NAV/WS/Contoso{0}?tenant=$TenantName"

    if ([string]::IsNullOrWhiteSpace($CompanyName))
    {
        $CompanyName = "CRONUS International Ltd."
    }
    $global:ManagedService.CompanyName = $CompanyName
}

Function Set-Tenant()
{
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Tenant
    )
    $global:State.Tenant = $Tenant
    Return $Tenant
}

Function Set-TenantUser()
{
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $User,
        [Parameter(Mandatory = $true)] [string] $Password
    )

    $global:State.TenantUser = $User
    $global:State.TenantUserPassword = $Password
    $global:State.TenantCredential = Get-Credential -UserName $User.User_Name -Password $Password

    Return $User
}

Function Create-ApplicationTenant()
{
    Param (
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Country
    )

    Assert-ManagedServiceSet

    $TenantService = Create-WebServiceProxy -ServiceName "/Page/ApplicationTenant" -Namespace NAV.ApplicationTenant

    $Tenant = New-Object NAV.ApplicationTenant.ApplicationTenant
    $Tenant.ApplicationServiceName = $global:ManagedService.ServiceName
    $Tenant.Name = $Name
    $Tenant.Country = $Country

    $TenantService.Create([ref] $Tenant)

    Return $Tenant
}

Function Provision-ApplicationTenant()
{
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Tenant
    )

    Assert-ManagedServiceSet

    $OperationService = Create-WebServiceProxy -ServiceName "/Codeunit/Operation" -Namespace NAV.Operation
    $TenantService = Create-WebServiceProxy -ServiceName "/Page/ApplicationTenant" -Namespace NAV.ApplicationTenant

    $OperationID = $TenantService.BeginProvision($Tenant.Key)
    Do
    {
        Start-Sleep -Seconds 5
        $Status = $OperationService.GetOperationStatus($OperationID)
    } While ($Status -like "Provisioning")

    Return $TenantService.Read($Tenant.ID)
}

Function Create-ApplicationTenantUser()
{
    Param (
        [Parameter(ValueFromPipeline = $true)] $Tenant,
        [Parameter(Mandatory = $true)] $UserName,
        [string] $FullName,
        [Parameter(Mandatory = $true)] $EMail,
        [bool] $IsAdmin
    )

    Assert-ManagedServiceSet

    If ($Tenant -eq $null)
    {
        $Tenant = $global:State.Tenant
    }

    $UserService = Create-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser

    $TenantUser = New-Object NAV.ApplicationTenantUser.ApplicationTenantUser
    $TenantUser.Application_Tenant_ID = $Tenant.ID
    $TenantUser.User_Name = $UserName
    $TenantUser.Full_Name = $FullName
    $TenantUser.Contact_Email = $EMail
    $TenantUser.Administrator = $IsAdmin
    $TenantUser.AdministratorSpecified = $true

    $UserService.Create([ref] $TenantUser)

    if ($IsAdmin)
    {
        $global:State.AdminUser = $TenantUser
    }
    Return $TenantUser
}

Function Provision-ApplicationTenantUser()
{
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $TenantUser,
        $Password
    )

    Assert-ManagedServiceSet

    $UserService = Create-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser

    $UserPassword = $UserService.New($TenantUser.Key, [string]::IsNullOrWhiteSpace($Password))
    If(-not [string]::IsNullOrWhiteSpace($Password))
    {
        $TenantUser = $UserService.ReadByRecId($UserService.GetRecIdFromKey($TenantUser.Key))
        $UserService.SetPassword($TenantUser.Key, $Password, $true)
        $UserPassword = $Password
    }

    if ($TenantUser.Administrator)
    {
        $global:State.AdminUserPassword = $UserPassword
        $global:State.AdminUser = $TenantUser
    }
    Return $TenantUser
}

Function Set-ApplicationTenantCompanyName()
{
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Tenant,
        $Company,
        [string] $Name
    )

    Assert-ManagedServiceSet

    $TenantCompanyService = Create-WebServiceProxy -ServiceName "/Page/ApplicationTenantCompany" -Namespace NAV.ApplicationTenantCompany
    $TenantCompanyService.Timeout = 300000

    If ($Company -eq $null)
    {
        $Company = $TenantCompanyService.Read($Tenant.ID, $GlobalDefaultCompanyName)
    }
    $TenantCompanyService.SetName($Company.Key, $Name)

    Return $TenantCompanyService.Read($Tenant.ID, $Name)
}

Function Configure-TenantCompany()
{
    Param (
        [string] $Company,
        [Parameter(Mandatory = $true)] [string] $Name,
        [string] $Address,
        [string] $Address2,
        [string] $City,
        [string] $PostCode,
        [string] $VATRegNo,
        [string] $CountryCode
    )

    if ([string]::IsNullOrWhiteSpace($Company))
    {
        $Company = $global:ManagedService.CompanyName
    }

    $CompanyInformationService = Create-TenantWebServiceProxy -ServiceName "/Page/CompanyInformation" -Namespace Tenant.CompanyInformation

    $CompanyInfo = $CompanyInformationService.ReadMultiple($null, $null, 1)[0]
    $CompanyInfo.Name = $Name
    $CompanyInfo.Address = $Address
    $CompanyInfo.Address_2 = $Address2
    $CompanyInfo.City = $City
    $CompanyInfo.Post_Code = $PostCode
    $CompanyInfo.Country_Region_Code = $CountryCode

    $CompanyInfo.Ship_to_Name = $Name
    $CompanyInfo.Ship_to_Address = $Address
    $CompanyInfo.Ship_to_Address_2 = $Address2
    $CompanyInfo.Ship_to_City = $City
    $CompanyInfo.Ship_to_Post_Code = $PostCode
    $CompanyInfo.Ship_to_Country_Region_Code = $CountryCode

    $CompanyInfo.VAT_Registration_No = $VATRegNo

    $CompanyInformationService.Update([ref] $CompanyInfo)

    Return $CompanyInfo
}

Function Get-TenantUser()
{
    Param(
        [Parameter(Mandatory = $true)] [string] $UserName
    )

    $UserService = Create-TenantWebServiceProxy -ServiceName "/Page/User" -Namespace Tenant.User

    $UserFilter = New-Object Tenant.User.User_Filter
    $UserFilter.Field = [Tenant.User.User_Fields]::User_Name
    $UserFilter.Criteria = $UserName
    [Tenant.User.User_Filter[]] $UserFilters = $UserFilter

    Return $UserService.ReadMultiple($UserFilters, $null, 1)[0]
}

Function Set-UserPermissionSet()
{
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $User,   
        [string] $Company,
        [Parameter(Mandatory = $true)] [string[]] $PermissionSets
    )

    $UserService = Create-TenantWebServiceProxy -ServiceName "/Page/User" -Namespace Tenant.User

    $UserPermissionSets = New-Object "System.Collections.Generic.List[Tenant.User.User_Line]"
    Foreach ($Set in $PermissionSets)
    {
        $UserPermissionSet = New-Object Tenant.User.User_Line
        If (![string]::IsNullOrWhiteSpace($Company))
        {
            $UserPermissionSet.Company = $Company
        }
        $UserPermissionSet.Permission_Set = $set
        $UserPermissionSets.Add($UserPermissionSet)
    }

    $User.Permissions = $UserPermissionSets.ToArray()

    $UserService.Update([ref] $User)
}

# Global initialization

If (!$global:ManagedServiceModuleInitialized)
{
    Write-Host "Initializing module..."
    $global:ManagedService = @{}
    $global:State = @{}
    $global:Proxies = @{}
    $global:ManagedServiceModuleInitialized = $true

    $global:State.TenantCredentials = @{}
}




# General functions

Function Assert-ApplicationTenantSet()
{
    if ($global:State.Tenant -eq $null)
    {
        Throw "Application Tenant scope is not set. Call Set-ApplicationTenantScope first."
    }
}

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

Function Get-WebServiceProxy()
{
    Param (
        [Parameter(Mandatory = $true)] [string] $ServiceName,
        [Parameter(Mandatory = $true)] [string] $Namespace
    )

    if ([string]::IsNullOrWhiteSpace($global:ManagedService.ServiceName))
    {
        Throw "Managed Service scope is not initialized. Call Set-ManagedServiceScope first."
    }

    If(-not $global:Proxies.ContainsKey($ServiceName))
    {
        Try
        {
            $Proxy = New-WebServiceProxy `
                        -uri ($global:ManagedService.UrlBase -f $ServiceName) `
                        -Credential $global:ManagedService.Credential `
                        -Namespace $Namespace
            $Proxy.Timeout = 600000
            $global:Proxies.Add($ServiceName, $Proxy)
        }
        Catch
        {
            Throw "Could not create proxy for $ServiceName at $Url. Error message was $_.Exception.Message"
        }
    }

    Return $global:Proxies.Get_Item($ServiceName)
}

Function Get-TenantWebServiceProxy()
{
    Param (
        [Parameter(Mandatory = $true)] [string] $ServiceName,
        [string] $Company,
        [Parameter(Mandatory = $true)] [string] $Namespace
    )

    $TenantName = ""
    If ($global:State.Tenant -ne $null)
    {
        $TenantName = $global:State.Tenant.Name
    }

    $Credential = $global:State.TenantCredentials.Get_Item($TenantName)
    If ($Credential -eq $null)
    {
        Throw "Credentials for accessing NAV Tenant web services are not set. Call Set-TenantWebServiceCredentials first."
    }

    if ([string]::IsNullOrWhiteSpace($Company))
    {
        Assert-ApplicationTenantSet
        $Company = $global:State.Tenant.App_Tenant_Subpage_Companies[0].Name
    }

    $Url = ("{0}:7047/NAV/WS/{1}{2}" -f $global:State.Tenant.URL, [Uri]::EscapeUriString($Company), $ServiceName)
    If(-not $global:Proxies.ContainsKey($ServiceName))
    {
        Try
        {
            $Proxy = New-WebServiceProxy `
                        -uri $Url `
                        -Credential $Credential `
                        -Namespace $Namespace
            $Proxy.Timeout = 600000
            $global:Proxies.Add($ServiceName, $Proxy)                
        }
        Catch
        {
            Throw "Could not create proxy for $ServiceName at $Url. Error message was $_.Exception.Message"
        }
    }

    $Proxy = $global:Proxies.Get_Item($ServiceName)
    $Proxy.Url = $Url
    $Proxy.Credentials = $Credential
    Return $Proxy
}



# Scoping options

Function Set-ManagedServiceScope() 
{
    Param(
        [Parameter(Mandatory = $true)] [string] $TenantName,
        [Parameter(Mandatory = $true)] [string] $UrlBase,
        [Parameter(Mandatory = $true)] [string] $UserName,
        [Parameter(Mandatory = $true)] [string] $Password,
        [Parameter(Mandatory = $true)] [string] $ServiceName
    )

    $global:ManagedService.TenantName = $TenantName
    $global:ManagedService.UserName = $UserName
    $global:ManagedService.Password = $Password
    $global:ManagedService.Credential = Get-Credential -UserName "$TenantName\$UserName" -Password $Password
    $global:ManagedService.ServiceName = $ServiceName
    $global:ManagedService.UrlBase = $UrlBase + ":7047/NAV/WS/Contoso{0}?tenant=$TenantName"

    Write-Host ("Managed service set to tenant '{0}', application service '{1}'." -f $TenantName, $ServiceName)
}

Function Set-ApplicationTenantScope()
{
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Tenant
    )
    $global:State.Tenant = $Tenant
}

Function Set-TenantWebServiceCredentials()
{
    Param (
        [Parameter(ValueFromPipelineByPropertyName = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $UserName,
        [Parameter(Mandatory = $true)] [string] $Password
    )

    Process
    {
        $credential = Get-Credential -UserName $UserName -Password $Password
        If ($global:State.TenantCredentials.ContainsKey($Name))
        {
            $global:State.TenantCredentials.Remove($Name)
        }
        $global:State.TenantCredentials.Add($Name, $credential)
    }
}




# Application Tenant

Function Create-ApplicationTenant()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Country,
        [bool] $AllowDatabaseWrite
    )

    $TenantService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenant" -Namespace NAV.ApplicationTenant

    $Tenant = New-Object NAV.ApplicationTenant.ApplicationTenant
    $Tenant.ApplicationServiceName = $global:ManagedService.ServiceName
    $Tenant.Name = $Name
    $Tenant.Country = $Country
    $Tenant.Allow_App_Database_Write = $AllowDatabaseWrite
    $Tenant.Allow_App_Database_WriteSpecified = true

    $TenantService.Create([ref] $Tenant)

    Write-Output $Tenant
}

Function Provision-ApplicationTenant()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Tenant
    )

    Begin
    {
        $OperationService = Get-WebServiceProxy -ServiceName "/Codeunit/Operation" -Namespace NAV.Operation
        $TenantService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenant" -Namespace NAV.ApplicationTenant
    }

    Process
    {
        $OperationID = $TenantService.BeginProvision($Tenant.Key)
        Do
        {
            Start-Sleep -Seconds 5
            $Status = $OperationService.GetOperationStatus($OperationID)
        } While ($Status -like "Provisioning")

        Write-Output $TenantService.Read($Tenant.ID)
    }
}

Function Get-ApplicationTenant()
{
    [CmdletBinding()]
    Param (
        [string] $Name,
        [string] $ID
    )

    Begin
    {
        $TenantService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenant" -Namespace NAV.ApplicationTenant

        $TenantFilters = New-Object "System.Collections.Generic.List[NAV.ApplicationTenant.ApplicationTenant_Filter]"
        If (![string]::IsNullOrWhiteSpace($Name))
        {
            $TenantFilter = New-Object NAV.ApplicationTenant.ApplicationTenant_Filter
            $TenantFilter.Field = [NAV.ApplicationTenant.ApplicationTenant_Fields]::Name
            $TenantFilter.Criteria = $Name
            $add = $TenantFilters.Add($TenantFilter)
        }
        If (![string]::IsNullOrWhiteSpace($ID))
        {
            $TenantFilter = New-Object NAV.ApplicationTenant.ApplicationTenant_Filter
            $TenantFilter.Field = [NAV.ApplicationTenant.ApplicationTenant_Fields]::ID
            $TenantFilter.Criteria = $ID
            $add = $TenantFilters.Add($TenantFilter)
        }

        Write-Output $TenantService.ReadMultiple($TenantFilters.ToArray(), $null, 0)
    }
}




# Application Tenant User

Function Create-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(ParameterSetName = "Tenant", ValueFromPipeline = $true, Mandatory = $true)]
        [Parameter(ParameterSetName = "TenantName", Mandatory = $false)]
        [object[]] $Tenant,

        [Parameter(ParameterSetName = "Tenant", Mandatory = $false)]
        [Parameter(ParameterSetName = "TenantName", Mandatory = $true)]
        [string] $TenantName,

        [Parameter(Mandatory = $true)] [string] $UserName,
        [Parameter(Mandatory = $true)] [string] $FullName,
        [Parameter(Mandatory = $true)] [string] $EMail,
        [bool] $Administrator
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }

    Process
    {
        If ($Tenant -eq $null)
        {
            If ([string]::IsNullOrWhiteSpace($TenantName))
            {
                Assert-ApplicationTenantSet
                $Tenant = $global:State.Tenant
            }
            Else
            {
                $Tenant = Get-ApplicationTenant -Name $TenantName
            }
        }

        $TenantUser = New-Object NAV.ApplicationTenantUser.ApplicationTenantUser
        $TenantUser.Application_Tenant_ID = $Tenant.ID
        $TenantUser.User_Name = $UserName
        $TenantUser.Full_Name = $FullName
        $TenantUser.Contact_Email = $EMail
        $TenantUser.Administrator = $Administrator
        $TenantUser.AdministratorSpecified = $true

        $UserService.Create([ref] $TenantUser)

        Write-Output $TenantUser
    }
}

Function Get-ApplicationTenantUser()
{
    [CmdletBinding(DefaultParameterSetName = "TenantObject")]
    Param (
        [Parameter(ParameterSetName = "TenantObject", ValueFromPipeline = $true)] [object[]] $Tenant,
        [Parameter(ParameterSetName = "TenantID")] [string] $TenantID,
        [string] $UserName
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }

    Process
    {
        $UserFilters = New-Object "System.Collections.Generic.List[NAV.ApplicationTenantUser.ApplicationTenantUser_Filter]"
        If ($Tenant -ne $null)
        {
            $UserFilter = New-Object NAV.ApplicationTenantUser.ApplicationTenantUser_Filter
            $UserFilter.Field = [NAV.ApplicationTenantUser.ApplicationTenantUser_Fields]::Application_Tenant_ID;
            $UserFilter.Criteria = $Tenant.ID
            $UserFilters.Add($UserFilter)
        }

        If(![string]::IsNullOrWhiteSpace($TenantID))
        {
            $UserFilter = New-Object NAV.ApplicationTenantUser.ApplicationTenantUser_Filter
            $UserFilter.Field = [NAV.ApplicationTenantUser.ApplicationTenantUser_Fields]::Application_Tenant_ID;
            $UserFilter.Criteria = $TenantID
            $UserFilters.Add($UserFilter)
        }

        If(![string]::IsNullOrWhiteSpace($UserName))
        {
            $UserFilter = New-Object NAV.ApplicationTenantUser.ApplicationTenantUser_Filter
            $UserFilter.Field = [NAV.ApplicationTenantUser.ApplicationTenantUser_Fields]::User_Name;
            $UserFilter.Criteria = $UserName
            $UserFilters.Add($UserFilter)
        }

        Write-Output $UserService.ReadMultiple($UserFilters.ToArray(), $null, 0)
    }
}

Function Disable-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)] [Object[]] $TenantUser 
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        $UserService.SetEnabled($TenantUser.Key, $false)
        Get-ApplicationTenantUser -TenantID $TenantUser.Application_Tenant_ID -UserName $TenantUser.User_Name
    }
}

Function Enable-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)] [Object[]] $TenantUser 
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        $UserService.SetEnabled($TenantUser.Key, $true)
        Get-ApplicationTenantUser -TenantID $TenantUser.Application_Tenant_ID -UserName $TenantUser.User_Name
    }
}

Function Delete-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)] [Object[]] $TenantUser 
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        If ($TenantUser.Status -eq [NAV.ApplicationTenantUser.Status]::Disabled -or $TenantUser.Status -eq [NAV.ApplicationTenantUser.Status]::Provisioning_Failed -or $TenantUser.Status -eq [NAV.ApplicationTenantUser.Status]::Draft)
        {
            $result = $UserService.Delete($TenantUser.Key)
            If (!$result)
            {
                Write-Error "The user was not successfully deleted, but the service did not return an error."
            }
        } Else
        {
            $UserService.Remove($TenantUser.Key)
        }
    }
}

Function Provision-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $TenantUser,
        [bool] $SendWelcomeEMail
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        $pwd = $UserService.New($TenantUser.Key, $SendWelcomeEMail)
        Get-ApplicationTenantUser -TenantID $TenantUser.Application_Tenant_ID -UserName $TenantUser.User_Name
    }
}

Function Promote-ApplicationTenantUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $TenantUser
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        $UserService.MakeAdministrator($TenantUser.Key)
        Get-ApplicationTenantUser -TenantID $TenantUser.Application_Tenant_ID -UserName $TenantUser.User_Name
    }
}

Function Set-ApplicationTenantUserPassword()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $TenantUser,
        [Parameter(Mandatory = $true)] [string] $Password,
        [bool] $SendMail
    )

    Begin
    {
        $UserService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantUser" -Namespace NAV.ApplicationTenantUser
    }
    Process
    {
        $UserService.SetPassword($TenantUser.Key, $Password, $SendMail)
        Get-ApplicationTenantUser -TenantID $TenantUser.Application_Tenant_ID -UserName $TenantUser.User_Name
    }
}




# Application Tenant Company

Function Get-ApplicationTenantCompany
{
    [CmdletBinding()]
    Param (
        [Parameter(ValueFromPipeline = $true)] $Tenant
    )

    Begin
    {
        $TenantCompanyService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantCompany" -Namespace NAV.ApplicationTenantCompany
    }

    Process
    {
        $TenantCompanyFilter = New-Object NAV.ApplicationTenantCompany.ApplicationTenantCompany_Filter
        $TenantCompanyFilter.Field = [NAV.ApplicationTenantCompany.ApplicationTenantCompany_Fields]::Application_Tenant_ID
        $TenantCompanyFilter.Criteria = $Tenant.ID

        Write-Output $TenantCompanyService.ReadMultiple($TenantCompanyFilter, $null, 0)
    }
}

Function Rename-ApplicationTenantCompany()
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $Company,
        [Parameter(Mandatory = $true)] [string] $NewName
    )

    Begin
    {
        $TenantCompanyService = Get-WebServiceProxy -ServiceName "/Page/ApplicationTenantCompany" -Namespace NAV.ApplicationTenantCompany
    }

    Process
    {
        $TenantCompanyService.SetName($Company.Key, $NewName)
        Write-Output $TenantCompanyService.Read($Company.Application_Tenant_ID, $Name)
    }
}




# NAV User

Function Get-NAVUser()
{
    [CmdletBinding()]
    Param (
        [Parameter(ParameterSetName = "Name", ValueFromPipeline = $true, Position = 0)] [string[]] $Name,
        [Parameter(ParameterSetName = "UserObject", ValueFromPipeline = $true)] [object[]] $TenantUser
    )

    Process
    {
        If ($TenantUser -ne $null)
        {
            Get-ApplicationTenant -ID $TenantUser.Application_Tenant_ID | Set-ApplicationTenantScope
            $Name = $TenantUser.User_Name
        }
        $UserService = Get-TenantWebServiceProxy -ServiceName "/Page/User" -Namespace Tenant.User

        $UserFilter = New-Object Tenant.User.User_Filter
        $UserFilter.Field = [Tenant.User.User_Fields]::User_Name
        $UserFilter.Criteria = $Name
        Write-Output $UserService.ReadMultiple($UserFilter, $null, 1)
    }
}

Function Set-NAVUserPermissionSet()
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)] $User,   
        [string] $Company,
        [Parameter(Mandatory = $true)] [string[]] $PermissionSets
    )

    Begin 
    {
        $UserService = Get-TenantWebServiceProxy -ServiceName "/Page/User" -Namespace Tenant.User
    }

    Process
    {
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

        Write-Output $User
    }
}

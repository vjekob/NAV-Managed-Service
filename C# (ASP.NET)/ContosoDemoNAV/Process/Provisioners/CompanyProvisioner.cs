using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process.Workflow;
using ContosoDemoNAV.WebService;

namespace ContosoDemoNAV.Process.Provisioners
{
    public class CompanyProvisioner : WorkflowTaskGroup
    {
        public CompanyProvisioner()
        {
            Ordinal = 2000000;
            HighLevelTask = true;
            Description = "Configuring company";
        }

        protected override void RequestTasks()
        {
            var svcAppTenantCompany = WebServiceFactory.ApplicationTenantCompany();
            ApplicationTenantCompany.ApplicationTenantCompany company = null;

            RegisterTask(
                new AsyncWorkflowTask("Waiting for company provisioning to complete", (state, action) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var attempts = 0;
                            var svcTenant = WebServiceFactory.ApplicationTenant();
                            while (company == null)
                            {
                                var tenant = svcTenant.Read(state.Get<TenantModel>().Id);
                                company = svcAppTenantCompany.Read(tenant.ID, Configuration.DefaultCompanyName);
                                if (company == null)
                                    Thread.Sleep(5000);
                                if (attempts++ > 60)
                                    throw new Exception(
                                        "Timeout during company provisioning. Please contact Contoso support department.");
                            }
                            action.Complete();
                        }
                        catch (Exception e)
                        {
                            action.Fail(e);
                        }
                    });
                }).Then(
                    new AsyncWorkflowTask("Renaming company", (state, action) =>
                    {
                        svcAppTenantCompany.SetNameCompleted += (sender, args) =>
                        {
                            action.CompleteAsyncOperation(args, () =>
                            {
                                state["CompanyRenamed"] = true;
                            });
                        };
                        svcAppTenantCompany.SetNameAsync(company?.Key, state.Get<TenantModel>().CompanyName);
                    }).Then(
                        new AsyncWorkflowTask("Waiting for user account provisioning", (state, task) =>
                        {
                            Task.Factory.StartNew(() =>
                            {
                                while (!state["UserCreated"])
                                    Thread.Sleep(1000);
                                task.Complete();
                            });
                        })
                            .Then(
                                new List<ITask>
                                {
                                    new AsyncWorkflowTask("Configuring company", (state, action) =>
                                    {
                                        var features = state.Get<FeaturesModel>();
                                        var tenant = state.Get<TenantModel>();

                                        var svcCompanyInfo = WebServiceFactory.Tenant.CompanyInformation(state);
                                        var companyInfo = svcCompanyInfo.ReadMultiple(null, null, 0)[0];

                                        companyInfo.Name = tenant.CompanyName;
                                        companyInfo.Address = features.CompanyAddress;
                                        companyInfo.Address_2 = features.CompanyAddress2;
                                        companyInfo.Post_Code = features.CompanyPostCode;
                                        companyInfo.City = features.CompanyCity;
                                        companyInfo.Country_Region_Code = features.CompanyCountryCode;
                                        companyInfo.Ship_to_Name = tenant.CompanyName;
                                        companyInfo.Ship_to_Address = features.CompanyAddress;
                                        companyInfo.Ship_to_Address_2 = features.CompanyAddress2;
                                        companyInfo.Ship_to_Post_Code = features.CompanyPostCode;
                                        companyInfo.Ship_to_City = features.CompanyCity;
                                        companyInfo.Ship_to_Country_Region_Code = features.CompanyCountryCode;
                                        companyInfo.VAT_Registration_No = features.VatRegistrationNumber;

                                        svcCompanyInfo.UpdateCompleted += (sender, args) =>
                                        {
                                            action.CompleteAsyncOperation(args, null);
                                        };
                                        svcCompanyInfo.UpdateAsync(companyInfo);
                                    }),
                                    new AsyncWorkflowTask("Configuring G/L", (state, action) =>
                                    {
                                        var glSvc = WebServiceFactory.Tenant.GeneralLedgerSetup(state);
                                        var glSetup = glSvc.ReadMultiple(null, null, 0)[0];

                                        var features = state.Get<FeaturesModel>();
                                        glSetup.LCY_Code = features.LocalCurrencyCode;
                                        glSetup.Register_Time = features.RegisterTime;

                                        glSvc.UpdateCompleted += (sender, args) =>
                                        {
                                            action.CompleteAsyncOperation(args, null);
                                        };
                                        glSvc.UpdateAsync(glSetup);
                                    })
                                })
                        )
                    )
                );
        }
    }
}
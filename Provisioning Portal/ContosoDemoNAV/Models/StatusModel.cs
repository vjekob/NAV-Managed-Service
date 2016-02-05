using System;
using System.Collections.Generic;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class StatusModel
    {
        public NewUserModel User { get; set; }
        public TenantModel Tenant { get; set; }
        public IEnumerable<ITask> Steps { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Redirect { get; set; }
        public ITask Workflow { get; set; }
    }
}
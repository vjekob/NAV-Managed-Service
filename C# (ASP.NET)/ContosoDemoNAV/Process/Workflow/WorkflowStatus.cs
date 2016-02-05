namespace ContosoDemoNAV.Process.Workflow
{
    public enum WorkflowStatus
    {
        None,
        Running,
        Completed = 99,

        Error = -1
    }
}
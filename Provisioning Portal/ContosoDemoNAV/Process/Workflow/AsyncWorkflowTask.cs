using System;
using System.ComponentModel;

namespace ContosoDemoNAV.Process.Workflow
{
    public class AsyncWorkflowTask : BaseWorkflowTask
    {
        private readonly Action<State, AsyncWorkflowTask> _action;

        public AsyncWorkflowTask(string desc, Action<State, AsyncWorkflowTask> action)
        {
            Description = desc;
            _action = action;
        }

        public override void Run(State state)
        {
            Status = WorkflowStatus.Running;
            SafeInvoke(_action, state);
        }

        public void CompleteAsyncOperation(AsyncCompletedEventArgs asyncResults, Action a)
        {
            if (IsAsyncSuccess(asyncResults))
                a?.Invoke();
            Complete();
        }

        protected bool IsAsyncSuccess(AsyncCompletedEventArgs args)
        {
            if (args.Error == null)
                return true;

            Fail(args.Error);
            return false;
        }
    }
}
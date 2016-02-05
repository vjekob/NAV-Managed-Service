using System.Collections.Generic;

namespace ContosoDemoNAV.Process.Workflow
{
    public static class WorkflowExtensions
    {
        public static ITask Then(this ITask task, ITask nextTask)
        {
            (nextTask as IWorkflowTask)?.RegisterPredecessor(task);
            (task as IWorkflowTask)?.RegisterSuccessor(nextTask);
            return task;
        }

        public static ITask Then(this ITask task, List<ITask> nextTasks)
        {
            nextTasks.ForEach(t =>
            {
                (t as IWorkflowTask)?.RegisterPredecessor(task);
                (task as IWorkflowTask)?.RegisterSuccessor(t);
            });
            return task;
        }

        public static List<ITask> Then(this List<ITask> tasks, ITask nextTask)
        {
            tasks.ForEach(t =>
            {
                (nextTask as IWorkflowTask)?.RegisterPredecessor(t);
                (t as IWorkflowTask)?.RegisterSuccessor(nextTask);
            });
            return tasks;
        }

        public static List<ITask> Then(this List<ITask> tasks, List<ITask> nextTasks)
        {
            nextTasks.ForEach(nt =>
            {
                tasks.ForEach(t =>
                {
                    (nt as IWorkflowTask)?.RegisterPredecessor(t);
                    (t as IWorkflowTask)?.RegisterSuccessor(t);
                });
            });
            return tasks;
        }
    }
}

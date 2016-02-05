using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContosoDemoNAV.Process;
using ContosoDemoNAV.Process.Workflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContosoDemoNAVTest.Mock;
using WebGrease.Css.Extensions;

namespace ContosoDemoNAVTest
{
    [TestClass]
    public class WorkflowTest
    {
        [TestMethod]
        public void TestWorkflowTaskGroupSingleTask()
        {
            var group = new MockTaskGroup(new BaseWorkflowTask("Step 1", (state) =>
            {
                Debug.WriteLine("Executing step 1");
            }));
            group.Run(new State());
            group.GetTasks().ForEach(t => Assert.AreEqual(WorkflowStatus.Completed, t.Status));
            Assert.AreEqual(WorkflowStatus.Completed, group.Status);
        }

        [TestMethod]
        public void TestWorkflowTaskGroupMultipleTasks()
        {
            var group =
                new MockTaskGroup(new BaseWorkflowTask("Step 1", (state) =>
                {
                    Debug.WriteLine("Executing step 1");
                })
                    .Then(new BaseWorkflowTask("Step 2", (state) =>
                    {
                        Debug.WriteLine("Executing step 2");
                    }))
                    .Then(new BaseWorkflowTask("Step 3", (state) =>
                    {
                        Debug.WriteLine("Executing step 3");
                    })));

            group.Run(new State());
            group.GetTasks().ForEach(t => Assert.AreEqual(WorkflowStatus.Completed, t.Status));
            Assert.AreEqual(WorkflowStatus.Completed, group.Status);
        }

        [TestMethod]
        public void TestWorkflowTaskGroupMultipleTasksRandom()
        {
            var tasksWith1 = new List<ITask>();
            for (var i = 1; i <= 10; i++)
            {
                var i1 = i;
                tasksWith1.Add(new BaseWorkflowTask($"Task with 1 - {i}",
                    (state) => Debug.WriteLine($"Executing task with 1 - {i1}")));
            }

            var tasksAfter1 = new List<ITask>();
            for (var i = 1; i <= 10; i++)
            {
                var i1 = i;
                tasksAfter1.Add(new BaseWorkflowTask($"Task after 1 - {i}",
                    (state) => Debug.WriteLine($"Executing task after 1 - {i1}")));
            }

            var tasksAfter10 = new List<ITask>();
            for (var i = 1; i <= 5; i++)
            {
                var i1 = i;
                tasksAfter10.Add(new BaseWorkflowTask($"Task after 1.10 - {i}",
                    (state) => Debug.WriteLine($"-- After 1.10, parallel - {i1}")));
            }

            tasksAfter1[4].Then(tasksAfter10[2]);
            tasksWith1.Last().Then(tasksAfter10);
            tasksAfter10.Then(tasksAfter1[3]);

            var group =
                new MockTaskGroup(new BaseWorkflowTask("Step 1", (state) =>
                {
                    Debug.WriteLine("Executing step 1");
                })
                    .Then(tasksAfter1)
                    .Then(new BaseWorkflowTask("Step 3", (state) =>
                    {
                        Debug.WriteLine("Executing step 3");
                    })));
            group.Add(tasksWith1);

            group.Run(new State());
            group.GetTasks().ForEach(t => Assert.AreEqual(WorkflowStatus.Completed, t.Status));
            Assert.AreEqual(WorkflowStatus.Completed, group.Status);
        }

        [TestMethod]
        public void TestWorkflowTaskAsync()
        {
            var group = new MockTaskGroup(
                new AsyncWorkflowTask("Step 1", (state, action) =>
                {
                    Debug.WriteLine("Executing Step 1");
                    Task.Factory.StartNew(() =>
                    {
                        Debug.WriteLine("--- Inside task...");
                        Thread.Sleep(2000);
                        Debug.WriteLine("--- I slept for 2000 ms");
                        action.Complete();
                        Debug.WriteLine("--- and I now completed.");
                    });
                    Debug.WriteLine("Executed Step 1, I should now wait");
                }).Then(new AsyncWorkflowTask("Step 2", (state, action) =>
                {
                    Debug.WriteLine("Executing Step 2");
                    Task.Factory.StartNew(() =>
                    {
                        Debug.WriteLine("--- Inside task...");
                        Thread.Sleep(2000);
                        Debug.WriteLine("--- I slept for 2000 ms");
                        action.Complete();
                        Debug.WriteLine("--- and I now completed.");
                    });
                    Debug.WriteLine("Executed Step 2, I should now wait");
                }).Then(new AsyncWorkflowTask("Step 1", (state, action) =>
                {
                    Debug.WriteLine("Executing Step 3");
                    Task.Factory.StartNew(() =>
                    {
                        Debug.WriteLine("--- Inside task...");
                        Thread.Sleep(2000);
                        Debug.WriteLine("--- I slept for 2000 ms");
                        action.Complete();
                        Debug.WriteLine("--- and I now completed.");
                    });
                    Debug.WriteLine("Executed Step 3, I should now wait");
                }))));

            group.Run(new State());
            group.GetTasks().ForEach(t => Assert.AreEqual(WorkflowStatus.Completed, t.Status));
            Assert.AreEqual(WorkflowStatus.Completed, group.Status);
        }
    }
}

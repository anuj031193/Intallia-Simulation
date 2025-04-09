using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobSimulation.DAL;
using JobSimulation.Models;

namespace JobSimulation.BLL
{
    public class TaskService
    {
        private readonly SectionRepository _sectionRepository;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;

        public TaskService(SectionRepository sectionRepository, SkillMatrixRepository skillMatrixRepository, TaskRepository taskRepository)
        {
            _sectionRepository = sectionRepository;
            _skillMatrixRepository = skillMatrixRepository;
            _taskRepository = taskRepository;
        }

        public async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)> LoadTaskDetailsForSectionAsync(string sectionId, string userId, string activityId)
        {
            var tasks = await _taskRepository.GetTasksBySectionIdAsync(sectionId, activityId);
            if (tasks == null || tasks.Count == 0)
            {
                return (new List<JobTask>(), 0, 0);
            }

            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var incompleteTasks = tasks
                .Where(t => skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed))
                .ToList();

            int currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks, activityId);
            int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, tasks[currentTaskIndex].TaskId);

            return (tasks, currentTaskIndex, timeElapsed);
        }

        private async Task<int> GetCurrentTaskIndexAsync(List<JobTask> incompleteTasks, List<JobTask> allTasks, string activityId)
        {
            var inProgressTasks = new List<JobTask>();
            foreach (var task in incompleteTasks)
            {
                var skillMatrixEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(activityId, task.TaskId);
                if (skillMatrixEntry?.Status == StatusTypes.InProgress)
                {
                    inProgressTasks.Add(task);
                }
            }

            if (inProgressTasks.Count > 0)
                return allTasks.IndexOf(inProgressTasks.First());
            if (incompleteTasks.Count > 0)
                return allTasks.IndexOf(incompleteTasks.First());

            return 0;
        }
    }
}
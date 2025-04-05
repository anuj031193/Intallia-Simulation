using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobSimulation.DAL;
using JobSimulation.Models;

namespace JobSimulation.BLL
{
    public class SectionService
    {
        private readonly SectionRepository _repository;
        private readonly FileService _fileService;
        private readonly ActivityRepository _activityRepository;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;

        public SectionService(SectionRepository repository, FileService fileService, ActivityRepository activityRepository, SkillMatrixRepository skillMatrixRepository, TaskRepository taskRepository)
        {
            _repository = repository;
            _fileService = fileService;
            _activityRepository = activityRepository;
            _skillMatrixRepository = skillMatrixRepository;
            _taskRepository = taskRepository;
        }

        public async Task<List<JobTask>> GetAllTasksForSectionAsync(string sectionId, string userId)
        {
            return await _taskRepository.GetTasksBySectionIdAsync(sectionId, userId);
        }

        public async Task<Section> LoadNextSectionAsync(string userId, string simulationId, string currentSectionId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(simulationId))
            {
                throw new ArgumentNullException(nameof(simulationId), "Simulation ID cannot be null or empty.");
            }

            var lastSession = await _activityRepository.GetLastSessionForUserAsync(userId);

            if (lastSession == null)
            {
                var firstSection = await _repository.GetFirstSectionAsync(simulationId);
                if (firstSection == null)
                {
                    throw new InvalidOperationException($"No sections found for simulation ID {simulationId}.");
                }
                return firstSection;
            }

            if (!string.IsNullOrEmpty(currentSectionId))
            {
                var nextSection = await _repository.GetNextSectionAsync(simulationId, currentSectionId);
                if (nextSection == null)
                {
                    throw new InvalidOperationException($"No next section found after section ID {currentSectionId}.");
                }
                return nextSection;
            }

            var currentSection = await _repository.GetSectionByIdAsync(lastSession.SectionId);
            if (currentSection == null)
            {
                throw new InvalidOperationException($"Section with ID {lastSession.SectionId} not found.");
            }

            var nextSectionBasedOnOrder = await _repository.GetNextSectionByOrderAsync(simulationId, currentSection.Order);
            if (nextSectionBasedOnOrder == null)
            {
                throw new InvalidOperationException($"No next section found after section order {currentSection.Order}.");
            }

            return nextSectionBasedOnOrder;
        }

        public async Task<Section> GetNextSectionAsync(string userId, string simulationId, string currentSectionId)
        {
            var currentActivity = await _activityRepository.GetLastSessionForUserAsync(userId);
            if (currentActivity != null && currentActivity.SectionId == currentSectionId)
            {
                var nextSection = await _repository.GetNextSectionAsync(simulationId, currentSectionId);
                if (nextSection != null)
                {
                    return nextSection;
                }
            }
            return await LoadNextSectionAsync(userId, simulationId, currentSectionId);
        }

        public async Task<Section> GetPreviousSectionAsync(string userId, string simulationId, string currentSectionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(simulationId) || string.IsNullOrEmpty(currentSectionId))
            {
                throw new ArgumentException("User ID, simulation ID, and current section ID cannot be null or empty.");
            }

            try
            {
                // Check for previous activity before continuing
                var lastActivity = await _activityRepository.GetPreviousActivityAsync(userId, simulationId, currentSectionId);

                if (lastActivity == null)
                {
                    // Log the message and exit early without closing the form
                    Console.WriteLine("No previous activity found, cannot load previous section.");
                    MessageBox.Show("No previous activity found, cannot load previous section.");
                    return null;  // Early return to avoid further processing that could lead to form closing
                }

                // Proceed with fetching the previous section if the activity exists
                var prevSection = await _repository.GetSectionByIdAsync(lastActivity.SectionId);
                if (prevSection == null)
                {
                    Console.WriteLine($"No previous section found for Section ID: {lastActivity.SectionId}");
                    return null;  // Return null if no previous section is found, preventing form closure
                }

                return prevSection; // Return the previous section if found
            }
            catch (Exception ex)
            {
                // Log any exceptions to avoid form crash
                Console.WriteLine($"Error fetching previous section: {ex.Message}");
                MessageBox.Show($"An error occurred while fetching the previous section: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;  // Return null to prevent unwanted side effects
            }
        }
        public async Task<bool> ValidateSectionCompletion(string userId, string sectionId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(sectionId))
            {
                throw new ArgumentNullException(nameof(sectionId), "Section ID cannot be null or empty.");
            }

            var tasks = await _taskRepository.GetTasksBySectionIdAsync(sectionId, userId);
            var activityId = (await _activityRepository.GetLastSessionForUserAsync(userId)).ActivityId;
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);

            foreach (var task in tasks)
            {
                var taskEntry = skillMatrixEntries.FirstOrDefault(sm => sm.TaskId == task.TaskId);
                if (taskEntry == null || taskEntry.Status != "Completed")
                {
                    return false;
                }
            }

            return true;
        }
    }
}
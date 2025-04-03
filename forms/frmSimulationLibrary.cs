using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;

namespace JobSimulation.Forms
{
    public partial class frmSimulationLibrary : Form
    {
        private readonly SimulationRepository _simulationRepository;
        private readonly string _userId;
        private readonly UserRepository _userRepository;

        public frmSimulationLibrary(string userId, UserRepository userRepository)
        {
            InitializeComponent();
            _userId = userId;
            _simulationRepository = new SimulationRepository(ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString);
            LoadSimulations();
            _userRepository = userRepository;
        }

        private async Task LoadSimulations()
        {
            try
            {
                // Fetch simulations asynchronously
                var simulations = await _simulationRepository.GetAllSimulationsAsync();

                // Create a list to hold simulations with index
                var simulationList = simulations
                    .Select((simulation, index) => new
                    {
                        No = index + 1,
                        SimulationId = simulation.SimulationId
                    })
                    .ToList();

                // Bind the list to the DataGridView
                dgvSimulations.DataSource = simulationList;
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void btnSelectSimulation_Click(object sender, EventArgs e)
        {
            // Check if any row is selected in the DataGridView
            if (dgvSimulations.SelectedRows.Count > 0)
            {
                // Retrieve the SimulationId of the selected row
                string simulationId = dgvSimulations.SelectedRows[0].Cells["SimulationId"].Value.ToString();

                // Retrieve the connection string
                var connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString;

                // Create instances of SectionRepository, FileService, TaskRepository, SkillMatrixRepository, and UserRepository
                var sectionRepository = new SectionRepository(connectionString);
                var fileService = new FileService();
                var taskRepository = new TaskRepository(connectionString);
                var skillMatrixRepository = new SkillMatrixRepository(connectionString);
                var userRepository = new UserRepository(connectionString);

                // Create an instance of SectionService, passing all required parameters in the correct order
                var sectionService = new SectionService(sectionRepository, fileService, null, skillMatrixRepository, taskRepository);

                // Create an instance of ActivityRepository, passing the connection string, sectionService, skillMatrixRepository, and taskRepository
                var activityRepository = new ActivityRepository(
                    connectionString,
                    sectionService,
                    skillMatrixRepository,
                    taskRepository
                );

                // Update the SectionService instance to include the activityRepository
                sectionService = new SectionService(sectionRepository, fileService, activityRepository, skillMatrixRepository, taskRepository);

                // Since this is the initial section, we don't have a current section or activity ID yet
                Section initialSection = null;
                string initialActivityId = null;

                // Create an instance of frmSectionLauncher, passing all required parameters including userRepository and userId
                frmSectionLauncher sectionLauncher = new frmSectionLauncher(
                    sectionRepository,
                    fileService,
                    sectionService,
                    taskRepository,
                    skillMatrixRepository,
                    activityRepository,
                    userRepository,
                    simulationId,
                    _userId, // Ensure _userId is correctly passed
                    initialSection,
                    initialActivityId
                );

                // Show the frmSectionLauncher form
                sectionLauncher.Show();

                // Hide the current form
                this.Hide();
            }
            else
            {
                // Display a message if no simulation is selected
                MessageBox.Show("Please select a simulation.");
            }
        }
        private void btnLogout_Click(object sender, EventArgs e)
        {
            // Close the current form
            this.Close();

            // Create an instance of frmUserLogin
            frmUserLogin loginForm = new frmUserLogin(_userRepository);

            // Show the frmUserLogin form
            loginForm.Show();
        }
    }
}
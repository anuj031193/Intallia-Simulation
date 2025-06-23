using System;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using JobSimulation.Models;
using JobSimulation.BLL;
using JobSimulation.DAL;

namespace JobSimulation.Forms
{
    public partial class frmSimulationLibrary : Form
    {
        private readonly SimulationRepository _simulationRepository;
        private readonly UserRepository _userRepository;
        private readonly string _userId;

        public frmSimulationLibrary(string userId, UserRepository userRepository)
        {
            InitializeComponent();
            _userId = userId;
            _userRepository = userRepository;
            _simulationRepository = new SimulationRepository(ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString);
        }
        private async void frmSimulationLibrary_Load(object sender, EventArgs e)
        {
            await LoadSimulations();
        }
        private async Task LoadSimulations()
        {
            try
            {
                var simulations = await _simulationRepository.GetAllSimulationsAsync();
                dgvSimulations.Rows.Clear();

                int index = 1;
                foreach (var simulation in simulations)
                {
                    dgvSimulations.Rows.Add(false, index++, simulation.SimulationId, simulation.Name, simulation.Description);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading simulations: {ex.Message}");
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            var selectedRow = dgvSimulations.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(row => Convert.ToBoolean(row.Cells["selectionColumn"].Value));

            if (selectedRow != null)
            {
                string simulationId = selectedRow.Cells["simulationColumn"].Value.ToString();

                var connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString;
                var sectionRepository = new SectionRepository(connectionString);
                var fileService = new FileService();
                var taskRepository = new TaskRepository(connectionString);
                var skillMatrixRepository = new SkillMatrixRepository(connectionString);
                var userRepository = new UserRepository(connectionString);

                var sectionService = new SectionService(sectionRepository, fileService, null, skillMatrixRepository, taskRepository);
                var activityRepository = new ActivityRepository(connectionString, sectionService, skillMatrixRepository, taskRepository);
                sectionService = new SectionService(sectionRepository, fileService, activityRepository, skillMatrixRepository, taskRepository);
                var taskService = new TaskService(sectionRepository, skillMatrixRepository, taskRepository);

                frmSectionLauncher launcher = new frmSectionLauncher(
                    sectionRepository,
                    fileService,
                    sectionService,
                    taskRepository,
                    skillMatrixRepository,
                    activityRepository,
                    userRepository,
                    taskService,
                    simulationId,
                    _userId,
                    null,
                    null
                );

                launcher.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Please select a simulation.");
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
            var loginForm = new frmUserLogin(_userRepository);
            loginForm.Show();
        }

        private void dgvSimulations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvSimulations.Columns["selectionColumn"].Index)
            {
                foreach (DataGridViewRow row in dgvSimulations.Rows)
                {
                    row.Cells["selectionColumn"].Value = false;
                }

                dgvSimulations.Rows[e.RowIndex].Cells["selectionColumn"].Value = true;
            }
        }
    }

}
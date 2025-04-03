using System;
using System.Windows.Forms;
using System.Configuration;
using JobSimulation.Models;
using JobSimulation.DAL;


namespace JobSimulation.Forms
{
    public partial class frmUserLogin : Form
    {
        private readonly UserRepository _userRepository;

        public frmUserLogin(UserRepository userRepository)
        {
            InitializeComponent();
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string userId = txtUserId.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both User ID and Password.");
                return;
            }

            try
            {
                string validatedUserId = await _userRepository.ValidateUserAsync(userId, password);
                if (validatedUserId != null)
                {
                    // Login successful, pass the UserId to the next form
                    var frmLibrary = new frmSimulationLibrary(validatedUserId, _userRepository);
                    frmLibrary.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid User ID or Password.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
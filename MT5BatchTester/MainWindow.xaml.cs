using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace MT5BatchTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string EXE_MT5 = "terminal64.exe";
        private static readonly string DIR_MT5_INST = "MetaTrader 5";
        private static readonly string DIR_EXPERT = "MQL5\\Experts";
        private static readonly string DIR_TESTER = "MQL5\\Profiles\\Tester";

        private BackgroundWorker worker = null;
        private Cursor _previousCursor;

        private string CurrentFileName;
        private bool _CancelBatch = true;

        public MainWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (object sender, ProgressChangedEventArgs e) => 
            { 
                pbProgress.Value = e.ProgressPercentage;
                lblFileName.Text = $"{e.ProgressPercentage}% - {CurrentFileName}";
            };

            txtMT5InstallationFolder.Text = UserSettings.MT5InstallationFolder;
            txtEAPath.Text = UserSettings.EAPath;
            txtParametersFolder.Text = UserSettings.ParametersFolder;
            txtReportsFolder.Text = UserSettings.ReportsFolder;

            txtBacktestingPeriod.Text = UserSettings.BacktestingPeriod.ToString();
            txtDeposit.Text = UserSettings.Deposit;
            txtLeverage.Text = UserSettings.Leverage;
            cmbModel.SelectedIndex = UserSettings.Model;
        }

        private void txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // avoid non numeric chars
            Regex _regex = new Regex("[^0-9.-]+"); 
            e.Handled = _regex.IsMatch(e.Text);
        }

        private bool CheckInputs()
        {
            if (txtMT5InstallationFolder.Text.Trim() == string.Empty || !Directory.Exists(txtMT5InstallationFolder.Text) || !txtMT5InstallationFolder.Text.Contains(DIR_MT5_INST))
            {
                MessageBox.Show("Invalid MT5 installation folder!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtEAPath.Text.Trim() == string.Empty || !File.Exists(Path.Combine(txtMT5InstallationFolder.Text.Trim(), DIR_EXPERT, txtEAPath.Text.Trim())))
            {
                MessageBox.Show("Invalid Expert Advisor path!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtParametersFolder.Text.Trim() == string.Empty || !Directory.Exists(Path.Combine(txtMT5InstallationFolder.Text.Trim(), DIR_TESTER, txtParametersFolder.Text.Trim())))
            {
                MessageBox.Show("Invalid Expert Advisor parameters files folder!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtBacktestingPeriod.Text.Trim() == string.Empty || Convert.ToInt32(txtBacktestingPeriod.Text) < 1)
            {
                MessageBox.Show("Invalid backtesting period!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtReportsFolder.Text.Trim() == string.Empty || !Directory.Exists(Path.Combine(txtMT5InstallationFolder.Text.Trim(), txtReportsFolder.Text.Trim())))
            {
                MessageBox.Show("Invalid reports output folder!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtDeposit.Text.Trim() == string.Empty || Convert.ToInt32(txtDeposit.Text) < 1)
            {
                MessageBox.Show("Deposit must be greater than 0!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (txtLeverage.Text.Trim() == string.Empty || Convert.ToInt32(txtLeverage.Text) < 1)
            {
                MessageBox.Show("Leverage must be greater than 0!", "Invalid Parameter", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {   
            _CancelBatch = !_CancelBatch;

            if (_CancelBatch)
            {
                grpFolders.IsEnabled = true;
                grpSettings.IsEnabled = true;
                return;
            }
            else
            {
                grpFolders.IsEnabled = false;
                grpSettings.IsEnabled = false;
            }   

            cmdRun.Content = "Cancel Test";

            pbProgress.Value = 0;
            CurrentFileName = string.Empty;
            lblFileName.Text = "0%";

            if (!CheckInputs())
                return;

            // Save settings
            UserSettings.MT5InstallationFolder = txtMT5InstallationFolder.Text.Trim();
            UserSettings.EAPath = txtEAPath.Text.Trim();
            UserSettings.ParametersFolder = txtParametersFolder.Text.Trim();
            UserSettings.BacktestingPeriod = Convert.ToInt32(txtBacktestingPeriod.Text);
            UserSettings.ReportsFolder = txtReportsFolder.Text.Trim();
            UserSettings.Deposit = txtDeposit.Text.Trim();
            UserSettings.Leverage = txtLeverage.Text.Trim();
            UserSettings.Model = cmbModel.SelectedIndex;

            // Set wait cursor
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;

            worker.RunWorkerAsync();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {   
            Mouse.OverrideCursor = _previousCursor;
            cmdRun.Content = "Run Test";
            grpFolders.IsEnabled = true;
            grpSettings.IsEnabled = true;

            if (_CancelBatch)
                return;
            else
                _CancelBatch = true; // Reset cancel flag

            pbProgress.Value = 100;
            lblFileName.Text = "100%";

            // Open reports folder
            Process.Start("explorer.exe", Path.Combine(UserSettings.MT5InstallationFolder, UserSettings.ReportsFolder));
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Get all EA parameters files
                string[] files = Directory.GetFiles(Path.Combine(UserSettings.MT5InstallationFolder, DIR_TESTER, UserSettings.ParametersFolder), "*.set");

                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        if (_CancelBatch)
                            return;

                        CurrentFileName = Path.GetFileNameWithoutExtension(files[i]);
                        string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        // Report update progress bar
                        worker.ReportProgress((i * 100) / files.Length);

                        // Get symbol and period from file name
                        string[] info = CurrentFileName.Split("-");
                        string Symbol = info[0];
                        string Period = info[2];

                        // Create default ini file
                        File.WriteAllText(tempFileName, Resource.default_ini);

                        // Set ini file parameters
                        var parser = new FileIniDataParser();
                        IniData data = parser.ReadFile(tempFileName);

                        data["Tester"]["Expert"] = UserSettings.EAPath;
                        data["Tester"]["ExpertParameters"] = Path.Combine(UserSettings.ParametersFolder, CurrentFileName + ".set");
                        data["Tester"]["Symbol"] = Symbol;
                        data["Tester"]["Period"] = Period;
                        data["Tester"]["Deposit"] = UserSettings.Deposit;
                        data["Tester"]["Leverage"] = UserSettings.Leverage;
                        data["Tester"]["Model"] = UserSettings.Model.ToString();
                        data["Tester"]["Report"] = UserSettings.ReportsFolder;
                        data["Tester"]["FromDate"] = DateTime.Today.AddMonths(-UserSettings.BacktestingPeriod).ToString("yyyy.MM.dd");
                        data["Tester"]["ToDate"] = DateTime.Today.AddDays(-1).ToString("yyyy.MM.dd");

                        parser.WriteFile(tempFileName, data);

                        // Run test
                        var process = Process.Start(Path.Combine(UserSettings.MT5InstallationFolder, EXE_MT5), $"/config:{tempFileName}");
                        process.WaitForExit();

                        File.Delete(tempFileName);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Error processing the file {CurrentFileName}\n {ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error getting parameters files!\n {ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

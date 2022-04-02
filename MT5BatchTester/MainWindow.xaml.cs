using IniParser;
using IniParser.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

        private BackgroundWorker _worker = null;
        private Cursor _previousCursor;
        private DispatcherTimer _timer;
        private DateTime _startTime;

        private string _currentFileName;
        private bool _cancelBatch = true;

        public MainWindow()
        {
            InitializeComponent();

            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += (object sender, ProgressChangedEventArgs e) => 
            { 
                pbProgress.Value = e.ProgressPercentage;
                lblFileName.Text = $"{e.ProgressPercentage}% - {_currentFileName}";
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += (object sender, EventArgs e) => 
            { 
                lblElapsedTime.Text = $"Elapsed Time: {DateTime.Now.Subtract(_startTime):hh\\:mm\\:ss}";
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
            if (!CheckInputs())
                return;

            if (!_cancelBatch && MessageBox.Show("Are you sure to cancel the test?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;

            // Toggle cancel batch
            _cancelBatch = !_cancelBatch;

            if (_cancelBatch)
            {
                // Enable inputs and exit
                grpFolders.IsEnabled = true;
                grpSettings.IsEnabled = true;
                return;
            }
            else
            {
                // disable inputs
                grpFolders.IsEnabled = false;
                grpSettings.IsEnabled = false;
            }

            cmdRun.Content = "Cancel Test";

            pbProgress.Value = 0;
            _currentFileName = string.Empty;
            lblFileName.Text = "0%";
            lblElapsedTime.Text = "Elapsed Time: 00:00:00";
            
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

            _worker.RunWorkerAsync();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {   
            Mouse.OverrideCursor = _previousCursor;
            cmdRun.Content = "Run Test";
            grpFolders.IsEnabled = true;
            grpSettings.IsEnabled = true;
            _timer.Stop();

            if (_cancelBatch)
                return;
            else
                _cancelBatch = true; // Reset cancel flag

            pbProgress.Value = 100;
            lblFileName.Text = "100%";

            // Open reports folder
            Process.Start("explorer.exe", Path.Combine(UserSettings.MT5InstallationFolder, UserSettings.ReportsFolder));
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Clean old results
                Directory.GetFiles(Path.Combine(UserSettings.MT5InstallationFolder, UserSettings.ReportsFolder)).ToList().ForEach(File.Delete);

                // Elapsed time calc
                _startTime = DateTime.Now;
                _timer.Start();

                // Get all EA parameters files
                string[] files = Directory.GetFiles(Path.Combine(UserSettings.MT5InstallationFolder, DIR_TESTER, UserSettings.ParametersFolder), "*.set");

                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        if (_cancelBatch)
                            return;

                        _currentFileName = Path.GetFileNameWithoutExtension(files[i]);
                        string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        // Report update progress bar
                        _worker.ReportProgress((i * 100) / files.Length);

                        // Get symbol and period from file name
                        string[] info = _currentFileName.Split("-");
                        string Symbol = info[0];
                        string Period = info[1];

                        // Create default ini file
                        File.WriteAllText(tempFileName, Resource.default_ini);

                        // Set ini file parameters
                        var parser = new FileIniDataParser();
                        IniData data = parser.ReadFile(tempFileName);

                        data["Tester"]["Expert"] = UserSettings.EAPath;
                        data["Tester"]["ExpertParameters"] = Path.Combine(UserSettings.ParametersFolder, _currentFileName + ".set");
                        data["Tester"]["Symbol"] = Symbol;
                        data["Tester"]["Period"] = Period;
                        data["Tester"]["Deposit"] = UserSettings.Deposit;
                        data["Tester"]["Leverage"] = UserSettings.Leverage;
                        data["Tester"]["Model"] = UserSettings.Model.ToString();
                        data["Tester"]["Report"] = Path.Combine(UserSettings.ReportsFolder, _currentFileName);
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
                        MessageBox.Show($"Error processing the file {_currentFileName}\n {ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error getting parameters files!\n {ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmdShowResults_Click(object sender, RoutedEventArgs e)
        {
            // Open reports folder
            Process.Start("explorer.exe", Path.Combine(UserSettings.MT5InstallationFolder, UserSettings.ReportsFolder));
        }
    }
}

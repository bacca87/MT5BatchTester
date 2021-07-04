using System;
using System.Collections.Generic;
using System.Text;

namespace MT5BatchTester
{
    public static class UserSettings
    {
        public static string MT5InstallationFolder
        {
            get => Settings.Default.MT5InstallationFolder;
            set
            {
                Settings.Default.MT5InstallationFolder = value;
                Settings.Default.Save();
            }
        }

        public static string EAPath
        {
            get => Settings.Default.EAPath;
            set
            {
                Settings.Default.EAPath = value;
                Settings.Default.Save();
            }
        }

        public static string ParametersFolder
        {
            get => Settings.Default.ParametersFolder;
            set
            {
                Settings.Default.ParametersFolder = value;
                Settings.Default.Save();
            }
        }

        public static int BacktestingPeriod
        {
            get => Settings.Default.BacktestingPeriod;
            set
            {
                Settings.Default.BacktestingPeriod = value;
                Settings.Default.Save();
            }
        }

        public static string ReportsFolder
        {
            get => Settings.Default.ReportsFolder;
            set
            {
                Settings.Default.ReportsFolder = value;
                Settings.Default.Save();
            }
        }

        public static string Deposit
        {
            get => Settings.Default.Deposit;
            set
            {
                Settings.Default.Deposit = value;
                Settings.Default.Save();
            }
        }

        public static string Leverage
        {
            get => Settings.Default.Leverage;
            set
            {
                Settings.Default.Leverage = value;
                Settings.Default.Save();
            }
        }

        public static int Model
        {
            get => Settings.Default.Model;
            set
            {
                Settings.Default.Model = value;
                Settings.Default.Save();
            }
        }
    }
}

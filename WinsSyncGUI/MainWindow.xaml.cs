using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

namespace WinsSyncGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker backgroundWorker;
        WinsSync winsSync;
        public MainWindow()
        {
            InitializeComponent();

            // Setup background worker to perform the bulk of the work.
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;

            // Try to load credentials from a file, if successful, put them in the form.
            // If there's something wrong with them, delete them.
            winsSync = new WinsSync();
            //try
            //{
            //    winsSync.LoadLoginCreds();
            //    textBox.Text = winsSync.Username;
            //    passwordBox.Password = winsSync.ConvertToUnsecureString(winsSync.getSPassword());
            //}
            ////todo: fix the dll, throws null reference exception if the files don't exist at all.
            //// We do catch it and handle it (by doing nothing), but it's not a good implementation.
            //catch (CryptographicException)
            //{
            //    winsSync.DeleteLoginCreds();
            //}
            //catch(NullReferenceException)
            //{
            //    //do nothing
            //}
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            textBlock.Visibility = Visibility.Visible;
            winsSync.Username = textBox.Text;
            if (!winsSync.Username.Contains("@"))
            {
                winsSync.Username += "@Wegmans.com";
            }
            winsSync.setSPassword(passwordBox.SecurePassword);
            if (!backgroundWorker.IsBusy)  // Makes sure clicking more than once doesn't crash it.
            {
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // If background worker is busy, request to cancel it.
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            winsSync.SetupGoogleCreds();
            backgroundWorker.ReportProgress(20);
            int retries = 5;
            while (retries > 0)
            {
                try
                {
                    winsSync.ScheduleGET();
                    break;
                }
                catch (ScheduleGETException ex)
                {
                    // Check to see if a cancellation request is pending. If it is, clean up then return.
                    //todo: Make this check a function since it's repeated.
                    if (backgroundWorker.CancellationPending)
                    {
                        backgroundWorker.ReportProgress(0);
                        winsSync.ClearSchedules();
                        return;
                    }
                    if (--retries == 0)
                    {
                        // Failed 5 times, reset progress state and start over.
                        backgroundWorker.ReportProgress(0);
                        string message = ex.ToString();
                        MessageBox.Show(message);
                        winsSync.ClearSchedules();
                        return;
                    }
                }
            }
            if (backgroundWorker.CancellationPending)
            {
                backgroundWorker.ReportProgress(0);
                winsSync.ClearSchedules();
                return;
            }
            backgroundWorker.ReportProgress(60);
            winsSync.FindTable();
            winsSync.ParseTable();
            winsSync.ParseRows();
            backgroundWorker.ReportProgress(80);
            retries = 5;
            while (retries > 0)
            {
                try
                {
                    winsSync.UploadResults().Wait();
                    break;
                }
                catch (Exception ex)
                {
                    if (backgroundWorker.CancellationPending)
                    {
                        backgroundWorker.ReportProgress(0);
                        winsSync.ClearSchedules();
                        return;
                    }
                    if (--retries == 0)
                    {
                        // Failed 5 times, reset progress state and start over.
                        string message = ex.ToString();
                        MessageBox.Show(message);
                        backgroundWorker.ReportProgress(0);
                        winsSync.ClearSchedules();
                        return;
                    }
                }
            }
            winsSync.ClearSchedules();
            backgroundWorker.ReportProgress(100);
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            textBlock.Text = e.ProgressPercentage.ToString() + "%";
            if (e.ProgressPercentage == 100)
            {
                textBlock.Text = "Complete!";
            }
            if (e.ProgressPercentage == 0)
            {
                textBlock.Text = "Canceled!";
            }
        }
    }
}

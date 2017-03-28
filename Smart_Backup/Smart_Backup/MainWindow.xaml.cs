using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using System.ComponentModel;
using System.Timers;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Smart_Backup.Properties;

// add some intelligence: ie if a folder was renamed but the files are the same name and date, just rename the local folder and don't recopy
// 	save the difference data in a file or something so can rapidly do copying and delting in 2 steps if wanted
	//some file size info might be nice
//Ways to make faster

// FYI: this backup system doesn't copy empty folders

namespace Smart_Backup
{
    public partial class MainWindow : Window
    {
        private FileValidator v;

        private int filesCopied; // files replaced or added
        private int filesRemoved;
        private int dirsRemoved;
        private int dirsCreated; // new directories made

        private int diffFiles; // files to copy
        private int diffLocalFiles; // files to delete

        private bool deleting;

        BackgroundWorker b; // for counting files that need backup
        BackgroundWorker bu; // for copying files
        BackgroundWorker bd; // for deleting files
        BackgroundWorker bf; // for deleting empty folders

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Rendered(object sender, EventArgs e) {
            //Diff_Click(null, null);   // REMOVE FOR RELEASE
            string src = (string) Settings.Default["src"];
            string dst = (string)Settings.Default["dst"];

            v = new FileValidator(src, dst);
            //            v = new FileValidator(@"E:\4-3-16 Bkup\Documents", @"c:\Users\Justin\Documents");
            // (DESTINATION, SOURCE)

            // file difference data
            v.networkFileMostRecent += netMoreRecentH;
            v.localFileMostRecent += locMoreRecentH;
            v.localFileMissing += locFileMissingH;
            v.localFileNotOnNetwork += locFileNotOnNetworkH;

            v.fileCopy += fileCopyH;
            v.dirCreated += dirCreatedH;

            v.fileDeleted += fileDelH;
            v.folderDeleted += folderDelH;

            // should these background workers be made here?    

            //does difference finding
            b = new BackgroundWorker();
            b.DoWork += new DoWorkEventHandler(bDoWork);
            b.ProgressChanged += new ProgressChangedEventHandler(bProgressChanged);
            b.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bCompleted);
            b.WorkerReportsProgress = true;
            b.WorkerSupportsCancellation = true;

            //does copying
            bu = new BackgroundWorker();
            bu.DoWork += new DoWorkEventHandler(buDoWork);
            bu.ProgressChanged += new ProgressChangedEventHandler(buProgressChanged);
            bu.RunWorkerCompleted += new RunWorkerCompletedEventHandler(buCompleted);
            bu.WorkerReportsProgress = true;
            bu.WorkerSupportsCancellation = true;

            //does deleting of files
            bd = new BackgroundWorker();
            bd.DoWork += new DoWorkEventHandler(bdDoWork);
            bd.ProgressChanged += new ProgressChangedEventHandler(bdProgressChanged);
            bd.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bdCompleted);
            bd.WorkerReportsProgress = true;
            bd.WorkerSupportsCancellation = true;

            // deletes empty folders
            bf = new BackgroundWorker();
            bf.DoWork += new DoWorkEventHandler(bfDoWork);
            bf.ProgressChanged += new ProgressChangedEventHandler(bfProgressChanged);
            bf.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bfCompleted);
            bf.WorkerReportsProgress = true;
            bf.WorkerSupportsCancellation = true;

            Begin.IsEnabled = false;
            Cancel.IsEnabled = false;
            //Update_Diff.IsEnabled = false;  // ADD FOR RELEASE

            Backup_Dest.Content = v.localDir;
            Backup_Source.Content = v.networkDir;
        }

        private void Window_Closing(object sender, CancelEventArgs e){
            Settings.Default["src"] = v.networkDir;
            Settings.Default["dst"] = v.localDir;
            Settings.Default.Save();
        }

        private void Source_Click(object sender, RoutedEventArgs e) {
            v.browseAndSetNetworkDir();
            Backup_Source.Content = v.networkDir;
            if (v.pathsLegal() && !b.IsBusy) {
                Update_Diff.IsEnabled = true; // ADD FOR RELEASE
                PanelText.Content = "Calculate file differences, then backup";
            }
        }

        private void Destination_Click(object sender, RoutedEventArgs e) {
            v.browseAndSetLocalDir();
            Backup_Dest.Content = v.localDir;
            if (v.pathsLegal() && !b.IsBusy) {
                Update_Diff.IsEnabled = true; // ADD FOR RELEASE
                PanelText.Content = "Calculate file differences, then backup";
            }
        }

        // copy from network path to local path
        private void Begin_Click(object sender, RoutedEventArgs e) {
            string msgBoxTxt = "Are you sure you want to copy from:\n" + v.networkDir + "\n\nto:\n" + v.localDir;
            if (Remove_Old_Destination.IsChecked == true) {
                msgBoxTxt += "\n\nand/or delete files from:\n" + v.localDir;
            }
            msgBoxTxt += '?';
            MessageBoxResult result = MessageBox.Show(msgBoxTxt, "Verify Backup", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            switch (result) {
                case MessageBoxResult.Yes:
                    break;
                case MessageBoxResult.No:
                    return;
            }
            filesCopied = 0;
            dirsCreated = 0;
            filesRemoved = 0;
            dirsRemoved = 0;
            deleting = false;
            Cancel.IsEnabled = true;
            Begin.IsEnabled = false;
            Destination.IsEnabled = false;
            Source.IsEnabled = false;
            Update_Diff.IsEnabled = false;
            Remove_Old_Destination.IsEnabled = false;

            PanelText.Content = "Updating"; // panel text conflict here
            infoTxtBox.Text = "";
            // copy if needed
            if (diffFiles != 0 && !bu.IsBusy) {
                bu.RunWorkerAsync();
            }
            // delete if needed
            if (diffLocalFiles !=0 && Remove_Old_Destination.IsChecked == true && !bd.IsBusy) {
                deleting = true;
                bd.RunWorkerAsync();
            }               
        }

        private void Diff_Click(object sender, RoutedEventArgs e) {
            if (!b.IsBusy) {
                PanelText.Content = "Loading";
                infoTxtBox.Text = "File Differences:\n";
                b.RunWorkerAsync();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {//to be added
            //if (b.WorkerSupportsCancellation) {
            //    b.CancelAsync();
            //}
        }

        // txt file with log info in same location as executable
        //private FileStream makeTXT() {
        //    string path = @"c:\Users\Justin\Desktop\Triumph\smart_backup_log.txt";
        //    if (File.Exists(path)) File.Delete(path);
        //    FileStream fs = File.Create(path);
        //    return fs;
        //}
        //private void writeTXTLine(string line, FileStream fs) {
        //    byte[] info = Encoding.ASCII.GetBytes(line + Environment.NewLine);
        //    fs.Write(info, 0, info.Length);
        //}

        private void finalScreen() {
            string msg = "\n\nFiles Copied:\t\t" + filesCopied + " / " + diffFiles + '\n'
                + "Directories Created:\t" + dirsCreated + '\n'
                + "\nLocal Files Deleted:\t" + filesRemoved + " / " + diffLocalFiles + '\n'
                + "Local Directories Deleted:\t" + dirsRemoved;
            infoTxtBox.AppendText(msg);

            if (Shutdown.IsChecked == true) {
                var psi = new ProcessStartInfo("shutdown", "/s /t 0");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }

            infoTxtBox.ScrollToEnd();
            Cancel.IsEnabled = false;            

            diffLocalFiles = 0; // confident system works - set these to 0            
            diffFiles = 0;

            Diff_Files.Content = diffFiles;
            Diff_Local_Files.Content = diffLocalFiles;

            PanelText.Content = "No Updates Needed";
            Update_Diff.IsEnabled = true;
            Destination.IsEnabled = true;
            Remove_Old_Destination.IsEnabled = true;
            Source.IsEnabled = true;
        }        




        // background worker events
        // executed in UI thread
        private void bCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                PanelText.Content = "Cancelled";
            } else if (e.Error != null) {
                PanelText.Content = "Error: " + e.Error.Message;
            } else { // success
                if (v.pathsLegal()) {
                    Diff_Files.Content = diffFiles;
                    Diff_Local_Files.Content = diffLocalFiles;
                } else {
                    Diff_Files.Content = "N/A";
                    Diff_Local_Files.Content = "N/A";
                }
                if (diffFiles != 0 || diffLocalFiles != 0) {
                    PanelText.Content = "Ready to Update";
                    Begin.IsEnabled = true;
                } else {
                    PanelText.Content = "No Updates Needed";
                    Begin.IsEnabled = false;
                    infoTxtBox.AppendText("None");
                }
            }            
        }
        // executed in UI thread
        private void bProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (e.ProgressPercentage == 0) {// net more recent
                infoTxtBox.AppendText("Source file more recent:\t" + e.UserState + '\n');
            } else if (e.ProgressPercentage == 1) {//loc more recent
                infoTxtBox.AppendText("Backup file more recent:\t" + e.UserState + '\n');
            } else if (e.ProgressPercentage == 2) {// loc missing
                infoTxtBox.AppendText("Backup missing:\t\t" + e.UserState + '\n');
                if (infoTxtBox.LineCount > 3000) {
                    infoTxtBox.Text = "";
                }
            } else {// loc not on network
                infoTxtBox.AppendText("Backup file not in source:\t" + e.UserState + '\n');
            }
            infoTxtBox.ScrollToEnd();
        }
        // executed in worker thread
        private void bDoWork(object sender, DoWorkEventArgs e) {
            if (v.pathsLegal()) {
                diffFiles = v.differentFiles();
                diffLocalFiles = v.countLocalFilesNotOnNetwork();
            }
        }
        // FileValidator diffFiles() handlers - executed by backgroundworker b
        private void netMoreRecentH(object sender, FileInfoEventArgs e) {
            b.ReportProgress(0, e.path);
        }
        private void locMoreRecentH(object sender, FileInfoEventArgs e) {
            b.ReportProgress(1, e.path);
        }
        private void locFileMissingH(object sender, FileInfoEventArgs e) {
            b.ReportProgress(2, e.path);
        }
        private void locFileNotOnNetworkH(object sender, FileInfoEventArgs e) {
            b.ReportProgress(3, e.path);
        }






        // b runs: examines differences
        // bu and bd run simultaneously and delete local files not on network and copy updated files to local
        // bf runs and removes empty folders locally

        private void buCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                PanelText.Content = "Cancelled";
            } else if (e.Error != null) {
                PanelText.Content = "Error: " + e.Error.Message;
            } else { // success  
                if (!bd.IsBusy && !bf.IsBusy && deleting) bf.RunWorkerAsync(); // delete empty folders if we are last to finish and we deleted files       
                else if (!deleting) finalScreen(); // no deleting needed, finish
            }
        }
        private void buProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (e.ProgressPercentage == 0) {// file copy
                filesCopied++;
                infoTxtBox.AppendText("File Copied:\t   " + e.UserState + '\n');

                if (infoTxtBox.LineCount > 3000) {
                    infoTxtBox.Text = "";
                }


            } else {//dir made
                dirsCreated++; 
                infoTxtBox.AppendText("Directory Created:   " + e.UserState + '\n');
            }
            infoTxtBox.ScrollToEnd();
        }
        // executed in backgroundworker thread
        private void buDoWork(object sender, DoWorkEventArgs e) {
            v.copyFilesInList();
        }
        private void fileCopyH(object sender, FileInfoEventArgs e) {
            bu.ReportProgress(0, e.path);
        }
        private void dirCreatedH(object sender, FileInfoEventArgs e) {
            bu.ReportProgress(1, e.path);
        }







        private void bdCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                PanelText.Content = "Cancelled";
            } else if (e.Error != null) {
                PanelText.Content = "Error: " + e.Error.Message;
            } else { // success
                if (!bu.IsBusy && !bf.IsBusy) bf.RunWorkerAsync(); // after delete files, delete folders if other bkgrndwrkr is done
            }
        }
        private void bdProgressChanged(object sender, ProgressChangedEventArgs e) {
            filesRemoved++;
            infoTxtBox.AppendText("File Deleted:\t   " + e.UserState + '\n');
            infoTxtBox.ScrollToEnd();
        }
        // executed in different thread
        private void bdDoWork(object sender, DoWorkEventArgs e) {
            v.deleteFilesInList();
        }
        private void fileDelH(object sender, FileInfoEventArgs e) {
            bd.ReportProgress(0, e.path);
        }







        private void bfCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                PanelText.Content = "Cancelled";
            } else if (e.Error != null) {
                PanelText.Content = "Error: " + e.Error.Message;
            } else { // success                
                finalScreen();
            }
        }
        private void bfProgressChanged(object sender, ProgressChangedEventArgs e) {
            dirsRemoved++;
            infoTxtBox.AppendText("Folder Deleted:\t   " + e.UserState + '\n');
            infoTxtBox.ScrollToEnd();
        }
        // executed in different thread
        private void bfDoWork(object sender, DoWorkEventArgs e) {
            v.removeEmptyFolders(v.localDir);
        }
        private void folderDelH(object sender, FileInfoEventArgs e) {
            bf.ReportProgress(0, e.path);
        }
    }
}

using System;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Collections;


namespace Smart_Backup {
    // used for sending an extra string as an argument in the events
    class FileInfoEventArgs : EventArgs {
        public String path { get; set; }

        public FileInfoEventArgs(string pathParam) {
            path = pathParam;
        }
    }

    class FileValidator {
        public string networkDir { get; set; } // network directory path
        public string localDir { get; set; } // local directory path

        private const string networkInfoFileName = "releaseInfo"; // name for the .txt and .xml summary file(s)
        private string networkInfoTXTFilePath { get { return networkDir + "\\" + networkInfoFileName + ".txt"; } }
        private string networkInfoXMLFilePath { get { return networkDir + "\\" + networkInfoFileName + ".xml"; } }

        public ArrayList filesToCopy;
        public ArrayList localFilesToDelete;

        public event EventHandler<FileInfoEventArgs> fileOverWrite;
        public event EventHandler<FileInfoEventArgs> fileExamined;
        public event EventHandler<FileInfoEventArgs> filesIdentical;
        public event EventHandler<FileInfoEventArgs> localFileMissing;
        public event EventHandler<FileInfoEventArgs> localFileMostRecent;
        public event EventHandler<FileInfoEventArgs> fileCopy;
        public event EventHandler<FileInfoEventArgs> networkFileMostRecent;
        public event EventHandler<FileInfoEventArgs> dirCreated;
        public event EventHandler<FileInfoEventArgs> fileDeleted;
        public event EventHandler<FileInfoEventArgs> localFileNotOnNetwork;
        public event EventHandler<FileInfoEventArgs> folderDeleted;

        FolderBrowserDialog folderBrowserDialog;

        // zero argument constructor
        public FileValidator()
            : this(null, null) { }

        // constructor that stores local and network dir
        public FileValidator(string src, string dest) {
            this.localDir = dest;
            this.networkDir = src;
        }

        // returns true if both networkDir and localDir are legal paths
        public bool pathsLegal() {
            return Directory.Exists(networkDir) && Directory.Exists(localDir);
        }

        // opens a dialog and allows user to browse to the Network Directory
        public void browseAndSetNetworkDir() {
            if(folderBrowserDialog == null) folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                networkDir = folderBrowserDialog.SelectedPath;
            }
        }

        // opens a dialog and allows user to browse to the local Directory
        public void browseAndSetLocalDir() {
            if (folderBrowserDialog == null) folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                localDir = folderBrowserDialog.SelectedPath;
            }
        }

        // returns the number of files at the given path, includes subfolders if recuse is true
        public int numFiles(string path, bool recurse) {
            if (recurse) {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            } else {
                return Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length;
            }
        }

        // Recursively compares network and local files. Returns the number of files that are different.
        // Counts any pair of files with different dates or a missing local file as different.
        // Ignores computer-generated summary files.
        public int differentFiles() {
            if (!pathsLegal()) throw new InvalidOperationException("Paths not legal");
            Environment.CurrentDirectory = localDir;
            filesToCopy = new ArrayList();
            return differentFiles(networkDir);
        }
        private int differentFiles(string netFilePath) {
            if (File.Exists(netFilePath)) {               
                string relativePath = netFilePath.Substring(networkDir.Length + 1);
                //string localFilePath = localDir + "\\" + relativePath;

                
                //FileInfo info = new FileInfo(netFilePath);
                //Console.WriteLine("File Size in bytes: {0}: {1}", relativePath, info.Length);


                if (File.Exists(relativePath)) {
                    DateTime net = File.GetLastWriteTime(netFilePath);
                    DateTime loc = File.GetLastWriteTime(relativePath);
                    int diff = DateTime.Compare(net, loc);
                    if (diff == 0) {
                        return 0;
                    } else { // dates don't match - either one could be newer
                        if (diff > 0) {
                            onNetworkFileMostRecent(null, new FileInfoEventArgs(relativePath));
                        } else {
                            onLocalFileMostRecent(null, new FileInfoEventArgs(relativePath));
                        }
                        filesToCopy.Add(relativePath);
                        return 1;
                    }
                } else { //The local directory is missing the file


                    // check to see if the folder was just renamed



                    onLocalFileMissing(null, new FileInfoEventArgs(relativePath));
                    filesToCopy.Add(relativePath);
                    return 1;
                }
            } else if (Directory.Exists(netFilePath)) {
                try {
                    string[] netEntries = Directory.GetFileSystemEntries(netFilePath);
                    int ret = 0;
                    foreach (string path in netEntries) {
                        ret += differentFiles(path);
                    }
                    return ret;
                } catch (UnauthorizedAccessException ex) {
                    Trace.WriteLine("Could not get File system entries from: " + netFilePath);
                    return 0;
                }                
            } else { // not existing file or directory
                Trace.WriteLine("\n\n\nfile or directory doesn't exist: " + netFilePath + "\n\n\n");
                return 0;
                //throw new InvalidDataException("file or directory doesn't exist: " + netFilePath);
            }
        }

        // copies the files in the array list from the network to the local path
        public void copyFilesInList() {
            if (filesToCopy != null) {
                Environment.CurrentDirectory = localDir;
                foreach (string relativePath in filesToCopy) {
                    //string localFilePath = localDir + "\\" + relativePath;
                    string netFilePath = networkDir + "\\" + relativePath;
                    string dir = Path.GetDirectoryName(relativePath);
                    if (dir != String.Empty && !Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                        onDirCreated(null, new FileInfoEventArgs(dir));
                    }
                    File.SetAttributes(netFilePath, FileAttributes.Normal);
                    try {
                        File.Copy(netFilePath, relativePath, true);
                        onFileCopy(null, new FileInfoEventArgs(relativePath));
                    } catch (Exception e) {
                        Trace.WriteLine("Could not copy the file: " + netFilePath);
                    }
                }
            }            
        }



























        // removes files at local path without corresponding files on network
        // then recursively removes all empty folders that remain
        public void removeLocalFilesNotOnNetwork() {
            if (!pathsLegal()) throw new InvalidOperationException("Paths not legal");
            Environment.CurrentDirectory = networkDir;
            removeFiles(localDir);
            removeEmptyFolders(localDir);
        }
        private void removeFiles(string localPath) {
            if (File.Exists(localPath)) {
                string relativePath = localPath.Substring(localDir.Length + 1);
                //string networkPath = networkDir + "\\" + relativePath;
                if (!File.Exists(relativePath)) {
                    //Trace.WriteLine("deleted local file:  " + localPath);
                    File.SetAttributes(localPath, FileAttributes.Normal);
                    File.Delete(localPath);
                }
            } else if (Directory.Exists(localPath)) {
                string[] netEntries = Directory.GetFileSystemEntries(localPath);
                foreach (string path in netEntries) {
                    removeFiles(path);
                }
            } else { // not existing file or directory
                throw new InvalidDataException("file or directory doesn't exist: " + localPath);
            }
        }
        public void removeEmptyFolders(string startLocation) {
            foreach (var directory in Directory.GetDirectories(startLocation)) {
                removeEmptyFolders(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0) {
                    Directory.Delete(directory, false);
                    //Trace.WriteLine("deleted local directory:  " + directory);
                    onFolderDeleted(null, new FileInfoEventArgs(directory));
                }
            }
        }

        // countLocalFilesNotOnNetwork and deleteFilesInList are a 2-part breakdown of removeLocalFilesNotOnNetwork
        public int countLocalFilesNotOnNetwork() {
            if (!pathsLegal()) throw new InvalidOperationException("Paths not legal");
            Environment.CurrentDirectory = networkDir;
            localFilesToDelete = new ArrayList();
            return countFiles(localDir);
        }
        private int countFiles(string localPath) {
            if (File.Exists(localPath)) {
                string relativePath = localPath.Substring(localDir.Length + 1);
                //string networkPath = networkDir + "\\" + relativePath;
                if (!File.Exists(relativePath)) {
                    localFilesToDelete.Add(relativePath);
                    //Trace.WriteLine("added: " + relativePath);
                    onlocalFileNotOnNetwork(null, new FileInfoEventArgs(relativePath));
                    return 1;
                }
                return 0;
            } else if (Directory.Exists(localPath)) {
                try {
                    string[] netEntries = Directory.GetFileSystemEntries(localPath);
                    int sum = 0;
                    foreach (string path in netEntries) {
                        sum += countFiles(path);
                    }
                    return sum;
                } catch (UnauthorizedAccessException) {
                    Trace.WriteLine("\n\nUnauthorized Access Exception: " + localPath + "\n\n\n");
                    return 0;
                }                
            } else { // not existing file or directory
                Trace.WriteLine("\n\nfile or directory doesn't exist: " + localPath + "\n\n\n");
                return 0;
                //throw new InvalidDataException("file or directory doesn't exist: " + localPath);
            }
        }

        // deletes the files in the array list from countLocalFilesNotOnNetwork()
        public void deleteFilesInList() {
            if (localFilesToDelete != null) {
                Environment.CurrentDirectory = localDir;
                foreach (string relativePath in localFilesToDelete) {
                    //string localFilePath = localDir + "\\" + relativePath;
                    File.SetAttributes(relativePath, FileAttributes.Normal);
                    File.Delete(relativePath);
                    //Trace.WriteLine("deleted: " + relativePath);
                    onFileDelete(null, new FileInfoEventArgs(relativePath));
                }
                //removeEmptyFolders(localDir);
            }            
        }


        // compare files within stored networkDir to the files within localDir
        // recursively searches files within subfolders
        // copies newer files from network to local if copy is true - see compareFile() for details
        // TODO: make this a bool and return true upon success
        public void compareDirectories(bool copy, bool replaceRecentLocal, bool copyMissing) {
            if (!pathsLegal()) {
                throw new InvalidOperationException("Paths not legal");
            }
            compareDirectories(networkDir, copy, replaceRecentLocal, copyMissing);
        }

        // Recursively searches files within subfolders of targetDirectory.
        // Calls compareFile() for every file found.
        // Ignores the computer-generated file inventory documents.
        private void compareDirectories(string targetDirectory, bool copy, bool replaceRecentLocal, bool copyMissing) {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries) {
                compareFile(fileName, copy, replaceRecentLocal, copyMissing);                
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                compareDirectories(subdirectory, copy, replaceRecentLocal, copyMissing);
        }

        // Compares timestamp of file at given network file path to the corresponding file in the local
        // directory. Replaces local file with network file if network file is newer and copy is true.
        // Replaces local file even if local file is newer if replaceRecentLocal and copy are true.
        // Creates a path if needed and copies a network file that doesn't exist locally if copy and copyMissing
        // are true. Fires many events as seen below.
        private void compareFile(string netFilePath, bool copy, bool replaceRecentLocal, bool copyMissing) {
            string relativePath = netFilePath.Substring(networkDir.Length + 1);
            string localFilePath = localDir + "\\" + relativePath;

            //Trace.WriteLine("Processing network file: " + netFilePath);
            //Trace.WriteLine("Looking for local file: " + localFilePath);

            if (File.Exists(localFilePath)) {
                //Trace.Write("The local directory has a copy of the file: " + netFilePath + "  :  ");
                DateTime net = File.GetLastWriteTime(netFilePath);
                DateTime loc = File.GetLastWriteTime(localFilePath);
                int diff = DateTime.Compare(net, loc);
                if (diff == 0) {
                    onFilesIdentical(null, new FileInfoEventArgs(relativePath));
                    //Trace.WriteLine("up to date");
                } else if (diff > 0) {
                    // Trace.Write("network copy is more recent");
                    
                    if (copy) {
                        //Trace.WriteLine(" - Copied and replaced local file");
                        onLocalFileOverWrite(null, new FileInfoEventArgs(relativePath));
                        File.Copy(netFilePath, localFilePath, true);
                    }
                } else {
                    //Trace.WriteLine("local is more recent: error");
                    
                    if (replaceRecentLocal && copy) {
                        onLocalFileOverWrite(null, new FileInfoEventArgs(relativePath));
                        File.Copy(netFilePath, localFilePath, true);
                    }
                }                
            } else {
                
                if (copyMissing && copy) {
                    // create needed dir if it doesn't exist
                    string dir = Path.GetDirectoryName(localFilePath);
                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                        onDirCreated(null, new FileInfoEventArgs(dir));
                    }
                    onFileCopy(null, new FileInfoEventArgs(localFilePath));
                    File.Copy(netFilePath, localFilePath, true);
                }
                //Trace.WriteLine("The local directory is missing the file: " + netFilePath);
            }
            onFileExamined(null, new FileInfoEventArgs(relativePath));
            //Trace.WriteLine("");
        }







        public void tryBackup() {
            Environment.CurrentDirectory = localDir;
            string[] files = Directory.GetFiles(networkDir, "*", SearchOption.AllDirectories);
            foreach (string file in files) {
                string relativePath = file.Substring(networkDir.Length + 1);
                //Trace.WriteLine(file);
                try {
                    if (File.Exists(relativePath)) {
                        DateTime net = File.GetLastWriteTime(file);
                        DateTime loc = File.GetLastWriteTime(relativePath);
                        int diff = DateTime.Compare(net, loc);
                        if (diff == 0) {
                            //Trace.WriteLine("up to date");
                        } else if (diff > 0) {
                            //Trace.Write("network copy is more recent");
                            //Trace.WriteLine(" - Copied and replaced local file");
                            File.Copy(file, relativePath, true);
                        } else {
                            //Trace.WriteLine("local is more recent: error");
                            File.Copy(file, relativePath, true);
                        }
                    } else {
                        //Trace.WriteLine("Local file doesn't exist");
                        string dir = Path.GetDirectoryName(relativePath);
                        if (dir != "")  Directory.CreateDirectory(dir);
                        File.Copy(file, relativePath);
                    }  
                } catch (Exception ex) {
                    Trace.WriteLine("Exception copying file. " + file + ex.Data + ex.Message);
                }  
            }
        }












        // functions to trigger events
        private void onFileExamined(object sender, FileInfoEventArgs e) {
            if (fileExamined != null) { // if we have been given a function point provided by the client
                fileExamined(this, e); // trigger the event
            }
        }
        private void onLocalFileOverWrite(object sender, FileInfoEventArgs e) {
            if (fileOverWrite != null) {
                fileOverWrite(this, e);
            }
        }
        private void onLocalFileMissing(object sender, FileInfoEventArgs e) {
            if (localFileMissing != null) {
                localFileMissing(this, e);
            }
        }
        private void onNetworkFileMostRecent(object sender, FileInfoEventArgs e) {
            if (networkFileMostRecent != null) {
                networkFileMostRecent(this, e);
            }
        }
        private void onLocalFileMostRecent(object sender, FileInfoEventArgs e) {
            if (localFileMostRecent != null) {
                localFileMostRecent(this, e);
            }
        }
        private void onFilesIdentical(object sender, FileInfoEventArgs e) {
            if (filesIdentical != null) {
                filesIdentical(this, e);
            }
        }
        private void onFileCopy(object sender, FileInfoEventArgs e) {
            if (fileCopy != null) {
                fileCopy(this, e);
            }
        }
        private void onDirCreated(object sender, FileInfoEventArgs e) {
            if (dirCreated != null) {
                dirCreated(this, e);
            }
        }
        private void onFileDelete(object sender, FileInfoEventArgs e) {
            if (fileDeleted != null) {
                fileDeleted(this, e);
            }
        }
        private void onlocalFileNotOnNetwork(object sender, FileInfoEventArgs e) {
            if (localFileNotOnNetwork != null) {
                localFileNotOnNetwork(this, e);
            }
        }
        private void onFolderDeleted(object sender, FileInfoEventArgs e) {
            if (folderDeleted != null) {
                folderDeleted(this, e);
            }
        }
    }
}
﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace GameLauncher
{
    //Hosted in thomas.bruu81
    //Google Drive -> https://drive.google.com/drive/folders/1nQoMZuKlcqfZ9fFJ203r3e5fPcr_wbWI
    //Insérer build et version file
    //clique droit avoir le lien share à anyone en viewer puis aller dans un converter en ligne de type google-drive-direct-link-generator/ -> et mettre ce link les lignes de codes ou on le demande
    //change images icon or other -> select -> properties -> Generation Action -> ressource
    //si on voudra faire un update de juste les fichiers qui sont modifiés faudra faire des hash pour chacun de nos fichiers dans le dossier build
    //et les comparer et si ils sont différents on update que certains fichiers

    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        dowloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string roothPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.dowloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            roothPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(roothPath, "Version.txt");
            gameZip = Path.Combine(roothPath, "Build.zip");
            gameExe = Path.Combine(roothPath, "Build", "AS.exe"); //Mon nom de fichier .exe et nom de dossier ou est retenu le jeu
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient(); //update soon en http client
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1aqrqhVloRuBwucYAfEGib7izyr3c0UFO")); //google drive direct link au version.txt

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion); //si r marche le passer en glase
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch(Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();

                //installer le jeu pour la première fois ou update ?
                if (_isUpdate)
                {
                    Status = LauncherStatus.dowloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1aqrqhVloRuBwucYAfEGib7izyr3c0UFO")); //google drive direct link au version.txt
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);

                //google drive direct link au build.zip
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1VE_BKzvUYyWErht64-JxXrR6Ii1nXtk9"), gameZip, _onlineVersion); // on fait un async car si on le faisait de manière synchrone ça voudrait dire que l'app ne répondrait pas jusqu'a la fin du téléchargement
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();

                MessageBox.Show($"Online version is: {onlineVersion}");
                ZipFile.ExtractToDirectory(gameZip, roothPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);
                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;

            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        /// <summary>
        /// Checker dans la mainWindow.xaml -> propriétées -> Content Renderer au dessus de Title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        /// <summary>
        /// Quand on clique sur le bouton -> référer au main window xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(roothPath, "Build");
                MessageBox.Show(startInfo.WorkingDirectory);
                Process.Start(startInfo);

                Close();
            }
            else if(Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else
            {
                MessageBox.Show("start button hitted but no exe");
                Close();
            }
        }
    }

    //Struct qui permet de stocker les versions du jeu
    //En gros une version(la classe par défaut) contient majeur, mineur/sub mineur, build et je sais plus et nous on va avoir
    //majeur / mineur, submineur
    struct Version
    {
        internal static Version zero = new Version(0, 0, 0); //on l'utilise en tant que default

        private short major;
        private short minor;
        private short subminor;

        internal Version(short _major, short _minor, short _subminor)
        {
            this.major = _major;
            this.minor = _minor;
            this.subminor = _subminor;
        }

        //utilisée quand on lit les chiffres de la version depuis un fichier
        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.'); //divise une chaine de charactères _version en sous-chaines séparée par le caractère .

            if (_versionStrings.Length != 3) //vérifier si la version n'est pas au bon format
            {
                major = 0;
                minor = 0;
                subminor = 0;
                return;
            }

            //Si au bon format
            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subminor  = short.Parse(_versionStrings[2]);    
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            //si il y'a une différence dans les versions
            if(major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if(minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if(subminor != _otherVersion.subminor)
                    {
                        return true;
                    }
                }
            }

            //si il n'y a aucune différence
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subminor}"; //quand on get la version les . sont séparée par un split plus haut
        }
    }
}
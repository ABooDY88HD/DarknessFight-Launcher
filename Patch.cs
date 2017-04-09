using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Text;
using System.Reflection;

using ICSharpCode.SharpZipLib.Zip;

namespace Infinite_Darkness_Launcher
{
    class Patch
    {
        private const string PATCH_SERVER = "https://darknessfight.com/patch/";
        public string LAUNCHER_EXE = Assembly.GetExecutingAssembly().Location;
        public string ROOT_FOLDER = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        List<CSPATCH_INDEX> m_lPatchList = new List<CSPATCH_INDEX>();
        private MainWindow m_pWnd;

        private const int VERSION = 1103;
        private int m_nCurrentVersion = 0;
        private int m_nNewVersion = 0;

        public void PatchClient() {
            m_pWnd = MainWindow.m_pWnd;
            try {
                CheckForLauncherUpdate();
                CheckForUpdates();
            }
            catch ( System.Net.WebException ex ) {
                var result = MessageBox.Show( "There was an error connecting to the patch server.\nWould you like to try to connect to the game anyway?\n\n" + ex.Message, "Error!", MessageBoxButton.YesNo, MessageBoxImage.Question );
                if ( result == MessageBoxResult.Yes ) {
                    InvokeStartButton();
                    InvokeUpdatePercentage( 100 );
                    InvokeUpdateLabel( "Cancelled update." );
                }
                else {
                    m_pWnd.Dispatcher.Invoke( (Action)( () => {
                        m_pWnd.Close();
                    } ) );
                }
            }
            catch ( Exception ex ) {
                MessageBox.Show( string.Format( "There was an error at Patch.PatchClient()\r\n{0}", ex.Message ), "Error!", MessageBoxButton.OK, MessageBoxImage.Error );
            }
        }

        private void CheckForLauncherUpdate() {
            string szDownloadBuffer;
            using ( System.Net.WebClient wc = new System.Net.WebClient() ) {
                szDownloadBuffer = wc.DownloadString( PATCH_SERVER + "version.txt" );

                if ( Convert.ToInt32(szDownloadBuffer) > VERSION) {
                    string szNewLauncherZipPath = "";
                    wc.DownloadFile( string.Format( "{0}Launcher.zip", PATCH_SERVER ), szNewLauncherZipPath = Path.GetTempFileName() );

                    File.Move( LAUNCHER_EXE, Path.Combine( ROOT_FOLDER, "Launcher.exe_OLD" ) );
                    ExtractZipFile( szNewLauncherZipPath, null, ROOT_FOLDER );

                    if ( File.Exists( szNewLauncherZipPath ) ) {
                        File.Delete( szNewLauncherZipPath );
                    }

                    ProcessStartInfo Info = new ProcessStartInfo();
                    Info.Arguments = "/C ping 127.0.0.1 -n 3 && \"" + LAUNCHER_EXE + "\"";
                    Info.WindowStyle = ProcessWindowStyle.Hidden;
                    Info.CreateNoWindow = true;
                    Info.FileName = "cmd.exe";
                    Process.Start( Info );
                    m_pWnd.Dispatcher.Invoke( (Action)( () => {
                        m_pWnd.Close();
                    } ) );
                }
            }
        }

        private void CheckForUpdates() {
            using ( System.Net.WebClient webClient = new System.Net.WebClient() ) {
                string version = Encoding.ASCII.GetString( webClient.DownloadData( PATCH_SERVER + "maxversion.txt" ) );
                string webData = Encoding.ASCII.GetString( webClient.DownloadData( PATCH_SERVER + "patchlist.txt" ) );
                int.TryParse( version, out m_nNewVersion );

                string[] patchList = webData.Split( '\n' );

                foreach ( string s in patchList ) {
                    string[] szTmp = s.Split( '\t' );
                    CSPATCH_INDEX index;
                    index.hashedName = szTmp[0];
                    index.realName = szTmp[1];
                    index.version = int.Parse( szTmp[2] );
                    m_lPatchList.Add( index );
                }

                if ( CreatePatchList() ) {
                    InvokeUpdateLabel( "Found update!" );
                    PatchFiles();
                    File.WriteAllText( Path.Combine( ROOT_FOLDER, "version" ), version );
                }
                else {
                    m_pWnd.updateProgress.Dispatcher.Invoke((Action)(() =>
                    {
                        m_pWnd.updateProgress.IsIndeterminate = false;
                    }));
                    InvokeUpdateLabel( "Already updated." );
                    InvokeUpdatePercentage( 100 );
                }
                InvokeStartButton();
            }
        }

        private bool CreatePatchList() {
            bool bResult = false;
            List<CSPATCH_INDEX> newPatch = new List<CSPATCH_INDEX>();
            foreach ( CSPATCH_INDEX index in m_lPatchList ) {
                if ( index.version > GetVersion() && index.version <= m_nNewVersion ) {
                    newPatch.Add( index );
                    bResult = true;
                }
            }
            m_lPatchList = newPatch;
            return bResult;
        }

        private int GetVersion() {
            if ( m_nCurrentVersion == 0 ) {
                string szFile = Path.Combine( ROOT_FOLDER, "version" );
                if ( File.Exists( szFile ) ) {
                    string szContent = File.ReadAllText( szFile );
                    if ( !int.TryParse( szContent, out m_nCurrentVersion ) ) {
                        File.Delete( szFile );
                        m_nCurrentVersion = 1;
                    }
                    return m_nCurrentVersion;
                }
                else {
                    m_nCurrentVersion = 1;
                    return m_nCurrentVersion;
                }
            }
            else {
                return m_nCurrentVersion;
            }
        }

        private void PatchFiles() {
            XClientManager manager = new XClientManager();
            manager.Open( Path.Combine( ROOT_FOLDER, "data.000" ) );
            m_pWnd.updateProgress.Dispatcher.Invoke((Action)(() =>
            {
                m_pWnd.updateProgress.IsIndeterminate = false;
                m_pWnd.updateProgress.Maximum = m_lPatchList.Count;
            }));

            foreach ( CSPATCH_INDEX index in m_lPatchList ) {
                InvokeUpdateLabel( "Currently updating: " + index.hashedName );
                string szZip = "";
                string szTemp = Path.Combine( Path.GetTempPath(), index.realName );

                using ( var webClient = new System.Net.WebClient() ) {
                    string szDownloadURL = string.Format( "{0}/files/{2}.zip", PATCH_SERVER, index.version, index.realName );
                    webClient.DownloadFile( szDownloadURL, ( szZip = Path.GetTempFileName() ) );
                    if ( File.Exists( szTemp ) )
                        File.Delete( szTemp );

                    ExtractZipFile( szZip, null, Path.GetTempPath() );

                }
                //if(new System.IO.FileInfo(szTemp).Length > 15728640 ) // MB
                manager.Patch( szTemp, ROOT_FOLDER, index );
                if ( File.Exists( szTemp ) )
                    File.Delete( szTemp );

                InvokeUpdatePercentage(1);
            }
            manager.Save( Path.Combine( ROOT_FOLDER, "data.000" ) );

            InvokeUpdatePercentage( 100 );
            InvokeUpdateLabel( "Update successful." );
        }

        #region Invokes

        private void InvokeUpdateLabel(string szContent)
        {
            m_pWnd.lbUpdates.Dispatcher.Invoke((Action)(() =>
            {
                m_pWnd.lbUpdates.Content = szContent;
            }));
        }

        private void InvokeUpdatePercentage(int percentage)
        {
            int strPercentage = 100;
            m_pWnd.updateProgress.Dispatcher.Invoke((Action)(() =>
            {
                if (percentage == 100)
                    m_pWnd.updateProgress.Value = 100;
                else
                {
                    m_pWnd.updateProgress.Value += percentage;
                    strPercentage = (int)Math.Round((m_pWnd.updateProgress.Value / m_pWnd.updateProgress.Maximum) * 100);
                }
            }));

            m_pWnd.lbUpdatePercentage.Dispatcher.Invoke((Action)(() =>
            {
                m_pWnd.lbUpdatePercentage.Content = strPercentage.ToString() + "%";
            }));
        }

        private void InvokeStartButton()
        {

            m_pWnd.btnStart.Dispatcher.Invoke((Action)(() =>
            {
                var uri = new Uri("pack://application:,,,/play_on.png");
                System.Windows.Media.ImageSource img = new System.Windows.Media.Imaging.BitmapImage(uri); // set the ImageSource property
                m_pWnd.btnStart.Source = img;
            }));
            MainWindow.isStartable = true;
        }

        #endregion

        public void ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
        {
            FastZip fastZip = new FastZip();
            string fileFilter = null;

            // Will always overwrite if target filenames already exist
            fastZip.ExtractZip(archiveFilenameIn, outFolder, fileFilter);
        }
    }

    public struct CSPATCH_INDEX
    {
        public string hashedName;
        public string realName;
        public int version;
    };
}

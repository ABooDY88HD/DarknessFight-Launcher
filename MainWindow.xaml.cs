using System;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using HANDLE = System.IntPtr;
using System.Threading;

namespace Infinite_Darkness_Launcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window {
        #region For Starting
        [DllImport( "Kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto )]
        public static extern HANDLE CreateEvent(ref SECURITY_ATTRIBUTES lpEventAttributes, [In, MarshalAs( UnmanagedType.Bool )] bool bManualReset, [In, MarshalAs( UnmanagedType.Bool )] bool bIntialState, [In, MarshalAs( UnmanagedType.BStr )] string lpName);

        [DllImport( "Kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi )]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [StructLayout( LayoutKind.Sequential )]
        public struct SECURITY_ATTRIBUTES {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        static HANDLE createEvent() {
            SECURITY_ATTRIBUTES securityAttrib = new SECURITY_ATTRIBUTES();

            securityAttrib.bInheritHandle = 1;
            securityAttrib.lpSecurityDescriptor = IntPtr.Zero;
            securityAttrib.nLength = Marshal.SizeOf( typeof( SECURITY_ATTRIBUTES ) );

            return CreateEvent( ref securityAttrib, false, false, String.Empty );
        }
        #endregion

        private string ROOT_FOLDER = "";
        private System.Collections.Generic.List<string> m_lLink = new System.Collections.Generic.List<string>();

        private Thread oThread;
        public static MainWindow m_pWnd;
        public static bool isStartable = false;

        public MainWindow() {
            InitializeComponent();
            ROOT_FOLDER = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
        }

        private void imgClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Application.Current.Shutdown();
        }

        private void imgMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void btnStart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if ( isStartable ) {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                HANDLE eventHandle = createEvent();

                string sframeFilename = "SFrame.exe";
                startInfo.EnvironmentVariables[String.Format( "{0}_PARENT", sframeFilename )] = "Launcher.exe";
                startInfo.EnvironmentVariables[String.Format( "{0}_RUNNER", sframeFilename )] = eventHandle.ToString();
                startInfo.UseShellExecute = false;
                startInfo.FileName = "SFrame.exe";
                startInfo.Arguments = " /auth_ip:auth.heavensfall.net /use_nprotect:0 /help_url_w:611 /help_url_h:625 /locale:ASCII /country:US /cash /commercial_shop /layout_dir:6 /layout_auto:0 /cash_url_w:800 /cash_url_h:631 /network.max_msg_process:1 /user_no:1";
                try {
                    Process.Start( startInfo );
                }
                catch ( Exception ex ) {
                    MessageBox.Show( ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error );
                    Application.Current.Shutdown();
                }

                WaitForSingleObject( eventHandle, 10 * 1000 );
                Application.Current.Shutdown();
            }
        }

        private void Window_Initialized(object sender, EventArgs e) {
            try {
                CheckForOldFiles();
                using ( System.Net.WebClient net = new System.Net.WebClient() ) {
                    string[] t = net.DownloadString( "https://darknessfight.com/patch/patchnews.php" ).Split('\n');
                    for(int i = 0; i < 6; i++)
                    {
                        var a = t[i].Split('%');
                        m_lLink.Add(a[1]);
                        a[0] = System.Net.WebUtility.HtmlDecode(a[0]);
                        switch (i)
                        {
                            case 0:
                                label_Copy1.Content = a[0];
                                label_Copy1.MouseLeftButtonDown += openLink;
                                break;
                            case 1:
                                label_Copy2.Content = a[0];
                                label_Copy2.MouseLeftButtonDown += openLink;
                                break;
                            case 2:
                                label_Copy3.Content = a[0];
                                label_Copy3.MouseLeftButtonDown += openLink;
                                break;
                            case 3:
                                label_Copy4.Content = a[0];
                                label_Copy4.MouseLeftButtonDown += openLink;
                                break;
                            case 4:
                                label_Copy5.Content = a[0];
                                label_Copy5.MouseLeftButtonDown += openLink;
                                break;
                            case 5:
                                label_Copy.Content = a[0];
                                label_Copy.MouseLeftButtonDown += openLink;
                                break;
                        }
                    }
                }
                    m_pWnd = this;
                Patch patch = new Patch();
                oThread = new Thread( new ThreadStart( patch.PatchClient ) ) { IsBackground = true };
                oThread.Start();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error );

            }
        }

        private void openLink(object sender, MouseButtonEventArgs e)
        {
            string url = "";
            if (sender == label_Copy1)
                url = m_lLink[0];
            else if (sender == label_Copy2)
                url = m_lLink[1];
            else if (sender == label_Copy3)
                url = m_lLink[2];
            else if (sender == label_Copy4)
                url = m_lLink[3];
            else if (sender == label_Copy5)
                url = m_lLink[4];
            else if (sender == label_Copy)
                url = m_lLink[5];
            Process.Start(url);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if ( oThread.IsAlive )
                oThread.Abort();
        }

        private void CheckForOldFiles() {
            string[] szFiles = { "Launcher.exe_OLD", "Launcher2.exe" };
            foreach ( string szFile in szFiles ) {
                string szExecutable = Path.Combine( ROOT_FOLDER, szFile );
                if ( File.Exists( szExecutable ) ) {
                    File.Delete( szExecutable );
                }
            }
        }

        private void btnSettings_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(System.IO.File.Exists("RappelzConfig.exe"))
            {
                Process.Start("RappelzConfig.exe", "/locale:ASCII");
            }
            else
            {
                MessageBox.Show("You do not have a RappelzConfig.exe.");
            }
        }
    }
}
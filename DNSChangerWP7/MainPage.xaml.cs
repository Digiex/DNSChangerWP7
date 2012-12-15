using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using WP7RootToolsSDK;
#if DEBUG
using System.Diagnostics;
#endif

namespace DNSChangerWP7
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            if (!WP7RootToolsSDK.Environment.HasRootAccess())
            {
                MessageBox.Show("This app requires Root access");
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            refreshAdapters();
        }

        private void refreshAdapters()
        {
            try
            {
                RegistryKey commKey = Registry.GetKey(RegistryHyve.LocalMachine, "Comm");
                foreach (RegistryItem subitem in commKey.GetSubItems())
                {
                    if (subitem is RegistryKey)
                    {
                        RegistryKey subkey = (RegistryKey)subitem;
#if DEBUG
                        Debug.WriteLine(subkey.Name);
#endif
                        //TODO: Cleaner way to check if a key exists
                        try
                        {
                            RegistryKey tcpipkey = subkey.GetSubKey("Parms\\Tcpip");
                            if (tcpipkey.GetValueCount() > 0)
                            {
                                string DNS = "none";
                                string primaryDNS = "";
                                try
                                {
                                    string[] dnss = Registry.GetMultiStringValue(tcpipkey.Root, tcpipkey.Path, "DNS");
                                    DNS = String.Join(", ", dnss);
                                    primaryDNS = dnss[0];
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        DNS = "DHCP: " + String.Join(", ", Registry.GetMultiStringValue(tcpipkey.Root, tcpipkey.Path, "DhcpDNS"));
                                    }
                                    catch (Exception) { }
                                }
                                lstAdapters.Items.Add(new lstAdapterItem() { Name = subkey.Name, Description = DNS, PrimaryDNS = primaryDNS, KeyPath = tcpipkey.Path, KeyRoot = tcpipkey.Root });
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OK);
            }
        }
        public class lstAdapterItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string PrimaryDNS { get; set; }
            public RegistryHyve KeyRoot { get; set; }
            public string KeyPath { get; set; }
        }

        private void lstAdapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lstAdapterItem selItem = ((lstAdapterItem)lstAdapters.SelectedItem);
            TextBox dnsBox = new TextBox() { Text = selItem.PrimaryDNS };

            TiltEffect.SetIsTiltEnabled(dnsBox, true);

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = "Edit DNS for " + selItem.Name,
                Message = "Primary DNS Server:",
                Content = dnsBox,
                LeftButtonContent = "save",
                RightButtonContent = "cancel",
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                if (e1.Result == CustomMessageBoxResult.LeftButton)
                {
                    Registry.SetMultiStringValue(selItem.KeyRoot, selItem.KeyPath, "DNS", new string[] { dnsBox.Text });
                    //TODO: Clear the list first
                    refreshAdapters();
                }
                //TODO: Fix this
                //lstAdapters.SelectedIndex = -1;
            };

            messageBox.Show();

        }
    }
}
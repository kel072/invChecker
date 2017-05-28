// ********************************************************************************************************* *

// Copyright (c) 2017 Michal Kelnar

// SPDX-License-Identifier: BSD-3-Clause
// The BSD-3-Clause license for this file can be found in the LICENSE.txt file included with this distribution
// or at https://spdx.org/licenses/BSD-3-Clause.html#licenseText

// ********************************************************************************************************* *

using System;
using System.Windows;
using System.Xml;
using System.Text.RegularExpressions;

using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;
using System.Collections.Generic;
using System.Windows.Input;

namespace invChecker
{
    public partial class MainWindow : Window
    {
        private double defHeight;

        public MainWindow()
        {
            InitializeComponent();
            defHeight = this.Height;
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void openInvList_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "Pdf files (.pdf)|*.pdf";
            ofd.Multiselect = false;
            if (System.IO.File.Exists(this.invListPath.Text))
            {
                ofd.FileName = System.IO.Directory.GetParent(this.invListPath.Text).FullName;
            }
            if (ofd.ShowDialog().Value)
            {
                Properties.Settings.Default.invListPath = this.invListPath.Text = ofd.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void openCurrentInv_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "Pdf files (.pdf)|*.pdf";
            ofd.Multiselect = false;
            if (System.IO.File.Exists(this.invCurrentPath.Text))
            {
                ofd.FileName = System.IO.Directory.GetParent(this.invCurrentPath.Text).FullName;
            }
            if (ofd.ShowDialog().Value)
            {
                Properties.Settings.Default.invCurrentPath = this.invCurrentPath.Text = ofd.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void check_Click(object sender, RoutedEventArgs e)
        {
            string dataList = string.Empty;
            string dataCurrent = string.Empty;

            Func<string, string> load = (string path) =>
             {
                 PDDocument pdf = null;
                 string data = string.Empty;
                 try
                 {
                     pdf = PDDocument.load(path);
                     PDFTextStripper stripper = new PDFTextStripper();
                     data = stripper.getText(pdf);
                 }
                 finally
                 {
                     if (pdf != null)
                     {
                         pdf.close();
                     }
                 }
                 return data;
             };

            Mouse.OverrideCursor = Cursors.Wait;
            this.lbMissing.ItemsSource = null;
            this.lbMissing.Items.Clear();
            this.Height = this.defHeight;

            dataList = load(Properties.Settings.Default.invListPath);
            dataCurrent = load(Properties.Settings.Default.invCurrentPath);

            if (dataList != string.Empty && dataCurrent != string.Empty)
            {
                MatchCollection matchCurrent = Regex.Matches(dataCurrent, "FA\\d{8}", RegexOptions.IgnoreCase);
                MatchCollection matchList = Regex.Matches(dataList, "173\\d{5}|až", RegexOptions.IgnoreCase);

                Console.WriteLine("Current: " + matchCurrent.Count);
                Console.WriteLine("List: " + matchList.Count);

                List<string> updatedList = new List<string>();

                // expand invoices list
                for (int i = 0; i < matchList.Count; i++)
                {
                    if (matchList[i].Value == "až" && i > 0 && i < matchList.Count - 1)
                    {
                        int before = int.Parse(matchList[i - 1].Value);
                        int after = int.Parse(matchList[i + 1].Value);

                        for (int j = before + 1; j < after; j++)
                        {
                            updatedList.Add(j.ToString());
                        }
                    }
                    else
                    {
                        updatedList.Add(matchList[i].Value);
                    }
                }

                // check
                List<string> compResult = new List<string>();
                foreach (var item in matchCurrent)
                {
                    string tmp = item.ToString();
                    string upItem = tmp.Replace("f", "").Replace("a", "").Replace("F", "").Replace("A", "").Replace(".", "");
                    if (!updatedList.Contains(upItem))
                    {
                        compResult.Add(upItem);
                    }
                }

                if(compResult.Count > 0)
                {
                    compResult.Sort();
                    this.lbMissing.ItemsSource = compResult;
                    this.Height = this.Height + 100;
                    this.lbMissing.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MessageBox.Show("Can not read data from PDF file or file is empty.");
            }
            Mouse.OverrideCursor = null;
        }
    }
}

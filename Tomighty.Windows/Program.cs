﻿//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Windows.Forms;

namespace Tomighty.Windows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleUnhandledException(args.ExceptionObject as Exception);
            Application.ThreadException += (sender, args) => HandleUnhandledException(args.Exception);

            try
            {
                Application.Run(new TomightyApplication());
            }
            catch(Exception e)
            {
                Application.Run(new ErrorReportWindow(e));
            }
        }

        private static void HandleUnhandledException(Exception exception)
        {
            new ErrorReportWindow(exception).Show();
        }
    }
}

using System;
using System.Windows.Forms;
using PointGame.Forms;

namespace PointGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainMenuForm());
        }
    }
}
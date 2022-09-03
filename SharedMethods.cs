using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SharedMethods
{
    public partial class Output
    {
        public static void WriteToUI(string LinetoWrite,Window OutputWindow,string NameofOutputComponent = "edtLogOutput")
        {
            OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text = OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text + $"\n- {LinetoWrite}";
        }


    }

}

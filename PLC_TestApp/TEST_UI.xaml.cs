using PLCCommunication.Mitsubishi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PLC_TestApp
{
    /// <summary>
    /// TEST_UI.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TEST_UI : Window
    {
        private SerialPLC m_PLC;
        public TEST_UI()
        {
            InitializeComponent();

            m_PLC = new SerialPLC();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            m_PLC.Connect();
        }
    }
}

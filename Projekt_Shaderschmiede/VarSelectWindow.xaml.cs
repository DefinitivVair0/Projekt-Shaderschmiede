using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projekt_Shaderschmiede
{
    public partial class VarSelectWindow : Window
    {
        public VarSelectWindow()
        {
            InitializeComponent();
            CB_Type.Items.Add("Float");
            CB_Type.Items.Add("Range");
            CB_Type.Items.Add("Texture");
            CB_Type.Items.Add("Color");
            UpdateLB();
        }

        private void UpdateLB()
        {
            LBVar.Items.Clear();
            foreach (string s in MainWindow.VarList)
            {
                LBVar.Items.Add(s);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TB_Name.Text != "" && TB_Desc.Text != "" && CB_Type.SelectedItem != null && TB_Val.Text != "")
            {
                //Editor Variablen
                if (CB_Type.SelectedItem.ToString() == "Texture") 
                    MainWindow.VarList.Add($"        {TB_Name.Text} (\"{TB_Desc.Text}\", 2D) = \"{TB_Val.Text}\" {{}}");
                else if (CB_Type.SelectedItem.ToString() == "Range") 
                    MainWindow.VarList.Add($"        {TB_Name.Text} (\"{TB_Desc.Text}\", Range({TB_LR.Text}, {TB_UR.Text})) = {TB_Val.Text}");
                else 
                    MainWindow.VarList.Add($"        {TB_Name.Text} (\"{TB_Desc.Text}\", {CB_Type.SelectedItem}) = {TB_Val.Text}");


                //CG Variablen
                if (CB_Type.SelectedItem.ToString() == "Texture")
                    MainWindow.CGVarList.Add($"            sampler2D {TB_Name.Text};");
                else if (CB_Type.SelectedItem.ToString() == "Color")
                    MainWindow.CGVarList.Add($"            float4 {TB_Name.Text};");
                else
                    MainWindow.CGVarList.Add($"            float {TB_Name.Text};");

                this.Close();
            }
            else
            {
                if (TB_Name.Text == "") TB_Name.BorderBrush = Brushes.Red;
                else TB_Name.BorderBrush = MainWindow.GetDefaultBrush();

                if (TB_Desc.Text == "") TB_Desc.BorderBrush = Brushes.Red;
                else TB_Desc.BorderBrush = MainWindow.GetDefaultBrush();

                if (CB_Type.SelectedItem == null) CB_Type.BorderBrush = Brushes.Red;
                else CB_Type.BorderBrush = MainWindow.GetDefaultBrush();

                if (TB_Val.Text == "") TB_Val.BorderBrush = Brushes.Red;
                else TB_Val.BorderBrush = MainWindow.GetDefaultBrush();
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CB_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sel = CB_Type.SelectedItem.ToString();
            if (sel == "Range")
            {
                LRLabel.IsEnabled = true;
                URLabel.IsEnabled = true;
                TB_LR.IsEnabled = true;
                TB_UR.IsEnabled = true;
            }
            else
            {
                LRLabel.IsEnabled = false;
                URLabel.IsEnabled = false;
                TB_LR.IsEnabled = false;
                TB_UR.IsEnabled = false;
            }

            if (sel == "Float") { TB_Val.Text = "1.0f"; }
            else if (sel == "Range") { TB_Val.Text = "0"; }
            else if (sel == "Texture") { TB_Val.Text = "\"white\""; }
            else if (sel == "Color") { TB_Val.Text = "(1,1,1,1)"; }
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LBVar.SelectedItem.ToString() != "        _MainTex (\"Texture\", 2D) = \"white\" {}")
            {
                int index = LBVar.SelectedIndex;
                MainWindow.VarList.RemoveAt(index);
                MainWindow.CGVarList.RemoveAt(index);
                UpdateLB();
            }
            else
            {
                MessageBox.Show("_MainTex cannot be removed since it is essential to the shader.", "Error deleting variable", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
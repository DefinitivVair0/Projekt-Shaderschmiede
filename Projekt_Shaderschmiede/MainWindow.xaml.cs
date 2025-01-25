using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Projekt_Shaderschmiede
{
    public partial class MainWindow : Window
    {
    //public
        public MainWindow() //execution at start
        {
            InitializeComponent();
            Default = TB_Name.BorderBrush;
            TB_Path.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TB_LOD.Text = "100";
            VarList.Add("        _MainTex (\"Texture\", 2D) = \"white\" {}");
            CGVarList.Add("            sampler2D _MainTex;");
            CGVarList.Add("            float4 _MainTex_ST;");
        }

        //public variables
        public static List<string> VarList = new List<string>();
        public static List<string> CGVarList = new List<string>();

        public static Brush GetDefaultBrush() { return Default; }

    //protected
        protected static Brush Default; //standart BorderBrush -> border color for components

    //private
        private static string execpath; //executable path

        private struct FileContent
        {
            public string Kategorie;
            public string Name;

            public List<string> Variablen;

            public string TagsContent;
            public string LOD;

            public bool TranspRenderAddit;

            public List<string> CGVariablen;
            public List<string> VertCode;
            public List<string> FragCode;
        } //file data structure

        //variables for demo code
        private readonly string[] DemoCodeFrag = 
        {
            "                col = col + _TintColor;",
            "                col = col * _Brightness;"
        };
        private bool DemoCodeFragAct = false;

        private readonly string[] DemoCodeVert =
        {
            "                v.vertex.x += _VertX;",
            "                v.vertex.y += _VertY;",
        };
        private bool DemoCodeVertAct = false;


        //create file
        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            FileContent f = new FileContent
            {
                VertCode = new List<string>
                {
                    "\r\n                //Vertex Code"
                },

                FragCode = new List<string>
                {
                    "                //Fragment Code"
                }
            };
            if (DemoCodeVertAct) { f.VertCode.AddRange(DemoCodeVert); }
            if (DemoCodeFragAct && ModeSelect_Opaque.IsChecked == true) { f.FragCode.AddRange(DemoCodeFrag); }
            else if (DemoCodeFragAct && ModeSelect_Transparent.IsChecked == true) { f.FragCode.AddRange(DemoCodeFrag); f.FragCode.Add("                col.a = 1-_Transp;"); }


            //assign name
            if (TB_Name.Text == "")
            {
                TB_Name.BorderBrush = Brushes.Red;
                return;
            }
            f.Name = TB_Name.Text;

            //assign category
            if (TB_Kategorie.Text == "") f.Kategorie = "Unlit";
            else f.Kategorie = TB_Kategorie.Text;

            //assign variables [editor]
            f.Variablen = VarList;

            //assign render mode
            if (ModeSelect_Opaque.IsChecked == true)
            {
                f.TagsContent = "\"RenderType\"=\"Opaque\"";
                f.TranspRenderAddit = false;
            }
            else if (ModeSelect_Transparent.IsChecked == true)
            {
                f.TagsContent = "\"Queue\"=\"Transparent\" \"RenderType\"=\"Transparent\"";
                f.TranspRenderAddit = true;
            }

            //assign Level Of Detail (LOD)
            if (Int32.TryParse(TB_LOD.Text, out int LOD) && LOD > 0) { f.LOD = LOD.ToString(); }
            else if (LOD <= 0)
            {
                MessageBox.Show("Value must be greater than 0.", "Error at assigning value", MessageBoxButton.OK, MessageBoxImage.Error);
                TB_LOD.BorderBrush = Brushes.Red;
                return;
            }
            else
            {
                f.LOD = "100";
            }

            //assign variables [CG]
            f.CGVariablen = CGVarList;

            execpath = TB_Path.Text;

            PrintToFile(f);
        }

        private static Task PrintToFile(FileContent f)
        {
            string path = execpath;

            List<string> print = CollectFileData(f);

            try
            {
                using (StreamWriter sw = File.CreateText(path + $"\\{f.Name}.shader"))
                {
                    foreach (string s in print)
                    {
                        sw.WriteLine(s);
                    }
                }

                
                if (MessageBox.Show("Save succesful.\nOpen folder?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    Process.Start(path);
                }
                
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString(), "Error writing to file", MessageBoxButton.OK, MessageBoxImage.Error); }

            return Task.CompletedTask;
        }

        private static List<string> CollectFileData(FileContent f)
        {
            List<string> output = new List<string> { };

            string[] description = { "//Code was made using Schaderschmiede V1!", "//------------------------------------------------\r\n" };
            output.AddRange(description);

            output.Add($"Shader \"{f.Kategorie + "/" + f.Name}\"\r\n{{");
            output.Add("    Properties\r\n    {");

            output.AddRange(f.Variablen);

            output.Add("    }\r\n\r\n    Subshader\r\n    {");
            output.Add($"        Tags {{{f.TagsContent}}}");
            output.Add($"        LOD {f.LOD}\r\n");

            if (f.TranspRenderAddit) { output.Add("        ZWrite On\r\n        Blend SrcAlpha OneMinusSrcAlpha\r\n"); }

            string[] cg1 = new string[16]
            {
                "        Pass",
                "        {",
                "            CGPROGRAM",
                "            #pragma vertex vert",
                "            #pragma fragment frag\r\n",
                "            #include \"UnityCG.cginc\"\r\n",
                "            struct appdata",
                "            {",
                "                float4 vertex : POSITION;",
                "                float2 uv : TEXCOORD0;",
                "            };\r\n",
                "            struct v2f",
                "            {",
                "                float4 vertex : SV_POSITION;",
                "                float2 uv : TEXCOORD0;",
                "            };\r\n"
            };
            output.AddRange(cg1);

            output.AddRange(f.CGVariablen);

            string[] cg2 = new string[3]
            {
                "            v2f vert(appdata v)",
                "            {",
                "                v2f o;",
            };
            output.AddRange(cg2);

            output.AddRange(f.VertCode);

            string[] cg3 = new string[7]
            {
                "                o.vertex=UnityObjectToClipPos(v.vertex);",
                "                o.uv=TRANSFORM_TEX(v.uv, _MainTex);",
                "                return o;",
                "            }\r\n",
                "            fixed4 frag(v2f i) : SV_Target",
                "            {",
                "                fixed4 col = tex2D(_MainTex, i.uv);\r\n"
            };
            output.AddRange(cg3);

            output.AddRange(f.FragCode);

            string[] cg4 = new string[6]
            {
                "                return col;",
                "            }",
                "            ENDCG",
                "        }",
                "    }",
                "}"
            };
            output.AddRange(cg4);

            return output;
        }


        //Name einstellen
        private void TB_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TB_Name.BorderBrush == Brushes.Red) { TB_Name.BorderBrush = Default; }
            else if (TB_Name.Text == "") { TB_Name.BorderBrush = Brushes.Red; }
        }


        //Variablen anpassen
        private void VarAddBtn_Click(object sender, RoutedEventArgs e)
        {
            VarSelectWindow varSelectWindow = new VarSelectWindow();
            varSelectWindow.Show();
        }


        //Level Of Detail einstellen
        private void TB_LOD_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TB_LOD.BorderBrush == Brushes.Red) { TB_LOD.BorderBrush = Default; }
            else if (TB_LOD.Text == "") { TB_LOD.BorderBrush = Brushes.Red; }
        }


        //Pfad einstellen
        private void ChangePathBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (System.Windows.Forms.DialogResult.OK == result)
                {
                    TB_Path.Text = dialog.SelectedPath.ToString();
                }
            }
        }


        //Demo-Code einstellungen
        private void CB_DemoCodeFrag_Checked(object sender, RoutedEventArgs e)
        {
            if (!VarList.Contains("        _TintColor (\"Tint Color\", Color) = (1,1,1,1)"))
            {
                VarList.Add("        _TintColor (\"Tint Color\", Color) = (1,1,1,1)");
                VarList.Add("        _Brightness (\"Brightness\", Float) = 1");
                if (ModeSelect_Transparent.IsChecked == true) VarList.Add("        _Transp (\"Transparency\", Range(0,1)) = 0");
                CGVarList.Add("            float4 _TintColor;");
                CGVarList.Add("            float _Brightness;");
                if (ModeSelect_Transparent.IsChecked == true) CGVarList.Add("            float _Transp;");
            }
            DemoCodeFragAct = true;
        }

        private void CB_DemoCodeVert_Checked(object sender, RoutedEventArgs e)
        {
            if (!VarList.Contains("        _VertX (\"X Offset\", Float) = 0"))
            {
                VarList.Add("        _VertX (\"X Offset\", Float) = 0");
                VarList.Add("        _VertY (\"Y Offset\", Float) = 0");
                VarList.Add("        _VertZ (\"Z Offset\", Float) = 0");
                CGVarList.Add("            float _VertX;");
                CGVarList.Add("            float _VertY;");
                CGVarList.Add("            float _VertZ;");
            }
            DemoCodeVertAct = true;
        }

        private void CB_DemoCodeFrag_Unchecked(object sender, RoutedEventArgs e)
        {
            VarList.Remove("        _TintColor (\"Tint Color\", Color) = (1,1,1,1)");
            VarList.Remove("        _Brightness (\"Brightness\", Float) = 1");
            VarList.Remove("        _Transp (\"Transparency\", Range(0,1)) = 0");
            CGVarList.Remove("            float4 _TintColor;");
            CGVarList.Remove("            float _Brightness;");
            CGVarList.Remove("            float _Transp;");
            DemoCodeFragAct = false;
        }

        private void CB_DemoCodeVert_Unchecked(object sender, RoutedEventArgs e)
        {
            VarList.Remove("        _VertX (\"X Offset\", Float) = 0");
            VarList.Remove("        _VertY (\"Y Offset\", Float) = 0");
            VarList.Remove("        _VertZ (\"Z Offset\", Float) = 0");
            CGVarList.Remove("            float _VertX;");
            CGVarList.Remove("            float _VertY;");
            CGVarList.Remove("            float _VertZ;");
            DemoCodeVertAct = false;
        }
    }
}
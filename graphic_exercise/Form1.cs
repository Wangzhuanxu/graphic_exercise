using graphic_exercise.RenderData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace graphic_exercise
{
    public partial class Form1 : Form
    {
        Label l;
        Bitmap bp;
        Graphics g;
        public Form1()
        {
            InitializeComponent();
            this.Width = 800;
            this.Height = 600;
            Vector v = new Vector(1, 0, 0);
            Matrix4x4 m = Matrix4x4.rotate(0, 1, 0,-(float)Math.PI/2);
            v = v * m;
            l = new Label();
            l.SetBounds(100, 100, 300, 300);
            l.Text = "sdfdf";
            this.Controls.Add(l);

            print(l, m);
            this.Text = v.x + " " + v.y + "  " + v.z+" "+v.w;
            bp= new Bitmap(this.Width, this.Height);
            for (int i = 0; i < bp.Width; i++)
            {
                
                    bp.SetPixel(i, i/2, Color.Green);                
            };
        }

        private void print(Label l,Matrix4x4 m)
        {
            StringBuilder sb = new StringBuilder();
            for(int i= 0;i <m.matrix.GetLength(0);i++)
            {
                for(int j=0;j<m.matrix.GetLength(1);j++)
                {
                    sb.Append(m.matrix[i, j] + " ");
                }
                sb.Append("\n");
            }
            l.Text = sb.ToString()+"    "+Math.Cos((float)Math.PI / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //if (g == null)
            //{
            //    g = this.CreateGraphics();
            //}
            //g.Clear(System.Drawing.Color.Black);
            //g.DrawImage(bp, 0, 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
    }
}

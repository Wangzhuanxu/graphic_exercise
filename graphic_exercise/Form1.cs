using graphic_exercise.RenderData;
using graphic_exercise.Test;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace graphic_exercise
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 测试数据
        /// </summary>
        Label l;
        Thread t;
        private Bitmap frameBuff;//用一张bitmap来做帧缓冲（颜色缓冲）
        private Graphics frameG;
        private Triangle triangles;//三角形类，也就是mesh类
        private Camera camera;//摄像机类
        int width=800;
        int height=600;
        public Form1()
        {


            InitializeComponent();


            //设置窗口大小
            this.Width = width;
            this.Height = height;
            //窗口标题
            this.Text = "width=" + this.Width + "  height=" + this.Height;
            //初始化帧缓冲
            frameBuff = new Bitmap(this.Width, this.Height);
            frameG = Graphics.FromImage(frameBuff);
            //初始化顶点
            triangles = new Triangle(TriangleTestData.pointList, TriangleTestData.indexs, TriangleTestData.uvs, TriangleTestData.norlmas);
            //初始化摄像机 Vector look,Vector up,Vector pos,float fov,float aspect,float near,float far
            camera = new Camera(new Vector(0, 0, 30, 1), new Vector(0, 1, 0, 0), new Vector(0, 0, -1, 1), (float)System.Math.PI / 3, this.Width / (float)this.Height, 5f, 40f);
            t = new Thread(new ThreadStart(Tick));
            t.Start();
            this.Closed += close;

            // 测试数据
            //Vector v = new Vector(1, 0, 0);
            //Matrix4x4 m = Matrix4x4.translate(5, 0, 25) * Matrix4x4.rotateY((float)(Math.PI / 18 * 15)) * Matrix4x4.scale(2, 2, 2);
            //Matrix4x4 vv = Matrix4x4.view(camera.look, camera.up, camera.pos);
            //Matrix4x4 p = Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
            //v = m * v;
            //l = new Label();
            //l.SetBounds(100, 100, 300, 300);
            //l.Text = "sdfdf";
            //this.Controls.Add(l);
            //print(l, m, vv, p);


        }
        /// <summary>
        /// 测试方法，主要测试矩阵是否正确
        /// </summary>
        /// <param name="l"></param>
        /// <param name="m"></param>
        /// <param name="mv"></param>
        /// <param name="p"></param>
        private void print(Label l, Matrix4x4 m, Matrix4x4 mv, Matrix4x4 p)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < p.matrix.GetLength(0); i++)
            {
                for (int j = 0; j < p.matrix.GetLength(1); j++)
                {
                    sb.Append(p.matrix[i, j] + " ");
                }
                sb.Append("\n");
            }
            Vector v = new Vector(0, 2,4, 1);
            v =p*mv* m * v;
            sb.Append(v.x + "  " + v.y + " " + v.z + " " + v.w );
            l.Text = sb.ToString();
        }


        public void clearBuff()
        {
            frameG.Clear(Color.Black);//清除颜色缓存
        }
        /// <summary>
        /// 绘制主方法
        /// </summary>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <param name="p"></param>
        private void draw(Matrix4x4 m,Matrix4x4 v,Matrix4x4 p)
        {
            for(int i=0;i<triangles.indexsList.Count;i++)//遍历顶点索引数组
            {
                drawTriangle(triangles.vertexList[triangles.indexsList[i].one].clone(), 
                    triangles.vertexList[triangles.indexsList[i].two].clone(), 
                    triangles.vertexList[triangles.indexsList[i].three].clone()
                    , m, v, p);
                //this.Text = triangles.vertexList[triangles.indexsList[i].one].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].one].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].one].pos.z + " "
                //           + triangles.vertexList[triangles.indexsList[i].two].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].two].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].two].pos.z + " " +
                //           triangles.vertexList[triangles.indexsList[i].three].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].three].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].three].pos.z;
            }
        }

        

        private  void drawTriangle(Vertex v1,Vertex v2, Vertex v3,Matrix4x4 m,Matrix4x4 v,Matrix4x4 p)
        {
            ///本地到摄像机空间
            objectToCamera(m, v, v1);
            objectToCamera(m, v, v2);
            objectToCamera(m, v, v3);
            
            ///摄像机到裁剪空间
            cameraToProject(p, v1);
            cameraToProject(p, v2);
            cameraToProject(p, v3);


            //TODO 裁剪算法


            ///透视除法
            projectToScreen(v1);
            projectToScreen(v2);
            projectToScreen(v3);

            //this.Text = v1.pos.x + "   " + v1.pos.y + "    "  + " "
            //           + v2.pos.x + "   " + v2.pos.y + "    " + " " +
            //           v3.pos.x + "   " + v3.pos.y + "    " ;

            //画线框
            BresenhamDrawLine(v1, v2);
            BresenhamDrawLine(v2, v3);
            BresenhamDrawLine(v3, v1);

            this.Text = v1.pos.x + "   " + v1.pos.y + "    " + " "
                       + v2.pos.x + "   " + v2.pos.y + "    " + " " +
                       v3.pos.x + "   " + v3.pos.y + "    ";

        }
        /// <summary>
        /// 本地到世界坐标系
        /// </summary>
        /// <param name="m"></param>
        /// <param name="v"></param>
        private void cameraToProject(Matrix4x4 p, Vertex v)
        {
            v.pos = p * v.pos;
        }
        /// <summary>
        /// 世界到摄像机坐标系
        /// </summary>
        /// <param name="m"></param>
        /// <param name="v"></param>
        private void objectToCamera(Matrix4x4 m,Matrix4x4 v, Vertex vv)
        {
            vv.pos = v * m * vv.pos;
        }
        //TODO 裁剪方法
        //public bool clip(Vertex v)
        //{
        //    if()
        //}

       /// <summary>
       /// 透视除法
       /// </summary>
       private void projectToScreen(Vertex v)
       {
            if (v.pos.w != 0)
            {
                //先进行透视除法，转到NDC
                v.pos.x *= 1 / v.pos.w;
                v.pos.y *= 1 / v.pos.w;
                v.pos.z *= 1 / v.pos.w;
                v.pos.w = 1;
                //NDC到屏幕坐标
                v.pos.x = (v.pos.x + 1) * 0.5f * width;
                v.pos.y = (1 -v.pos.y) * 0.5f * height;
            }
        }

        /// <summary>
        /// 画线框
        /// 推导过程：https://blog.csdn.net/u012319493/article/details/53289132
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private void BresenhamDrawLine(Vertex p1, Vertex p2)
        {
            int x = (int)(System.Math.Round(p1.pos.x, MidpointRounding.AwayFromZero));
            int y = (int)(System.Math.Round(p1.pos.y, MidpointRounding.AwayFromZero));
            int dx = (int)(System.Math.Round(p2.pos.x - p1.pos.x, MidpointRounding.AwayFromZero));
            int dy = (int)(System.Math.Round(p2.pos.y - p1.pos.y, MidpointRounding.AwayFromZero));
            int stepx = 1;
            int stepy = 1;

            if (dx >= 0)
            {
                stepx = 1;
            }
            else
            {
                stepx = -1;
                dx = System.Math.Abs(dx);
            }

            if (dy >= 0)
            {
                stepy = 1;
            }
            else
            {
                stepy = -1;
                dy = System.Math.Abs(dy);
            }
            int dx2 = 2 * dx;
            int dy2 = 2 * dy;
            if (dx > dy)
            {
                int error = dy2 - dx;
                for (int i = 0; i <= dx; i++)
                {
                    frameBuff.SetPixel(x, y, System.Drawing.Color.White);
                    if (error >= 0)
                    {
                        error -= dx2;
                        y += stepy;
                    }
                    error += dy2;
                    x += stepx;

                }
            }
            else
            {
                int error = dx2 - dy;
                for (int i = 0; i <= dy; i++)
                {
                    frameBuff.SetPixel(x, y, System.Drawing.Color.White);
                    if (error >= 0)
                    {
                        error -= dy2;
                        x += stepx;
                    }
                    error += dx2;
                    y += stepy;

                }
            }

        }

        Graphics g = null;
        //旋转角度
        private float rotX = 0;
        private float rotY = 0;
        private float rotZ = 0;
        private void Tick()
        {
           //int x = 0;
            //while (true)
            //{
            //    //清除颜色缓存
            //    clearBuff();
            //    //求mvp矩阵
            //    Matrix4x4 m = Matrix4x4.translate(0, 0, 10) * Matrix4x4.rotateY(rotY) * Matrix4x4.rotateX(rotX) * Matrix4x4.rotateZ(rotZ);
            //    Matrix4x4 v = Matrix4x4.view(camera.look, camera.up, camera.pos);
            //    Matrix4x4 p = Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
            //    //绘制
            //    draw(m, v, p);

            //    if (g == null)
            //    {
            //        g = this.CreateGraphics();
            //    }
            //    g.Clear(Color.Black);
            //    g.DrawImage(frameBuff, 0, 0);
            //    //this.Text = "x=" + x;
            //   // x++;
            //    try
            //    {
            //        Thread.Sleep(1000);//帧数
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e.Data);
            //    }
            //}

        }

        /// <summary>
        /// 刷帧方法
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //清除颜色缓存
            clearBuff();
            //求mvp矩阵
            Matrix4x4 m = Matrix4x4.translate(0, 0, 10) * Matrix4x4.rotateY(rotY) * Matrix4x4.rotateX(rotX) * Matrix4x4.rotateZ(rotZ);
            Matrix4x4 v = Matrix4x4.view(camera.look, camera.up, camera.pos);
            Matrix4x4 p = Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
            //绘制
            draw(m, v, p);

            if (g == null)
            {
                g = this.CreateGraphics();
            }
            g.Clear(Color.Black);
            g.DrawImage(frameBuff, 0, 0);
            //this.Text = "x=" + x;
            // x++;
            try
            {
                Thread.Sleep(1000);//帧数
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Data);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public  void close(object sender, EventArgs e)
        {
            t.Abort();
            //this.Text = "sdfsdf";
        }

    }
}

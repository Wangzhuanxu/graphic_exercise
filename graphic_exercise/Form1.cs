#define DEBUG

using graphic_exercise.RenderData;
using graphic_exercise.Test;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using graphic_exercise.Util;
using System.Runtime.InteropServices;
using System.IO;

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
        private Graphics frameG; //背景色画板
        private Triangle triangles;//三角形类，也就是mesh类
        private Camera camera;//摄像机类
        private float[,] zbuffer;//深度缓冲
        Light light;//灯光位置
        LightMode lightMode;//是否启用光照
        RenderMode renderMode;//渲染模式,线框还是实体
        FaceCullMode faceCullMode;//是否启用背面剪裁
        Bitmap texture;//图片
        const int imgWidth = 256;//图片宽高
        const int imgHeight = 256;
        System.Drawing.Color[,] textureArray;//纹理颜色值
        
        //屏幕宽高
        int width = 800;
        int height = 600;

        /// <summary>
        /// 显示console窗口
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();


        public Form1()
        {


            InitializeComponent();
            //显示Console窗口
            AllocConsole();
            //设置窗口大小
            this.Width = width;
            this.Height = height;
            //窗口标题
            //this.Text = "width=" + this.Width + "  height=" + this.Height;
            //初始化帧缓冲
            frameBuff = new Bitmap(this.Width, this.Height);
            frameG = Graphics.FromImage(frameBuff);
            //初始化顶点
            triangles = new Triangle(CubeTestDatacs.pointList, CubeTestDatacs.indexs, CubeTestDatacs.uvs, CubeTestDatacs.norlmas, CubeTestDatacs.vertColors,CubeTestDatacs.mat);
            //triangles = new Triangle(TriangleTestData.pointList, TriangleTestData.indexs, TriangleTestData.uvs, TriangleTestData.norlmas, TriangleTestData.vertColors,TriangleTestData.mat);
            //初始化摄像机 Vector look,Vector up,Vector pos,float fov,float aspect,float near,float far
            camera = new Camera(new Vector(0, 0, 10, 0), new Vector(0, 1, 0, 0), new Vector(0,2,5, 0), (float)System.Math.PI / 3f,this.Width / (float)this.Height, 1f, 1000f);
            //初始化深度缓冲
            zbuffer = new float[width, height];
            //初始化灯光
            light = new Light(new Vector(-1, 1, -1), graphic_exercise.RenderData.Color.White);
            //是否启用光照
            lightMode = LightMode.ON;
            //设置渲染模式
            renderMode = RenderMode.Entity;
            //开启背面剪裁
            faceCullMode = FaceCullMode.ON;
            //初始化纹理
            System.Drawing.Image img = System.Drawing.Image.FromFile("../../Texture/fireFox.png");
            texture = new Bitmap(img, imgWidth, imgHeight);
            initTexture();

#if DEBUG
            Console.WriteLine(" this.Width / (float)this.Height=" + this.Width / (float)this.Height);
#endif



            //启用绘制线程
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
        private void print( Matrix4x4 m, Matrix4x4 mv, Matrix4x4 p)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m.matrix.GetLength(0); i++)
            {
                for (int j = 0; j < m.matrix.GetLength(1); j++)
                {
                    sb.Append(m.matrix[i, j] + " ");
                }
                sb.Append("\n");
            }
            Vector v = new Vector(-1, 1, 8, 1);
            v = mv * m * v;
            sb.Append(v.x + "  " + v.y + " " + v.z + " " + v.w + " " + v.z / v.w + "  " + 1 / v.w);
            Console.WriteLine(sb.ToString());
        }


        public void clearBuff()
        {
            frameG.Clear(graphic_exercise.RenderData.Color.Black.TransFormToSystemColor());//清除颜色缓存
            clearDeath();
        }
        /// <summary>
        /// 清除深度缓冲
        /// </summary>
        public void clearDeath()
        {
            for (int i = 0; i < zbuffer.GetLength(0); i++)
            {
                for (int j = 0; j < zbuffer.GetLength(1); j++)
                {
                    zbuffer[i, j] = 1;
                }
            }
        }
        /// <summary>
        /// 绘制主方法
        /// </summary>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <param name="p"></param>
        Vertex p1=new Vertex();
        Vertex p2=new Vertex();
        Vertex p3=new Vertex();
        private void draw(Matrix4x4 m, Matrix4x4 v, Matrix4x4 p)
        {
            
            for (int i = 0; i < triangles.vertexList.Count; i += 3)//遍历顶点索引数组
            {
                Vertex.Clone(triangles.vertexList[i], p1);
                Vertex.Clone(triangles.vertexList[i + 1], p2);
                Vertex.Clone(triangles.vertexList[i + 2], p3);
                drawTriangle(p1,
                   p2,
                    p3
                    , m, v, p);
                //drawTriangle(triangles.vertexList[triangles.indexsList[i].one].clone(), 
                //    triangles.vertexList[triangles.indexsList[i].two].clone(), 
                //    triangles.vertexList[triangles.indexsList[i].three].clone()
                //    , m, v, p);
                //drawTriangle(triangles.vertexList[i].clone(),
                //    triangles.vertexList[i + 1].clone(),
                //    triangles.vertexList[i + 2].clone()
                //    , m, v, p);
                //this.Text = triangles.vertexList[triangles.indexsList[i].one].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].one].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].one].pos.z + " "
                //           + triangles.vertexList[triangles.indexsList[i].two].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].two].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].two].pos.z + " " +
                //           triangles.vertexList[triangles.indexsList[i].three].pos.x + "   " + triangles.vertexList[triangles.indexsList[i].three].pos.y + "    " + triangles.vertexList[triangles.indexsList[i].three].pos.z;
            }
        }


        /// <summary>
        /// 绘制三角形
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <param name="p"></param>
        private void drawTriangle(Vertex v1, Vertex v2, Vertex v3, Matrix4x4 m, Matrix4x4 v, Matrix4x4 p)
        {
            
            //本地到世界坐标空间
            objectToWorld(m, v1);
            objectToWorld(m, v2);
            objectToWorld(m, v3);
#if DEBUG
            //Console.WriteLine(v1.pos.x + "   " + v1.pos.y + "    " + " "
            //           + v2.pos.x + "   " + v2.pos.y + "    " + " " +
            //           v3.pos.x + "   " + v3.pos.y + "    ");
#endif
            //顶点光照,在世界坐标系下进行
            if(lightMode==LightMode.ON)
            {
                vertexLighting(v1, m);
                vertexLighting(v2, m);
                vertexLighting(v3, m);
            }
           
            ///世界到摄像机空间
            worldToCamera(v, v1);
            worldToCamera(v, v2);
            worldToCamera(v, v3);

            //在相机空间进行背面消隐，原因是相机空间中，相机位置为0，0，0
            if (faceCullMode==FaceCullMode.ON&&backFaceCulling(v1, v2, v3) == false)
            {
                return;
            }

            ///摄像机到裁剪空间
            cameraToProject(p, v1);
            cameraToProject(p, v2);
            cameraToProject(p, v3);


            //TODO 裁剪算法


            ///透视除法
            projectToScreen(v1);
            projectToScreen(v2);
            projectToScreen(v3);

#if DEBUG
            //Console.WriteLine(v1.pos.x + "   " + v1.pos.y + "    " + " "
            //           + v2.pos.x + "   " + v2.pos.y + "    " + " " +
            //           v3.pos.x + "   " + v3.pos.y + "    "
            //          + v1.color.r + "  " + v1.color.g + " " + v1.color.b);
#endif
            if(renderMode==RenderMode.Wireframe)
            {
                //画线框
                BresenhamDrawLine(v1, v2);
                BresenhamDrawLine(v2, v3);
                BresenhamDrawLine(v3, v1);
            }
            else if(renderMode==RenderMode.Entity)
            {
                //光栅化
                rasterizationTriangle(v1, v2, v3);
            }
#if DEBUG
            //Console.WriteLine(v1.pos.x + "   " + v1.pos.y + "    " + " "
            //           + v2.pos.x + "   " + v2.pos.y + "    " + " " +
            //           v3.pos.x + "   " + v3.pos.y + "    ");
#endif

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
        private void worldToCamera( Matrix4x4 v, Vertex vv)
        {
            vv.pos = v * vv.pos;
        }
        
        private void objectToWorld(Matrix4x4 m,Vertex vv)
        {
            vv.pos = m * vv.pos;
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
                //插值矫正系数
                v.onePerZ =1/ v.pos.w;
                //先进行透视除法，转到NDC
                v.pos.x *= 1 / v.pos.w;
                v.pos.y *= 1 / v.pos.w;
                v.pos.z *= 1 / v.pos.w;
                v.pos.w = 1;
                //保存顶点的深度值
                v.depth = (v.pos.z + 1) / 2;
                //NDC到屏幕坐标
                v.pos.x = (v.pos.x + 1) * 0.5f * width;
                v.pos.y = (1 - v.pos.y) * 0.5f * height;
                //插值矫正
                v.uv[0] *= v.onePerZ;
                v.uv[1] *= v.onePerZ;
                v.color *= v.onePerZ;
                v.lightColor *= v.onePerZ;
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
                for (int i = 0; i < dx; i++)
                {
                    if(x>=0&&y>=0&&x<=width&&y<=height)
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
                for (int i = 0; i <dy; i++)
                {
                    if (x >= 0 && y >= 0 && x <= width && y <= height)
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
        /// <summary>
        /// 光栅化三角形
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        private void rasterizationTriangle(Vertex v1, Vertex v2, Vertex v3)
        {
            if (v1.pos.y == v2.pos.y)
            {
                if (v1.pos.y < v3.pos.y)
                {
                    //平顶
                    drawTriangleTop(v1, v2, v3);

                }
                else
                {

                    //平底
                    drawTriangleBottom(v3, v1, v2);
                }

            }
            else if (v1.pos.y == v3.pos.y)
            {
                if (v1.pos.y < v2.pos.y)
                {
                    //平顶
                    drawTriangleTop(v3, v1, v2);
                }
                else
                {
                    //平底
                    drawTriangleBottom(v2, v3, v1);
                }

            }
            else if (v3.pos.y == v2.pos.y)
            {
                if (v3.pos.y < v1.pos.y)
                {
                    //平顶
                    drawTriangleTop(v2, v3, v1);
                }
                else
                {

                    //平底
                    drawTriangleBottom(v1, v2, v3);
                }

            }
            else
            {
                //分割三角形,先求出三角形的三个顶点y值大小
                Vertex top = new Vertex();
                Vertex bottom = new Vertex();
                Vertex middle = new Vertex();
                //                                              y由大到小，y小的在上
                if (v1.pos.y > v2.pos.y && v2.pos.y > v3.pos.y)//v1 v2 v3
                {
                    top = v3;
                    middle = v2;
                    bottom = v1;
                }
                else if (v3.pos.y > v2.pos.y && v2.pos.y > v1.pos.y)//3 2 1
                {
                    top = v1;
                    middle = v2;
                    bottom = v3;
                }
                else if (v2.pos.y > v1.pos.y && v1.pos.y > v3.pos.y)//2 1 3
                {
                    top = v3;
                    middle = v1;
                    bottom = v2;
                }
                else if (v3.pos.y > v1.pos.y && v1.pos.y > v2.pos.y)// 3 1 2
                {
                    top = v2;
                    middle = v1;
                    bottom = v3;
                }
                else if (v1.pos.y > v3.pos.y && v3.pos.y > v2.pos.y)// 1 3 2 
                {
                    top = v2;
                    middle = v3;
                    bottom = v1;
                }
                else if (v2.pos.y > v3.pos.y && v3.pos.y > v1.pos.y)//2 3 1
                {
                    top = v1;
                    middle = v3;
                    bottom = v2;
                }

                //插值求中间点x
                float middlex = (middle.pos.y - top.pos.y) * (bottom.pos.x - top.pos.x) / (bottom.pos.y - top.pos.y) + top.pos.x;
                //求插值因子
                float t = (middle.pos.y - top.pos.y) / (bottom.pos.y - top.pos.y);
                //插值生成左右顶点
                Vertex new1 = new Vertex();
                new1.pos.x = middlex;
                new1.pos.y = middle.pos.y;
                //插值
                Util.Util.lerp(new1, top, bottom, t);


                ////平底
                if (middlex > middle.pos.x)
                {
                    drawTriangleBottom(top, new1, middle);
                    drawTriangleTop(middle, new1, bottom);
                }
                else
                {
                    drawTriangleBottom(top, middle, new1);
                    drawTriangleTop(new1, middle, bottom);
                }
                ////平顶

            }
        }
        /// <summary>
        /// 平底三角形
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        private void drawTriangleBottom(Vertex v1, Vertex v2, Vertex v3)
        {
            int x1 = (int)(System.Math.Round(v1.pos.x, MidpointRounding.AwayFromZero));
            int x2 = (int)(System.Math.Round(v2.pos.x, MidpointRounding.AwayFromZero));
            int x3 = (int)(System.Math.Round(v3.pos.x, MidpointRounding.AwayFromZero));
            int y1 = (int)(System.Math.Round(v1.pos.y, MidpointRounding.AwayFromZero));
            int y2 = (int)(System.Math.Round(v2.pos.y, MidpointRounding.AwayFromZero));
            int y3 = (int)(System.Math.Round(v3.pos.y, MidpointRounding.AwayFromZero));
            for (int y = y1; y < y3; y += 1)
            {
                //防止浮点数精度不准，四舍五入，使y的值每次增加1
                // int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                int yIndex = y;// (int)Math.Ceiling(y);
                //裁剪掉屏幕外的线
                if (yIndex >= 0 && yIndex < height)
                {

                    int xl = (y - y1) * (x3 - x1) / (y3 - y1) + x1;
                    int xr = (y - y1) * (x2 - x1) / (y2 - y1) + x1;
                    //插值因子
                    float t = (y - y1) /(float) (y3- y1);

                    //左顶点
                    Vertex left = new Vertex();
                    left.pos.x = xl;
                    left.pos.y = y;
                    Util.Util.lerp(left, v1, v3, t);
                    //
                    Vertex right = new Vertex();
                    right.pos.x = xr;
                    right.pos.y = y;
                    Util.Util.lerp(right, v1, v2, t);

#if DEBUG
                    //Console.WriteLine(left.pos.x + "   " + left.pos.y + "    " + " "
                    //  + right.pos.x + "   " + right.pos.y + "    " + " " +
                    //  xl + "   " + xr + "    ");
#endif
                    //扫描线填充
                    if (left.pos.x < right.pos.x)
                    {
                        scanLine(left, right, yIndex);
                    }
                    else
                    {
                        scanLine(right, left, yIndex);
                    }

                }

            }
        }

        /// <summary>
        /// 平顶三角形
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        private void drawTriangleTop(Vertex v1, Vertex v2, Vertex v3)
        {
            int x1 = (int)(System.Math.Round(v1.pos.x, MidpointRounding.AwayFromZero));
            int x2 = (int)(System.Math.Round(v2.pos.x, MidpointRounding.AwayFromZero));
            int x3 = (int)(System.Math.Round(v3.pos.x, MidpointRounding.AwayFromZero));
            int y1 = (int)(System.Math.Round(v1.pos.y, MidpointRounding.AwayFromZero));
            int y2 = (int)(System.Math.Round(v2.pos.y, MidpointRounding.AwayFromZero));
            int y3 = (int)(System.Math.Round(v3.pos.y, MidpointRounding.AwayFromZero));
            for (int y = y1; y < y3; y += 1)
            {
                //防止浮点数精度不准，四舍五入，使y的值每次增加1
                //int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                int yIndex = y;// (int)Math.Ceiling(y);
                //裁剪掉屏幕外的线
                if (yIndex >= 0 && yIndex <height)
                {
                    int xl = (y - y1) * (x3 - x1) / (y3 - y1) + x1;
                    int xr = (y -y2) * (x3 - x2) / (y3 - y2) + x2;
                    //插值因子
                    float t = (y - y1) /(float) (y3 - y1);

                    //左顶点
                    Vertex left = new Vertex();
                    left.pos.x = xl;
                    left.pos.y = y;
                    Util.Util.lerp(left, v1, v3, t);
                    //
                    Vertex right = new Vertex();
                    right.pos.x = xr;
                    right.pos.y = y;
                    Util.Util.lerp(right, v2, v3, t);
                    //扫描线填充
                    if (left.pos.x < right.pos.x)
                    {
                        scanLine(left, right, yIndex);
                    }
                    else
                    {
                        scanLine(right, left, yIndex);
                    }
                }
            }
        }


        /// <summary>
        /// 填充
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="yIndex"></param>
        //private void scanLine(Vertex left, Vertex right, int yIndex)
        //{
        //    //求线段长度
        //    float dx = right.pos.x - left.pos.x;
        //    //求w缓冲系数
        //    float w = 0;
        //    //插值因子
        //    float t = 0;
        //    //该点像素的深度值
        //    float death = 0;
        //    for (float x = left.pos.x; x < right.pos.x; x += 1f)
        //    {
        //        if (dx != 0)
        //        {
        //            t = (x - left.pos.x) / dx;
        //        }
        //        int xIndex = (int)Math.Ceiling(x);
        //        //int xIndex = (int)(System.Math.Round(x, MidpointRounding.AwayFromZero));
        //        if (xIndex >= 0 && xIndex <= width)
        //        {
        //            ///计算该片元的深度值
        //            death = Util.Util.lerp(left.depth, right.depth, t);
        //            if (zbuffer[xIndex, yIndex] >= death)
        //            {
        //                w =1/Util.Util.lerp(left.onePerZ, right.onePerZ, t);
        //                zbuffer[xIndex, yIndex] = death;
        //                graphic_exercise.RenderData.Color vertexColor = new RenderData.Color();
        //                if (lightMode == LightMode.ON)
        //                {
        //                    vertexColor = Util.Util.lerp(left.lightColor, right.lightColor, t);
        //                }
        //                else
        //                {
        //                    vertexColor = Util.Util.lerp(left.color, right.color, t);
        //                }
        //                vertexColor = vertexColor  * w;
        //                frameBuff.SetPixel(xIndex, yIndex, vertexColor.TransFormToSystemColor());
        //            }


        //        }

        //    }   
        //}
        private void scanLine(Vertex left, Vertex right, int yIndex)
        {
            //求线段长度
            int x = (int)(System.Math.Round(left.pos.x, MidpointRounding.AwayFromZero));
            int dx = (int)(System.Math.Round(right.pos.x - left.pos.x, MidpointRounding.AwayFromZero));
            int stepx = 1;
            if (dx >= 0)
            {
                stepx = 1;
            }
            else
            {
                stepx = -1;
                dx = System.Math.Abs(dx);
            }
            //求w缓冲系数
            float w = 0;
            //插值因子
            float t = 0;
            //该点像素的深度值
            float death = 0;
            //uv坐标
            int u = 0;
            int v = 0;
            for (int i = 0; i <= dx; i +=1)
            {
                if (dx != 0)
                {
                    t = i /(float)dx;
                }
                int xIndex = x;// (int)Math.Ceiling(x);
                //int xIndex = (int)(System.Math.Round(x, MidpointRounding.AwayFromZero));
                if (xIndex >= 0 && xIndex <width)
                {
                    ///计算该片元的深度值
                    death = Util.Util.lerp(left.depth, right.depth, t);
                    if (zbuffer[xIndex, yIndex] >= death)
                    {
                        //w缓冲
                        w = 1 / Util.Util.lerp(left.onePerZ, right.onePerZ, t);
                        //深度值
                        zbuffer[xIndex, yIndex] = death;
                        //uv坐标
                        u =(int)(Util.Util.lerp(left.uv[0], right.uv[0], t)*w*(imgWidth-1));
                        v = (int)(Util.Util.lerp(left.uv[1], right.uv[1], t) * w * (imgHeight - 1));

                        graphic_exercise.RenderData.Color vertexColor = new RenderData.Color();
                        //if (lightMode == LightMode.ON)
                        //{
                        //    vertexColor = tex(u, v);
                        //    vertexColor = Util.Util.lerp(left.lightColor, right.lightColor, t);
                        //}
                        //else
                        //{
                        //    vertexColor = Util.Util.lerp(left.color, right.color, t);
                        //}
                        //vertexColor = vertexColor * w;
                        //frameBuff.SetPixel(xIndex, yIndex, vertexColor.TransFormToSystemColor());
                        frameBuff.SetPixel(xIndex, yIndex,tex(u,v));
                    }
                }
                x += stepx;
            }
        }
        /// <summary>
        /// 保存纹理颜色值
        /// </summary>
        public void initTexture()
        {
            textureArray = new System.Drawing.Color[imgWidth,imgHeight];
            for(int i=0;i<imgWidth;i++)
            {
                for(int j=0;j<imgHeight;j++)
                {
                    textureArray[i, j] = texture.GetPixel(i, j);
                }
            }
        }
        /// <summary>
        /// 纹理采样
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private System.Drawing.Color tex(int i,int j)
        {
            return textureArray[i, imgHeight - 1 - j];
        }

        /// <summary>
        /// 逐顶点光照
        /// </summary>
        /// <param name="v1"></param>
        private void vertexLighting(Vertex v1, Matrix4x4 m)
        {
            //环境光
            graphic_exercise.RenderData.Color ambient = v1.material.ambient;
            //世界坐标
            Vector worldPos = m * v1.pos;
            //将法线转到世界空间
            Vector worldNormal = m.Inverse().Transpose() * v1.normal;
            worldNormal.normalize();
            //光线方向
            Vector worldLight = light.WorldSpaceLightPos;
            worldLight.normalize();
            //视线方向
            Vector worldView = camera.pos - worldPos;
            worldView.normalize();
            //漫反射
            graphic_exercise.RenderData.Color diffuse = light.LightColor * v1.material.diffuse * Math.Max(0, Vector.dot(worldNormal, worldLight));
            //半向量
            Vector halfDir = (worldView + worldLight).normalize();
            //高光
            graphic_exercise.RenderData.Color specular = light.LightColor * v1.material.specular * (float)Math.Pow(Math.Max(0, Vector.dot(worldNormal, halfDir)), v1.material.gloss);
#if DEBUG
            //Console.WriteLine(specular.r + " " + specular.g + "  " + specular.b);
#endif
            //顶点颜色
            v1.lightColor = ambient + diffuse + specular;
        }
        /// <summary>
        /// 背面消隐，裁剪摄像机不可见的面,加快渲染效率,顺序必须是逆时针顺序
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        bool backFaceCulling(Vertex v1, Vertex v2, Vertex v3)
        {
            //顺时针顺序,计算朝向外的发现，顺序必须为u* v;
            Vector u = v2.pos - v1.pos;
            Vector v = v3.pos - v1.pos;
            Vector n = Vector.cross(u,v);//计算法线
            //由于在视空间中，所以相机点就是（0,0,0）
            Vector viewDir =  v2.pos- new Vector(0, 0, 0);
#if DEBUG
            //Console.WriteLine(u.x + "   " + u.y + "    " + " " + u.z + "   "
            //           + v.x + "   " + v.y + "    " + " " + v.z + "   " +
            //           n.x + "   " + n.y + "    " + " " + n.z + "   " + "    " +
            //           viewDir.x + "   " + viewDir.y + "    " + " " + viewDir.z + "   " + Vector.dot(n, viewDir));
#endif

            if (Vector.dot(n, viewDir) > 0)
            {
                //夹角小于90度
                triangleNum++;
                return true;
            }
            //夹角大于90度，说明面不可见
            return false;
        }
















        Graphics g = null;
        //旋转角度
        private float rotX = 0;
        private float rotY = 0;//-(float)Math.PI/4;
        private float rotZ = 0;
        //帧率变量
        private TimeSpan timeSpan;
        private DateTime lastTime;
        private DateTime now;

        //三角形个数
        private int triangleNum = 0;
        /// <summary>
        /// 刷帧方法
        /// </summary>
        private void Tick()
        {
            while (true)
            {
                triangleNum = 0;
                lastTime = DateTime.Now;
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
                g.DrawImage(frameBuff, 0, 0);
                now = DateTime.Now;
                timeSpan = now - lastTime;
                this.Text ="帧率："+(int)Math.Ceiling(1000 / timeSpan.TotalMilliseconds)+ "         绘制的三角形个数:"+triangleNum;
                lastTime = now;
            }

        }

        /// <summary>
        /// 测试方法
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //triangleNum = 0;
            //lastTime = DateTime.Now;
            ////清除颜色缓存
            //clearBuff();
            ////求mvp矩阵
            //Matrix4x4 m = Matrix4x4.translate(0, 0, 10) * Matrix4x4.rotateY(rotY) * Matrix4x4.rotateX(rotX) * Matrix4x4.rotateZ(rotZ);
            //Matrix4x4 v = Matrix4x4.view(camera.look, camera.up, camera.pos);
            //Matrix4x4 p = Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
            ////绘制
            //print(m, v, p);
            //draw(m, v, p);
            //if (g == null)
            //{
            //    g = this.CreateGraphics();
            //}
            //g.DrawImage(frameBuff, 0, 0);
            //now = DateTime.Now;
            //timeSpan = now - lastTime;
            //this.Text = "帧率：" + (int)Math.Ceiling(1000 / timeSpan.TotalMilliseconds) + "         绘制的三角形个数:" + triangleNum;
            //lastTime = now;
        }

       

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public  void close(object sender, EventArgs e)
        {
            t.Abort();
            //this.Text = "sdfsdf";
        }

        /// <summary>
        /// 键盘事件监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.W)
            {
                camera.pos.z += 0.1f;
                Console.WriteLine("前进");
            }
            else if(keyData == Keys.S)
            {
                camera.pos.z -= 0.1f;
                Console.WriteLine("后退");
            }
            else if (keyData == Keys.A)
            {
                camera.pos.x -= 0.1f;
                Console.WriteLine("左移");
            }
            else if (keyData == Keys.D)
            {
                camera.pos.x += 0.1f;
                Console.WriteLine("右移");
            }
            else if (keyData == Keys.Q)
            {
                camera.pos.y += 0.1f;
                Console.WriteLine("上升");
            }
            else if (keyData == Keys.E)
            {
                camera.pos.y -= 0.1f;
                Console.WriteLine("下降");
            }
            else if(keyData==Keys.Z)
            {
                lightMode = LightMode.ON;
                Console.WriteLine("开灯");
            }
            else if(keyData==Keys.X)
            {
                lightMode = LightMode.OFF;
                Console.WriteLine("关灯");
            }
            else if(keyData==Keys.C)
            {
                renderMode = RenderMode.Wireframe;
                Console.WriteLine("画线框");
            }
            else if(keyData==Keys.V)
            {
                renderMode = RenderMode.Entity;
                Console.WriteLine("画实体");
            }
            else if(keyData==Keys.B)
            {
                faceCullMode = FaceCullMode.ON;
                Console.WriteLine("开启背面剪裁");
            }
            else if (keyData == Keys.N)
            {
                faceCullMode = FaceCullMode.OFF;
                Console.WriteLine("关闭背面剪裁");
            }

            return true;
        }

    }
}

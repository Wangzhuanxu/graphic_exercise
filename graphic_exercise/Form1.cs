﻿#define DEBUG

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
        Thread t;//刷帧率线程
        private Bitmap frameBuff;//用一张bitmap来做帧缓冲（颜色缓冲）       
        private Graphics frameG; //背景色画板
        private Triangle triangles;//三角形类，也就是mesh类
        private Camera camera;//摄像机类
        private float[,] zbuffer;//深度缓冲
        Light light;//灯光位置
        LightMode lightMode;//是否启用光照
        RenderMode renderMode;//渲染模式,线框还是实体
        FaceCullMode faceCullMode;//是否启用背面剪裁
        WuXiaoLinLine xiaoLinLine;//吴小林抗锯齿
        ClipTest clipTest;//视锥体剪裁
        TextColor textColors;//纹理采样
        Bitmap texture;//图片
        const int imgWidth = 256;//图片宽高
        const int imgHeight = 256;
        System.Drawing.Color[,] textureArray;//纹理颜色值
        System.Drawing.Color[,] frameArray;//屏幕颜色
        //屏幕宽高
        int width = 800+16;
        int height = 600+40;

        /// <summary>
        /// 显示console窗口
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        Button BLightSwitch= new Button();//灯开关
        Button BRenderMode = new Button();//渲染模式
        Button BFaceCullMode = new Button();//剪裁
        Button BWuXiaoLinLine = new Button();//抗锯齿
        Button BClipTest = new Button();//视锥体剪裁
        Button BTextColor = new Button();//纹理采样
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
            camera = new Camera(new Vector(0, 1, 30, 1), new Vector(0, 1, 0, 0), new Vector(0,5,5, 1), (float)System.Math.PI / 3f,this.Width / (float)this.Height,3f, 30);
            //初始化深度缓冲
            zbuffer = new float[width, height];
            //初始化灯光
            light = new Light(new Vector(0, 0, -10), graphic_exercise.RenderData.Color.White);
            //是否启用光照
            lightMode = LightMode.ON;
            //设置渲染模式
            renderMode = RenderMode.Wireframe;
            //开启背面剪裁
            faceCullMode = FaceCullMode.ON;
            //吴小林抗锯齿画线
            xiaoLinLine = WuXiaoLinLine.OFF;
            //视锥体剪裁
            clipTest = ClipTest.ON;
            //纹理采样
            textColors = TextColor.OFF;
            //初始化纹理
            System.Drawing.Image img = System.Drawing.Image.FromFile("../../Texture/fireFox.png");
            texture = new Bitmap(img, imgWidth, imgHeight);
            initTexture();

            frameArray = new System.Drawing.Color[width, height];
#if DEBUG
           // Console.WriteLine(" this.Width / (float)this.Height=" + this.Width / (float)this.Height);
#endif
            //启用绘制线程
            t = new Thread(new ThreadStart(Tick));
            t.Start();
            this.Closed += close;

            this.MouseMove += mouseMove;
            this.MouseDown += mouseDown;
            this.MouseUp += mouseUp;

            // winform按钮
            this.Controls.Add(BLightSwitch);
            BLightSwitch.SetBounds(5, 5, 40, 20);
            BLightSwitch.Text = "关灯";
            BLightSwitch.Click += b_Light;

            this.Controls.Add(BRenderMode);
            BRenderMode.SetBounds(5, 30, 40, 20);
            BRenderMode.Text = "实体";
            BRenderMode.Click += b_Render;

            //this.Controls.Add(BFaceCullMode);
            //BFaceCullMode.SetBounds(5, 105, 60, 20);
            //BFaceCullMode.Text = "不消隐";
            //BFaceCullMode.Click += b_Cull;

            //this.Controls.Add(BWuXiaoLinLine);
            //BWuXiaoLinLine.SetBounds(5, 105, 60, 20);
            //BWuXiaoLinLine.Text = "平滑线";
            //BWuXiaoLinLine.Click += b_Wu;

            this.Controls.Add(BClipTest);
            BClipTest.SetBounds(5, 55, 60, 20);
            BClipTest.Text = "不剪裁";
            BClipTest.Click += b_Clip;

            this.Controls.Add(BTextColor);
            BTextColor.SetBounds(5, 80, 40, 20);
            BTextColor.Text = "纹理";
            BTextColor.Click += b_TextColor;


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
            Array.Clear(frameArray, 0, width * height);
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
        /// <summary>
        /// 主绘制方法
        /// </summary>
        private void draw(Matrix4x4 m, Matrix4x4 v, Matrix4x4 p)
        {
            
            for (int i = 0; i < triangles.vertexList.Count; i += 3)//遍历顶点索引数组
            {
                Vertex.Clone2(triangles.vertexList[i], p1);
                Vertex.Clone2(triangles.vertexList[i + 1], p2);
                Vertex.Clone2(triangles.vertexList[i + 2], p3);
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
            if (xiaoLinLine == WuXiaoLinLine.ON&& renderMode == RenderMode.Entity)
            {
                for (int i = 1; i < width -1; i++)
                {
                    for (int j = 1; j < height - 1; j++)
                    {
                        edgeTest2(i, j);
                    }
                }
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
#endif
            //顶点光照,在世界坐标系下进
            vertexLighting(v1, m);
            vertexLighting(v2, m);
            vertexLighting(v3, m);
           
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

            //TODO 简单剔除
            if (clip(v1)==false&&clip(v2)==false&&clip(v3)==false)
            {
                return;
            }

            ///透视除法
            projectToScreen(v1);
            projectToScreen(v2);
            projectToScreen(v3);

            //剪裁
            if (clipTest == ClipTest.ON)
            {
                clipTest_near(v1, v2, v3);
            }
            else
            {
                drawTriangle2(v1, v2, v3);
            }
        }
        /// <summary>
        /// 不包含剪裁
        /// </summary>
        private void drawTriangle2(Vertex v1,Vertex v2,Vertex v3)
        {

            if (renderMode == RenderMode.Wireframe)
            {
                //画线框
                if (xiaoLinLine == WuXiaoLinLine.OFF)
                {
                    BresenhamDrawLine(v1, v2);
                    BresenhamDrawLine(v2, v3);
                    BresenhamDrawLine(v3, v1);
                }
                else   
                {
                    WuXiaoLinDrawLine(v1, v2);
                    WuXiaoLinDrawLine(v2, v3);
                    WuXiaoLinDrawLine(v3, v1);
                }
            }
            else if (renderMode == RenderMode.Entity)
            {
                //光栅化
                rasterizationTriangle(v1, v2, v3);
                if(xiaoLinLine==WuXiaoLinLine.ON)
                {
                    //WuXiaoLinDrawLine(v1, v2);
                    //WuXiaoLinDrawLine(v2, v3);
                    //WuXiaoLinDrawLine(v3, v1);
                }
               
            }
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
        /// <summary>
        /// 物体到世界坐标系
        /// </summary>
        private void objectToWorld(Matrix4x4 m,Vertex vv)
        {
            vv.pos = m * vv.pos;
        }
        /// <summary>
        /// TODO 三角形剔除操作
        /// </summary>
        private bool clip(Vertex v)
        {
            if (v.pos.x>=-v.pos.w&&v.pos.x<=v.pos.w
                && v.pos.y >= -v.pos.w && v.pos.y <= v.pos.w
                && v.pos.z >= -v.pos.w && v.pos.z <= v.pos.w)
            {
                return true;
            }
            return false;
        }
       
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
        private void BresenhamDrawLine(Vertex p1, Vertex p2)
        {
          
            int x = (int)(System.Math.Round(p1.pos.x, MidpointRounding.AwayFromZero));
            int y = (int)(System.Math.Round(p1.pos.y, MidpointRounding.AwayFromZero));
            int dx = (int)(System.Math.Round(p2.pos.x - p1.pos.x, MidpointRounding.AwayFromZero));
            int dy = (int)(System.Math.Round(p2.pos.y - p1.pos.y, MidpointRounding.AwayFromZero));
            int stepx = 1;
            int stepy = 1;

         

            //求w缓冲系数
            float w = 0;
            //插值因子
            float t = 0;
            //uv坐标
            int u = 0;
            int v = 0;
            //最终颜色
            graphic_exercise.RenderData.Color finalColor = new RenderData.Color(1, 1, 1, 1);

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
                int max = dx;
                if (max == 0)
                {
                    max = int.MaxValue;
                }
                int error = dy2 - dx;
                for (int i = 0; i < dx; i++)
                {
                    //w缓冲
                    t = i / (float)max;
                    w = Util.Util.lerp(p1.onePerZ, p2.onePerZ, t);
                    if ( w== 0)
                    {
                        w = 1;
                    }
                    else
                    {
                        w = 1 / w;
                    }

                    //初始化颜色值
                    finalColor.r = 1;
                    finalColor.g = 1;
                    finalColor.b = 1;
                    finalColor.a = 1;

                    if (textColors == TextColor.OFF)
                    {
                        //光照颜色
                        if (lightMode == LightMode.ON)
                        {
                            finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                        }
                        //颜色和光照混合
                        finalColor = Util.Util.lerp(p1.color, p2.color, t) * w * finalColor;
                    }
                    else
                    {
                        //uv坐标
                        u = (int)(Util.Util.lerp(p1.uv[0], p2.uv[0], t) * w * (imgWidth - 1));
                        v = (int)(Util.Util.lerp(p1.uv[1], p2.uv[1], t) * w * (imgHeight - 1));
                        //光照颜色
                        if(lightMode==LightMode.ON)
                        {
                            finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                        }
                        ////纹理颜色
                        finalColor = new RenderData.Color(tex(u, v)) * finalColor;
                    }

                       

                    if (x >= 0 && y >= 0 && x < width && y < height)
                    {
                        frameBuff.SetPixel(x, y, finalColor.TransFormToSystemColor());
                    }
                    else
                    {
                       // Console.WriteLine(p1.pos.x + "  " + p1.pos.y + "  " + p2.pos.x + "  " + p2.pos.y + "  v2在外");
                    }
                       
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
                int max = dy;
                if (max == 0)
                {
                    max = int.MaxValue;
                }
                int error = dx2 - dy;
                for (int i = 0; i < dy; i++)
                {
                    //w缓冲
                    t = i / (float)max;
                    w = Util.Util.lerp(p1.onePerZ, p2.onePerZ, t);
                    if (w == 0)
                    {
                        w = 1;
                    }
                    else
                    {
                        w = 1 / w;
                    }

                    //初始化颜色值
                    finalColor.r = 1;
                    finalColor.g = 1;
                    finalColor.b = 1;
                    finalColor.a = 1;

                    if (textColors == TextColor.OFF)
                    {
                        //光照颜色
                        if (lightMode == LightMode.ON)
                        {
                            finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                        }
                        //颜色和光照混合
                        finalColor = Util.Util.lerp(p1.color, p2.color, t) * w * finalColor;
                    }
                    else
                    {
                        //uv坐标
                        u = (int)(Util.Util.lerp(p1.uv[0], p2.uv[0], t) * w * (imgWidth - 1));
                        v = (int)(Util.Util.lerp(p1.uv[1], p2.uv[1], t) * w * (imgHeight - 1));
                        //光照颜色
                        if (lightMode == LightMode.ON)
                        {
                            finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                        }
                        ////纹理颜色
                        finalColor = new RenderData.Color(tex(u, v)) * finalColor;
                    }

                    if (x >= 0 && y >= 0 && x < width && y < height)
                    {
                        frameBuff.SetPixel(x, y, finalColor.TransFormToSystemColor());
                    }
                    else
                    {
                        
                    }
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
        /// 吴小林抗锯齿画线，线条有颜色
        /// 图形程序开发人员指南
        /// 基本思想：画该条线上下方两个像素点，这两个像素点的颜色值的亮度和为1
        /// </summary>
        private void WuXiaoLinDrawLine(Vertex p1, Vertex p2)
        {
            //绘制起始点的x y坐标
            int x = (int)(System.Math.Round(p1.pos.x, MidpointRounding.AwayFromZero));
            int y = (int)(System.Math.Round(p1.pos.y, MidpointRounding.AwayFromZero));
            //用于求斜率
            int dx = (int)(System.Math.Round(p2.pos.x - p1.pos.x, MidpointRounding.AwayFromZero));
            int dy = (int)(System.Math.Round(p2.pos.y - p1.pos.y, MidpointRounding.AwayFromZero));
            //移动步长
            int stepx = 1;
            int stepy = 1;
            //绘制距离
            int xlength = dx;
            int ylength = dy;
            //求w缓冲系数
            float w = 0;
            //插值因子
            float t = 0;
            //uv坐标
            int u = 0;
            int v = 0;
            //最终颜色
            graphic_exercise.RenderData.Color finalColor = new RenderData.Color(1, 1, 1, 1);
            if (dx >= 0)
            {
                stepx = 1;
            }
            else
            {
                stepx = -1;
                xlength = System.Math.Abs(dx);
            }

            if (dy >= 0)
            {
                stepy = 1;
            }
            else
            {
                stepy = -1;
                ylength = System.Math.Abs(dy);
            }
            //起始x,y值
            int a;
            int b;
            if (xlength > ylength)
            {
                //斜率
                float k = 0;
                if (dx != 0)
                    k = dy / (float)dx;
                float error = k;
                //抗锯齿的插值因子
                float e = 1;

                int max = xlength;
                if (max == 0)
                {
                    max = int.MaxValue;
                }
                for (int i = 0; i <xlength; i += 1)
                {
                    a = x;
                    b = y;
                    //w缓冲
                    t = i / (float)max;
                    if (a >= 0 && b >= 0 && a < width && b < height)
                    {
                        w = Util.Util.lerp(p1.onePerZ, p2.onePerZ, t);
                        if (w==0)
                        {
                            w = 1;
                        }
                        else
                        {
                            w = 1 /w;
                        }

                        //初始化颜色值
                        finalColor.r = 1;
                        finalColor.g = 1;
                        finalColor.b = 1;
                        finalColor.a = 1;

                        if (textColors==TextColor.OFF)
                        {
                            //光照颜色
                            if(lightMode==LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                            }
                            //颜色和光照混合
                            finalColor = Util.Util.lerp(p1.color, p2.color, t) * w * finalColor;
                        }
                        else
                        {
                            //uv坐标
                            u = (int)(Util.Util.lerp(p1.uv[0], p2.uv[0], t) * w * (imgWidth - 1));
                            v = (int)(Util.Util.lerp(p1.uv[1], p2.uv[1], t) * w * (imgHeight - 1));
                            //光照颜色
                            if (lightMode == LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                            }
                            ////纹理颜色
                            finalColor = new RenderData.Color(tex(u, v))* finalColor;
                        } 
                        e = Math.Abs(error);
                        frameBuff.SetPixel(a, b, (finalColor * (1 - e)).TransFormToSystemColor());
                        b = b + stepy;
                        if (b >= 0 && b < height)
                        {
                            frameBuff.SetPixel(a, b, (finalColor * e).TransFormToSystemColor());
                        }
                        
                    }
                    x += stepx;
                    error += k * stepx;
                    if (error > 1)
                    {
                        error -= 1;
                        y += stepy;

                    }
                    else if (error < -1)
                    {
                        error += 1;
                        y += stepy;

                    }

                }
            }
            else
            {
                float k = 0;
                if (dy != 0)
                    k = dx / (float)dy;
                float error = k;
                float e = 1;

                int max = ylength;
                if (max == 0)
                {
                    max = int.MaxValue;
                }
                for (int i = 0; i < ylength; i++)
                {
                    a = x;
                    b = y;
                    //w缓冲
                    t = i / (float)max;
                    if (a >= 0 && b >= 0 && a < width && b < height)
                    {
                        w = Util.Util.lerp(p1.onePerZ, p2.onePerZ, t);
                        if (w == 0)
                        {
                            w = 1;
                        }
                        else
                        {
                            w = 1 / w;
                        }

                        //初始化颜色值
                        finalColor.r = 1;
                        finalColor.g = 1;
                        finalColor.b = 1;
                        finalColor.a = 1;

                        if (textColors==TextColor.OFF)
                        {
                            //光照颜色
                            if (lightMode == LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                            }
                            //颜色和光照混合
                            finalColor = Util.Util.lerp(p1.color, p2.color, t) * w * finalColor;
                        }
                        else
                        {
                            //uv坐标
                            u = (int)(Util.Util.lerp(p1.uv[0], p2.uv[0], t) * w * (imgWidth - 1));
                            v = (int)(Util.Util.lerp(p1.uv[1], p2.uv[1], t) * w * (imgHeight - 1));
                            //光照颜色
                            if (lightMode == LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(p1.lightColor, p2.lightColor, t) * w;
                            }
                            ////纹理颜色
                            finalColor = new RenderData.Color(tex(u, v))*finalColor;
                            
                        }


                        e = Math.Abs(error);
                        frameBuff.SetPixel(a, b, (finalColor * (1 - e)).TransFormToSystemColor());
                        a = x + stepx;
                        if (a >= 0 && a < width)
                            frameBuff.SetPixel(a, b, (finalColor * e).TransFormToSystemColor());

                    }
                    error += k * stepy;
                    y += stepy;
                    if (error > 1)
                    {
                        error -= 1;
                        x += stepx;
                    }
                    else if (error < -1)
                    {
                        error += 1;
                        x += stepx;
                    }
                }
            }

        }

        /// <summary>
        /// 光栅化三角形
        /// </summary>
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

        #region 光栅化1.0
        private void drawTriangleBottom(Vertex v1, Vertex v2, Vertex v3)
        {
            //int x1 = (int)(System.Math.Round(v1.pos.x, MidpointRounding.AwayFromZero));
            //int x2 = (int)(System.Math.Round(v2.pos.x, MidpointRounding.AwayFromZero));
            //int x3 = (int)(System.Math.Round(v3.pos.x, MidpointRounding.AwayFromZero));
            //int y1 = (int)(System.Math.Round(v1.pos.y, MidpointRounding.AwayFromZero));
            //int y2 = (int)(System.Math.Round(v2.pos.y, MidpointRounding.AwayFromZero));
            //int y3 = (int)(System.Math.Round(v3.pos.y, MidpointRounding.AwayFromZero));

            int x1 = (int)(System.Math.Ceiling(v1.pos.x));
            int x2 = (int)(System.Math.Ceiling(v2.pos.x));
            int x3 = (int)(System.Math.Ceiling(v3.pos.x));
            int y1 = (int)(System.Math.Ceiling(v1.pos.y));
            int y2 = (int)(System.Math.Ceiling(v2.pos.y));
            int y3 = (int)(System.Math.Ceiling(v3.pos.y));
            float dx3 = (x3 - x1) / (float)(y3 - y1);
            float dx2 = (x2 - x1) / (float)(y2 - y1);
            for (int y = y1; y < y3; y += 1)
            {
                //防止浮点数精度不准，四舍五入，使y的值每次增加1
                // int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                // int yIndex = y;// (int)Math.Ceiling(y);
                //裁剪掉屏幕外的线
                if (y >= 0 && y < height)
                {

                    int xl = (int)Math.Ceiling((y - y1) * dx3 + x1);
                    int xr = (int)Math.Ceiling((y - y1) * dx2 + x1);
                    //插值因子
                    float t = (y - y1) / (float)(y3 - y1);
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
                    //扫描线填充
                    if (left.pos.x < right.pos.x)
                    {
                        scanLine(left, right, y, xl, xr);
                    }
                    else
                    {
                        scanLine(right, left, y, xr, xl);
                    }

                }

            }
        }
        private void drawTriangleTop(Vertex v1, Vertex v2, Vertex v3)
        {
            //int x1 = (int)(System.Math.Round(v1.pos.x, MidpointRounding.AwayFromZero));
            //int x2 = (int)(System.Math.Round(v2.pos.x, MidpointRounding.AwayFromZero));
            //int x3 = (int)(System.Math.Round(v3.pos.x, MidpointRounding.AwayFromZero));
            //int y1 = (int)(System.Math.Round(v1.pos.y, MidpointRounding.AwayFromZero));
            //int y2 = (int)(System.Math.Round(v2.pos.y, MidpointRounding.AwayFromZero));
            //int y3 = (int)(System.Math.Round(v3.pos.y, MidpointRounding.AwayFromZero));

            int x1 = (int)(System.Math.Ceiling(v1.pos.x));
            int x2 = (int)(System.Math.Ceiling(v2.pos.x));
            int x3 = (int)(System.Math.Ceiling(v3.pos.x));
            int y1 = (int)(System.Math.Ceiling(v1.pos.y));
            int y2 = (int)(System.Math.Ceiling(v2.pos.y));
            int y3 = (int)(System.Math.Ceiling(v3.pos.y));
            float dx1 = (x3 - x1) / (float)(y3 - y1);
            float dx2 = (x3 - x2) / (float)(y3 - y2);
            for (int y = y1; y < y3; y += 1)
            {
                //防止浮点数精度不准，四舍五入，使y的值每次增加1
                //int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                // int yIndex = y;// (int)Math.Ceiling(y);
                //裁剪掉屏幕外的线
                if (y >= 0 && y < height)
                {
                    int xl = (int)Math.Ceiling((y - y1) * dx1 + x1);
                    int xr = (int)Math.Ceiling((y - y2) * dx2 + x2);
                    //插值因子
                    float t = (y - y1) / (float)(y3 - y1);
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
                        scanLine(left, right, y, xl, xr);
                    }
                    else
                    {
                        scanLine(right, left, y, xr, xl);
                    }
                }
            }
        }
        #endregion

        #region 光栅化2.0

        //private void drawTriangleBottom(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    //int x1 = (int)(System.Math.Ceiling(v1.pos.x));
        //    //int x2 = (int)(System.Math.Ceiling(v2.pos.x));
        //    //int x3 = (int)(System.Math.Ceiling(v3.pos.x));
        //    int y1 = (int)(System.Math.Ceiling(v1.pos.y));
        //    //int y2 = (int)(System.Math.Ceiling(v2.pos.y));
        //    int y3 = (int)(System.Math.Ceiling(v3.pos.y));
        //    float dx3 = (v3.pos.x - v1.pos.x) / (float)(v3.pos.y - v1.pos.y);
        //    float dx2 = (v2.pos.x - v1.pos.x) / (float)(v2.pos.y - v1.pos.y);

        //    //Vertex temp1=

        //    float xleft = (float)(v1.pos.x +(Math.Ceiling(v1.pos.y) - v1.pos.y) *dx3);
        //    float xright = (float)(v1.pos.x + (Math.Ceiling(v1.pos.y) - v1.pos.y) * dx2);
        //    for (int y = y1; y < y3; y += 1)
        //    {
        //        //防止浮点数精度不准，四舍五入，使y的值每次增加1
        //        // int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
        //        // int yIndex = y;// (int)Math.Ceiling(y);
        //        //裁剪掉屏幕外的线
        //        if (y >= 0 && y < height)
        //        {

        //            //插值因子
        //            float t = (y - y1) / (float)(y3 - y1);
        //            //左顶点
        //            Vertex left = new Vertex();
        //            left.pos.x = xleft;
        //            left.pos.y = y;
        //            Util.Util.lerp(left, v1, v3, t);
        //            //
        //            Vertex right = new Vertex();
        //            right.pos.x = xright;
        //            right.pos.y = y;
        //            Util.Util.lerp(right, v1, v2, t);
        //            //扫描线填充
        //            if (left.pos.x < right.pos.x)
        //            {
        //                scanLine(left, right, y);
        //            }
        //            else
        //            {
        //                scanLine(right, left, y);
        //            }

        //        }
        //        xleft += dx3;
        //        xright += dx2;
        //    }
        //}

        ///// <summary>
        ///// 平顶三角形
        ///// </summary>
        //private void drawTriangleTop(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    //int x1 = (int)(System.Math.Ceiling(v1.pos.x));
        //    //int x2 = (int)(System.Math.Ceiling(v2.pos.x));
        //    //int x3 = (int)(System.Math.Ceiling(v3.pos.x));
        //    int y1 = (int)(System.Math.Ceiling(v1.pos.y));
        //    //int y2 = (int)(System.Math.Ceiling(v2.pos.y));
        //    int y3 = (int)(System.Math.Ceiling(v3.pos.y));
        //    float dx1 = (v3.pos.x - v1.pos.x) / (float)(v3.pos.y - v1.pos.y);
        //    float dx2 = (v3.pos.x - v2.pos.x) / (float)(v3.pos.y - v2.pos.y);

        //    float xleft = (float)(v1.pos.x +(Math.Ceiling(v1.pos.y)- v1.pos.y) *dx1);
        //    float xright =(float)( v2.pos.x + (Math.Ceiling(v1.pos.y) - v1.pos.y) * dx2);
        //    for (int y = y1; y < y3; y += 1)
        //    {
        //        if (y >= 0 && y < height)
        //        {
        //            //插值因子
        //            float t = (y - y1) / (float)(y3 - y1);
        //            //左顶点
        //            Vertex left = new Vertex();
        //            left.pos.x = xleft;
        //            left.pos.y = y;
        //            Util.Util.lerp(left, v1, v3, t);
        //            //
        //            Vertex right = new Vertex();
        //            right.pos.x = xright;
        //            right.pos.y = y;
        //            Util.Util.lerp(right, v2, v3, t);
        //            //扫描线填充
        //            if (left.pos.x < right.pos.x)
        //            {
        //                scanLine(left, right, y);
        //            }
        //            else
        //            {
        //                scanLine(right, left, y);
        //            }
        //        }
        //        xleft += dx1;
        //        xright += dx2;
        //    }
        //}

        #endregion

        /// <summary>
        /// 填充
        /// </summary>
        /// <param name="left">左顶点</param>
        /// <param name="right">右顶点</param>
        /// <param name="yIndex">y值</param>
        private void scanLine(Vertex left, Vertex right, int yIndex,int xl,int xr)
        {
            //求线段长度
            //int x = (int)(System.Math.Round(left.pos.x, MidpointRounding.AwayFromZero));
            //int dx = (int)(System.Math.Round(right.pos.x - left.pos.x, MidpointRounding.AwayFromZero));
            //int x = (int)(System.Math.Ceiling(left.pos.x));
            //int dx = (int)(System.Math.Ceiling(right.pos.x - left.pos.x));

            int x = xl;
            int dx = xr - xl;

            //int x = (int)(System.Math.Ceiling(left.pos.x));
            //int x2= (int)(System.Math.Ceiling(right.pos.x));
            //int dx = x2 - x;

            int stepx = 1;
            //求w缓冲系数
            float w = 0;
            //插值因子
            float t = 0;
            //该点像素的深度值
            float death = 0;
            //uv坐标
            int u = 0;
            int v = 0;

            int max = dx;
            if(max==0)
            {
                max = 9999;
            }  

            for (int i = 0; i <=dx; i +=1)
            {
                t = i / (float)max;
                int xIndex = x;
                if (xIndex >= 0 && xIndex <width)
                {
                    ///计算该片元的深度值
                    death = Util.Util.lerp(left.depth, right.depth, t);
                    if (zbuffer[xIndex, yIndex] > death)
                    {
                        //w缓冲
                        w = Util.Util.lerp(left.onePerZ, right.onePerZ, t);
                        if (w==0)
                        {
                            w = 1/w;
                        }
                        else
                        {
                            w = 1 / w;
                        }
                      
                        //深度值
                        zbuffer[xIndex, yIndex] = death;
                        //uv坐标
                        u =(int)(Util.Util.lerp(left.uv[0], right.uv[0], t)*w*(imgWidth-1));
                        v = (int)(Util.Util.lerp(left.uv[1], right.uv[1], t) * w * (imgHeight - 1));
                        //最终颜色
                        graphic_exercise.RenderData.Color finalColor = new RenderData.Color(1,1,1,1);
                        if (textColors == TextColor.OFF)
                        {
                            
                            //光照颜色
                            if(lightMode==LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(left.lightColor, right.lightColor, t) * w;
                            }
                            //颜色和光照混合
                            finalColor = Util.Util.lerp(left.color, right.color, t) * w * finalColor;
                        }
                        else
                        {
                            //光照颜色
                            if (lightMode == LightMode.ON)
                            {
                                finalColor = Util.Util.lerp(left.lightColor, right.lightColor, t) * w;
                            }
                            //纹理颜色
                            finalColor = new RenderData.Color(tex(u, v))*finalColor;
                        }
                        frameBuff.SetPixel(xIndex, yIndex, finalColor.TransFormToSystemColor());
                        frameArray[xIndex, yIndex] = finalColor.TransFormToSystemColor();
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
        private System.Drawing.Color tex(int i,int j)
        {
            if(i<0||i>imgWidth-1||j<0||j>imgHeight-1)
            {
                Console.WriteLine("2324324");
                return System.Drawing.Color.Black;
                
            }
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
            Vector worldPos = v1.pos;
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
            //Console.WriteLine(diffuse.r + "  " + diffuse.g + "  " + diffuse.b + "  " + diffuse.a);
        }
        /// <summary>
        /// 背面消隐，裁剪摄像机不可见的面,加快渲染效率,顺序必须是逆时针顺序
        /// 在相机空间是因为相机在该空间的位置为0，0，0
        /// </summary>
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

        #region 刷帧线程方法

        Graphics g = null;
        //旋转角度
        private float rotX = 0;
        private float rotY = 0;//-(float)Math.PI/4;
        private float rotZ = 0;
        private float tranZ = 10;
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
            Matrix4x4 m;
            Matrix4x4 v;
            Matrix4x4 p;
            while (true)
            {
                triangleNum = 0;
                //清除颜色缓存
                clearBuff();
                //求mvp矩阵
                 m= Matrix4x4.translate(0, 3, tranZ) * Matrix4x4.rotateY(rotY) * Matrix4x4.rotateX(rotX) * Matrix4x4.rotateZ(rotZ);
                 v= Matrix4x4.view(camera.look, camera.up, camera.pos);
                 p= Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
                lastTime = DateTime.Now;
                //绘制
                draw(m, v, p);
                
                if (g == null)
                {
                    g = this.CreateGraphics();
                }
                g.DrawImage(frameBuff, 0, 0);
                now = DateTime.Now;
                timeSpan = now - lastTime;
                this.Text = "帧率：" + (int)Math.Ceiling(1000 / timeSpan.TotalMilliseconds) ;
                lastTime = now;
            }
        }
        /// <summary>
        /// 刷帧测试方法
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
            //// print(m, v, p);
            //draw(m, v, p);
            //now = DateTime.Now;
            //timeSpan = now - lastTime;
            //this.Text = "帧率：" + (int)Math.Ceiling(1000 / timeSpan.TotalMilliseconds) + "         绘制的三角形个数:" + triangleNum + "    " + timeSpan.TotalMilliseconds;
            //lastTime = now;
            //if (g == null)
            //{
            //    g = this.CreateGraphics();
            //}
            //g.DrawImage(frameBuff, 0, 0);


        }

        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        #region 窗口关闭事件
        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        public void close(object sender, EventArgs e)
        {
            t.Abort();
        }
        #endregion

        #region 鼠标键盘监听事件

        /// <summary>
        /// 上一个位置
        /// </summary>
        Point lastPoint =new Point();
        //摄像机位置
        Vector pos;
        //旋转角度
        float rx = 0;
        float ry = 0;
        //能否移动
        bool canMove = false;
        float angle = (float)(Math.PI / 90);
        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        public void mouseDown(object sender, MouseEventArgs e)
        {
           // lastPoint = e.Location;
            canMove = true;
        }
        /// <summary>
        /// 鼠标移动
        /// </summary>
        public void mouseMove(object sender, MouseEventArgs e)
        {
            float dx = Util.Util.distance(e.X, 0, lastPoint.X, 0);
            float dy = Util.Util.distance(e.Y, 0, lastPoint.Y, 0);
            if (dx>=dy&&dx>= 5&& canMove)
            {
                if (e.Location.X - lastPoint.X > 0)
                {
                    ry = angle;
                }
                else
                {
                    ry = -angle;
                }
            }
            else if (dy>dx&&dy >= 5&& canMove)
            {
                if (e.Location.Y - lastPoint.Y > 0)
                {
                    rx = angle;
                }
                else
                {
                    rx = -angle;
                }
            }

            if (canMove)
            {
                //pos = camera.pos;
                //pos = Matrix4x4.translate(camera.look.x, camera.look.y, camera.look.z) * Matrix4x4.rotateY(ry) * Matrix4x4.rotateX(rx) * Matrix4x4.translate(-camera.look.x, -camera.look.y, -camera.look.z) * pos;
                //camera.pos = pos;
                pos = camera.look-camera.pos;
                pos =  Matrix4x4.rotateY(ry) * Matrix4x4.rotateX(rx) * pos;
                camera.look = pos+camera.pos;

                //Vector N = camera.look - camera.pos;
                //N.normalize();
                //Vector U = Vector.cross(camera.up, N);
                //U.normalize();
                //camera.up = Vector.cross(N, U);
                //camera.up.normalize();

                lastPoint.X = e.Location.X;
                lastPoint.Y = e.Location.Y;
                ry = 0;
                rx = 0;
            }
        }
        public void mouseUp(object sender, MouseEventArgs e)
        {
            canMove = false;
        }
        /// <summary>
        /// 键盘事件监听
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Vector N = camera.look - camera.pos;
            N.normalize();
            Vector U = Vector.cross(camera.up, N);
            U.normalize();
            Vector up = Vector.cross(N, U);
            up.normalize();

            if (keyData == Keys.W)
            {
                camera.pos += N*0.1f;
                //camera.pos.z += 0.1f;
                //camera.look.z += 0.1f;
                //Console.WriteLine("前进");
            }
            else if(keyData == Keys.S)
            {
                camera.pos -= N * 0.1f;
                //camera.pos.z -= 0.1f;
                //camera.look.z -= 0.1f;
                //Console.WriteLine("后退");
            }
            else if (keyData == Keys.A)
            {
                camera.pos -= U * 0.1f;
                //camera.pos.x -= 0.1f;
                camera.look -= U * 0.1f;
                //Console.WriteLine("左移");
            }
            else if (keyData == Keys.D)
            {
                camera.pos += U * 0.1f;
                //camera.pos.x += 0.1f;
                camera.look+= U * 0.1f;
                //Console.WriteLine("右移");
            }
            else if (keyData == Keys.Q)
            {
                camera.pos.y += 0.1f;
                camera.look.y += 0.1f;
                Console.WriteLine("上升");
            }
            else if (keyData == Keys.E)
            {
                camera.pos.y -= 0.1f;
                camera.look.y -= 0.1f;
                Console.WriteLine("下降");
            }
            return true;
        }

        public void b_Light(object sender, EventArgs e)
        {
            if(lightMode==LightMode.ON)
            {
                lightMode = LightMode.OFF;
                BLightSwitch.Text = "开灯";
            }
            else if(lightMode==LightMode.OFF)
            {
                lightMode = LightMode.ON;
                BLightSwitch.Text = "关灯";
            }
        }

        public void b_Render(object sender, EventArgs e)
        {
            if (renderMode == RenderMode.Entity)
            {
                renderMode = RenderMode.Wireframe;
                BRenderMode.Text = "实体";
            }
            else if (renderMode == RenderMode.Wireframe)
            {
                renderMode = RenderMode.Entity;
                BRenderMode.Text = "线框";
            }
        }

        public void b_Cull(object sender, EventArgs e)
        {
            if (faceCullMode == FaceCullMode.ON)
            {
                faceCullMode = FaceCullMode.OFF;
                BFaceCullMode.Text = "消隐";
            }
            else if (faceCullMode == FaceCullMode.OFF)
            {
                faceCullMode = FaceCullMode.ON;
                BFaceCullMode.Text = "不消隐";
            }
        }

        public void b_Wu(object sender, EventArgs e)
        {
            if (xiaoLinLine == WuXiaoLinLine.ON)
            {
                xiaoLinLine = WuXiaoLinLine.OFF;
                BWuXiaoLinLine.Text = "平滑线";
            }
            else if (xiaoLinLine == WuXiaoLinLine.OFF)
            {
                xiaoLinLine = WuXiaoLinLine.ON;
                BWuXiaoLinLine.Text = "锯齿线";
            }
        }

        public void b_Clip(object sender, EventArgs e)
        {
            if(clipTest==ClipTest.ON)
            {
                clipTest = ClipTest.OFF;
                BClipTest.Text = "不剪裁";
            }
            else if(clipTest==ClipTest.OFF)
            {
                clipTest = ClipTest.ON;
                BClipTest.Text = "剪裁";
            }
        }

        public void b_TextColor(object sender, EventArgs e)
        {
            if (textColors == TextColor.ON)
            {
                textColors = TextColor.OFF;
                BTextColor.Text = "纹理";
            }
            else if (textColors == TextColor.OFF)
            {
                textColors = TextColor.ON;
                BTextColor.Text = "颜色";
            }
        }


        #endregion

        #region 后处理
        private void edgeTest(int u,int v)
        {
            float edge = Sobel(u, v);
            //System.Drawing.Color withEdgeColor = lerp(System.Drawing.Color.White, frameArray[u, v], edge);
            //System.Drawing.Color onlyColor = lerp(frameArray[u, v], System.Drawing.Color.Black, edge);
            System.Drawing.Color c = frameArray[u, v];
            frameBuff.SetPixel(u, v, System.Drawing.Color.FromArgb(number((int)Math.Abs((c.A * edge)),255),c.R,c.G,c.B));
            //frameBuff.SetPixel(u, v, c);
            
        }
        System.Drawing.Color lerp(System.Drawing.Color c1,System.Drawing.Color c2,float t)
        { 
            float R = c2.R * t + (1 - t) * c1.R;
            float G = c2.G * t + (1 - t) * c1.G;
            float B = c2.B * t + (1 - t) * c1.B;
            return System.Drawing.Color.FromArgb(getInt(R), getInt(G), getInt(B));
        }

        int number(int x,int y)
        {
            while (x > y)
            {
                x = x - y;
            }
            //if(x>y)
            //{
            //    return 255;
            //}
            return x;
        }

        int getInt(float t)
        {
            if(t<0)
            {
                return 0;
            }
            else if(t>255)
            {
                return 255;
            }
            else
            {
                return (int)t;
            }
        }

        float luminance(System.Drawing.Color color) {
				return  (0.299f * color.R + 0.587f * color.G + 0.114f * color.B)/255f; 
		}

        float Sobel(int u,int v)
        {
             float []Gx = {-1,  0,  1,
                                        -2,  0,  2,
                                        -1,  0,  1};
             float[] Gy = {-1, -2, -1,
                                        0,  0,  0,
                                        1,  2,  1};
            int[] ux= {-1, 0,1,
                                        -1,  0,  1,
                                        -1,  0,  1};
            int[] vx = {1, 1, 1,
                                        0,  0,  0,
                                        -1,  -1,  -1};

            float texColor;
            float edgeX = 0;
            float edgeY = 0;
            int uu = 0;
            int vv = 0;
            for (int it = 0; it < 9; it++)
            {
                uu = u + ux[it];
                vv = v + vx[it];
                if(uu>=0&&uu<width&&vv>=0&&vv<height)
                {
                    texColor = luminance(frameArray[uu,vv]);
                    edgeX += texColor * Gx[it];
                    edgeY += texColor * Gy[it];
                }
               
            }

            float edge = 1 - Math.Abs(edgeX) - Math.Abs(edgeY);
            return edge;
        }

        private void edgeTest2(int u, int v)
        {
            float uu = u / (float)width;
            float vv = v / (float)height;

            System.Drawing.Color colorCenter = frameArray[u,v];

            // Luma at the current fragment
            float lumaCenter = luminance(colorCenter);

            // Luma at the four direct neighbours of the current fragment.
            float lumaDown = luminance(frameArray[u, v-1]);
            float lumaUp = luminance(frameArray[u, v+1]);
            float lumaLeft = luminance(frameArray[u-1, v]);
            float lumaRight = luminance(frameArray[u+1,v]);

            // Find the maximum and minimum luma around the current fragment.
            float lumaMin = Math.Min(lumaCenter, Math.Min(Math.Min(lumaDown, lumaUp), Math.Min(lumaLeft, lumaRight)));
            float lumaMax = Math.Max(lumaCenter, Math.Max(Math.Max(lumaDown, lumaUp), Math.Max(lumaLeft, lumaRight)));

            // Compute the delta.
            float lumaRange = lumaMax - lumaMin;
            float EDGE_THRESHOLD_MIN = 0.0312f;
            float EDGE_THRESHOLD_MAX = 0.125f;
            // If the luma variation is lower that a threshold (or if we are in a really dark area), we are not on an edge, don't perform any AA.
            if (lumaRange < Math.Max(EDGE_THRESHOLD_MIN, lumaMax * EDGE_THRESHOLD_MAX))
            {
                frameBuff.SetPixel(u, v, colorCenter);
                return;
            }

            float lumaDownLeft = luminance(frameArray[u-1, v-1]);
            float lumaUpRight = luminance(frameArray[u+1, v+1]);
            float lumaUpLeft = luminance(frameArray[u-1,v+ 1]);
            float lumaDownRight = luminance(frameArray[u+1, v-1]);

            // Combine the four edges lumas (using intermediary variables for future computations with the same values).
            float lumaDownUp = lumaDown + lumaUp;
            float lumaLeftRight = lumaLeft + lumaRight;

            // Same for corners
            float lumaLeftCorners = lumaDownLeft + lumaUpLeft;
            float lumaDownCorners = lumaDownLeft + lumaDownRight;
            float lumaRightCorners = lumaDownRight + lumaUpRight;
            float lumaUpCorners = lumaUpRight + lumaUpLeft;

            // Compute an estimation of the gradient along the horizontal and vertical axis.
            float edgeHorizontal = Math.Abs(-2.0f * lumaLeft + lumaLeftCorners) + Math.Abs(-2.0f * lumaCenter + lumaDownUp) * 2.0f + Math.Abs(-2.0f * lumaRight + lumaRightCorners);
            float edgeVertical = Math.Abs(-2.0f * lumaUp + lumaUpCorners) + Math.Abs(-2.0f * lumaCenter + lumaLeftRight) * 2.0f + Math.Abs(-2.0f * lumaDown + lumaDownCorners);

            // Is the local edge horizontal or vertical ?
            bool isHorizontal = (edgeHorizontal >= edgeVertical);

            float luma1 = isHorizontal ? lumaDown : lumaLeft;
            float luma2 = isHorizontal ? lumaUp : lumaRight;
            // Compute gradients in this direction.
            float gradient1 = luma1 - lumaCenter;
            float gradient2 = luma2 - lumaCenter;

            // Which direction is the steepest ?
            bool is1Steepest = Math.Abs(gradient1) >= Math.Abs(gradient2);

            // Gradient in the corresponding direction, normalized.
            float gradientScaled = 0.25f * Math.Max(Math.Abs(gradient1), Math.Abs(gradient2));


            float inverseScreenSizex = 1 / (float)width;
            float inverseScreenSizey = 1 / (float)height;
            float stepLength = isHorizontal ? inverseScreenSizey : inverseScreenSizex;

            // Average luma in the correct direction.
            float lumaLocalAverage = 0.0f;

            if (is1Steepest)
            {
                // Switch the direction
                stepLength = -stepLength;
                lumaLocalAverage = 0.5f * (luma1 + lumaCenter);
            }
            else
            {
                lumaLocalAverage = 0.5f * (luma2 + lumaCenter);
            }

            //// Shift UV in the correct direction by half a pixel.
            UV currentUv = new UV(uu,vv);
            if (isHorizontal)
            {
                currentUv.y += stepLength * 0.5f;
            }
            else
            {
                currentUv.x += stepLength * 0.5f;
            }


            UV offset = isHorizontal ? new UV(inverseScreenSizex, 0.0f) : new UV(0.0f, inverseScreenSizey);
            // Compute UVs to explore on each side of the edge, orthogonally. The QUALITY allows us to step faster.
            UV uv1 = currentUv - offset;
            UV uv2 = currentUv + offset;

            // Read the lumas at both current extremities of the exploration segment, and compute the delta wrt to the local average luma.
            float lumaEnd1 =luminance(frameArray[(int)(System.Math.Round(uv1.x * width, MidpointRounding.AwayFromZero)), (int)(System.Math.Round((uv1.y * height), MidpointRounding.AwayFromZero))]);
            float lumaEnd2 = luminance(frameArray[(int)(System.Math.Round(uv2.x * width, MidpointRounding.AwayFromZero)), (int)(System.Math.Round((uv2.y * height), MidpointRounding.AwayFromZero))]);
            
            lumaEnd1 -= lumaLocalAverage;
            lumaEnd2 -= lumaLocalAverage;

            // If the luma deltas at the current extremities are larger than the local gradient, we have reached the side of the edge.
            bool reached1 = Math.Abs(lumaEnd1) >= gradientScaled;
            bool reached2 = Math.Abs(lumaEnd2) >= gradientScaled;
            bool reachedBoth = reached1 && reached2;

            // If the side is not reached, we continue to explore in this direction.
            if (!reached1)
            {
                uv1 -= offset;
            }
            if (!reached2)
            {
                uv2 += offset;
            }


            if (!reachedBoth)
            {
                float[] num = { 0.5f, 0.5f, 0.5f, 0.5f,1.5f, 1.5f, 1.5f };
                for (int i = 2; i <9; i++)
                {
                    // If needed, read luma in 1st direction, compute delta.
                    if (!reached1)
                    {
                        lumaEnd1 = luminance(frameArray[(int)(System.Math.Round(uv1.x * width, MidpointRounding.AwayFromZero)), (int)(System.Math.Round((uv1.y * height), MidpointRounding.AwayFromZero))]);
                        lumaEnd1 = lumaEnd1 - lumaLocalAverage;
                    }
                    // If needed, read luma in opposite direction, compute delta.
                    if (!reached2)
                    {
                        lumaEnd2 =luminance(frameArray[(int)(System.Math.Round(uv2.x * width, MidpointRounding.AwayFromZero)), (int)(System.Math.Round((uv2.y * height), MidpointRounding.AwayFromZero))]);
                        lumaEnd2 = lumaEnd2 - lumaLocalAverage;
                    }
                    // If the luma deltas at the current extremities is larger than the local gradient, we have reached the side of the edge.
                    reached1 = Math.Abs(lumaEnd1) >= gradientScaled;
                    reached2 = Math.Abs(lumaEnd2) >= gradientScaled;
                    reachedBoth = reached1 && reached2;

                    // If the side is not reached, we continue to explore in this direction, with a variable quality.
                    if (!reached1)
                    {
                        uv1 -= offset *num[i-2];
                    }
                    if (!reached2)
                    {
                        uv2 += offset * num[i - 2];
                    }

                    // If both sides have been reached, stop the exploration.
                    if (reachedBoth) { break; }
                }
            }


            float distance1 = isHorizontal ? (uu- uv1.x) : (vv - uv1.y);
            float distance2 = isHorizontal ? (uv2.x - uu) : (uv2.y -vv);

            // In which direction is the extremity of the edge closer ?
            bool isDirection1 = distance1 < distance2;
            float distanceFinal = Math.Min(distance1, distance2);

            // Length of the edge.
            float edgeThickness = (distance1 + distance2);

            //中心的亮度是否小于当地的平均值？
            bool isLumaCenterSmaller = lumaCenter < lumaLocalAverage;

            //如果中心的亮度小于其邻域的亮度，则每端的增量亮度应为正（相同的变化）。
            bool correctVariation1 =(lumaEnd1 < 0.0f)!= isLumaCenterSmaller;
            bool correctVariation2 =(lumaEnd2 < 0.0f)!= isLumaCenterSmaller;

            //仅将结果保持在边缘较近侧的方向上。
            bool correctVariation = isDirection1 ?correctVariation1 :correctVariation2;


            // UV offset: read in the direction of the closest side of the edge.
            float pixelOffset = -distanceFinal / edgeThickness + 0.5f;

            //bool isLumaCenterSmaller = lumaCenter < lumaLocalAverage;

            //// If the luma at center is smaller than at its neighbour, the delta luma at each end should be positive (same variation).
            //// (in the direction of the closer side of the edge.)
            //bool correctVariation = ((isDirection1 ? lumaEnd1 : lumaEnd2) < 0.0f) != isLumaCenterSmaller;

            // If the luma variation is incorrect, do not offset.
            float finalOffset = correctVariation ? pixelOffset : 0.0f;

            float lumaAverage = (1.0f / 12.0f) * (2.0f * (lumaDownUp + lumaLeftRight) + lumaLeftCorners + lumaRightCorners);
            // Ratio of the delta between the global average and the center luma, over the luma range in the 3x3 neighborhood.
            float subPixelOffset1 = clamp(Math.Abs(lumaAverage - lumaCenter) / lumaRange);
            float subPixelOffset2 = (-2.0f * subPixelOffset1 + 3.0f) * subPixelOffset1 * subPixelOffset1;
            // Compute a sub-pixel offset based on this delta.
            float SUBPIXEL_QUALITY = 0.75f;
            float subPixelOffsetFinal = subPixelOffset2 * subPixelOffset2 * SUBPIXEL_QUALITY;

            // Pick the biggest of the two offsets.
            finalOffset = Math.Max(finalOffset, subPixelOffsetFinal);

            UV finalUv = new UV(uu,vv);
            if (isHorizontal)
            {
                finalUv.y += finalOffset * stepLength;
            }
            else
            {
                finalUv.x += finalOffset * stepLength;
            }

            // Read the color at the new UV coordinates, and use it.
            System.Drawing.Color finalColor = frameArray[(int)(System.Math.Round((finalUv.x * width), MidpointRounding.AwayFromZero)), (int)(System.Math.Round((finalUv.y * height), MidpointRounding.AwayFromZero))];
           
            frameBuff.SetPixel(u, v, new graphic_exercise.RenderData.Color(finalColor).TransFormToSystemColor());
        }

        float clamp(float x)
        {
            if(x<0)
            {
                return 0;
            }
            else if(x>1)
            {
                return 1;
            }
            return x;
        }
        #endregion

        #region 裁剪

        /// <summary>
        /// 近平面裁剪
        /// </summary>
        //private void clipTest_near(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    指向立方体内部
        //    Vector near_n = new Vector(0, 0, 1);
        //    float distance = -camera.near;
        //    插值因子
        //    float t = 0;
        //    点在法线上的投影
        //    float projectV1 = Vector.dot(near_n, v1.pos);
        //    float projectV2 = Vector.dot(near_n, v2.pos);
        //    float projectV3 = Vector.dot(near_n, v3.pos);
        //    点与点之间的距离
        //    float dv1v2 = Math.Abs(projectV1 - projectV2);
        //    float dv1v3 = Math.Abs(projectV1 - projectV3);
        //    float dv2v3 = Math.Abs(projectV2 - projectV3);
        //    点倒平面的距离
        //    float pv1 = Math.Abs(projectV1 - distance);
        //    float pv2 = Math.Abs(projectV2 - distance);
        //    float pv3 = Math.Abs(projectV3 - distance);
        //    t = pv2 / dv2v3;
        //    v1,v2,v3都在立方体内
        //    if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
        //    {
        //        不做任何处理
        //        drawTriangle2(v1, v2, v3);
        //        clipTest_far(v1, v2, v3);
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        temp2.pos.z = distance;
        //        temp2.pos.w = -distance;
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipTest_far(temp1, temp2, v2);
        //        clipTest_far(temp4, temp3, v3);


        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = -distance; ;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipTest_far(temp1, temp2, v3);
        //        clipTest_far(temp4, temp3, v1);
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = -distance; ;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipTest_far(temp1, temp2, v1);
        //        clipTest_far(temp4, temp3, v2);
        //    }

        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = -distance; ;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        画线或光栅化
        //        clipTest_far(temp1, temp2, v1);
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = -distance; ;
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        画线或光栅化
        //        clipTest_far(temp1, temp2, v2);
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = -distance; ;
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = -distance; ;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        画线或光栅化
        //        clipTest_far(temp1, temp2, v3);
        //    }
        //}

        private void clipTest_near(Vertex v1, Vertex v2, Vertex v3)
        {
            //指向立方体内部
            Vector near_n = new Vector(0, 0, 1);
            float distance = -1;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(near_n, v1.pos);
            float projectV2 = Vector.dot(near_n, v2.pos);
            float projectV3 = Vector.dot(near_n, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //点倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
            t = pv2 / dv2v3;
            //v1,v2,v3都在立方体内
            if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
            {
                //不做任何处理
                //drawTriangle2(v1, v2, v3);
                clipTest_far(v1, v2, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                temp2.pos.z = distance;
                temp2.pos.w = -distance;
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_far(temp1, temp2, v2);
                clipTest_far(temp4, temp3, v3);
                //drawTriangle2(temp1, temp2, v2);
                //drawTriangle2(temp4, temp3, v3);

            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v1, v2, t);
               

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = -distance; ;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_far(temp1, temp2, v3);
                clipTest_far(temp4, temp3, v1);
                //drawTriangle2(temp1, temp2, v3);
                //drawTriangle2(temp4, temp3, v1);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = -distance; ;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_far(temp1, temp2, v1);
                clipTest_far(temp4, temp3, v2);
                //drawTriangle2(temp1, temp2, v1);
                //drawTriangle2(temp4, temp3, v2);
            }

            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = -distance; ;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                clipTest_far(temp1, temp2, v1);
                //drawTriangle2(temp1, temp2, v1);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = -distance; ;
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                clipTest_far(temp1, temp2, v2);
                //drawTriangle2(temp1, temp2, v2);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = -distance; ;
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = -distance; ;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                clipTest_far(temp1, temp2, v3);
                //drawTriangle2(temp1, temp2, v3);
            }
        }

        ///// <summary>
        ///// 远剪裁平面
        ///// </summary>
        //private void clipTest_far(Vertex v1, Vertex v2, Vertex v3)
        //{

        //    //指向立方体内部
        //    Vector far_n = new Vector(0, 0, 1);
        //    float distance = camera.far;
        //    //插值因子
        //    float t = 0;
        //    //点在法线上的投影
        //    float projectV1 = Vector.dot(far_n, v1.pos);
        //    float projectV2 = Vector.dot(far_n, v2.pos);
        //    float projectV3 = Vector.dot(far_n, v3.pos);
        //    //点与点之间的距离
        //    float dv1v2 = Math.Abs(projectV1 - projectV2);
        //    float dv1v3 = Math.Abs(projectV1 - projectV3);
        //    float dv2v3 = Math.Abs(projectV2 - projectV3);
        //    //颠倒平面的距离
        //    float pv1 = Math.Abs(projectV1 - distance);
        //    float pv2 = Math.Abs(projectV2 - distance);
        //    float pv3 = Math.Abs(projectV3 - distance);
        //    //v1,v2,v3都在立方体内
        //    if (projectV1 < distance && projectV2 < distance && projectV3 < distance)
        //    {
        //        //不做任何处理
        //        drawTriangle2(v1, v2, v3);
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        drawTriangle2(temp1, temp2, v2);
        //        drawTriangle2(temp4, temp3, v3);
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        drawTriangle2(temp1, temp2, v3);
        //        drawTriangle2(temp4, temp3, v1);
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        drawTriangle2(temp1, temp2, v1);
        //        drawTriangle2(temp4, temp3, v2);
        //    }

        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        drawTriangle2(temp1, temp2, v1);
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        //画线或光栅化
        //        drawTriangle2(temp1, temp2, v2);
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        temp1.pos.z = distance; ;
        //        temp1.pos.w = distance; ;
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        temp2.pos.z = distance; ;
        //        temp2.pos.w = distance; ;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        drawTriangle2(temp1, temp2, v3);
        //    }
        //}

        private void clipTest_far(Vertex v1, Vertex v2, Vertex v3)
        {

            //指向立方体内部
            Vector far_n = new Vector(0, 0, 1);
            float distance = 1;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(far_n, v1.pos);
            float projectV2 = Vector.dot(far_n, v2.pos);
            float projectV3 = Vector.dot(far_n, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //颠倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
           
            //v1,v2,v3都在立方体内
            if (projectV1 < distance && projectV2 < distance && projectV3 < distance)
            {
                //不做任何处理
                clipTest_left(v1, v2, v3);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_left(temp1, temp2, v2);
                clipTest_left(temp4, temp3, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_left(temp1, temp2, v3);
                clipTest_left(temp4, temp3, v1);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_left(temp1, temp2, v1);
                clipTest_left(temp4, temp3, v2);
            }

            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                clipTest_left(temp1, temp2, v1);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                clipTest_left(temp1, temp2, v2);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                temp1.pos.z = distance; ;
                temp1.pos.w = distance; ;
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                temp2.pos.z = distance; ;
                temp2.pos.w = distance; ;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                clipTest_left(temp1, temp2, v3);
            }
        }

        private void clipTest_left(Vertex v1, Vertex v2, Vertex v3)
        {
            //指向立方体内部
            Vector left = new Vector(1, 0, 0);
            float distance = 0;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(left, v1.pos);
            float projectV2 = Vector.dot(left, v2.pos);
            float projectV3 = Vector.dot(left, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //点倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
            //v1,v2,v3都在立方体内
            if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
            {
                //不做任何处理
                clipTest_right(v1, v2, v3);
                //clipTest_far(v1, v2, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                //clipTest_far(temp1, temp2, v2);
                //clipTest_far(temp4, temp3, v3);
                clipTest_right(temp1, temp2, v2);
                clipTest_right(temp4, temp3, v3);

            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                //clipTest_far(temp1, temp2, v3);
                //clipTest_far(temp4, temp3, v1);
                clipTest_right(temp1, temp2, v3);
                clipTest_right(temp4, temp3, v1);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                //clipTest_far(temp1, temp2, v1);
                //clipTest_far(temp4, temp3, v2);
                clipTest_right(temp1, temp2, v1);
                clipTest_right(temp4, temp3, v2);
            }

            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                // clipTest_far(temp1, temp2, v1);
                clipTest_right(temp1, temp2, v1);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                //clipTest_far(temp1, temp2, v2);
                clipTest_right(temp1, temp2, v2);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                //clipTest_far(temp1, temp2, v3);
                clipTest_right(temp1, temp2, v3);
            }
        }


        private void clipTest_right(Vertex v1, Vertex v2, Vertex v3)
        {

            //指向立方体内部
            Vector right = new Vector(1, 0, 0);
            float distance = 800-1;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(right, v1.pos);
            float projectV2 = Vector.dot(right, v2.pos);
            float projectV3 = Vector.dot(right, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //颠倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
           
            //v1,v2,v3都在立方体内
            if (projectV1 < distance && projectV2 < distance && projectV3 < distance)
            {
                //不做任何处理
                clipTest_top(v1, v2, v3);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_top(temp1, temp2, v2);
                clipTest_top(temp4, temp3, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp1, v1, v2, t);

                //Console.WriteLine(v1.pos.x + "  " + v1.pos.y + "  " + v2.pos.x + "  " + v2.pos.y + "   " + temp1.pos.x + "  " + temp1.pos.y+"  v2在外");
                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_top(temp1, temp2, v3);
                clipTest_top(temp4, temp3, v1);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_top(temp1, temp2, v1);
                clipTest_top(temp4, temp3, v2);
            }

            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp1, v1, v2, t);
               
                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp2, v1, v3, t);
                //Console.WriteLine(v1.pos.x + "  " + v1.pos.y + "  " + v3.pos.x + "  " + v3.pos.y + "   " + temp2.pos.x + "  " + temp2.pos.y + "  v1在内");
                //画线或光栅化
                clipTest_top(temp1, temp2, v1);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                clipTest_top(temp1, temp2, v2);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = distance;
                temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = distance;
                temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                clipTest_top(temp1, temp2, v3);
            }
        }


        private void clipTest_top(Vertex v1, Vertex v2, Vertex v3)
        {
            //指向立方体内部
            Vector near_n = new Vector(0, 1, 0);
            float distance =1;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(near_n, v1.pos);
            float projectV2 = Vector.dot(near_n, v2.pos);
            float projectV3 = Vector.dot(near_n, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //点倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
            t = pv2 / dv2v3;
            //v1,v2,v3都在立方体内
            if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
            {
                //不做任何处理
                //drawTriangle2(v1, v2, v3);
                clipTest_bottom(v1, v2, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_bottom(temp1, temp2, v2);
                clipTest_bottom(temp4, temp3, v3);
                //drawTriangle2(temp1, temp2, v2);
                //drawTriangle2(temp4, temp3, v3);

            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v1, v2, t);


                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_bottom(temp1, temp2, v3);
                clipTest_bottom(temp4, temp3, v1);
                //drawTriangle2(temp1, temp2, v3);
                //drawTriangle2(temp4, temp3, v1);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                clipTest_bottom(temp1, temp2, v1);
                clipTest_bottom(temp4, temp3, v2);
                //drawTriangle2(temp1, temp2, v1);
                //drawTriangle2(temp4, temp3, v2);
            }

            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                clipTest_bottom(temp1, temp2, v1);
                //drawTriangle2(temp1, temp2, v1);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                clipTest_bottom(temp1, temp2, v2);
                //drawTriangle2(temp1, temp2, v2);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                clipTest_bottom(temp1, temp2, v3);
                //drawTriangle2(temp1, temp2, v3);
            }
        }

        private void clipTest_bottom(Vertex v1, Vertex v2, Vertex v3)
        {

            //指向立方体内部
            Vector far_n = new Vector(0, 1, 0);
            float distance = 600-1;
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(far_n, v1.pos);
            float projectV2 = Vector.dot(far_n, v2.pos);
            float projectV3 = Vector.dot(far_n, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //颠倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);

            //v1,v2,v3都在立方体内
            if (projectV1 < distance && projectV2 < distance && projectV3 < distance)
            {
                //不做任何处理
                drawTriangle2(v1, v2, v3);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v2, v1, t);

                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                drawTriangle2(temp1, temp2, v2);
                drawTriangle2(temp4, temp3, v3);
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                drawTriangle2(temp1, temp2, v3);
                drawTriangle2(temp4, temp3, v1);
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                drawTriangle2(temp1, temp2, v1);
                drawTriangle2(temp4, temp3, v2);
            }

            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                drawTriangle2(temp1, temp2, v1);
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
                temp2.pos.y = distance;
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                drawTriangle2(temp1, temp2, v2);
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
                temp1.pos.y = distance;
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
                temp2.pos.y = distance;

                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                drawTriangle2(temp1, temp2, v3);
            }
        }

        #endregion












    }
}

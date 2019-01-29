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
using System.Collections.Generic;

namespace graphic_exercise
{
    public partial class Form1 : Form
    {
        #region 变量
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
        Queue<OneTriangle> clipQueue = new Queue<OneTriangle>();//裁剪列表
        List<OneTriangle> rasterizationList = new List<OneTriangle>();//所要光栅化的三角形列表
        //屏幕宽高
        int width = 800+16;
        int height = 600+40;
        //UI线程
        SynchronizationContext _syncContext = null;
        System.Object myLock = new object();
        /// 显示console窗口
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
        #endregion

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
            //初始化UI线程
            _syncContext = SynchronizationContext.Current;
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
        Vertex p1;
        Vertex p2;
        Vertex p3;
        /// <summary>
        /// 主绘制方法
        /// </summary>
        private void draw(Matrix4x4 m, Matrix4x4 v, Matrix4x4 p)
        {
            //清空裁剪队列和绘制列表
            clipQueue.Clear();
            rasterizationList.Clear();
            for (int i = 0; i < triangles.vertexList.Count; i += 3)//遍历顶点索引数组
            {
                p1 = new Vertex();
                p2 = new Vertex();
                p3 = new Vertex();
                Vertex.Clone2(triangles.vertexList[i], p1);
                Vertex.Clone2(triangles.vertexList[i + 1], p2);
                Vertex.Clone2(triangles.vertexList[i + 2], p3);
                drawTriangle(p1,
                   p2,
                    p3
                    , m, v, p);              
            }
            //遍历产生的所有的三角形
            for(int i=0;i<rasterizationList.Count;i++)
            {
                //画线或者光栅化
                drawTriangle2(rasterizationList[i].v1, rasterizationList[i].v2,rasterizationList[i].v3);
            }
        }
        /// <summary>
        /// 绘制三角形
        /// </summary>
        private void drawTriangle(Vertex v1, Vertex v2, Vertex v3, Matrix4x4 m, Matrix4x4 v, Matrix4x4 p)
        {
            //本地到世界坐标空间
            objectToWorld(m, v1);
            objectToWorld(m, v2);
            objectToWorld(m, v3);
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
            //简单剔除
            if (exclude(v1)==false&& exclude(v2)==false&& exclude(v3)==false)
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
                clip(new OneTriangle(v1, v2, v3));
            }
            else
            {
                //drawTriangle2(v1, v2, v3);
                rasterizationList.Add(new OneTriangle(v1, v2, v3));
            }
        }
        /// <summary>
        /// 画线还是光栅化
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
            }
        }
        /// <summary>
        /// 本地到世界坐标系
        /// </summary>
        private void cameraToProject(Matrix4x4 p, Vertex v)
        {
            v.pos = p * v.pos;
        }
        /// <summary>
        /// 世界到摄像机坐标系
        /// </summary>
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
        private bool exclude(Vertex v)
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
        #region 画线
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
                    w = 1 / w;
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
                    w = 1 / w;
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
        #endregion
     
        #region 光栅化1.0
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
            }
        }
        private void drawTriangleBottom(Vertex v1, Vertex v2, Vertex v3)
        {
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

        /// <summary>
        /// 填充
        /// </summary>
        /// <param name="left">左顶点</param>
        /// <param name="right">右顶点</param>
        /// <param name="yIndex">y值</param>
        private void scanLine(Vertex left, Vertex right, int yIndex,int xl,int xr)
        {
            int x = xl;
            int dx = xr - xl;
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
                    }
                }
                x += stepx;
            }
        }

        #region 纹理
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
                return System.Drawing.Color.Black;
                
            }
            return textureArray[i, imgHeight - 1 - j];
        }
        #endregion

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
            //顶点颜色
            v1.lightColor = ambient + diffuse + specular;
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
                lock(myLock)
                {
                    triangleNum = 0;
                    //清除颜色缓存
                    clearBuff();
                    //求mvp矩阵
                    m = Matrix4x4.translate(0, 3, tranZ) * Matrix4x4.rotateY(rotY) * Matrix4x4.rotateX(rotX) * Matrix4x4.rotateZ(rotZ);
                    v = Matrix4x4.view(camera.look, camera.up, camera.pos);
                    p = Matrix4x4.project(camera.fov, camera.aspect, camera.near, camera.far);
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
                    _syncContext.Post(SetLabelText, "帧率：" + (int)Math.Ceiling(1000 / timeSpan.TotalMilliseconds));//子线程中通过UI线程上下文更新UI 
                    lastTime = now;
                }
            }
        }
        private void SetLabelText(object text)
        {
            this.Text = text.ToString();
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
        //xz轴转动角度
        float total = 0;
        float maxAngle = (float)(Math.PI / 3/3*4);
        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        public void mouseDown(object sender, MouseEventArgs e)
        {
            canMove = true;
            //if(e.Button==MouseButtons.Left)
            //{
            //    canMove = true;
            //}
         
        }
        /// <summary>
        /// 鼠标移动
        /// </summary>
        public void mouseMove(object sender, MouseEventArgs e)
        {
            float dx = Util.Util.distance(e.X, 0, lastPoint.X, 0);
            float dy = Util.Util.distance(e.Y, 0, lastPoint.Y, 0);
            if (dx>=dy&&dx>= 1&& canMove)
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
            else if (dy>dx&&dy >= 1&& canMove)
            {
                if (e.Location.Y - lastPoint.Y > 0)
                {
                    rx = angle;
                }
                else
                {
                    rx = -angle;
                }

                if (total >-maxAngle&&total<maxAngle)
                {
                    total += rx;
                }
                else
                {
                    if(total<=-maxAngle&&rx<0)
                    {
                        rx = 0;
                    }
                    else if(total>=maxAngle&&rx>0)
                    {
                        rx = 0;
                    }
                    else
                    {
                        total += rx;
                    }
                }
            }
            if (canMove)
            {

                Vector N = camera.look - camera.pos;
                N.normalize();
                Vector U = Vector.cross(camera.up, N);
                U.normalize();
                pos = camera.look-camera.pos;
                pos = Matrix4x4.ArbitraryAxis(Vector.opposite(U),rx) * Matrix4x4.rotateY(ry) *pos;
                camera.look = pos+camera.pos;

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
            }
            else if(keyData == Keys.S)
            {
                camera.pos -= N * 0.1f;
            }
            else if (keyData == Keys.A)
            {
                camera.pos -= U * 0.1f;
                camera.look -= U * 0.1f;
            }
            else if (keyData == Keys.D)
            {
                camera.pos += U * 0.1f;
                camera.look+= U * 0.1f;
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
            lock(myLock)
            {
                if (lightMode == LightMode.ON)
                {
                    lightMode = LightMode.OFF;
                    BLightSwitch.Text = "开灯";
                }
                else if (lightMode == LightMode.OFF)
                {
                    lightMode = LightMode.ON;
                    BLightSwitch.Text = "关灯";
                }
            }
        }

        public void b_Render(object sender, EventArgs e)
        {
            lock(myLock)
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
        }

        public void b_Cull(object sender, EventArgs e)
        {
            lock(myLock)
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
        }

        public void b_Wu(object sender, EventArgs e)
        {
            lock(myLock)
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
        }

        public void b_Clip(object sender, EventArgs e)
        {
            lock (myLock)
            {
                if (clipTest == ClipTest.ON)
                {
                    clipTest = ClipTest.OFF;
                    BClipTest.Text = "剪裁";
                }
                else if (clipTest == ClipTest.OFF)
                {
                    clipTest = ClipTest.ON;
                    BClipTest.Text = "不剪裁";
                }
            }
        }

        public void b_TextColor(object sender, EventArgs e)
        {
            lock(myLock)
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
        }


        #endregion

        #region 裁剪-- 一个方法，可以用于裁剪六个面

        Vector[] dotVectors =//顶点和该向量插值，判断顶点到平面的直线距离
          {
                new Vector(0,0,1),//前
                new Vector(0,0,-1),//后
                new Vector(1,0,0),//左
                new Vector(-1,0,0),//右
                new Vector(0,1,0),//上
                new Vector(0,-1,0)//下
        };
        float[] distance = new float[] { -1, -1, 0f, -799, 0f, -599 };//各个平面到原点“距离”
        bool[] isfront = { true, false, false, false, false, false };//是否是近平面剪裁
        //裁剪方法
        private void clip(OneTriangle ot)
        {
            bool isClip = false;
            //加入一个三角形
            clipQueue.Enqueue(ot);
            OneTriangle triangle;
            while (clipQueue.Count > 0)
            {
                //取出
                triangle = clipQueue.Dequeue();
                for (int i = 0; i < distance.Length; i++)
                {
                    if (isClip == false)
                    {
                        isClip = clip_Test(triangle.v1, triangle.v2, triangle.v3, dotVectors[i], distance[i], isfront[i]);
                    }
                    else
                    {
                        break;
                    }
                }
                if (isClip == false)//不需要裁剪
                {
                    rasterizationList.Add(new OneTriangle(triangle.v1, triangle.v2, triangle.v3));
                }
                isClip = false;
                // Console.WriteLine(clipQueue.Count);
            }
        }
        float errorInterval = 0.01f;//误差区间
        /// <summary>
        /// 消除插值误差，xy方向
        /// </summary>
        ///   /// <param name="n1">插值后顶点x/y值</param>
        /// <param name="n2">上/下/左/右平面位置</param>
        /// <returns>消除误差后顶点x/y值</returns>
        float eliminateErrors(float n1,float n2)
        {
            if(Math.Abs(n1+n2)<errorInterval)
            {
                n1 = -n2;
            }
            return n1;
        }
       /// <summary>
       /// 消除误差
       /// </summary>
       /// <param name="n1">插值后顶点z值</param>
       /// <param name="n2">远/近平面位置</param>
       /// <param name="isfront">是否时近剪裁平面剪裁</param>
       /// <returns>消除误差后顶点z值</returns>
        float eliminateErrors2(float n1, float n2,bool isfront)
        {
            if (isfront)//近平面
            {
                if (Math.Abs(n1 - n2) < errorInterval)
                {
                    n1 = n2;
                }
            }
            else
            {
                if (Math.Abs(n1 + n2) < errorInterval)
                {
                    n1 = -n2;
                }
            }
            return n1;
        }
        /// <summary>
        /// 裁剪主方法
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">顶点</param>
        /// <param name="v3">顶点</param>
        /// <param name="dotVector">点积向量，求顶点到平面的距离</param>
        /// <param name="distance">平面“位置”</param>
        /// <param name="isfront">是否是近平面剪裁</param>
        /// <returns>是否被裁剪</returns>
        private bool clip_Test(Vertex v1, Vertex v2, Vertex v3,Vector dotVector,float distance,bool isfront)
        {
            //插值因子
            float t = 0;
            //点在法线上的投影
            float projectV1 = Vector.dot(dotVector, v1.pos);
            float projectV2 = Vector.dot(dotVector, v2.pos);
            float projectV3 = Vector.dot(dotVector, v3.pos);
            //点与点之间的距离
            float dv1v2 = Math.Abs(projectV1 - projectV2);
            float dv1v3 = Math.Abs(projectV1 - projectV3);
            float dv2v3 = Math.Abs(projectV2 - projectV3);
            //点倒平面的距离
            float pv1 = Math.Abs(projectV1 - distance);
            float pv2 = Math.Abs(projectV2 - distance);
            float pv3 = Math.Abs(projectV3 - distance);
            
            //v1,v2,v3都在立方体内
            if ( projectV1 > distance && projectV2 > distance && projectV3 > distance)
            {
                //不做任何处理
                return false;
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
            {
                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x =eliminateErrors( Util.Util.lerp(v2.pos.x, v1.pos.x, t),distance);
                temp2.pos.y =eliminateErrors( Util.Util.lerp(v2.pos.y, v1.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v2.pos.z, v1.pos.z, t),distance,isfront);
              
                Util.Util.lerp(temp2, v2, v1, t);
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v3.pos.x, v1.pos.x, t), distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v3.pos.y, v1.pos.y, t), distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v3.pos.z, v1.pos.z, t), distance, isfront);
                Util.Util.lerp(temp1, v3, v1, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v2, temp3);
                Vertex.Clone(temp1, temp4);
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
                clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
                return true;
            }
            else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v1.pos.x, v2.pos.x, t),distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v1.pos.y, v2.pos.y, t),distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v1.pos.z, v2.pos.z, t), distance, isfront);
                Util.Util.lerp(temp1, v1, v2, t);


                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = eliminateErrors(Util.Util.lerp(v3.pos.x, v2.pos.x, t), distance);
                temp2.pos.y = eliminateErrors(Util.Util.lerp(v3.pos.y, v2.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v3.pos.z, v2.pos.z, t), distance, isfront);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v3, temp3);
                Vertex.Clone(temp1, temp4);
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
                clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
                return true;
            }
            else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v2.pos.x, v3.pos.x, t),distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v2.pos.y, v3.pos.y, t),distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v2.pos.z, v3.pos.z, t), distance, isfront);
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = eliminateErrors(Util.Util.lerp(v1.pos.x, v3.pos.x, t),distance);
                temp2.pos.y = eliminateErrors(Util.Util.lerp(v1.pos.y, v3.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v1.pos.z, v3.pos.z, t), distance, isfront);
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                Vertex temp3 = new Vertex();
                Vertex temp4 = new Vertex();
                Vertex.Clone(v1, temp3);
                Vertex.Clone(temp1, temp4);
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
                clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
                return true;
            }

            else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
            {
                Vertex temp1 = new Vertex();
                t = pv1 / dv1v2;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v1.pos.x, v2.pos.x, t), distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v1.pos.y, v2.pos.y, t),distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v1.pos.z, v2.pos.z, t), distance, isfront);
                Util.Util.lerp(temp1, v1, v2, t);

                Vertex temp2 = new Vertex();
                t = pv1 / dv1v3;
                temp2.pos.x = eliminateErrors(Util.Util.lerp(v1.pos.x, v3.pos.x, t), distance);
                temp2.pos.y = eliminateErrors(Util.Util.lerp(v1.pos.y, v3.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v1.pos.z, v3.pos.z, t), distance, isfront);
                Util.Util.lerp(temp2, v1, v3, t);
                //画线或光栅化
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
                return true;
            }
            else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
            {
                Vertex temp1 = new Vertex();
                t = pv2 / dv2v3;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v2.pos.x, v3.pos.x, t),distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v2.pos.y, v3.pos.y, t),distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v2.pos.z, v3.pos.z, t), distance, isfront) ;
                Util.Util.lerp(temp1, v2, v3, t);

                Vertex temp2 = new Vertex();
                t = pv2 / dv1v2;
                temp2.pos.x = eliminateErrors(Util.Util.lerp(v2.pos.x, v1.pos.x, t),distance);
                temp2.pos.y = eliminateErrors(Util.Util.lerp(v2.pos.y, v1.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v2.pos.z, v1.pos.z, t), distance, isfront);
                Util.Util.lerp(temp2, v2, v1, t);
                //画线或光栅化
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
                return true;
            }
            else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
            {
                Vertex temp1 = new Vertex();
                t = pv3 / dv1v3;
                temp1.pos.x = eliminateErrors(Util.Util.lerp(v3.pos.x, v1.pos.x, t), distance);
                temp1.pos.y = eliminateErrors(Util.Util.lerp(v3.pos.y, v1.pos.y, t),distance);
                temp1.pos.z = eliminateErrors2(Util.Util.lerp(v3.pos.z, v1.pos.z, t), distance, isfront);
                Util.Util.lerp(temp1, v3, v1, t);

                Vertex temp2 = new Vertex();
                t = pv3 / dv2v3;
                temp2.pos.x = eliminateErrors(Util.Util.lerp(v3.pos.x, v2.pos.x, t), distance);
                temp2.pos.y = eliminateErrors(Util.Util.lerp(v3.pos.y, v2.pos.y, t),distance);
                temp2.pos.z = eliminateErrors2(Util.Util.lerp(v3.pos.z, v2.pos.z, t), distance, isfront);
                Util.Util.lerp(temp2, v3, v2, t);
                //画线或光栅化
                clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
                return true;
            }
            return false;
        }

        #endregion

        #region 裁剪---一个面一个裁剪方法，比较麻烦

        //private void clip(OneTriangle ot)
        //{
        //    bool isClip = false;
        //    //加入一个三角形
        //    clipQueue.Enqueue(ot);
        //    OneTriangle triangle;
        //    while(clipQueue.Count>0)
        //    {
        //        //取出
        //        triangle = clipQueue.Dequeue();
        //        ///裁剪测试
        //        if(isClip==false)//前
        //        {
        //            isClip=clipTest_front(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if (isClip == false)//后
        //        {
        //            isClip = clipTest_back(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if (isClip == false)//左
        //        {
        //            isClip = clipTest_left(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if (isClip == false)//右
        //        {
        //            isClip = clipTest_right(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if (isClip == false)//上
        //        {
        //            isClip = clipTest_top(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if (isClip == false)//下
        //        {
        //            isClip = clipTest_bottom(triangle.v1, triangle.v2, triangle.v3);
        //        }
        //        if(isClip==false)//不需要裁剪
        //        {
        //            rasterizationList.Add(new OneTriangle(triangle.v1, triangle.v2, triangle.v3));
        //        }
        //        isClip = false;
        //    }
        //}

        //private bool clipTest_front(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    //指向立方体内部
        //    Vector near_n = new Vector(0, 0, 1);
        //    float distance = -1;
        //    //插值因子
        //    float t = 0;
        //    //点在法线上的投影
        //    float projectV1 = Vector.dot(near_n, v1.pos);
        //    float projectV2 = Vector.dot(near_n, v2.pos);
        //    float projectV3 = Vector.dot(near_n, v3.pos);
        //    //点与点之间的距离
        //    float dv1v2 = Math.Abs(projectV1 - projectV2);
        //    float dv1v3 = Math.Abs(projectV1 - projectV3);
        //    float dv2v3 = Math.Abs(projectV2 - projectV3);
        //    //点倒平面的距离
        //    float pv1 = Math.Abs(projectV1 - distance);
        //    float pv2 = Math.Abs(projectV2 - distance);
        //    float pv3 = Math.Abs(projectV3 - distance);
        //    t = pv2 / dv2v3;
        //    //v1,v2,v3都在立方体内
        //    if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
        //    {
        //        //不做任何处理
        //        return false;
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
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;
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
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
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
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
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
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
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
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
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
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}

        //private bool clipTest_back(Vertex v1, Vertex v2, Vertex v3)
        //{

        //    //指向立方体内部
        //    Vector far_n = new Vector(0, 0, 1);
        //    float distance = 1;
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
        //        return false;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
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
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}

        //private bool clipTest_left(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    //指向立方体内部
        //    Vector left = new Vector(1, 0, 0);
        //    float distance = 0;
        //    //插值因子
        //    float t = 0;
        //    //点在法线上的投影
        //    float projectV1 = Vector.dot(left, v1.pos);
        //    float projectV2 = Vector.dot(left, v2.pos);
        //    float projectV3 = Vector.dot(left, v3.pos);
        //    //点与点之间的距离
        //    float dv1v2 = Math.Abs(projectV1 - projectV2);
        //    float dv1v3 = Math.Abs(projectV1 - projectV3);
        //    float dv2v3 = Math.Abs(projectV2 - projectV3);
        //    //点倒平面的距离
        //    float pv1 = Math.Abs(projectV1 - distance);
        //    float pv2 = Math.Abs(projectV2 - distance);
        //    float pv3 = Math.Abs(projectV3 - distance);
        //    //v1,v2,v3都在立方体内
        //    if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
        //    {
        //        //不做任何处理
        //        return false;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;

        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
        //    }

        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}

        //private bool clipTest_right(Vertex v1, Vertex v2, Vertex v3)
        //{

        //    //指向立方体内部
        //    Vector right = new Vector(1, 0, 0);
        //    float distance = 800-1;
        //    //插值因子
        //    float t = 0;
        //    //点在法线上的投影
        //    float projectV1 = Vector.dot(right, v1.pos);
        //    float projectV2 = Vector.dot(right, v2.pos);
        //    float projectV3 = Vector.dot(right, v3.pos);
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
        //        return false;
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
        //    }

        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v1.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v1.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v2.pos.y, v3.pos.y, t);
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v2.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = distance;
        //        temp1.pos.y = Util.Util.lerp(v3.pos.y, v1.pos.y, t);
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = distance;
        //        temp2.pos.y = Util.Util.lerp(v3.pos.y, v2.pos.y, t);
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}

        //private bool clipTest_top(Vertex v1, Vertex v2, Vertex v3)
        //{
        //    //指向立方体内部
        //    Vector near_n = new Vector(0, 1, 0);
        //    float distance =1;
        //    //插值因子
        //    float t = 0;
        //    //点在法线上的投影
        //    float projectV1 = Vector.dot(near_n, v1.pos);
        //    float projectV2 = Vector.dot(near_n, v2.pos);
        //    float projectV3 = Vector.dot(near_n, v3.pos);
        //    //点与点之间的距离
        //    float dv1v2 = Math.Abs(projectV1 - projectV2);
        //    float dv1v3 = Math.Abs(projectV1 - projectV3);
        //    float dv2v3 = Math.Abs(projectV2 - projectV3);
        //    //点倒平面的距离
        //    float pv1 = Math.Abs(projectV1 - distance);
        //    float pv2 = Math.Abs(projectV2 - distance);
        //    float pv3 = Math.Abs(projectV3 - distance);
        //    t = pv2 / dv2v3;
        //    //v1,v2,v3都在立方体内
        //    if (projectV1 > distance && projectV2 > distance && projectV3 > distance)
        //    {
        //        //不做任何处理
        //        return false;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v1, v2, t);


        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
        //    }

        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}

        //private bool clipTest_bottom(Vertex v1, Vertex v2, Vertex v3)
        //{

        //    //指向立方体内部
        //    Vector far_n = new Vector(0, 1, 0);
        //    float distance = 600-1;
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
        //        return false;
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 < distance)//只有v1在外
        //    {
        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v2, v1, t);

        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v3, v1, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v2, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v3));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 > distance && projectV3 < distance)//只有v2在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v3, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v1));
        //        return true;
        //    }
        //    else if (projectV1 < distance && projectV2 < distance && projectV3 > distance)//只有v3在外
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        Vertex temp3 = new Vertex();
        //        Vertex temp4 = new Vertex();
        //        Vertex.Clone(v1, temp3);
        //        Vertex.Clone(temp1, temp4);
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        clipQueue.Enqueue(new OneTriangle(temp4, temp3, v2));
        //        return true;
        //    }

        //    else if (projectV1 < distance && projectV2 > distance && projectV3 > distance)//只有v1在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv1 / dv1v2;
        //        temp1.pos.x = Util.Util.lerp(v1.pos.x, v2.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v1, v2, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv1 / dv1v3;
        //        temp2.pos.x = Util.Util.lerp(v1.pos.x, v3.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v1, v3, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v1));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 < distance && projectV3 > distance)//只有v2在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv2 / dv2v3;
        //        temp1.pos.x = Util.Util.lerp(v2.pos.x, v3.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v2, v3, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv2 / dv1v2;
        //        temp2.pos.x = Util.Util.lerp(v2.pos.x, v1.pos.x, t);
        //        temp2.pos.y = distance;
        //        Util.Util.lerp(temp2, v2, v1, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v2));
        //        return true;
        //    }
        //    else if (projectV1 > distance && projectV2 > distance && projectV3 < distance)//只有v3在内
        //    {
        //        Vertex temp1 = new Vertex();
        //        t = pv3 / dv1v3;
        //        temp1.pos.x = Util.Util.lerp(v3.pos.x, v1.pos.x, t);
        //        temp1.pos.y = distance;
        //        Util.Util.lerp(temp1, v3, v1, t);

        //        Vertex temp2 = new Vertex();
        //        t = pv3 / dv2v3;
        //        temp2.pos.x = Util.Util.lerp(v3.pos.x, v2.pos.x, t);
        //        temp2.pos.y = distance;

        //        Util.Util.lerp(temp2, v3, v2, t);
        //        //画线或光栅化
        //        clipQueue.Enqueue(new OneTriangle(temp1, temp2, v3));
        //        return true;
        //    }
        //    return false;
        //}


        #endregion











    }
}

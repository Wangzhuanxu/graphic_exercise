using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    public class Color
    {
        private float _r;
        private float _b;
        private float _g;
        private float _a;
        /// <summary>
        /// 设置rgba四个值
        /// </summary>
        public float r
        {
            get { return Range(_r, 0, 1); }
            set { _r = Range(value, 0, 1); }
        }
        public float g
        {
            get { return Range(_g, 0, 1); }
            set { _g = value; }
        }
        public float b
        {
            get { return Range(_b, 0, 1); }
            set { _b = value; }
        }
        public float a
        {
            get { return Range(_a, 0, 1); }
            set { _a = value; }
        }
        /// <summary>
        /// 初始构造器
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public Color(float r, float g, float b, float a)
        {
            this._r = Range(r, 0, 1);
            this._g = Range(g, 0, 1);
            this._b = Range(b, 0, 1);
            this._a = Range(a, 0, 1);
        }

        public Color(System.Drawing.Color c)
        {
            this._r = Range((float)c.R / 255, 0, 1);
            this._g = Range((float)c.G / 255, 0, 1);
            this._b = Range((float)c.B / 255, 0, 1);
            this._a = Range((float)c.A / 255, 0, 1);
        }
        public Color()
        {

        }
        /// <summary>
        /// 转换为系统的color
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Color TransFormToSystemColor()
        {
            float r = this.r * 255;
            float g = this.g * 255;
            float b = this.b * 255;
            float a = this.a * 255;
            return System.Drawing.Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }
        /// <summary>
        /// 颜色乘法，用于颜色混合，模拟光照
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color operator *(Color a, Color b)
        {
            Color c = new Color();
            c.r = a.r * b.r;
            c.g = a.g * b.g;
            c.b = a.b * b.b;
            c.a = a.a * b.a;
            return c;
        }
        /// <summary>
        /// 扩大颜色强度
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color operator *(Color a, float b)
        {
            Color c = new Color();
            c.r = a.r * b;
            c.g = a.g * b;
            c.b = a.b * b;
            c.a = a.a * b;
            return c;
        }
        /// <summary>
        /// 颜色叠加
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color operator +(Color a, Color b)
        {
            Color c = new Color();
            c.r = a.r + b.r;
            c.g = a.g + b.g;
            c.b = a.b + b.b;
            c.a = a.a + b.a;
            return c;
        }


        /// <summary>
        /// 限制颜色范围
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public float Range(float v, float min, float max)
        {
            if (v < min)
            {
                return min;
            }
            else if (v > max)
            {
                return max;
            }
            return v;
        }
        /// <summary>
        /// 绿色
        /// </summary>
        public static Color Green
        {
            get
            {
                return new Color(0, 1, 0, 1);
            }
        }
        /// <summary>
        /// 红色
        /// </summary>
        public static Color Red
        {
            get
            {
                return new Color(1, 0, 0, 1);
            }
        }
        /// <summary>
        /// 蓝色
        /// </summary>
        public static Color Blue
        {
            get
            {
                return new Color(0, 0, 1, 1);
            }
        }
        /// <summary>
        /// 白色
        /// </summary>
        public static Color White
        {
            get
            {
                return new Color(1, 1, 1, 1);
            }

        }
        /// <summary>
        /// 黑色
        /// </summary>
        public static Color Black
        {
            get
            {
                return new Color(0, 0, 0, 1);
            }
        }

        //颜色克隆
        public  Color clone
        {
            get
            {
                return new Color(r, g, b, a);
            }
        }
    }
}

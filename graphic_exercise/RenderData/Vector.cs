using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    /// <summary>
    /// 列向量
    /// </summary>
    class Vector
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector()
        {

        }

        /// <summary>
        /// 初始化点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        public Vector(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        /// <summary>
        /// 初始化向量
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = 0;
        }
        /// <summary>
        /// 获取向量长度
        /// </summary>
        /// <returns></returns>
        public float getLength()
        {
            float l = x * x + y * y + z * z;
            return (float)Math.Sqrt(l);
        }
        /// <summary>
        /// 规格化
        /// </summary>
        /// <returns>返回向量本身</returns>
        public Vector normalize()
        {
            float l = getLength();
            if (l != 0)
            {
                x *= 1 / l;
                y *= 1 / l;
                z *= 1 / l;
            }
            return this;
        }
        /// <summary>
        /// 向量相减
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector operator -(Vector v1,Vector v2)
        {
            return new Vector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        /// <summary>
        /// 取反
        /// </summary>
        /// <param name="v">数值</param>
        /// <returns></returns>
        public static  Vector opposite(Vector v)
        {
            return new Vector(-v.x, -v.y, -v.z, v. w);
        }

        /// <summary>
        /// 向量相加
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        /// <summary>
        /// 向量乘法
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Vector operator *(Vector v1,float a)
        {
            return new Vector(v1.x * a, v1.y * a, v1.z * a);
        }

        /// <summary>
        /// 点乘
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static float dot(Vector lhs, Vector rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }
        /// <summary>
        /// 叉乘
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        /// 公式： ( x1 , y1 , z1 ) X ( x2 , y2 , z2 ) =( y1z2 - z1y2 , z1x2 - x1z2 , x1y2 - y1x2 )
        public static Vector cross(Vector lhs, Vector rhs)
        {
            float x = lhs.y * rhs.z - lhs.z * rhs.y;
            float y = lhs.z * rhs.x - lhs.x * rhs.z;
            float z = lhs.x * rhs.y - lhs.y * rhs.x;
            return new Vector(x, y, z);
        }
        //克隆
        public  Vector clone()
        {
            return new Vector(x, y, z, w);
        }

        public override string ToString()
        {
            return x+"  "+y+"   "+z;
        }

    }
}

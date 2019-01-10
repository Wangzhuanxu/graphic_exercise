using graphic_exercise.RenderData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.Util
{
    class Util
    {
        /// <summary>
        ///越来越接近才c2
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static graphic_exercise.RenderData.Color lerp(graphic_exercise.RenderData.Color c1, graphic_exercise.RenderData.Color c2,float t)
        {
            if(t<0)
            {
                t = 0;
            }
            else if(t>1)
            {
                t = 1;
            }
            Color c = new Color();
            c.r = t * c2.r + (1 - t) * c1.r;
            c.g = t * c2.g + (1 - t) * c1.g;
            c.b = t * c2.b + (1 - t) * c1.b;
            c.a = t * c2.a + (1 - t) * c1.a;
            return c;
        }
        /// <summary>
        /// 无线接近b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float lerp(float a,float b,float t)
        {
            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }
            return b * t + (1 - t) * a;
        }
        /// <summary>
        /// 对顶点中的颜色，uv坐标，进行插值
        /// </summary>
        /// <param name="v"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="t"></param>
        public  static void lerp(Vertex v,Vertex v1,Vertex v2,float t)
        {
            //颜色插值
            v.color = lerp(v1.color, v2.color, t);
            //uv插值
            v.uv[0] = lerp(v1.uv[0], v2.uv[0], t);
            v.uv[1] = lerp(v1.uv[1], v2.uv[1], t);
            //深度值插值
            v.depth = lerp(v1.depth, v2.depth, t);
            //光照颜色插值
            v.lightColor =lerp(v1.lightColor, v2.lightColor, t);
            //插值矫正系数
            v.onePerZ = lerp(v1.onePerZ, v2.onePerZ, t);
        }
    }
}

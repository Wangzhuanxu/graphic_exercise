using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    class Vertex
    {
        //顶点位置
        public Vector pos;
        //法线
        public Vector normal;
        //uv坐标
        public float[] uv;
        /// <summary>
        /// 顶点颜色
        /// </summary>
        public Color color;
        /// <summary>
        /// 顶点深度值，用1/z表示
        /// </summary>
        public float depth;

        /// <summary>
        /// 逐顶点光照
        /// </summary>
        public Color lightColor;
        /// <summary>
        /// 选用的材质
        /// </summary>
        public Material material;
        
        //初始化各项内容
        public Vertex(Vector pos,Vector normal,float uvx,float uvy,Color color)
        {
            this.pos = pos;
            this.normal = normal;
            this.uv = new float[2];
            uv[0] = uvx;
            uv[1] = uvy;
            this.color=color;
        }

        public Vertex()
        {
            this.pos = new Vector();
            this.color = new Color();
            this.uv = new float[2];
            this.normal = new Vector();
        }

        /// <summary>
        /// 克隆点
        /// </summary>
        /// <returns></returns>
        public Vertex clone()
        {
            return new Vertex(pos, normal, uv[0], uv[1],color);
        }

        public static void Clone(Vertex v1,Vertex v2)
        {
            v2.pos.x = v1.pos.x;
            v2.pos.y = v1.pos.y;
            v2.pos.z = v1.pos.z;
            v2.normal.x = v1.normal.x;
            v2.normal.y = v1.normal.y;
            v2.normal.z = v1.normal.z;
            v2.uv[0] = v1.uv[0];
            v2.uv[1] = v1.uv[1];
            v2.color.r = v1.color.r;
            v2.color.g = v1.color.g;
            v2.color.b = v1.color.b;
            v2.color.a = v1.color.a;
        }
    }
}

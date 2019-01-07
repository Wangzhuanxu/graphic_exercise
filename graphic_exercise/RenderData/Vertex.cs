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
    }
}

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
        
        //初始化各项内容
        public Vertex(Vector pos,Vector normal,float uvx,float uvy)
        {
            this.pos = pos;
            this.normal = normal;
            this.uv = new float[2];
            uv[0] = uvx;
            uv[1] = uvy;
        }

        public Vertex clone()
        {
            return new Vertex(pos, normal, uv[0], uv[1]);
        }
    }
}

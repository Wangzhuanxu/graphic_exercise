using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{   
    /// <summary>
    /// 单个三角形类
    /// </summary>
    class OneTriangle
    {
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;
        public int startIndex;//从哪个平面开始裁剪，默认从近平面
        public OneTriangle(Vertex v1,Vertex v2,Vertex v3,int startIndex)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.startIndex = startIndex;
        }
        public OneTriangle(Vertex v1, Vertex v2, Vertex v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.startIndex = 0;
        }
    }
}

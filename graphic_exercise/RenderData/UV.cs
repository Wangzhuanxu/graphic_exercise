using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    /// <summary>
    /// uv坐标
    /// </summary>
    class UV
    {
        public float x;
        public float y;
        public UV(float x,float y)
        {
            this.x = x;
            this.y = y;
        }
        public UV()
        {

        }

        public static UV operator- (UV uv1,UV uv2)
        {
            return new UV(uv1.x - uv2.x, uv1.y - uv2.y);
        }

        public static UV operator +(UV uv1, UV uv2)
        {
            return new UV(uv1.x + uv2.x, uv1.y + uv2.y);
        }

        public static UV operator *(UV uv1, float x)
        {
            return new UV(uv1.x *x, uv1.y *x);
        }
    }
}

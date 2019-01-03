using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    class Camera
    {
        public Vector look;//看的位置
        public Vector up;//up向量
        public Vector pos;//摄像机位置

        public float fov;//观察角度

        public float aspect;//宽高比

        public float near;//近平面
        public float far;//远平面

        public Camera(Vector look,Vector up,Vector pos,float fov,float aspect,float near,float far)
        {
            this.look = look;
            this.up = up;
            this.pos = pos;
            this.fov = fov;
            this.aspect = aspect;
            this.near = near;
            this.far = far;
        }
    }
}

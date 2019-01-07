using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    class Light
    {
        /// <summary>
        /// 灯光位置
        /// </summary>
        public Vector WorldSpaceLightPos;
        /// <summary>
        /// 灯光颜色
        /// </summary>
        public Color LightColor;

        public Light(Vector WorldSpaceLightPos, Color LightColor)
        {
            this.WorldSpaceLightPos = WorldSpaceLightPos;
            this.LightColor = LightColor;
        }
    }
}

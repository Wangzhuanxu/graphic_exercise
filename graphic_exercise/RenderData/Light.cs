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
        /// 灯光颜色,也就是光照强度
        /// </summary>
        public Color LightColor;

        public Light(Vector WorldSpaceLightPos, Color LightColor)
        {
            this.WorldSpaceLightPos = WorldSpaceLightPos;
            this.LightColor = LightColor;
        }
    }
    /// <summary>
    /// 是否启用光照
    /// </summary>
    enum LightMode
    {
        ON,
        OFF
    }
    /// <summary>
    /// 绘制模式
    /// </summary>
    enum RenderMode
    {
        Wireframe,//线框
        Entity//实体
    }
    /// <summary>
    /// 背面剪裁
    /// </summary>
    enum FaceCullMode
    {
        ON,
        OFF
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    class Material
    {
        /// <summary>
        /// 环境光颜色
        /// </summary>
        public Color ambient; 
        /// <summary>
        /// 漫反射颜色
        /// </summary>
        public Color diffuse;
        /// <summary>
        /// 镜面反射颜色
        /// </summary>
        public Color specular;
        /// <summary>
        /// 光泽度
        /// </summary>
        public float gloss;
        
        public Material()
        {

        }

        public Material(Color ambient, Color diffuse, Color specular,float gloss)
        {
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.gloss = gloss;
        }
    }
}

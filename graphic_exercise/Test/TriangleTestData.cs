using graphic_exercise.RenderData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.Test
{
    class TriangleTestData
    {
        //顶点坐标
        public static Vector[] pointList = {
                                            new Vector(-1,  1, 0),
                                            new Vector(-1, -1, 0),
                                            new Vector(1, -1, 0),
                                            new Vector(1, 1, 0),
                                        };
        //三角形顶点索引 12个面
        public static int[] indexs = {   0,1,2,
                                   0,2,3,
                               };

        //uv坐标
        public static UV[] uvs ={
                                   new UV(0, 0),new UV( 0, 1),new UV(1, 1),
                                   new UV(0, 0),new UV(1, 1),new UV(1, 0),
                              };
    }
}

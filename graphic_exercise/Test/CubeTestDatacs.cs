using graphic_exercise.RenderData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.Test
{
    class CubeTestDatacs
    {
        //顶点坐标
        public static Vector[] pointList = {
                                            new Vector(-1,  1,-1,1),
                                            new Vector(-1, -1, -1,1),
                                            new Vector(1, -1, -1,1),
                                            new Vector(1, 1, -1,1),

                                            new Vector( -1,  1, 1,1),
                                            new Vector(-1, -1, 1,1),
                                            new Vector(1, -1, 1,1),
                                            new Vector(1, 1, 1,1)
                                        };
        //三角形顶点索引 12个面
        public static Index[] indexs = {
                                    
                                    //前
                                   new Index(0,3,2),
                                   new Index(0,2,1),
                                   //后
                                   new Index(7,4,5),
                                   new Index(7,5,6),
                                  // //左
                                   new Index(4,0,1),
                                   new Index(4,1,5),
                                   //上
                                   new Index(4,7,3),
                                   new Index(4,3,0),
                                   //下
                                   new Index(6,5,1),
                                   new Index(6,1,2),
                                   //右
                                  new Index( 3,7,6),
                                   new Index(3,6,2),
                               };

        //uv坐标
        public static UV[] uvs ={
                                  new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),
                                   //
                                   new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),
                                   //
                                    new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),
                                   //
                                   new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),
                                   //
                                     new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),
                                   ///
                                     new UV(0, 1),new UV( 1, 1),new UV(1, 0),
                                   new UV(0, 1),new UV(1, 0),new UV(0, 0),

                                         };
        //法线
        public static Vector[] norlmas = {
                                                //前
                                                new Vector( 0, 0, -1), new Vector(0, 0, -1), new Vector( 0, 0, -1),
                                               new Vector(0, 0, -1), new Vector( 0, 0, -1), new Vector( 0, 0, -1),
                                               //后
                                                new Vector( 0, 0, 1), new Vector( 0, 0, 1), new Vector( 0, 0, 1),
                                               new Vector( 0, 0, 1), new Vector( 0, 0, 1), new Vector( 0, 0, 1),
                                               //左
                                                new Vector( -1, 0, 0), new Vector( -1, 0, 0), new Vector( -1, 0, 0),
                                               new Vector( -1, 0, 0), new Vector(-1, 0, 0), new Vector( -1, 0, 0),
                                               //上
                                                new Vector( 0, 1, 0), new Vector( 0, 1, 0), new Vector( 0, 1, 0),
                                               new Vector( 0, 1, 0 ), new Vector(0, 1, 0), new Vector( 0, 1, 0),
                                                //下
                                                new Vector( 0, -1, 0), new Vector(  0, -1, 0), new Vector(  0, -1, 0),
                                               new Vector(  0, -1, 0), new Vector( 0, -1, 0), new Vector( 0, -1, 0),
                                                //右
                                               new Vector( 1, 0, 0), new Vector( 1, 0, 0), new Vector( 1, 0, 0),
                                               new Vector( 1, 0, 0), new Vector( 1, 0, 0), new Vector( 1, 0, 0),
                                            };

        //顶点颜色
        public static Color[] vertColors = {
                                             new Color( 1, 0, 0,1), new Color( 0, 0, 1,1), new Color( 0, 1, 0,1),
                                               new Color( 1, 0, 0,1), new Color( 0, 1, 0,1), new Color( 0, 0, 1,1),
                                               ////
                                                new Color( 0, 1, 0,1), new Color( 0, 0, 1,1), new Color( 1, 0, 0,1),
                                               new Color( 0, 1, 0,1), new Color( 1, 0, 0,1), new Color( 0, 0, 1,1),
                                               ////
                                                new Color( 0, 1, 0,1), new Color( 0, 0, 1,1), new Color( 1, 0, 0,1),
                                               new Color( 0, 1, 0,1), new Color( 1, 0, 0,1), new Color( 0, 0, 1,1),
                                               ////
                                                new Color( 0, 1, 0,1), new Color( 0, 0, 1,1), new Color( 1, 0, 0,1),
                                               new Color( 0, 1, 0,1), new Color( 1, 0, 0,1), new Color( 0, 0, 1,1),
                                               //
                                                new Color( 0, 1, 0,1), new Color( 0, 0, 1,1), new Color( 1, 0, 0,1),
                                               new Color( 0, 1, 0,1), new Color( 1, 0, 0,1), new Color( 0, 0, 1,1),
                                               //
                                                new Color( 0, 1, 0,1), new Color( 0, 0, 1,1), new Color( 1, 0, 0,1),
                                               new Color( 0, 1, 0,1), new Color( 1, 0, 0,1), new Color( 0, 0, 1,1)



                                         };

        //材质
        public static Material mat = new Material(new Color(0.1f, 0, 0f, 1), new Color(1f, 0, 0.1f, 1), new Color(0, 1,1, 1), 1);
    }
}

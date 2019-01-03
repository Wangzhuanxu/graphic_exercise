using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    //列矩阵
    class Matrix4x4
    {
        public float[,] matrix = new float[4, 4];

        public Matrix4x4()
        {
            SetZero();
        }
        /// <summary>
        /// 一列一列传数据
        /// </summary>
        public Matrix4x4(float a1, float b1, float c1, float d1,
                         float a2, float b2, float c2, float d2,
                         float a3, float b3, float c3, float d3,
                         float a4, float b4, float c4, float d4)
        {
            matrix[0, 0] = a1; matrix[0, 1] = b1; matrix[0, 2] = c1; matrix[0, 3] = d1;
            matrix[1, 0] = a2; matrix[1, 1] = b2; matrix[1, 2] = c2; matrix[1, 3] = d2;
            matrix[2, 0] = a3; matrix[2, 1] = b3; matrix[2, 2] = c3; matrix[2, 3] = d3;
            matrix[3, 0] = a4; matrix[3, 1] = b4; matrix[3, 2] = c4; matrix[3, 3] = d4;
        }

        /// <summary>
        /// 矩阵乘法
        /// </summary>
        /// <param name="lhs">行数</param>
        /// <param name="rhs">列数</param>
        /// <returns></returns>
        public static Matrix4x4 operator *(Matrix4x4 lhs, Matrix4x4 rhs)
        {
            Matrix4x4 nm = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        nm.matrix[i, j] += lhs.matrix[i, k] * rhs.matrix[k, j];
                    }
                }
            }
            return nm;
        }
        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float this[int i, int j]
        {
            get { return matrix[i, j]; }
            set { matrix[i, j] = value; }
        }

        /// <summary>
        /// 单位化矩阵
        /// </summary>
        public void Identity()
        {
            matrix[0, 0] = 1; matrix[0, 1] = 0; matrix[0, 2] = 0; matrix[0, 3] = 0;
            //
            matrix[1, 0] = 0; matrix[1, 1] = 1; matrix[1, 2] = 0; matrix[1, 3] = 0;
            //
            matrix[2, 0] = 0; matrix[2, 1] = 0; matrix[2, 2] = 1; matrix[2, 3] = 0;
            //
            matrix[3, 0] = 0; matrix[3, 1] = 0; matrix[3, 2] = 0; matrix[3, 3] = 1;
        }
        /// <summary>
        /// 初始化矩阵
        /// </summary>
        public void SetZero()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    matrix[i, j] = 0;
                }
            }
        }

        /// <summary>
        /// 求转置
        /// </summary>
        /// <returns></returns>
        /// 第i行第j个元素与第j行第i列元素进行交换
        public  Matrix4x4 Transpose()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = i; j < 4; j++)
                {

                    float temp = matrix[i, j];
                    matrix[i, j] = matrix[j, i];
                    matrix[j, i] = temp;
                }
            }
            return this;
        }
        /// <summary>
        /// 求矩阵行列式
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public float Determinate()
        {
            return Determinate(matrix, 4);
        }

        /// <summary>
        /// 求行列式，需要将高阶矩阵逐渐转化为低阶矩阵
        /// 需要用递归来实现
        /// 
        /// Aij的余子式Bij为：划去Aij所在的第i行与第j列的元，剩下的元素不改变原来的顺序所构成的n-1阶矩阵的行列式称为元素Aij的余子式
        /// Aij的代数余子式为：-1的i+j次方乘以Bij
        /// 矩阵A的行列式为：矩阵A任意一行(列)的各元素与其对应的代数式余子式乘积之和（一般为第一行）
        /// 余子式矩阵：将矩阵A中所有元替换为其余子式后所组成的矩阵
        /// 伴随矩阵:代数余子式矩阵的转置矩阵：
        /// 逆矩阵：为行列式的倒数乘以伴随矩阵
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private  float Determinate(float[,] m, int n)
        {
            if (n == 1)
            {
                return m[0, 0];
            }
            else
            {
                float result = 0;
                float[,] tempM = new float[n - 1, n - 1];
                //原矩阵的第i列元素
                for (int i = 0; i < n; i++)
                {
                    //代数余子式
                    for (int j = 0; j < n - 1; j++)//新矩阵行
                    {
                        for (int k = 0; k < n - 1; k++)//新矩阵列
                        {
                            int x = j + 1;//原矩阵行
                            int y = k >= i ? k + 1 : k;//原矩阵列
                            tempM[j, k] = m[x, y];
                        }
                    }

                    result += (float)System.Math.Pow(-1, 0 + (i)) * m[0, i] * Determinate(tempM, n - 1);
                }
                return result;
            }
        }

        /// <summary>
        /// 获取当前矩阵的伴随矩阵
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetAdjoint()
        {
            int x, y;
            float[,] tempM = new float[3, 3];
            Matrix4x4 result = new Matrix4x4();
            //原矩阵行
            for (int i = 0; i < 4; i++)
            {
                //原矩阵列
                for (int j = 0; j < 4; j++)
                {
                    //新矩阵行
                    for (int k = 0; k < 3; k++)
                    {
                        //新矩阵列
                        for (int t = 0; t < 3; t++)
                        {
                            //需要去掉第i行第j列元素
                            x = k >= i ? k + 1 : k;
                            y = t >= j ? t + 1 : t;

                            tempM[k, t] = matrix[x, y];
                        }
                    }
                    //求每个元素的代数余子式
                    result.matrix[i, j] = (float)Math.Pow(-1, (j) + (i)) * Determinate(tempM, 3);
                }
            }
            return result.Transpose();
        }
        /// <summary>
        /// 求当前矩阵的逆矩阵
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 Inverse()
        {
            float a = Determinate();
            if (a == 0)
            {
                Console.WriteLine("矩阵不可逆");
                return null;
            }
            Matrix4x4 adj = GetAdjoint();//伴随矩阵
            //计算逆矩阵
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    adj.matrix[i, j] = adj.matrix[i, j] / a;
                }
            }
            return adj;
        }

        //////////////////////////////////////////////////////////////////////////////矩阵变换/////////////////////////////////////////////////
        /// <summary>
        /// 平移
        /// </summary>
        /// <param name="x">x轴位移</param>
        /// <param name="y">y轴位移</param>
        /// <param name="z">z轴位移</param>
        /// <returns></returns>
        public static Matrix4x4 translate(float x,float y,float z)
        {
            Matrix4x4 m = new Matrix4x4
                (
                    1, 0, 0, x,
                    0, 1, 0, y,
                    0, 0, 1, z,
                    0, 0, 0, 1
                );
            return m;
        }

        public static Matrix4x4 rotate(int x,int y,int z,float angle)
        {
            if(x==1)
            {
                return rotateX(angle);
            }
            else if(y==1)
            {
                return rotateY(angle);
            }
            else if(z==1)
            {
                return rotateZ(angle);
            }
            return null;
        }

        /// <summary>
        /// 绕x轴旋转
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public static Matrix4x4 rotateX(float angle)
        {
            Matrix4x4 m = new Matrix4x4();
            m.Identity();//获得原始矩阵
            m[1, 1] = (float)Math.Cos(angle);
            m[1, 2] = -(float)Math.Sin(angle);
            m[2, 1] = (float)Math.Sin(angle);
            m[2, 2] = (float)Math.Cos(angle);
            return m;
        }

        /// <summary>
        /// 绕y轴旋转
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public static Matrix4x4 rotateY(float angle)
        {
            Matrix4x4 m = new Matrix4x4();
            m.Identity();//获得原始矩阵
            m[0, 0] = (float)Math.Cos(angle);
            m[0, 2] = (float)Math.Sin(angle);
            m[2, 0] = -(float)Math.Sin(angle);
            m[2, 2] = (float)Math.Cos(angle);
            return m;
        }

        /// <summary>
        /// 绕z轴旋转
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public static Matrix4x4 rotateZ(float angle)
        {
            Matrix4x4 m = new Matrix4x4();
            m.Identity();//获得原始矩阵
            m[0, 0] = (float)Math.Cos(angle);
            m[0, 1] = -(float)Math.Sin(angle);
            m[1, 0] = (float)Math.Sin(angle);
            m[1, 1] = (float)Math.Cos(angle);
            return m;
        }
        /// <summary>
        /// 获取缩放矩阵
        /// xyz为缩放系数
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Matrix4x4 scale(float x,float y,float z)
        {
            Matrix4x4 m = new Matrix4x4
               (
                   x, 0, 0, 0,
                   0, y, 0, 0,
                   0, 0, z, 0,
                   0, 0, 0, 1
               );
            return m;
        }
        /// <summary>
        /// 推到网址：https://blog.csdn.net/Augusdi/article/details/20450065
        /// 最终目的将顶点转换到摄像机空间中
        /// </summary>
        /// <param name="look"></param>
        /// <param name="up"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Matrix4x4 view(Vector look,Vector up,Vector pos)
        {
            Vector N = look-pos;
            Vector U = Vector.cross( up,N);
            U.normalize();
            up = Vector.cross( N,U);

            Matrix4x4 t = new Matrix4x4
            (
                 1, 0, 0, -pos.x,
                 0, 1, 0, -pos.y,
                 0, 0, 1, -pos.z,
                 0, 0, 0, 1
            );

            Matrix4x4 r = new Matrix4x4
                (
                     U.x,  U.y,  U.y,  0,
                     up.x, up.y, up.z, 0,
                     N.x,  N.y,  N.z,  0,
                     0,    0,    0,    1

                );
            return r * t;
        }
        /// <summary>
        /// 投影矩阵
        /// </summary>
        /// <param name="fov">观察角度</param>
        /// <param name="aspect">宽高比</param>
        /// <param name="near">近平面</param>
        /// <param name="far">远平面</param>
        /// <returns></returns>
        public Matrix4x4 project(float fov,float aspect,float near,float far)
        {
            Matrix4x4 p = new Matrix4x4();
            p[0, 0] = (float)(1 / (Math.Tan(fov * 0.5f) * aspect));
            p[1, 1] = (float)(1 / Math.Tan(fov * 0.5f));
            p[2, 2] = far+near / (far - near);
            p[2, 3] = (2*far * near) / (near - far);
            p[3, 2] = -1f;
            return p;
        }

    }
}

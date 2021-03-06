﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graphic_exercise.RenderData
{
    /// <summary>
    /// 三角形网格类
    /// </summary>
    class Triangle
    {
        public List<Vertex> vertexList;//顶点列表
        public List<Index> indexsList;//索引列表
        /// <summary>
        /// 初始化三角形网格顶点
        /// </summary>
        /// <param name="vertexList">顶点位置列表</param>
        /// <param name="indexList">索引列表</param>
        /// <param name="uvList">uv坐标</param>
        public Triangle(Vector[] posList,Index []indexList,UV []uvList,Vector[] normals,Color[] colors,Material material)
        {
            vertexList = new List<Vertex>();
            indexsList = new List<Index>();

            //for(int i=0;i<posList.Length;i++)
            //{
            //    Vertex v = new Vertex(posList[i], normals[i], uvList[i].x, uvList[i].y,colors[i]);
            //    vertexList.Add(v);
            //}
            int t = 0;
            for(int i=0;i<indexList.Length;i++)
            {
                //indexsList.Add(indexList[i]);
                Vertex v1 = new Vertex(posList[indexList[i].one], normals[t], uvList[t].x, uvList[t].y, colors[t],material);
                t++;
                Vertex v2 = new Vertex(posList[indexList[i].two], normals[t], uvList[t].x, uvList[t].y, colors[t], material);
                t++;
                Vertex v3 = new Vertex(posList[indexList[i].three], normals[t], uvList[t].x, uvList[t].y, colors[t], material);
                t++;
                vertexList.Add(v1);
                vertexList.Add(v2);
                vertexList.Add(v3);
            }
        }
        
    }
    /// <summary>
    /// 三角形网格顶点
    /// </summary>
    public class Index
    {
        public int one;
        public int two;
        public int three;
        public Index(int one,int two,int three)
        {
            this.one = one;
            this.two = two;
            this.three = three;
        }
    }
}

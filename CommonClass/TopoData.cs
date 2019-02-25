using System.Collections.Generic;
using System.Data.SqlClient;
using System.ComponentModel;
using SnmpSharpNet;
using Swordfish.NET.Collections;
using System.Windows;
using System;
using NCMMS.UC;


namespace NCMMS.CommonClass
{
    /// <summary>
    /// 邻接矩阵用来保存发现的拓扑信息。坐标轴为发现设备类V，元素为连线关系类E
    /// </summary>
    public class TopoData<V,E>
    {
        int dimension = 50; //矩阵的维数，可以预先定义或者初始化一个初始值，如50。拓扑发现过程中如果维数不够，可动态扩展。

        
        List<V> equipList = new List<V>(); //按照发现顺序保存的设备列表，其count就是元素个数number
        //List<M> equipList = new List<M>();
        E[,] matrix;

        /// <summary>
        /// 实际的元素个数，只读
        /// </summary>
        public int Number
        {
            get { return equipList.Count; }
        }
        /// <summary>
        /// 矩阵的维数，初始值为50，当元素个数大于维数时，需要对维数进行扩充
        /// </summary>
        public int Dimension
        {
            get { return dimension; }
            set { dimension = value; }
        }

        public TopoData()
        {
            matrix = new E[dimension, dimension];
        }

        /// <summary>
        /// 当发现的拓扑设备个数超过了预先设置的矩阵维数时，需要对矩阵进行扩充，这里直接扩充2倍
        /// </summary>
        public void ExpandMatrix()
        {
            E[,] temp = matrix;
            matrix = new E[dimension*2, dimension*2];
            for (int i = 0; i < dimension; i++)
                for (int j = 0; j < dimension; j++)
                    matrix[i, j] = temp[i, j];
            dimension = dimension * 2;
        }

        public void Reset()
        {
            dimension = 50;
            for (int i = 0; i < dimension; i++)
                for (int j = 0; j < dimension; j++)
                    matrix[i, j] = default(E);
        }

        /// <summary>
        /// 根据矩阵的x,y来寻找元素。
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public E GetE(int i,int j)
        {
            if (i < dimension && i >= 0 && j >= 0 && j < dimension)
                return matrix[i, j];
            else
                return default(E);
        }

        /// <summary>
        /// 根据矩阵的x,y来寻找元素。x,y为设备信息类，元素为连线信息类
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public E GetE(V v1,V v2)
        {
            int i = equipList.IndexOf(v1);
            int j = equipList.IndexOf(v2);
            if (i != -1 && j != -1)
                return GetE(i, j);
            else
                return default(E);
        }
        /// <summary>
        /// 根据矩阵行列序号或者equiplist序号获取设备信息类
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public V GetV(int i)
        {
            if (i < 0)
                return default(V);
            else
                return equipList[i];
        }
        /// <summary>
        /// 添加设备信息类
        /// </summary>
        /// <param name="v"></param>
        public void AddV(V v)
        {
            equipList.Add(v);
            if (Number == dimension)
                ExpandMatrix();
        }
        /// <summary>
        /// 添加连线信息类,添加一次即可，函数中自动把对称的位置也添加上
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="e"></param>
        public void AddE(int i, int j, E e)
        {
            matrix[i, j] = e;
            matrix[j, i] = e;
        }

        /// <summary>
        /// 返回V在equiplist中的index，没有的话返回-1
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int GetIndexForV(V v)
        {
            return equipList.IndexOf(v);
        }
    }
}

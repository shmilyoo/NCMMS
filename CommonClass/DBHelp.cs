using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Configuration;
using System.Collections.Generic;

namespace NCMMS.CommonClass
{
    public class DBHelp
    {
        SqlConnection con = new SqlConnection(Properties.Settings.Default["ConnectionString"] as string); 
        //SqlConnection con = new SqlConnection("server=192.168.1.151;database=NCMMS;uid=NCMMS;pwd=NCMMS;Connect Timeout=3");
        SqlCommand cmd;
        
        public SqlConnection Con
        {
            get { return con; }
            set { con = value; }
        }

        /// <summary>
        /// 根据SQL查询返回DataSet对象，如果没有查询到则返回NULL
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>DataSet</returns>
        public DataSet returnDS(SqlCommand cmd, string TempTableName)
        {
            DataSet ds = new DataSet();
            try
            {
                cmd.Connection = con;
                cmd.CommandTimeout = 30;
                this.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds, TempTableName);
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                this.Close();
            }
            return ds;
        }
        /// <summary>
        /// 根据SQL查询返回DataSet对象，如果没有查询到则返回NULL
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="sRecord">开始记录数</param>
        /// <param name="mRecord">最大记录数</param>
        /// <returns>DataSet</returns>
        public DataSet returnDS(SqlCommand cmd, string TempTableName, int sRecord, int mRecord)
        {
            DataSet ds = new DataSet();
            try
            {
                cmd.Connection = con;
                cmd.CommandTimeout = 30;
                this.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds, sRecord, mRecord, TempTableName);
            }
            catch (Exception e)
            {
                ds = null;
                throw (e);
            }
            finally
            {
                this.Close();
            }
            return ds;
        }
        /// <summary>
        /// 返回DataTable对象
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public DataTable returnDb(SqlCommand cmd)
        {
            DataTable ds = new DataTable();
            try
            {
                cmd.Connection = con;
                cmd.CommandTimeout = 30;
                this.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                this.Close();
            }
            return ds;
        }
        /// <summary>
        /// 根据SqlDataComand对象返回查找的记录集,如果没有则返回NULL
        /// </summary>
        /// <param name="cmd">SqlCommand对象</param>
        /// <returns>SqlDataReader对象</returns>
        public SqlDataReader returnReader(string sql)
        {
            SqlDataReader reader;
            try
            {
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 3;
                this.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception e)
            {
                reader = null;
                throw (e);
            }
            return reader;
        }
        /// <summary>
        /// 根据SqlDataComand对象返回查找的值,如果没有则返回NULL
        /// </summary>
        /// <param name="cmd">SqlCommand对象</param>
        /// <returns>Object对象</returns>
        public object returnScalar(string sql)
        {
            object obj;
            try
            {
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 3;
                this.Open();
                obj = cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                obj = null;
                throw (e);
            }
            finally
            {
                this.Close();
            }
            return obj;
        }
        /// <summary>
        /// 对数据库的增，删，改的操作
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>是否成功</returns>
        public bool ExecuteReturnBool(string sql)
        {
            bool succeed = false;
            int cnt = 0;
            try
            {
                cmd = con.CreateCommand();
                cmd.Connection = con;
                cmd.CommandText = sql;
                cmd.CommandTimeout = 30;
                this.Open();
                cnt = cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                if (cnt > 0)
                {
                    succeed = true;
                }
                this.Close();
            }
            return succeed;
        }
        /// <summary>
        /// 对数据库的增，删，改的操作
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>是否成功</returns>
        public int ExecuteReturnInt(string sql)
        {
            int cnt = 0;
            try
            {
                cmd = con.CreateCommand();
                cmd.Connection = con;
                cmd.CommandText = sql;
                cmd.CommandTimeout = 30;
                this.Open();
                cnt = cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                this.Close();
            }
            return cnt;
        }
        /// <summary>
        ///  获得该SQL查询返回DataTable，如果没有查询到则返回NULL
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns></returns>
        public DataTable getTable(SqlCommand cmd, string TempTableName)
        {
            DataTable tb = null;
            DataSet ds = this.returnDS(cmd, TempTableName);
            if (ds != null)
            {
                tb = ds.Tables[TempTableName];
            }
            return tb;
        }

        #region ExecuteTransaction
        public bool ExecuteTransaction(List<string> cmdText)
        {
            bool isSuccess = false;
            this.Open();
            SqlTransaction trans = con.BeginTransaction();
            try
            {
                cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 3;
                cmd.Transaction = trans;
                for (int i = 0; i < cmdText.Count; i++)
                {
                    if (!string.IsNullOrEmpty(cmdText[i]))
                    {
                        cmd.CommandText = cmdText[i];
                        cmd.ExecuteNonQuery();
                    }
                }
                trans.Commit();
                isSuccess = true;
            }
            catch(Exception e)
            {
                trans.Rollback();
                isSuccess = false;
            }
            finally
            {
                trans.Dispose();
                this.Close();
            }
            return isSuccess;
        }
        public bool ExecuteTransaction(List<string> cmdText, SqlParameter[] para)
        {
            if (para == null)
                return ExecuteTransaction(cmdText);
            this.Open();
            SqlTransaction trans = con.BeginTransaction();
            try
            {
                cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 3;
                cmd.Transaction = trans;
                for (int i = 0; i < para.Length; i++)
                    cmd.Parameters.Add(para[i]);
                for (int i = 0; i < cmdText.Count; i++)
                {
                    if (!string.IsNullOrEmpty(cmdText[i]))
                    {
                        cmd.CommandText = cmdText[i];
                        cmd.ExecuteNonQuery();
                    }
                }
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception(ex.ToString());
            }
            finally
            {
                trans.Dispose();
                this.Close();
            }
        }
        #endregion



        /// <summary>
        /// 打开数据库连接.
        /// </summary>
        public void Open()
        {
            try
            {
                if (con.State == System.Data.ConnectionState.Closed)
                {
                    con.Open();
                }
                else if (con.State == System.Data.ConnectionState.Broken)
                {
                    con.Close();
                    con.Open();
                }
                App.databaseConState = true;
            }
            catch (Exception ex)
            {
                App.databaseConState = false;
                throw (ex);
            }
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void Close()
        {
            if (con != null)
            {
                con.Close();
            }
            if (cmd != null)
            {
                cmd.Dispose();
            }
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 确认连接是否已经关闭
            if (con != null)
            {
                con.Dispose();
                con = null;
            }
            if (cmd != null)
            {
                cmd.Dispose();
                cmd = null;
            }
        }

        public bool Test()
        {
            try
            {
                Open();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Close();
            }
        }
    }
}

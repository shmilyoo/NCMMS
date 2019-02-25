using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Threading;
using System.Data.SqlClient;
using NCMMS.CommonClass;
using SnmpSharpNet;
using System.Net;
using System.Collections.ObjectModel;
using Swordfish.NET.Collections;
using System.Xml;

namespace NCMMS.CommonClass
{
    public class SnmpHelper
    {
        /// <summary>
        /// 由表中一列的OID来获取整列的值集合,若出错，返回null，一般用来获取表格的索引列
        /// </summary>
        /// <param name="agentIP">目标IP地址</param>
        /// <param name="_rootOid">此列的OID</param>
        /// <param name="errorMessage">输出错误信息，若没有错误，返回空字符串</param>
        /// <returns>返回字符串集合，之后自行根据此列类型进行转换，若出错，返回null</returns>
        public static List<string> GetSingleColumnListFromTable(IpAddress agentIP, Oid rootOid,out string errorMessage)
        {
            OctetString community = new OctetString(App.snmpCommunity);
            AgentParameters param = new AgentParameters(community);
            param.DisableReplySourceCheck = !App.snmpCheckSrcFlag;
            // Set SNMP version to 2 (GET-BULK only works with SNMP ver 2 and 3)
            param.Version = SnmpVersion.Ver2;
            // Construct target

            UdpTarget target = new UdpTarget((IPAddress)agentIP, App.snmpPort, App.snmpTimeout, App.snmpRetry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            //Oid rootOid = rootOid;

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetBulk);

            // In this example, set NonRepeaters value to 0
            pdu.NonRepeaters = 0;
            // MaxRepetitions tells the agent how many Oid/Value pairs to return
            // in the response.
            pdu.MaxRepetitions = 5;

            List<string> resultList = new List<string>();
            // Loop through results
            while (lastOid != null)
            {
                // When Pdu class is first constructed, RequestId is set to 0
                // and during encoding id will be set to the random value
                // for subsequent requests, id will be set to a value that
                // needs to be incremented to have unique request ids for each
                // packet
                if (pdu.RequestId != 0)
                {
                    pdu.RequestId += 1;
                }
                // Clear Oids from the Pdu class.
                pdu.VbList.Clear();
                // Initialize request PDU with the last retrieved Oid
                pdu.VbList.Add(lastOid);
                // Make SNMP request
                SnmpV2Packet result = null;
                try
                {
                    result = (SnmpV2Packet)target.Request(pdu, param);
                }
                catch (Exception ex)
                {
                    errorMessage = "获取SNMP应答出现错误;" + ex.Message;
                    target.Close();
                    return null;
                }
                // You should catch exceptions in the Request if using in real application.

                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request
                        lastOid = null;
                        errorMessage = string.Format("SNMP应答数据包中有错误。 Error {0} index {1}", result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
                        return null;
                        //break;
                    }
                    else
                    {
                        // Walk through returned variable bindings
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            // Check that retrieved Oid is "child" of the root OID
                            if (rootOid.IsRootOf(v.Oid))
                            {
                                if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                    lastOid = null;
                                else
                                    lastOid = v.Oid;
                                resultList.Add(v.Value.ToString());
                            }
                            else
                            {
                                // we have reached the end of the requested
                                // MIB tree. Set lastOid to null and exit loop
                                lastOid = null;
                                break; // 每个数据包获取5个值，一旦有一个不是这一列的数据，后面的应该都不是了
                            }
                        }
                    }
                }
                else
                {
                    errorMessage = "指定网管代理未返回有效信息";
                    return null;
                }
            }
            target.Close();
            errorMessage = "";
            return resultList;
        }

        /// <summary>
        /// 根据提供信息获取一个或多个snmp值，返回VbCollection，若出错则返回null
        /// </summary>
        /// <param name="ip">目标地址</param>
        /// <param name="requestOids">目标oid数组</param>
        /// <param name="errorMessage">输出错误信息</param>
        /// <param name="community">共同体字符串，默认public</param>
        /// <param name="port">端口，默认161</param>
        /// <param name="timeout">超时，默认2000ms</param>
        /// <param name="retry">重试次数，默认1次</param>
        /// <returns>返回VbCollection，若出错则返回null</returns>
        public static VbCollection GetResultsFromOids(IpAddress ip, string[] requestOids,out string errorMessage)
        {
            int n = requestOids.Length;
            AgentParameters param = new AgentParameters(SnmpVersion.Ver2, new OctetString(App.snmpCommunity));
            param.DisableReplySourceCheck = !App.snmpCheckSrcFlag;
            UdpTarget target = new UdpTarget((IPAddress)ip, App.snmpPort, App.snmpTimeout, App.snmpRetry);
            Pdu pdu = new Pdu(PduType.Get);
            for (int i = 0; i < requestOids.Length; i++)
                pdu.VbList.Add(requestOids[i]);
            SnmpV2Packet result = null;
            try
            {
                result = (SnmpV2Packet)target.Request(pdu, param);
            }
            catch (Exception ex)
            {
                errorMessage = "获取SNMP应答出现错误\n" + ex.Message;
                target.Close();
                return null;
            }
            VbCollection vbs;
            if (result != null)
            {
                if (result.Pdu.ErrorStatus != 0)
                {
                    errorMessage = string.Format("SNMP应答数据包中有错误信息. Error {0} index {1}", result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
                    target.Close();
                    return null;
                }
                else
                    vbs = result.Pdu.VbList;
            }
            else
            {
                errorMessage = "没有SNMP应答数据包";
                target.Close();
                return null;
            }
            errorMessage = "";
            target.Close();
            return vbs;
        }

        public static string GetStrTypeNameFromEquipType(EquipType type)
        {
            switch (type)
            {
                case EquipType.Router:
                    return "路由器";
                case EquipType.Layer3Switch:
                    return "三层交换机";
                case EquipType.Layer2Switch:
                    return "二层交换机";
                case EquipType.Hub:
                    return "集线器";
                case EquipType.PC:
                    return "PC";
                case EquipType.ServerInTable:
                    return "台式服务器";
                case EquipType.Server:
                    return "机架式服务器";
                case EquipType.FireWall:
                    return "防火墙";
                default:
                    return "其他";
            }
        }

        public static EquipType GetEquipTypeFromStrTypeName(string value)
        {
            switch (value)
            {
                case "路由器":
                    return EquipType.Router;
                case "三层交换机":
                    return EquipType.Layer3Switch;
                case "二层交换机":
                    return EquipType.Layer2Switch;
                case "集线器":
                    return EquipType.Hub;
                case "PC":
                    return EquipType.PC;
                case "台式服务器":
                    return EquipType.ServerInTable;
                case "机架式服务器":
                    return EquipType.Server;
                case "防火墙":
                    return EquipType.FireWall;
                default:
                    return EquipType.Other;
            }
        }

        /// <summary>
        /// 从SNMP中system下的objectID中获取设备品牌
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Brand GetBrandFromObjectID(int id)
        {
            switch (id)
            {
                case 2011:
                    return Brand.Huawei;
                case 311:
                    return Brand.MicroSoft;
                default:
                    return Brand.Other;
            }
        }

        public static Brand GetBrandFromObjectID(string strID)
        {
            //1.3.6.1.4.1.311.1.1.3.1.2
            int id;
            try
            {
                id = Convert.ToInt32(strID.Split('.')[6]);
                return GetBrandFromObjectID(id);
            }
            catch
            {
                return Brand.Other;
            }
        }

        public static string GetBrandStringFromBrand(Brand brand)
        {
            switch (brand)
            {
                case Brand.Cisco:
                    return "思科";
                case Brand.H3Com:
                    return "华三";
                case Brand.Huawei:
                    return "华为";
                case Brand.MicroSoft:
                    return "微软";
                case Brand.IBM:
                    return "IBM";
                case Brand.HP:
                    return "HP";
                default:
                    return "其他";
            }
        }
        public static string[] BrandNames = new string[] {  "思科", "华为", "华三", "微软", "IBM", "HP", "其他"};
        public static string[] TypeNames = new string[] {  "路由器", "三层交换机", "二层交换机", "集线器", "PC", "台式服务器", "机架式服务器", "防火墙", "其他"};

        public static Brand GetBrandFromString(string name)
        {
            switch (name.ToUpper())
            {
                case "思科":
                    return Brand.Cisco;
                case "CISCO":
                    return Brand.Cisco;
                case "华三":
                    return Brand.H3Com;
                case "华为3COM":
                    return Brand.H3Com;
                case "华为":
                    return Brand.Huawei;
                case "HUAWEI":
                    return Brand.Huawei;
                case "微软":
                    return Brand.MicroSoft;
                default:
                    return Brand.Other;
            }
        }

        public static string GetRouteTypeFromNum(int i)
        {
            switch (i)
            {
                case 0:
                    return "static";
                case 1:
                    return "other";
                case 2:
                    return "invalid";
                case 3:
                    return "direct";
                case 4:
                    return "indirect";
                default:
                    return "other";
            }
        }
        public static string GetRouteTypeFromStrNum(string i)
        {
            int num;
            if (!int.TryParse(i,out num))
                return "other";
            return GetRouteTypeFromNum(num);
        }
        public static string GetRouteProtocolFromNum(int i)
        {
            switch (i)
            {
                case 1:
                    return "other";
                case 2:
                    return "local";
                case 3:
                    return "netmgmt";
                case 4:
                    return "icmp";
                case 5:
                    return "egp";
                case 6:
                    return "ggp";
                case 7:
                    return "hello";
                case 8:
                    return "rip";
                case 9:
                    return "is-is";
                case 10:
                    return "es-is";
                case 11:
                    return "ciscoIgrp";
                case 12:
                    return "bbnSpfIgp";
                case 13:
                    return "ospf";
                case 14:
                    return "bgp";
                default:
                    return "other";
            }
        }
        /// <summary>
        /// 获取ipNetToMediaType的名字
        /// </summary>
        /// <param name="num">ipNetToMediaType的值</param>
        /// <returns></returns>
        public static string GetStrMediaTypeFromNum(int num)
        {
            switch (num)
            {
                case 1:
                    return "other";
                case 2:
                    return "invalid";
                case 3:
                    return "dynamic";
                case 4:
                    return "static";
                default:
                    return "other";
            }
        }
    }
}
using System.Collections.Generic;
using System.Data.SqlClient;
using System.ComponentModel;
using SnmpSharpNet;
using Swordfish.NET.Collections;
using System.Windows;
using System;
using NCMMS.UC;
using NCMMS.CommonClass;

namespace NCMMS.Config
{
    public class Equip4DB : INotifyPropertyChanged
    {
        int index, typeIndex;
        string name, typeName;//设备名称，设备类型名称，snmp返回的sysdescr

        EquipType type;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        public string TypeName
        {
            get { 
                //return typeName;Other=0,Router,Layer3Switch,Layer2Switch,Hub,PC,ServerInTable,Server,FireWall
                return SnmpHelper.GetStrTypeNameFromEquipType(type);
            }
            set
            {
                if (!string.IsNullOrEmpty(typeName) && !typeName.Equals(value))
                {
                    string updateSql = "UPDATE Equipments SET Equip_TypeIndex = (select type_index from equipmenttype where type_name = '" + value + "') where Equip_Index = " + index;
                    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    {
                        MessageBox.Show("更新失败");
                        return;
                    }
                    //同时更新静态缓存列表中的值
                    App.idAndEquipList[index].TypeName = value;
                    App.idAndEquipList[index].TypeIndex = (int)App.DBHelper.returnScalar(string.Format("select type_index from equipmenttype where type_name ='{0}'", value));
                }
                this.typeName = value;
                Type = SnmpHelper.GetEquipTypeFromStrTypeName(value);
                NotifyPropertyChanged("TypeName");
            }
        }

        public EquipType Type
        {
            get { return type; }
            set { 
                type = value;
                typeName = SnmpHelper.GetStrTypeNameFromEquipType(value);
                NotifyPropertyChanged("Type");
                NotifyPropertyChanged("TypeName");
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (!string.IsNullOrEmpty(name) && !name.Equals(value))
                {
                    string updateSql = "UPDATE Equipments SET Equip_Name = '" + value + "' where Equip_Index = " + index;
                    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    {
                        MessageBox.Show("更新失败");
                        return;
                    }
                    //同时更新静态缓存列表中的值
                    App.idAndEquipList[index].Name = value;
                }
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public int TypeIndex
        {
            get { return typeIndex; }
            set { typeIndex = value; }
        }

        /// <summary>
        /// 在牵涉到数据库操作的页面中，id>0：数据库中有对应设备；id小于0：不在数据库中设备
        /// </summary>
        public int Index
        {
            get { return index; }
            set { index = value; }
        }
        public Equip4DB(int _index, string _name)
        {
            index = _index;
            name = _name;
        }
        public Equip4DB() { }



    }

}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace Stardust.Data.Nodes
{
    /// <summary>节点在线</summary>
    [Serializable]
    [DataObject]
    [Description("节点在线")]
    [BindIndex("IU_NodeOnline_SessionID", true, "SessionID")]
    [BindIndex("IX_NodeOnline_Token", false, "Token")]
    [BindIndex("IX_NodeOnline_UpdateTime", false, "UpdateTime")]
    [BindIndex("IX_NodeOnline_ProvinceID_CityID", false, "ProvinceID,CityID")]
    [BindTable("NodeOnline", Description = "节点在线", ConnName = "StardustData", DbType = DatabaseType.None)]
    public partial class NodeOnline
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

        private String _SessionID;
        /// <summary>会话</summary>
        [DisplayName("会话")]
        [Description("会话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("SessionID", "会话", "")]
        public String SessionID { get => _SessionID; set { if (OnPropertyChanging("SessionID", value)) { _SessionID = value; OnPropertyChanged("SessionID"); } } }

        private Int32 _NodeID;
        /// <summary>节点</summary>
        [DisplayName("节点")]
        [Description("节点")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("NodeID", "节点", "")]
        public Int32 NodeID { get => _NodeID; set { if (OnPropertyChanging("NodeID", value)) { _NodeID = value; OnPropertyChanged("NodeID"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private String _IP;
        /// <summary>本地IP</summary>
        [DisplayName("本地IP")]
        [Description("本地IP")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("IP", "本地IP", "")]
        public String IP { get => _IP; set { if (OnPropertyChanging("IP", value)) { _IP = value; OnPropertyChanged("IP"); } } }

        private String _Category;
        /// <summary>分类</summary>
        [DisplayName("分类")]
        [Description("分类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Category", "分类", "")]
        public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

        private Int32 _ProvinceID;
        /// <summary>省份</summary>
        [DisplayName("省份")]
        [Description("省份")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ProvinceID", "省份", "")]
        public Int32 ProvinceID { get => _ProvinceID; set { if (OnPropertyChanging("ProvinceID", value)) { _ProvinceID = value; OnPropertyChanged("ProvinceID"); } } }

        private Int32 _CityID;
        /// <summary>城市</summary>
        [DisplayName("城市")]
        [Description("城市")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CityID", "城市", "")]
        public Int32 CityID { get => _CityID; set { if (OnPropertyChanging("CityID", value)) { _CityID = value; OnPropertyChanged("CityID"); } } }

        private Int32 _PingCount;
        /// <summary>心跳</summary>
        [DisplayName("心跳")]
        [Description("心跳")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("PingCount", "心跳", "")]
        public Int32 PingCount { get => _PingCount; set { if (OnPropertyChanging("PingCount", value)) { _PingCount = value; OnPropertyChanged("PingCount"); } } }

        private Boolean _WebSocket;
        /// <summary>长连接。WebSocket长连接</summary>
        [DisplayName("长连接")]
        [Description("长连接。WebSocket长连接")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("WebSocket", "长连接。WebSocket长连接", "")]
        public Boolean WebSocket { get => _WebSocket; set { if (OnPropertyChanging("WebSocket", value)) { _WebSocket = value; OnPropertyChanged("WebSocket"); } } }

        private String _Version;
        /// <summary>版本</summary>
        [DisplayName("版本")]
        [Description("版本")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Version", "版本", "")]
        public String Version { get => _Version; set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } } }

        private DateTime _CompileTime;
        /// <summary>编译时间</summary>
        [DisplayName("编译时间")]
        [Description("编译时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CompileTime", "编译时间", "")]
        public DateTime CompileTime { get => _CompileTime; set { if (OnPropertyChanging("CompileTime", value)) { _CompileTime = value; OnPropertyChanged("CompileTime"); } } }

        private Int32 _Memory;
        /// <summary>内存。单位M</summary>
        [DisplayName("内存")]
        [Description("内存。单位M")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Memory", "内存。单位M", "")]
        public Int32 Memory { get => _Memory; set { if (OnPropertyChanging("Memory", value)) { _Memory = value; OnPropertyChanged("Memory"); } } }

        private Int32 _AvailableMemory;
        /// <summary>可用内存。单位M</summary>
        [DisplayName("可用内存")]
        [Description("可用内存。单位M")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("AvailableMemory", "可用内存。单位M", "")]
        public Int32 AvailableMemory { get => _AvailableMemory; set { if (OnPropertyChanging("AvailableMemory", value)) { _AvailableMemory = value; OnPropertyChanged("AvailableMemory"); } } }

        private Int32 _AvailableFreeSpace;
        /// <summary>可用磁盘。应用所在盘，单位M</summary>
        [DisplayName("可用磁盘")]
        [Description("可用磁盘。应用所在盘，单位M")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("AvailableFreeSpace", "可用磁盘。应用所在盘，单位M", "")]
        public Int32 AvailableFreeSpace { get => _AvailableFreeSpace; set { if (OnPropertyChanging("AvailableFreeSpace", value)) { _AvailableFreeSpace = value; OnPropertyChanged("AvailableFreeSpace"); } } }

        private Double _CpuRate;
        /// <summary>CPU率。占用率</summary>
        [DisplayName("CPU率")]
        [Description("CPU率。占用率")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CpuRate", "CPU率。占用率", "")]
        public Double CpuRate { get => _CpuRate; set { if (OnPropertyChanging("CpuRate", value)) { _CpuRate = value; OnPropertyChanged("CpuRate"); } } }

        private Double _Temperature;
        /// <summary>温度</summary>
        [DisplayName("温度")]
        [Description("温度")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Temperature", "温度", "")]
        public Double Temperature { get => _Temperature; set { if (OnPropertyChanging("Temperature", value)) { _Temperature = value; OnPropertyChanged("Temperature"); } } }

        private Double _Battery;
        /// <summary>电量</summary>
        [DisplayName("电量")]
        [Description("电量")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Battery", "电量", "")]
        public Double Battery { get => _Battery; set { if (OnPropertyChanging("Battery", value)) { _Battery = value; OnPropertyChanged("Battery"); } } }

        private Int64 _UplinkSpeed;
        /// <summary>上行速度。网络发送速度，字节每秒</summary>
        [DisplayName("上行速度")]
        [Description("上行速度。网络发送速度，字节每秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UplinkSpeed", "上行速度。网络发送速度，字节每秒", "")]
        public Int64 UplinkSpeed { get => _UplinkSpeed; set { if (OnPropertyChanging("UplinkSpeed", value)) { _UplinkSpeed = value; OnPropertyChanged("UplinkSpeed"); } } }

        private Int64 _DownlinkSpeed;
        /// <summary>下行速度。网络接收速度，字节每秒</summary>
        [DisplayName("下行速度")]
        [Description("下行速度。网络接收速度，字节每秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("DownlinkSpeed", "下行速度。网络接收速度，字节每秒", "")]
        public Int64 DownlinkSpeed { get => _DownlinkSpeed; set { if (OnPropertyChanging("DownlinkSpeed", value)) { _DownlinkSpeed = value; OnPropertyChanged("DownlinkSpeed"); } } }

        private Int32 _ProcessCount;
        /// <summary>进程数</summary>
        [DisplayName("进程数")]
        [Description("进程数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ProcessCount", "进程数", "")]
        public Int32 ProcessCount { get => _ProcessCount; set { if (OnPropertyChanging("ProcessCount", value)) { _ProcessCount = value; OnPropertyChanged("ProcessCount"); } } }

        private Int32 _TcpConnections;
        /// <summary>连接数。传输数据Established的Tcp网络连接数</summary>
        [DisplayName("连接数")]
        [Description("连接数。传输数据Established的Tcp网络连接数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("TcpConnections", "连接数。传输数据Established的Tcp网络连接数", "")]
        public Int32 TcpConnections { get => _TcpConnections; set { if (OnPropertyChanging("TcpConnections", value)) { _TcpConnections = value; OnPropertyChanged("TcpConnections"); } } }

        private Int32 _TcpTimeWait;
        /// <summary>主动关闭。主动关闭后TimeWait的Tcp网络连接数，下一步Closed</summary>
        [DisplayName("主动关闭")]
        [Description("主动关闭。主动关闭后TimeWait的Tcp网络连接数，下一步Closed")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("TcpTimeWait", "主动关闭。主动关闭后TimeWait的Tcp网络连接数，下一步Closed", "")]
        public Int32 TcpTimeWait { get => _TcpTimeWait; set { if (OnPropertyChanging("TcpTimeWait", value)) { _TcpTimeWait = value; OnPropertyChanged("TcpTimeWait"); } } }

        private Int32 _TcpCloseWait;
        /// <summary>被动关闭。被动关闭后CloseWait的Tcp网络连接数，下一步TimeWait</summary>
        [DisplayName("被动关闭")]
        [Description("被动关闭。被动关闭后CloseWait的Tcp网络连接数，下一步TimeWait")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("TcpCloseWait", "被动关闭。被动关闭后CloseWait的Tcp网络连接数，下一步TimeWait", "")]
        public Int32 TcpCloseWait { get => _TcpCloseWait; set { if (OnPropertyChanging("TcpCloseWait", value)) { _TcpCloseWait = value; OnPropertyChanged("TcpCloseWait"); } } }

        private Int32 _Delay;
        /// <summary>延迟。网络延迟，客户端最近一次心跳耗时的一半，单位ms</summary>
        [DisplayName("延迟")]
        [Description("延迟。网络延迟，客户端最近一次心跳耗时的一半，单位ms")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Delay", "延迟。网络延迟，客户端最近一次心跳耗时的一半，单位ms", "")]
        public Int32 Delay { get => _Delay; set { if (OnPropertyChanging("Delay", value)) { _Delay = value; OnPropertyChanged("Delay"); } } }

        private Int32 _Offset;
        /// <summary>偏移。客户端UTC时间减服务端UTC时间，单位ms</summary>
        [DisplayName("偏移")]
        [Description("偏移。客户端UTC时间减服务端UTC时间，单位ms")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Offset", "偏移。客户端UTC时间减服务端UTC时间，单位ms", "")]
        public Int32 Offset { get => _Offset; set { if (OnPropertyChanging("Offset", value)) { _Offset = value; OnPropertyChanged("Offset"); } } }

        private DateTime _LocalTime;
        /// <summary>本地时间</summary>
        [DisplayName("本地时间")]
        [Description("本地时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LocalTime", "本地时间", "")]
        public DateTime LocalTime { get => _LocalTime; set { if (OnPropertyChanging("LocalTime", value)) { _LocalTime = value; OnPropertyChanged("LocalTime"); } } }

        private Int32 _Uptime;
        /// <summary>开机时间。单位s</summary>
        [DisplayName("开机时间")]
        [Description("开机时间。单位s")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Uptime", "开机时间。单位s", "", ItemType = "TimeSpan")]
        public Int32 Uptime { get => _Uptime; set { if (OnPropertyChanging("Uptime", value)) { _Uptime = value; OnPropertyChanged("Uptime"); } } }

        private String _MACs;
        /// <summary>网卡</summary>
        [DisplayName("网卡")]
        [Description("网卡")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("MACs", "网卡", "")]
        public String MACs { get => _MACs; set { if (OnPropertyChanging("MACs", value)) { _MACs = value; OnPropertyChanged("MACs"); } } }

        private String _Processes;
        /// <summary>进程列表</summary>
        [DisplayName("进程列表")]
        [Description("进程列表")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Processes", "进程列表", "")]
        public String Processes { get => _Processes; set { if (OnPropertyChanging("Processes", value)) { _Processes = value; OnPropertyChanged("Processes"); } } }

        private String _Token;
        /// <summary>令牌</summary>
        [DisplayName("令牌")]
        [Description("令牌")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Token", "令牌", "")]
        public String Token { get => _Token; set { if (OnPropertyChanging("Token", value)) { _Token = value; OnPropertyChanged("Token"); } } }

        private String _Data;
        /// <summary>数据</summary>
        [DisplayName("数据")]
        [Description("数据")]
        [DataObjectField(false, false, true, -1)]
        [BindColumn("Data", "数据", "")]
        public String Data { get => _Data; set { if (OnPropertyChanging("Data", value)) { _Data = value; OnPropertyChanged("Data"); } } }

        private String _Creator;
        /// <summary>创建者。服务端节点</summary>
        [Category("扩展")]
        [DisplayName("创建者")]
        [Description("创建者。服务端节点")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Creator", "创建者。服务端节点", "")]
        public String Creator { get => _Creator; set { if (OnPropertyChanging("Creator", value)) { _Creator = value; OnPropertyChanged("Creator"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [Category("扩展")]
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [Category("扩展")]
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [Category("扩展")]
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [Category("扩展")]
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "ID": return _ID;
                    case "SessionID": return _SessionID;
                    case "NodeID": return _NodeID;
                    case "Name": return _Name;
                    case "IP": return _IP;
                    case "Category": return _Category;
                    case "ProvinceID": return _ProvinceID;
                    case "CityID": return _CityID;
                    case "PingCount": return _PingCount;
                    case "WebSocket": return _WebSocket;
                    case "Version": return _Version;
                    case "CompileTime": return _CompileTime;
                    case "Memory": return _Memory;
                    case "AvailableMemory": return _AvailableMemory;
                    case "AvailableFreeSpace": return _AvailableFreeSpace;
                    case "CpuRate": return _CpuRate;
                    case "Temperature": return _Temperature;
                    case "Battery": return _Battery;
                    case "UplinkSpeed": return _UplinkSpeed;
                    case "DownlinkSpeed": return _DownlinkSpeed;
                    case "ProcessCount": return _ProcessCount;
                    case "TcpConnections": return _TcpConnections;
                    case "TcpTimeWait": return _TcpTimeWait;
                    case "TcpCloseWait": return _TcpCloseWait;
                    case "Delay": return _Delay;
                    case "Offset": return _Offset;
                    case "LocalTime": return _LocalTime;
                    case "Uptime": return _Uptime;
                    case "MACs": return _MACs;
                    case "Processes": return _Processes;
                    case "Token": return _Token;
                    case "Data": return _Data;
                    case "Creator": return _Creator;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "SessionID": _SessionID = Convert.ToString(value); break;
                    case "NodeID": _NodeID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "IP": _IP = Convert.ToString(value); break;
                    case "Category": _Category = Convert.ToString(value); break;
                    case "ProvinceID": _ProvinceID = value.ToInt(); break;
                    case "CityID": _CityID = value.ToInt(); break;
                    case "PingCount": _PingCount = value.ToInt(); break;
                    case "WebSocket": _WebSocket = value.ToBoolean(); break;
                    case "Version": _Version = Convert.ToString(value); break;
                    case "CompileTime": _CompileTime = value.ToDateTime(); break;
                    case "Memory": _Memory = value.ToInt(); break;
                    case "AvailableMemory": _AvailableMemory = value.ToInt(); break;
                    case "AvailableFreeSpace": _AvailableFreeSpace = value.ToInt(); break;
                    case "CpuRate": _CpuRate = value.ToDouble(); break;
                    case "Temperature": _Temperature = value.ToDouble(); break;
                    case "Battery": _Battery = value.ToDouble(); break;
                    case "UplinkSpeed": _UplinkSpeed = value.ToLong(); break;
                    case "DownlinkSpeed": _DownlinkSpeed = value.ToLong(); break;
                    case "ProcessCount": _ProcessCount = value.ToInt(); break;
                    case "TcpConnections": _TcpConnections = value.ToInt(); break;
                    case "TcpTimeWait": _TcpTimeWait = value.ToInt(); break;
                    case "TcpCloseWait": _TcpCloseWait = value.ToInt(); break;
                    case "Delay": _Delay = value.ToInt(); break;
                    case "Offset": _Offset = value.ToInt(); break;
                    case "LocalTime": _LocalTime = value.ToDateTime(); break;
                    case "Uptime": _Uptime = value.ToInt(); break;
                    case "MACs": _MACs = Convert.ToString(value); break;
                    case "Processes": _Processes = Convert.ToString(value); break;
                    case "Token": _Token = Convert.ToString(value); break;
                    case "Data": _Data = Convert.ToString(value); break;
                    case "Creator": _Creator = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得节点在线字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>会话</summary>
            public static readonly Field SessionID = FindByName("SessionID");

            /// <summary>节点</summary>
            public static readonly Field NodeID = FindByName("NodeID");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>本地IP</summary>
            public static readonly Field IP = FindByName("IP");

            /// <summary>分类</summary>
            public static readonly Field Category = FindByName("Category");

            /// <summary>省份</summary>
            public static readonly Field ProvinceID = FindByName("ProvinceID");

            /// <summary>城市</summary>
            public static readonly Field CityID = FindByName("CityID");

            /// <summary>心跳</summary>
            public static readonly Field PingCount = FindByName("PingCount");

            /// <summary>长连接。WebSocket长连接</summary>
            public static readonly Field WebSocket = FindByName("WebSocket");

            /// <summary>版本</summary>
            public static readonly Field Version = FindByName("Version");

            /// <summary>编译时间</summary>
            public static readonly Field CompileTime = FindByName("CompileTime");

            /// <summary>内存。单位M</summary>
            public static readonly Field Memory = FindByName("Memory");

            /// <summary>可用内存。单位M</summary>
            public static readonly Field AvailableMemory = FindByName("AvailableMemory");

            /// <summary>可用磁盘。应用所在盘，单位M</summary>
            public static readonly Field AvailableFreeSpace = FindByName("AvailableFreeSpace");

            /// <summary>CPU率。占用率</summary>
            public static readonly Field CpuRate = FindByName("CpuRate");

            /// <summary>温度</summary>
            public static readonly Field Temperature = FindByName("Temperature");

            /// <summary>电量</summary>
            public static readonly Field Battery = FindByName("Battery");

            /// <summary>上行速度。网络发送速度，字节每秒</summary>
            public static readonly Field UplinkSpeed = FindByName("UplinkSpeed");

            /// <summary>下行速度。网络接收速度，字节每秒</summary>
            public static readonly Field DownlinkSpeed = FindByName("DownlinkSpeed");

            /// <summary>进程数</summary>
            public static readonly Field ProcessCount = FindByName("ProcessCount");

            /// <summary>连接数。传输数据Established的Tcp网络连接数</summary>
            public static readonly Field TcpConnections = FindByName("TcpConnections");

            /// <summary>主动关闭。主动关闭后TimeWait的Tcp网络连接数，下一步Closed</summary>
            public static readonly Field TcpTimeWait = FindByName("TcpTimeWait");

            /// <summary>被动关闭。被动关闭后CloseWait的Tcp网络连接数，下一步TimeWait</summary>
            public static readonly Field TcpCloseWait = FindByName("TcpCloseWait");

            /// <summary>延迟。网络延迟，客户端最近一次心跳耗时的一半，单位ms</summary>
            public static readonly Field Delay = FindByName("Delay");

            /// <summary>偏移。客户端UTC时间减服务端UTC时间，单位ms</summary>
            public static readonly Field Offset = FindByName("Offset");

            /// <summary>本地时间</summary>
            public static readonly Field LocalTime = FindByName("LocalTime");

            /// <summary>开机时间。单位s</summary>
            public static readonly Field Uptime = FindByName("Uptime");

            /// <summary>网卡</summary>
            public static readonly Field MACs = FindByName("MACs");

            /// <summary>进程列表</summary>
            public static readonly Field Processes = FindByName("Processes");

            /// <summary>令牌</summary>
            public static readonly Field Token = FindByName("Token");

            /// <summary>数据</summary>
            public static readonly Field Data = FindByName("Data");

            /// <summary>创建者。服务端节点</summary>
            public static readonly Field Creator = FindByName("Creator");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName("UpdateIP");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得节点在线字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>会话</summary>
            public const String SessionID = "SessionID";

            /// <summary>节点</summary>
            public const String NodeID = "NodeID";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>本地IP</summary>
            public const String IP = "IP";

            /// <summary>分类</summary>
            public const String Category = "Category";

            /// <summary>省份</summary>
            public const String ProvinceID = "ProvinceID";

            /// <summary>城市</summary>
            public const String CityID = "CityID";

            /// <summary>心跳</summary>
            public const String PingCount = "PingCount";

            /// <summary>长连接。WebSocket长连接</summary>
            public const String WebSocket = "WebSocket";

            /// <summary>版本</summary>
            public const String Version = "Version";

            /// <summary>编译时间</summary>
            public const String CompileTime = "CompileTime";

            /// <summary>内存。单位M</summary>
            public const String Memory = "Memory";

            /// <summary>可用内存。单位M</summary>
            public const String AvailableMemory = "AvailableMemory";

            /// <summary>可用磁盘。应用所在盘，单位M</summary>
            public const String AvailableFreeSpace = "AvailableFreeSpace";

            /// <summary>CPU率。占用率</summary>
            public const String CpuRate = "CpuRate";

            /// <summary>温度</summary>
            public const String Temperature = "Temperature";

            /// <summary>电量</summary>
            public const String Battery = "Battery";

            /// <summary>上行速度。网络发送速度，字节每秒</summary>
            public const String UplinkSpeed = "UplinkSpeed";

            /// <summary>下行速度。网络接收速度，字节每秒</summary>
            public const String DownlinkSpeed = "DownlinkSpeed";

            /// <summary>进程数</summary>
            public const String ProcessCount = "ProcessCount";

            /// <summary>连接数。传输数据Established的Tcp网络连接数</summary>
            public const String TcpConnections = "TcpConnections";

            /// <summary>主动关闭。主动关闭后TimeWait的Tcp网络连接数，下一步Closed</summary>
            public const String TcpTimeWait = "TcpTimeWait";

            /// <summary>被动关闭。被动关闭后CloseWait的Tcp网络连接数，下一步TimeWait</summary>
            public const String TcpCloseWait = "TcpCloseWait";

            /// <summary>延迟。网络延迟，客户端最近一次心跳耗时的一半，单位ms</summary>
            public const String Delay = "Delay";

            /// <summary>偏移。客户端UTC时间减服务端UTC时间，单位ms</summary>
            public const String Offset = "Offset";

            /// <summary>本地时间</summary>
            public const String LocalTime = "LocalTime";

            /// <summary>开机时间。单位s</summary>
            public const String Uptime = "Uptime";

            /// <summary>网卡</summary>
            public const String MACs = "MACs";

            /// <summary>进程列表</summary>
            public const String Processes = "Processes";

            /// <summary>令牌</summary>
            public const String Token = "Token";

            /// <summary>数据</summary>
            public const String Data = "Data";

            /// <summary>创建者。服务端节点</summary>
            public const String Creator = "Creator";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>更新地址</summary>
            public const String UpdateIP = "UpdateIP";
        }
        #endregion
    }
}
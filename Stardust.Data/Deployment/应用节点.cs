﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace Stardust.Data.Deployment
{
    /// <summary>应用节点。应用和节点服务器的依赖关系</summary>
    [Serializable]
    [DataObject]
    [Description("应用节点。应用和节点服务器的依赖关系")]
    [BindIndex("IX_AppDeployNode_AppId", false, "AppId")]
    [BindIndex("IX_AppDeployNode_NodeId", false, "NodeId")]
    [BindTable("AppDeployNode", Description = "应用节点。应用和节点服务器的依赖关系", ConnName = "Stardust", DbType = DatabaseType.None)]
    public partial class AppDeployNode
    {
        #region 属性
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("Id", "编号", "")]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private Int32 _AppId;
        /// <summary>应用部署集。对应AppDeploy</summary>
        [DisplayName("应用部署集")]
        [Description("应用部署集。对应AppDeploy")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("AppId", "应用部署集。对应AppDeploy", "")]
        public Int32 AppId { get => _AppId; set { if (OnPropertyChanging("AppId", value)) { _AppId = value; OnPropertyChanged("AppId"); } } }

        private Int32 _NodeId;
        /// <summary>节点。节点服务器</summary>
        [DisplayName("节点")]
        [Description("节点。节点服务器")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("NodeId", "节点。节点服务器", "")]
        public Int32 NodeId { get => _NodeId; set { if (OnPropertyChanging("NodeId", value)) { _NodeId = value; OnPropertyChanged("NodeId"); } } }

        private String _IP;
        /// <summary>IP地址。节点所在内网IP地址</summary>
        [DisplayName("IP地址")]
        [Description("IP地址。节点所在内网IP地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("IP", "IP地址。节点所在内网IP地址", "")]
        public String IP { get => _IP; set { if (OnPropertyChanging("IP", value)) { _IP = value; OnPropertyChanged("IP"); } } }

        private Int32 _Sort;
        /// <summary>顺序。较大在前</summary>
        [DisplayName("顺序")]
        [Description("顺序。较大在前")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Sort", "顺序。较大在前", "")]
        public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private String _Environment;
        /// <summary>环境。prod/test/dev/uat等</summary>
        [DisplayName("环境")]
        [Description("环境。prod/test/dev/uat等")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Environment", "环境。prod/test/dev/uat等", "")]
        public String Environment { get => _Environment; set { if (OnPropertyChanging("Environment", value)) { _Environment = value; OnPropertyChanged("Environment"); } } }

        private Int32 _ProcessId;
        /// <summary>进程</summary>
        [DisplayName("进程")]
        [Description("进程")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ProcessId", "进程", "")]
        public Int32 ProcessId { get => _ProcessId; set { if (OnPropertyChanging("ProcessId", value)) { _ProcessId = value; OnPropertyChanged("ProcessId"); } } }

        private String _ProcessName;
        /// <summary>进程名称</summary>
        [DisplayName("进程名称")]
        [Description("进程名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ProcessName", "进程名称", "")]
        public String ProcessName { get => _ProcessName; set { if (OnPropertyChanging("ProcessName", value)) { _ProcessName = value; OnPropertyChanged("ProcessName"); } } }

        private String _UserName;
        /// <summary>用户名。启动该进程的用户名</summary>
        [DisplayName("用户名")]
        [Description("用户名。启动该进程的用户名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UserName", "用户名。启动该进程的用户名", "")]
        public String UserName { get => _UserName; set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } } }

        private DateTime _StartTime;
        /// <summary>进程时间</summary>
        [DisplayName("进程时间")]
        [Description("进程时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("StartTime", "进程时间", "")]
        public DateTime StartTime { get => _StartTime; set { if (OnPropertyChanging("StartTime", value)) { _StartTime = value; OnPropertyChanged("StartTime"); } } }

        private String _Version;
        /// <summary>版本。客户端</summary>
        [DisplayName("版本")]
        [Description("版本。客户端")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Version", "版本。客户端", "")]
        public String Version { get => _Version; set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } } }

        private DateTime _Compile;
        /// <summary>编译时间。客户端</summary>
        [DisplayName("编译时间")]
        [Description("编译时间。客户端")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("Compile", "编译时间。客户端", "")]
        public DateTime Compile { get => _Compile; set { if (OnPropertyChanging("Compile", value)) { _Compile = value; OnPropertyChanged("Compile"); } } }

        private DateTime _LastActive;
        /// <summary>最后活跃。最后一次上报心跳的时间</summary>
        [DisplayName("最后活跃")]
        [Description("最后活跃。最后一次上报心跳的时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastActive", "最后活跃。最后一次上报心跳的时间", "")]
        public DateTime LastActive { get => _LastActive; set { if (OnPropertyChanging("LastActive", value)) { _LastActive = value; OnPropertyChanged("LastActive"); } } }

        private Int32 _CreateUserId;
        /// <summary>创建人</summary>
        [Category("扩展")]
        [DisplayName("创建人")]
        [Description("创建人")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserId", "创建人", "")]
        public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

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

        private Int32 _UpdateUserId;
        /// <summary>更新者</summary>
        [Category("扩展")]
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserId", "更新者", "")]
        public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

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

        private String _Remark;
        /// <summary>备注</summary>
        [Category("扩展")]
        [DisplayName("备注")]
        [Description("备注")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "备注", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
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
                    case "Id": return _Id;
                    case "AppId": return _AppId;
                    case "NodeId": return _NodeId;
                    case "IP": return _IP;
                    case "Sort": return _Sort;
                    case "Enable": return _Enable;
                    case "Environment": return _Environment;
                    case "ProcessId": return _ProcessId;
                    case "ProcessName": return _ProcessName;
                    case "UserName": return _UserName;
                    case "StartTime": return _StartTime;
                    case "Version": return _Version;
                    case "Compile": return _Compile;
                    case "LastActive": return _LastActive;
                    case "CreateUserId": return _CreateUserId;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateUserId": return _UpdateUserId;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "AppId": _AppId = value.ToInt(); break;
                    case "NodeId": _NodeId = value.ToInt(); break;
                    case "IP": _IP = Convert.ToString(value); break;
                    case "Sort": _Sort = value.ToInt(); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "Environment": _Environment = Convert.ToString(value); break;
                    case "ProcessId": _ProcessId = value.ToInt(); break;
                    case "ProcessName": _ProcessName = Convert.ToString(value); break;
                    case "UserName": _UserName = Convert.ToString(value); break;
                    case "StartTime": _StartTime = value.ToDateTime(); break;
                    case "Version": _Version = Convert.ToString(value); break;
                    case "Compile": _Compile = value.ToDateTime(); break;
                    case "LastActive": _LastActive = value.ToDateTime(); break;
                    case "CreateUserId": _CreateUserId = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得应用节点字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>应用部署集。对应AppDeploy</summary>
            public static readonly Field AppId = FindByName("AppId");

            /// <summary>节点。节点服务器</summary>
            public static readonly Field NodeId = FindByName("NodeId");

            /// <summary>IP地址。节点所在内网IP地址</summary>
            public static readonly Field IP = FindByName("IP");

            /// <summary>顺序。较大在前</summary>
            public static readonly Field Sort = FindByName("Sort");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>环境。prod/test/dev/uat等</summary>
            public static readonly Field Environment = FindByName("Environment");

            /// <summary>进程</summary>
            public static readonly Field ProcessId = FindByName("ProcessId");

            /// <summary>进程名称</summary>
            public static readonly Field ProcessName = FindByName("ProcessName");

            /// <summary>用户名。启动该进程的用户名</summary>
            public static readonly Field UserName = FindByName("UserName");

            /// <summary>进程时间</summary>
            public static readonly Field StartTime = FindByName("StartTime");

            /// <summary>版本。客户端</summary>
            public static readonly Field Version = FindByName("Version");

            /// <summary>编译时间。客户端</summary>
            public static readonly Field Compile = FindByName("Compile");

            /// <summary>最后活跃。最后一次上报心跳的时间</summary>
            public static readonly Field LastActive = FindByName("LastActive");

            /// <summary>创建人</summary>
            public static readonly Field CreateUserId = FindByName("CreateUserId");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>更新者</summary>
            public static readonly Field UpdateUserId = FindByName("UpdateUserId");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName("UpdateIP");

            /// <summary>备注</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得应用节点字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>应用部署集。对应AppDeploy</summary>
            public const String AppId = "AppId";

            /// <summary>节点。节点服务器</summary>
            public const String NodeId = "NodeId";

            /// <summary>IP地址。节点所在内网IP地址</summary>
            public const String IP = "IP";

            /// <summary>顺序。较大在前</summary>
            public const String Sort = "Sort";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>环境。prod/test/dev/uat等</summary>
            public const String Environment = "Environment";

            /// <summary>进程</summary>
            public const String ProcessId = "ProcessId";

            /// <summary>进程名称</summary>
            public const String ProcessName = "ProcessName";

            /// <summary>用户名。启动该进程的用户名</summary>
            public const String UserName = "UserName";

            /// <summary>进程时间</summary>
            public const String StartTime = "StartTime";

            /// <summary>版本。客户端</summary>
            public const String Version = "Version";

            /// <summary>编译时间。客户端</summary>
            public const String Compile = "Compile";

            /// <summary>最后活跃。最后一次上报心跳的时间</summary>
            public const String LastActive = "LastActive";

            /// <summary>创建人</summary>
            public const String CreateUserId = "CreateUserId";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>更新者</summary>
            public const String UpdateUserId = "UpdateUserId";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>更新地址</summary>
            public const String UpdateIP = "UpdateIP";

            /// <summary>备注</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}
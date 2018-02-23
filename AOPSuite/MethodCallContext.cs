using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// 方法调用上下文
    /// </summary>
    public class MethodCallContext
    {
        #region 方法名常量

        internal const string StrSetMethod = "set_ThisMethod";

        internal const string StrSetParameter = "set_InputParameterns";

        internal const string StrSetIfReturnVoid = "set_IfReturnVoid";

        internal const string StrSetRetrunValue = "set_RetrunValue"; 
        #endregion

        /// <summary>
        /// 调用的方法封装
        /// </summary>
        private MethodBase m_thisMethod = null;

        /// <summary>
        /// 返回值
        /// </summary>
        private object m_retrunValue = null;

        /// <summary>
        /// 返回值是否为Void
        /// </summary>
        private bool m_ifReturnVoid = true;

        /// <summary>
        /// 输入参数列表
        /// </summary>
        private object[] m_inputParameterns = null;

        /// <summary>
        /// 调用的方法封装
        /// </summary>
        public MethodBase ThisMethod
        {
            get
            {
                return m_thisMethod;
            }

            set
            {
                m_thisMethod = value;
            }
        }

        /// <summary>
        /// 返回值
        /// </summary>
        public object RetrunValue
        {
            get
            {
                return m_retrunValue;
            }

            set
            {
                m_retrunValue = value;
            }
        }

        /// <summary>
        /// 返回值是否为Void
        /// </summary>
        public bool IfReturnVoid
        {
            get
            {
                return m_ifReturnVoid;
            }

            set
            {
                m_ifReturnVoid = value;
            }
        }

        /// <summary>
        /// 输入参数列表
        /// </summary>
        public object[] InputParameterns
        {
            get
            {
                return m_inputParameterns;
            }

            set
            {
                m_inputParameterns = value;
            }
        }
    }
}

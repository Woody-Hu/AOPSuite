using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// 使用的虚拟代理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UseProxy<T> : RealProxy
        where T:class
    {
        /// <summary>
        /// 代理源
        /// </summary>
        private T m_Tag = null;

        /// <summary>
        /// 构造虚拟代理
        /// </summary>
        /// <param name="input"></param>
        internal UseProxy(T input):base(typeof(T))
        {
            m_Tag = input;
        }

        /// <summary>
        /// 消息拦截
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage callMsg = msg as IMethodCallMessage;
            object[] args = callMsg.Args;
            IMessage message;
            MethodInfo useMethod = null;
            List<VoidDoAttribute> lstVoidDoAttribue = new List<VoidDoAttribute>();
            MethodCallContext thisContext = new AOPSuite.MethodCallContext();
            thisContext.ThisMethod = callMsg.MethodBase;
            thisContext.InputParameterns = callMsg.Args;
            try
            {
                useMethod = FindMethod(callMsg);
                Utility.GetVoidAttribute(useMethod, ref lstVoidDoAttribue);

                foreach (var oneAttribute in lstVoidDoAttribue)
                {
                    oneAttribute.DoBeforeMethod(thisContext);
                }

                object o = callMsg.MethodBase.Invoke(m_Tag, args);
                message = new ReturnMessage(o, args, args.Length - callMsg.InArgCount, callMsg.LogicalCallContext, callMsg);

                thisContext.RetrunValue = o;
                thisContext.IfReturnVoid = (callMsg.MethodBase as MethodInfo).ReturnType == typeof(void);

                for (int indexOfAttribute = lstVoidDoAttribue.Count - 1; indexOfAttribute >= 0; indexOfAttribute--)
                {
                    lstVoidDoAttribue[indexOfAttribute].DoAfterMethod(thisContext);
                }
            }
            catch (Exception e)
            {
                message = new ReturnMessage(e, callMsg);
            }

            return message;
        }

        /// <summary>
        /// 获得对象
        /// </summary>
        /// <returns></returns>
        internal object GetObject()
        {
            return this.GetTransparentProxy();
        }

        /// <summary>
        /// 寻找方法
        /// </summary>
        /// <param name="inputMessage">输入的方法消息</param>
        /// <returns>找到的方法封装</returns>
        private MethodInfo FindMethod(IMethodCallMessage inputMessage)
        {
            Type useType = m_Tag.GetType();

            foreach (var oneMethod in useType.GetRuntimeMethods())
            {
                if (oneMethod.Name.Equals(inputMessage.MethodName) && 
                    oneMethod.GetParameters().Count() == inputMessage.ArgCount)
                {
                    for (int argumentIndex = 0; argumentIndex < inputMessage.ArgCount; argumentIndex++)
                    {
                        if (oneMethod.GetParameters()[argumentIndex].ParameterType != inputMessage.Args[argumentIndex].GetType())
                        {
                            continue;
                        }
                    }
                    return oneMethod;
                }
            }
            return null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// AOP特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false,Inherited = false)]
    public class VoidDoAttribute:Attribute
    {
        /// <summary>
        /// 执行前方法
        /// </summary>
        /// <param name="inputContext"></param>
        public virtual void DoBeforeMethod(MethodCallContext inputContext)
        {
            ;
        }

        /// <summary>
        /// 执行后方法
        /// </summary>
        /// <param name="inputContext"></param>
        public virtual void DoAfterMethod(MethodCallContext inputContext)
        {
            ;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// 虚拟代理工厂
    /// </summary>
    public class RealProxyFactory
    {
        /// <summary>
        /// 创建一个虚拟代理
        /// </summary>
        /// <typeparam name="T">输入的泛型参数</typeparam>
        /// <param name="input">输入的代理源</param>
        /// <returns>创建的虚拟代理</returns>
        public T CreatProxy<T>(T input)
                    where T : class
        {
            UseProxy<T> useProxy = new UseProxy<T>(input);
            return useProxy.GetObject() as T;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// 公共方法组
    /// </summary>
    public static class Utility
    {
        public const string strGetMethodInfo = "GetMethodInfo";

        public const string strGetUseAttribute = "GetUseAttribute";

        public const string strUseAttribute = "UseAttribute";

        public const string m_strRef = "&";

        /// <summary>
        /// 获得需要特性
        /// </summary>
        /// <param name="useMethod"></param>
        /// <param name="lstVoidDoAttribue"></param>
        internal static void GetVoidAttribute(MethodInfo useMethod,ref List<VoidDoAttribute> lstVoidDoAttribue)
        {
            if (null == lstVoidDoAttribue)
            {
                lstVoidDoAttribue = new List<VoidDoAttribute>();
            }
            if (null != useMethod)
            {
                foreach (var oneAttribute in useMethod.GetCustomAttributes(typeof(VoidDoAttribute)))
                {
                    lstVoidDoAttribue.Add(oneAttribute as VoidDoAttribute);
                }
            }
        }

        /// <summary>
        /// 获得方法封装
        /// </summary>
        /// <param name="input"></param>
        /// <param name="strMethodName"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo(object input,string strMethodName,object[] inputParams)
        {
            try
            {
                Type inputType = input.GetType();

                Type[] paramTypes = (from n in inputParams select n.GetType()).ToArray();

                MethodInfo useMethodInfo = inputType.GetMethod(strMethodName);

                return useMethodInfo;
            }
            catch (Exception)
            {
                return null;
            }
          
        }

        /// <summary>
        /// 获得一个非引用类型
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static Type GetNonRefType(Type input)
        {
            if (!input.Name.Contains(m_strRef))
            {
                return input;
            }
            else
            {
                try
                {
                    string tempName = Regex.Replace(input.FullName, m_strRef, string.Empty);
                    Type useType = input.Assembly.GetType(tempName);
                    return useType;
                }
                catch (Exception)
                {
                    return typeof(int);
                }
               
            }
        }

        /// <summary>
        /// 获得使用特性
        /// </summary>
        /// <param name="inputContext"></param>
        /// <returns></returns>
        public static List<VoidDoAttribute> GetUseAttribute(MethodCallContext inputContext)
        {
            List<VoidDoAttribute> returnValue = new List<VoidDoAttribute>();

            try
            {
                GetVoidAttribute(inputContext.ThisMethod as MethodInfo, ref returnValue);
            }
            catch (Exception)
            {
                ;
            }

            return returnValue;
        }

        /// <summary>
        /// 将上下文应用到特性
        /// </summary>
        /// <param name="inputAttribute"></param>
        /// <param name="inputContext"></param>
        /// <param name="useBeforeTag"></param>
        public static void UseAttribute(List<VoidDoAttribute> inputAttribute, MethodCallContext inputContext,int useBeforeTag)
        {
            if (null == inputAttribute || null == inputContext)
            {
                return;
            }

            if (1 == useBeforeTag)
            {
                foreach (var oneAttribute in inputAttribute)
                {
                    if (null == oneAttribute)
                    {
                        continue;
                    }
                    oneAttribute.DoBeforeMethod(inputContext);
                }
            }
            else
            {
                for (int tempIndx = inputAttribute.Count -1; tempIndx >=0 ; tempIndx--)
                {
                    if (null == inputAttribute[tempIndx])
                    {
                        continue;
                    }
                    inputAttribute[tempIndx].DoAfterMethod(inputContext);
                }

            }
           
        }
    }
}

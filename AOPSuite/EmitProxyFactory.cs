using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AOPSuite
{
    /// <summary>
    /// Emit代理工厂
    /// </summary>
    public class EmitProxyFactory
    {
        #region 常量
        /// <summary>
        /// 基础程序集名称
        /// </summary>
        private const string m_strBaseAssemblyName = "EmitAssembly_";

        /// <summary>
        /// 基础模块名称
        /// </summary>
        private const string m_strBaseModelName = "EmitModel_";

        /// <summary>
        /// 基础类型名称
        /// </summary>
        private const string m_strBaseTypeName = "EmitType_";

        /// <summary>
        /// 转换为Boolean字符串
        /// </summary>
        private const string m_strConvertToBoolean = "ToBoolean";

        /// <summary>
        /// 使用的类型签名
        /// </summary>
        private const TypeAttributes m_useTypeAttributes = TypeAttributes.Public | TypeAttributes.Class;

        /// <summary>
        /// 使用的方法签名
        /// </summary>
        private const MethodAttributes m_useMethodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual;
        #endregion

        /// <summary>
        /// 使用的类型缓存
        /// </summary>
        private Dictionary<Type, Type> m_dicTypeCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 使用的动态程序集
        /// </summary>
        private AssemblyBuilder m_useAssemblyBuilder = null;

        /// <summary>
        /// 创建代理实例
        /// </summary>
        /// <typeparam name="T">泛型参数</typeparam>
        /// <param name="inputParams">构造参数</param>
        /// <returns>创造的实例</returns>
        public T CreatInstance<T>(params object[] inputParams)
            where T : class
        {
            Type useType = null;
            if (!m_dicTypeCache.TryGetValue(typeof(T),out useType))
            {
                useType = BuiltType(typeof(T));
            }
            
            return (T)Activator.CreateInstance(useType, inputParams);
        }

        /// <summary>
        /// 创建类型
        /// </summary>
        /// <param name="inputBaseType">输入的底层类</param>
        /// <returns>创建的类型</returns>
        private Type BuiltType(Type inputBaseType)
        {
            //判断是否有程序集缓存
            if (null == m_useAssemblyBuilder)
            {
                m_useAssemblyBuilder = PrepareAssemBlyBuilder();
            }

            //创建动态模块
            ModuleBuilder tempUseModuleBuilder = m_useAssemblyBuilder.DefineDynamicModule
                (m_strBaseModelName + inputBaseType.Name);

            //创建动态类型
            TypeBuilder tempUseTypeBuilder = tempUseModuleBuilder.DefineType
                (m_strBaseTypeName + inputBaseType.Name, m_useTypeAttributes, inputBaseType);

            //创建构造方法
            BuiltConstructorMethod(inputBaseType, tempUseTypeBuilder);

            //创建方法
            BuiltMethod(inputBaseType, tempUseTypeBuilder);

            return tempUseTypeBuilder.CreateType();
        }

        /// <summary>
        /// 创建方法
        /// </summary>
        /// <param name="inputBaseType">输入的基类</param>
        /// <param name="useTypeBuilder">输入的类型建造器</param>
        private void BuiltMethod(Type inputBaseType,TypeBuilder useTypeBuilder)
        {
            //循环所有的方法
            foreach (var oneMethod in inputBaseType.GetMethods())
            {
                //只扩展虚方法
                if (!(oneMethod.IsVirtual || oneMethod.IsAbstract))
                {
                    continue;
                }

                //获得所有的参数
                var useparams = oneMethod.GetParameters();

                //所有的参数类型
                var paramTypes = from n in useparams select n.ParameterType;

                int paramSize = paramTypes.Count();

                //创建方法
                MethodBuilder useMethodBuilder = MakeMethodBuilder(useTypeBuilder, oneMethod, useparams, paramTypes);

                //IL解释器
                ILGenerator tempUseGenerator = useMethodBuilder.GetILGenerator();

                //创建上下文局部变量
                LocalBuilder tempContext = CreatContext(tempUseGenerator);

                //创建返回值局部变量
                LocalBuilder tempReturnValue = null;
                if (oneMethod.ReturnType != typeof(void))
                {
                    tempReturnValue = tempUseGenerator.DeclareLocal(oneMethod.ReturnType);
                }

                //创建输入参数局部变量
                LocalBuilder tempInputParameter = MakeParameter(paramTypes, paramSize, tempUseGenerator);

                //设置上下文局部变量
                SetContextNoneReturnValue(oneMethod, tempUseGenerator, tempContext, tempReturnValue, tempInputParameter);

                //创建特性列表局部变量
                LocalBuilder lstAttributes = PrepareAttribute(tempUseGenerator, tempContext);

                //将上下文应用在特性上
                UseAttribute(tempUseGenerator, tempContext, lstAttributes, true);

                //调用基类方法
                CallBaseMethod(oneMethod, paramTypes, tempUseGenerator);

                //若有返回值
                if (null != tempReturnValue)
                {
                    //设置返回值上下文
                    PrepareRetrunContext(tempUseGenerator, tempContext, tempReturnValue,oneMethod);
                    //将上下文应用在特性上
                    UseAttribute(tempUseGenerator, tempContext, lstAttributes, false);
                    //将返回值放在操作栈上
                    tempUseGenerator.Emit(OpCodes.Ldloc, tempReturnValue);
                }

                //返回
                tempUseGenerator.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 准备返回值上下文
        /// </summary>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <param name="tempContext">使用的上下文局部变量</param>
        /// <param name="tempReturnValue">使用的返回值局部变量</param>
        /// <param name="inputMethodInfo">使用的方法信息</param>
        private void PrepareRetrunContext(ILGenerator tempUseGenerator, LocalBuilder tempContext, LocalBuilder tempReturnValue,MethodInfo inputMethodInfo)
        {
            tempUseGenerator.Emit(OpCodes.Stloc, tempReturnValue);
            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Ldloc, tempReturnValue);
            tempUseGenerator.Emit(OpCodes.Box, inputMethodInfo.ReturnType);
            tempUseGenerator.Emit(OpCodes.Call, typeof(MethodCallContext).GetMethod(MethodCallContext.StrSetRetrunValue));
        }

        /// <summary>
        /// 应用特性
        /// </summary>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <param name="tempContext">使用的上下文局部变量</param>
        /// <param name="lstAttributes">使用的特性列表局部变量</param>
        /// <param name="ifStart">Before/After</param>
        private void UseAttribute(ILGenerator tempUseGenerator, LocalBuilder tempContext, LocalBuilder lstAttributes,bool ifStart)
        {
            int useBeforeTag = ifStart? 1:0;
            tempUseGenerator.Emit(OpCodes.Ldloc, lstAttributes);
            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Ldc_I4, useBeforeTag);
            tempUseGenerator.Emit(OpCodes.Call, typeof(Utility).GetMethod(Utility.strUseAttribute));
        }

        /// <summary>
        /// 准备特性列表
        /// </summary>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <param name="tempContext">使用的上下文局部变量</param>
        /// <returns>创建的特性列表局部变量</returns>
        private LocalBuilder PrepareAttribute(ILGenerator tempUseGenerator, LocalBuilder tempContext)
        {
            LocalBuilder lstAttributes = tempUseGenerator.DeclareLocal(typeof(List<VoidDoAttribute>));

            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Call, typeof(Utility).GetMethod(Utility.strGetUseAttribute));
            tempUseGenerator.Emit(OpCodes.Stloc, lstAttributes);
            return lstAttributes;
        }

        /// <summary>
        /// 设置方法调用上下文
        /// </summary>
        /// <param name="oneMethod">使用的方法封装</param>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <param name="tempContext">使用的上下文局部变量</param>
        /// <param name="tempReturnValue">使用的返回值局部变量</param>
        /// <param name="tempInputParameter">使用的参数局部变量</param>
        private void SetContextNoneReturnValue
            (MethodInfo oneMethod, ILGenerator tempUseGenerator, LocalBuilder tempContext, LocalBuilder tempReturnValue, LocalBuilder tempInputParameter)
        {
            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Ldarg_0);
            tempUseGenerator.Emit(OpCodes.Ldstr, oneMethod.Name);
            tempUseGenerator.Emit(OpCodes.Ldloc, tempInputParameter);
            tempUseGenerator.Emit(OpCodes.Call, typeof(Utility).GetMethod(Utility.strGetMethodInfo));
            tempUseGenerator.Emit(OpCodes.Call, typeof(MethodCallContext).GetMethod(MethodCallContext.StrSetMethod));

            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Ldloc, tempInputParameter);
            tempUseGenerator.Emit(OpCodes.Call, typeof(MethodCallContext).GetMethod(MethodCallContext.StrSetParameter));

            int useValue = null == tempReturnValue ? 1 : 0;
            tempUseGenerator.Emit(OpCodes.Ldloc, tempContext);
            tempUseGenerator.Emit(OpCodes.Ldc_I4, useValue);
            tempUseGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod(m_strConvertToBoolean, new Type[] { typeof(int) }));
            tempUseGenerator.Emit(OpCodes.Call, typeof(MethodCallContext).GetMethod(MethodCallContext.StrSetIfReturnVoid));
        }

        /// <summary>
        /// 创建参数列表局部变量
        /// </summary>
        /// <param name="paramTypes">输入的参数类型</param>
        /// <param name="paramSize">使用的参数量</param>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <returns>创建的参数列表局部变量</returns>
        private LocalBuilder MakeParameter(IEnumerable<Type> paramTypes, int paramSize, ILGenerator tempUseGenerator)
        {
            LocalBuilder tempInputParameter = tempUseGenerator.DeclareLocal(typeof(object[]));

            tempUseGenerator.Emit(OpCodes.Ldc_I4, paramSize);
            tempUseGenerator.Emit(OpCodes.Newarr, typeof(object));
            tempUseGenerator.Emit(OpCodes.Stloc, tempInputParameter);

            for (int tempIndex = 0; tempIndex < paramSize; tempIndex++)
            {
                tempUseGenerator.Emit(OpCodes.Ldloc, tempInputParameter);
                tempUseGenerator.Emit(OpCodes.Ldc_I4, tempIndex);
                tempUseGenerator.Emit(OpCodes.Ldarg, tempIndex + 1);
                Type useType = Utility.GetNonRefType(paramTypes.ElementAt(tempIndex));
                tempUseGenerator.Emit(OpCodes.Box, useType);
                tempUseGenerator.Emit(OpCodes.Stelem_Ref);
            }

            return tempInputParameter;
        }

        /// <summary>
        /// 创建上下文局部变量
        /// </summary>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        /// <returns>创建的上下文局部变量</returns>
        private LocalBuilder CreatContext(ILGenerator tempUseGenerator)
        {
            LocalBuilder tempContext = null;
            tempContext = tempUseGenerator.DeclareLocal(typeof(MethodCallContext));
            tempUseGenerator.Emit(OpCodes.Newobj, typeof(MethodCallContext).GetConstructor(Type.EmptyTypes));
            tempUseGenerator.Emit(OpCodes.Stloc, tempContext);
            return tempContext;
        }

        /// <summary>
        /// 创建方法编辑器
        /// </summary>
        /// <param name="useTypeBuilder">使用的类型编辑器</param>
        /// <param name="oneMethod">输入的方法封装</param>
        /// <param name="useparams">输入的参数列表</param>
        /// <param name="paramTypes">输入的参数类型列表</param>
        /// <returns>方法编辑器</returns>
        private  MethodBuilder MakeMethodBuilder(TypeBuilder useTypeBuilder, MethodInfo oneMethod, ParameterInfo[] useparams, IEnumerable<Type> paramTypes)
        {
            MethodBuilder useMethodBuilder = useTypeBuilder.
                DefineMethod(oneMethod.Name, m_useMethodAttributes, oneMethod.ReturnType, paramTypes.ToArray());

            int paramSize = paramTypes.Count();

            for (int tempIndex = 0; tempIndex < paramSize; tempIndex++)
            {
                useMethodBuilder.DefineParameter(tempIndex + 1, useparams[tempIndex].Attributes, useparams[tempIndex].Name);
            }

            useMethodBuilder.SetParameters(paramTypes.ToArray());
            return useMethodBuilder;
        }

        /// <summary>
        /// 调用底层方法
        /// </summary>
        /// <param name="oneMethod">输入的方法封装</param>
        /// <param name="paramTypes">输入的参数类型列表</param>
        /// <param name="tempUseGenerator">使用的IL解释器</param>
        private void CallBaseMethod(MethodInfo oneMethod, IEnumerable<Type> paramTypes, ILGenerator tempUseGenerator)
        {
            int paramSize = paramTypes.Count();

            for (int tempIndex = 0; tempIndex <= paramSize; tempIndex++)
            {
                LoadParameter(tempUseGenerator, tempIndex);
            }

            tempUseGenerator.Emit(OpCodes.Call, oneMethod);
        }

        /// <summary>
        /// 创建构造方法
        /// </summary>
        /// <param name="inputBaseType">输入的基类类型</param>
        /// <param name="useTypeBuilder">输入的类型构造器</param>
        private void BuiltConstructorMethod(Type inputBaseType,TypeBuilder useTypeBuilder)
        {

            foreach (var oneConstructors in inputBaseType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var paramTypes = from n in oneConstructors.GetParameters() select n.ParameterType;

                var tempConstructors = useTypeBuilder.DefineConstructor
                    (MethodAttributes.Public, CallingConventions.Standard, paramTypes.ToArray());

                ILGenerator tempUseGenerator = tempConstructors.GetILGenerator();

                int patamTypeSize = paramTypes.Count();

                for (int tempParameterIndx = 0; tempParameterIndx <= patamTypeSize; tempParameterIndx++)
                {
                    LoadParameter(tempUseGenerator, tempParameterIndx);
                }

                tempUseGenerator.Emit(OpCodes.Call, oneConstructors);
                tempUseGenerator.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 加载参数列表
        /// </summary>
        /// <param name="useGenerator">使用的IL解释器</param>
        /// <param name="tempIndex">加载的参数索引</param>
        private void LoadParameter(ILGenerator useGenerator,int tempIndex)
        {
            switch (tempIndex)
            {
                case 0:
                    useGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    useGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    useGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    useGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (tempIndex <= 127)
                    {
                        useGenerator.Emit(OpCodes.Ldarg_S, tempIndex);
                    }
                    else
                    {
                        useGenerator.Emit(OpCodes.Ldarg, tempIndex);
                    }
                    break;
            }
        }

        /// <summary>
        /// 创建程序集
        /// </summary>
        /// <returns></returns>
        private AssemblyBuilder PrepareAssemBlyBuilder()
        {
            AssemblyBuilder tempAssemblyBuilder = null;
            AssemblyName useAssemblyName = new AssemblyName(m_strBaseAssemblyName + Guid.NewGuid().ToString());
            useAssemblyName.SetPublicKey(Assembly.GetExecutingAssembly().GetName().GetPublicKey());
            tempAssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(useAssemblyName, AssemblyBuilderAccess.Run);
            return tempAssemblyBuilder;
        }


    }
}

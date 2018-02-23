using AOPSuite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            EmitProxyFactory useFactory = new EmitProxyFactory();
            int a = 0;
            int b;
            int c;
            B useB = new ConsoleTest.B();
            useB.m_thisValue = 7;
            A useA = useFactory.CreatInstance<A>(5);
            c = useA.GetValue(ref a, out b, useB);

            Console.WriteLine();

            C useC = useFactory.CreatInstance<C>();

            int d = useC.TestMethod(0, 1);

            Console.WriteLine();

            RealProxyFactory useProxyFactory = new RealProxyFactory();
            D useD = new D(3);

            var proxyD = useProxyFactory.CreatProxy<ITestValue>(useD);

            proxyD.TestValue(1,0);

            Console.Read();
        }
    }

    #region Class

    public class A
    {
        int m_intValue = 0;

        public A(int inputValue)
        {
            m_intValue = inputValue;
        }

        [TestTwoAttribute]
        [TestAttribute]
        public virtual int GetValue(ref int a, out int b, B input)
        {
            a = 1;
            b = -1;

            int returnValue = m_intValue;
            Console.WriteLine(returnValue.ToString());
            return returnValue;
        }
    }

    public class B
    {
        public int m_thisValue = 5;
    }

    public class C
    {
        [TestAttribute]
        public virtual int TestMethod(int a, int b)
        {
            Console.WriteLine(a + b);
            return a + b;
        }
    }

    public interface ITestValue
    {
        int TestValue(int a, int b);
    }

    public class D : ITestValue
    {
        int m_nThisValue = 0;

        public D(int inputValue)
        {
            m_nThisValue = inputValue;
        }

        [TestAttribute]
        [TestTwoAttribute]
        public int TestValue(int a, int b)
        {
            m_nThisValue = m_nThisValue + a + b;
            Console.WriteLine(m_nThisValue);
            return m_nThisValue;
        }
    }
    #endregion


    #region Attribute
    public class TestAttribute : VoidDoAttribute
    {
        public override void DoBeforeMethod(MethodCallContext inputContext)
        {
            string useMethodName = null == inputContext.ThisMethod ? "N/A" : inputContext.ThisMethod.Name;

            Console.WriteLine(string.Format("{0}开始执行", useMethodName));

            base.DoBeforeMethod(inputContext);
        }

        public override void DoAfterMethod(MethodCallContext inputContext)
        {
            string useMethodName = null == inputContext.ThisMethod ? "N/A" : inputContext.ThisMethod.Name;

            Console.WriteLine(string.Format("{0}执行结束", useMethodName));
            base.DoAfterMethod(inputContext);
        }
    }

    public class TestTwoAttribute : VoidDoAttribute
    {
        public override void DoBeforeMethod(MethodCallContext inputContext)
        {

            Console.WriteLine(string.Format("AA"));

            base.DoBeforeMethod(inputContext);
        }

        public override void DoAfterMethod(MethodCallContext inputContext)
        {
            Console.WriteLine(string.Format("AA"));
            base.DoAfterMethod(inputContext);
        }
    } 
    #endregion

}

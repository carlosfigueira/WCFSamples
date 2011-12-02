using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSocketService
{
    public class CalculatorService
    {
        public int Add(int x, int y) { return x + y; }
        public int Subtract(int x, int y) { return x - y; }
        public int Multiply(int x, int y) { return x * y; }
        public int Divide(int x, int y)
        {
            try
            {
                return x / y;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error dividing", e);
            }
        }
    }
}

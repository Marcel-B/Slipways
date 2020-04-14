using NUnit.Framework;

namespace Slipways.Mobile.Tests
{
    public class Tests
    {
        private string _test;
        private string _tast;

        public string Test
        {
            get => _test ?? "Test";
            set => _test = value;
        }

        public string Tast
        {
            get => _tast ??= "Tast";
            set => _tast = value;
        }


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var test = Test;
            var tast = Tast;
         
        }
    }
}
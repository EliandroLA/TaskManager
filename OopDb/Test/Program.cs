using OopDb;
using System;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new Test();

            var a = new User();
            a.Value = 9;
            a.Name = "1234567";
            a.Status = 'S';

            t.User = a;

            var db = new DbFramework(typeof(Test));
            db.SetRules();
            var script = db.GetScript(true);

            Validator.ValidateObj(t);

            Console.ReadKey();

        }


    }

    class Test
    {
        [IsKey]
        public Guid Id { get; set; }
        [IsForeign("IdUser", "Id")]
        public User User { get; set; }
    }

    [Alias("Usuario"), Description(@"R=Runnig S=Stopped W=Waiting")]
    class User
    {
        [IsKey, Alias("IdUser")]
        public Guid Id { get; set; }
        [GreaterThan(0), LessThan(10), Default(1)]
        public int Value { get; set; }
        [MinLenght(6), MaxLengthName, IsRequired, IsUnique]
        public string Name { get; set; }
        [Check('R', 'S', 'W'), ToUpper]
        public char Status { get; set; }
        [Precision(10,3)]
        public double Teste { get; set; }

    }

    class MaxLengthName : Attribute, IMaxLengthAttr
    {
        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }

        public int GetLength()
        {
            return 50;
        }

        public void Validate(object input)
        {
            if (input.ToString().Length > 50)
                throw new Exception("Tamanho inválido para nome");
        }
    }
}
